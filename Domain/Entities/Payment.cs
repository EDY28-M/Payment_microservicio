using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentMicroservicio.Domain.Entities;

/// <summary>
/// Entidad que representa un pago en el sistema
/// </summary>
[Table("Payment")]
public class Payment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("idEstudiante")]
    public int IdEstudiante { get; set; }

    [Required]
    [Column("idPeriodo")]
    public int IdPeriodo { get; set; }

    [Required]
    [Column("stripe_session_id")]
    [MaxLength(255)]
    public string StripeSessionId { get; set; } = string.Empty;

    [Column("stripe_customer_id")]
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

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
    public string Status { get; set; } = "Pending";

    [Required]
    [Column("payment_type")]
    [MaxLength(50)]
    public string PaymentType { get; set; } = "Enrollment"; // 'Enrollment' o 'Course'

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }

    [Required]
    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("fecha_actualizacion")]
    public DateTime? FechaActualizacion { get; set; }

    [Column("fecha_pago_exitoso")]
    public DateTime? FechaPagoExitoso { get; set; }

    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("procesado")]
    public bool Procesado { get; set; } = false;

    // Navigation
    public virtual ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();

    // Domain methods
    public void MarkAsSucceeded()
    {
        Status = "Succeeded"; // Capitalizado para consistencia
        FechaPagoExitoso = DateTime.UtcNow;
        FechaActualizacion = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed"; // Capitalizado para consistencia
        ErrorMessage = errorMessage;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        Procesado = true;
        FechaActualizacion = DateTime.UtcNow;
    }

    public bool IsMatriculaPayment()
    {
        // Verificar primero por PaymentType (más confiable)
        if (PaymentType == "Enrollment")
            return true;
            
        // Fallback: verificar metadata
        return MetadataJson != null && 
               (MetadataJson.Contains("\"tipo\":\"matricula\"") || 
                MetadataJson.Contains("matricula"));
    }
}
