using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentMicroservicio.Domain.Entities;

/// <summary>
/// Entidad que representa un recibo digital de pago
/// </summary>
[Table("PaymentReceipt")]
public class PaymentReceipt
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("receipt_code")]
    [MaxLength(50)]
    public string ReceiptCode { get; set; } = string.Empty;

    [Required]
    [Column("stripe_session_id")]
    [MaxLength(255)]
    public string StripeSessionId { get; set; } = string.Empty;

    [Column("payment_intent_id")]
    [MaxLength(255)]
    public string? PaymentIntentId { get; set; }

    [Required]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Required]
    [Column("student_code")]
    [MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    [Column("student_name")]
    [MaxLength(200)]
    public string StudentName { get; set; } = string.Empty;

    [Required]
    [Column("university_name")]
    [MaxLength(200)]
    public string UniversityName { get; set; } = string.Empty;

    [Required]
    [Column("faculty_name")]
    [MaxLength(200)]
    public string FacultyName { get; set; } = string.Empty;

    [Required]
    [Column("concept")]
    [MaxLength(200)]
    public string Concept { get; set; } = string.Empty;

    [Required]
    [Column("period")]
    [MaxLength(50)]
    public string Period { get; set; } = string.Empty;

    [Required]
    [Column("academic_year")]
    public int AcademicYear { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [MaxLength(3)]
    public string Currency { get; set; } = "PEN";

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "PAID";

    [Column("paid_at")]
    public DateTime PaidAt { get; set; }

    [Required]
    [Column("stripe_event_id")]
    [MaxLength(255)]
    public string StripeEventId { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
