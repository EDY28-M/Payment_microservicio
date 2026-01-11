using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentMicroservicio.Domain.Entities;

/// <summary>
/// Entidad que representa un item de pago (curso)
/// </summary>
[Table("PaymentItem")]
public class PaymentItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("idPayment")]
    public int IdPayment { get; set; }

    [Required]
    [Column("idCurso")]
    public int IdCurso { get; set; }

    [Required]
    [Column("cantidad")]
    public int Cantidad { get; set; } = 1;

    [Required]
    [Column("precio_unitario", TypeName = "decimal(10,2)")]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Column("subtotal", TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    // Navigation
    [ForeignKey("IdPayment")]
    public virtual Payment? Payment { get; set; }

    // Domain methods
    public void CalculateSubtotal()
    {
        Subtotal = PrecioUnitario * Cantidad;
    }
}
