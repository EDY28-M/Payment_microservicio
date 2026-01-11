using PaymentMicroservicio.Application.DTOs;

namespace PaymentMicroservicio.Application.Interfaces;

/// <summary>
/// Servicio de pagos
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Crea una sesión de checkout para pago
    /// </summary>
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(int idEstudiante, CreateCheckoutSessionRequest request);

    /// <summary>
    /// Procesa el evento de pago completado
    /// </summary>
    Task ProcessPaymentCompletedAsync(string sessionId);

    /// <summary>
    /// Procesa el evento de pago fallido
    /// </summary>
    Task ProcessPaymentFailedAsync(string sessionId, string errorMessage);

    /// <summary>
    /// Obtiene el estado de un pago
    /// </summary>
    Task<PaymentStatusResponse?> GetPaymentStatusAsync(int paymentId);

    /// <summary>
    /// Obtiene el estado de un pago por session ID
    /// </summary>
    Task<PaymentStatusResponse?> GetPaymentStatusBySessionAsync(string sessionId);

    /// <summary>
    /// Verifica si el estudiante ha pagado la matrícula
    /// </summary>
    Task<VerificarPagoResponse> VerificarMatriculaPagadaAsync(int idEstudiante, int idPeriodo);

    /// <summary>
    /// Obtiene el historial de pagos de un estudiante
    /// </summary>
    Task<List<PaymentStatusResponse>> GetHistorialPagosAsync(int idEstudiante);
}
