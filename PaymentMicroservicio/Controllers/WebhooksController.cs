using Microsoft.AspNetCore.Mvc;
using PaymentMicroservicio.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace PaymentMicroservicio.Controllers;

/// <summary>
/// Controller para webhooks de Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IStripeService _stripeService;
    private readonly IPaymentReceiptService _receiptService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentService paymentService,
        IStripeService stripeService,
        IPaymentReceiptService receiptService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _paymentService = paymentService;
        _stripeService = stripeService;
        _receiptService = receiptService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para recibir webhooks de Stripe
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("WebhookSecret no está configurado");
            return BadRequest(new { error = "WebhookSecret no configurado" });
        }

        Event? stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al verificar firma de webhook: {Message}", ex.Message);
            return BadRequest(new { error = "Firma inválida" });
        }

        _logger.LogInformation("Webhook recibido: {EventType}, ID: {EventId}", 
            stripeEvent.Type, stripeEvent.Id);

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "checkout.session.expired":
                    await HandleCheckoutSessionExpired(stripeEvent);
                    break;

                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceeded(stripeEvent);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailed(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Evento no manejado: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar webhook: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error al procesar webhook" });
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("Session es null en evento checkout.session.completed");
            return;
        }

        _logger.LogInformation("Checkout completado: SessionId={SessionId}, PaymentStatus={PaymentStatus}",
            session.Id, session.PaymentStatus);

        if (session.PaymentStatus == "paid")
        {
            // Procesar pago (marcar como exitoso)
            await _paymentService.ProcessPaymentCompletedAsync(session.Id);
            
            // Crear recibo digital (idempotente - no duplicará si ya existe)
            try
            {
                await _receiptService.CreateReceiptFromSessionAsync(session, stripeEvent);
                _logger.LogInformation("Receipt creado para sesión: {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear receipt para sesión {SessionId}: {Message}", session.Id, ex.Message);
                // No lanzar excepción para no afectar el procesamiento del pago
            }
        }
    }

    private async Task HandleCheckoutSessionExpired(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        _logger.LogInformation("Checkout expirado: SessionId={SessionId}", session.Id);

        await _paymentService.ProcessPaymentFailedAsync(session.Id, "Sesión de pago expirada");
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogInformation("PaymentIntent exitoso: {PaymentIntentId}, Status: {Status}", 
            paymentIntent.Id, paymentIntent.Status);

        try
        {
            // Obtener todas las sesiones de checkout asociadas a este PaymentIntent
            var sessionService = new SessionService();
            var options = new SessionListOptions 
            { 
                PaymentIntent = paymentIntent.Id,
                Limit = 10 // Aumentar a 10 por si acaso
            };
            
            _logger.LogInformation("Buscando sesiones para PaymentIntent {PaymentIntentId}...", paymentIntent.Id);
            var sessions = await sessionService.ListAsync(options);
            _logger.LogInformation("Sesiones encontradas: {Count}", sessions.Data.Count);
            
            if (sessions.Data.Any())
            {
                var session = sessions.Data.First();
                _logger.LogInformation("Procesando sesión {SessionId} para PaymentIntent {PaymentIntentId}", 
                    session.Id, paymentIntent.Id);
                
                await _paymentService.ProcessPaymentCompletedAsync(session.Id);
                _logger.LogInformation("✅ Pago procesado exitosamente para sesión {SessionId}", session.Id);
                
                // Crear recibo digital (idempotente - no duplicará si ya existe)
                try
                {
                    // Obtener la sesión completa desde Stripe para crear el receipt
                    var fullSession = await _stripeService.GetSessionAsync(session.Id);
                    if (fullSession != null && fullSession.PaymentStatus == "paid")
                    {
                        await _receiptService.CreateReceiptFromSessionAsync(fullSession, stripeEvent);
                        _logger.LogInformation("Receipt creado para sesión: {SessionId}", session.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear receipt para sesión {SessionId}: {Message}", session.Id, ex.Message);
                    // No lanzar excepción para no afectar el procesamiento del pago
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No se encontró sesión de checkout para PaymentIntent {PaymentIntentId}. Metadata: {@Metadata}", 
                    paymentIntent.Id, paymentIntent.Metadata);
                
                // Intentar buscar por cliente
                if (!string.IsNullOrEmpty(paymentIntent.CustomerId))
                {
                    _logger.LogInformation("Intentando buscar por Customer {CustomerId}", paymentIntent.CustomerId);
                    var sessionsByCustomer = await sessionService.ListAsync(new SessionListOptions 
                    { 
                        Customer = paymentIntent.CustomerId,
                        Limit = 5
                    });
                    _logger.LogInformation("Sesiones por cliente: {Count}", sessionsByCustomer.Data.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al procesar PaymentIntent {PaymentIntentId}: {Message}", 
                paymentIntent.Id, ex.Message);
        }
    }

    private async Task HandlePaymentIntentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var errorMessage = paymentIntent.LastPaymentError?.Message ?? "Pago fallido";
        _logger.LogWarning("PaymentIntent fallido: {PaymentIntentId}, Error: {Error}",
            paymentIntent.Id, errorMessage);
    }
}
