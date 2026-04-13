using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentMicroservicio.Domain.Entities;

[Table("ServiceCatalog")]
public class ServiceCatalog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("nombre")]
    [MaxLength(255)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("descripcion")]
    [MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Column("detalle")]
    [MaxLength(500)]
    public string? Detalle { get; set; }

    [Required]
    [Column("monto", TypeName = "decimal(10,2)")]
    public decimal Monto { get; set; }

    [Required]
    [Column("categoria")]
    [MaxLength(100)]
    public string Categoria { get; set; } = string.Empty;

    [Required]
    [Column("tipo_pago")]
    [MaxLength(50)]
    public string TipoPago { get; set; } = string.Empty;

    [Required]
    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("orden")]
    public int Orden { get; set; } = 0;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
