namespace PaymentMicroservicio.Application.DTOs;

/// <summary>
/// DTO para la respuesta de un recibo de pago
/// </summary>
public class PaymentReceiptResponse
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string StripeSessionId { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
