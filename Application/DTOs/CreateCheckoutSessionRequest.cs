using System.ComponentModel.DataAnnotations;

namespace PaymentMicroservicio.Application.DTOs;

/// <summary>
/// Request para crear una sesión de checkout de Stripe
/// </summary>
public class CreateCheckoutSessionRequest
{
    [Required(ErrorMessage = "El período es requerido")]
    public int IdPeriodo { get; set; }

    [Required(ErrorMessage = "El tipo de pago es requerido")]
    public string TipoPago { get; set; } = "matricula"; // "matricula" o "cursos"

    /// <summary>
    /// Lista de cursos (solo para tipo "cursos")
    /// </summary>
    public List<CursoItem>? Cursos { get; set; }

    /// <summary>
    /// URL a donde redirigir después del pago exitoso
    /// </summary>
    public string? SuccessUrl { get; set; }

    /// <summary>
    /// URL a donde redirigir si el usuario cancela
    /// </summary>
    public string? CancelUrl { get; set; }
}

public class CursoItem
{
    [Required]
    public int IdCurso { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; set; }

    public int Cantidad { get; set; } = 1;
}
