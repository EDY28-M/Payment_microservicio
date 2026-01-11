using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace PaymentMicroservicio.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de Stripe
/// </summary>
public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey no está configurada");
        }

        StripeConfiguration.ApiKey = secretKey;
    }

    public async Task<(string sessionId, string checkoutUrl)> CreateCheckoutSessionAsync(
        int idEstudiante,
        int idPeriodo,
        string tipoPago,
        decimal amount,
        string currency,
        List<CursoItem>? cursos,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Crear line items para el checkout
            var lineItems = new List<SessionLineItemOptions>();

            if (tipoPago == "matricula")
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency.ToLower(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Pago de Matrícula",
                            Description = $"Matrícula para el período académico",
                        },
                        UnitAmount = (long)(amount * 100), // Stripe usa centavos
                    },
                    Quantity = 1,
                });
            }
            else if (cursos != null && cursos.Any())
            {
                foreach (var curso in cursos)
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLower(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = curso.Nombre,
                                Description = $"Curso académico - ID: {curso.IdCurso}",
                            },
                            UnitAmount = (long)(curso.Precio * 100),
                        },
                        Quantity = curso.Cantidad,
                    });
                }
            }

            // Metadata para tracking
            var sessionMetadata = new Dictionary<string, string>
            {
                { "idEstudiante", idEstudiante.ToString() },
                { "idPeriodo", idPeriodo.ToString() },
                { "tipo", tipoPago }
            };

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    sessionMetadata[kvp.Key] = kvp.Value;
                }
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                Metadata = sessionMetadata,
                CustomerEmail = null, // Se puede agregar si se tiene el email
                Locale = "es", // Español
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = sessionMetadata
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Sesión de checkout creada: {SessionId} para estudiante {IdEstudiante}", 
                session.Id, idEstudiante);

            return (session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error de Stripe al crear sesión de checkout: {Message}", ex.Message);
            throw new InvalidOperationException($"Error al crear sesión de pago: {ex.Message}", ex);
        }
    }

    public async Task<Session> GetSessionAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, new SessionGetOptions
            {
                Expand = new List<string> { "payment_intent" }
            });
            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al obtener sesión {SessionId}: {Message}", sessionId, ex.Message);
            throw;
        }
    }

    public Event? ConstructWebhookEvent(string json, string signature, string webhookSecret)
    {
        try
        {
            return EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al verificar webhook: {Message}", ex.Message);
            return null;
        }
    }
}
