namespace PaymentMicroservicio.Application.DTOs;

/// <summary>
/// Estado de un pago
/// </summary>
public class PaymentStatusResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PEN";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPagoExitoso { get; set; }
    public bool Procesado { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TipoPago { get; set; }
    public List<PaymentItemDto> Items { get; set; } = new();
}

public class PaymentItemDto
{
    public int IdCurso { get; set; }
    public string? NombreCurso { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
