namespace PaymentMicroservicio.Application.DTOs;

/// <summary>
/// Respuesta al crear una sesión de checkout
/// </summary>
public class CheckoutSessionResponse
{
    /// <summary>
    /// ID de la sesión de checkout de Stripe
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// URL para redirigir al usuario al checkout de Stripe
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID del payment en la base de datos
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Monto total
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string Currency { get; set; } = "PEN";
}
