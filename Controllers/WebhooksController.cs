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
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentService paymentService,
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _paymentService = paymentService;
        _stripeService = stripeService;
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
            await _paymentService.ProcessPaymentCompletedAsync(session.Id);
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

        _logger.LogInformation("PaymentIntent exitoso: {PaymentIntentId}", paymentIntent.Id);

        // El procesamiento principal se hace en checkout.session.completed
        // Este evento es para logging adicional
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
