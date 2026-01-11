namespace PaymentMicroservicio.Application.DTOs;

/// <summary>
/// Respuesta de verificación de pago de matrícula
/// </summary>
public class VerificarPagoResponse
{
    public bool Pagado { get; set; }
    public int? PaymentId { get; set; }
    public DateTime? FechaPago { get; set; }
    public decimal? Monto { get; set; }
}
