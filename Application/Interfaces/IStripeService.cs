using PaymentMicroservicio.Application.DTOs;

namespace PaymentMicroservicio.Application.Interfaces;

/// <summary>
/// Servicio de integración con Stripe
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Crea una sesión de checkout de Stripe
    /// </summary>
    Task<(string sessionId, string checkoutUrl)> CreateCheckoutSessionAsync(
        int idEstudiante,
        int idPeriodo,
        string tipoPago,
        decimal amount,
        string currency,
        List<CursoItem>? cursos,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Obtiene una sesión de checkout
    /// </summary>
    Task<Stripe.Checkout.Session> GetSessionAsync(string sessionId);

    /// <summary>
    /// Verifica la firma de un webhook
    /// </summary>
    Stripe.Event? ConstructWebhookEvent(string json, string signature, string webhookSecret);
}
