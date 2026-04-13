using System.ComponentModel.DataAnnotations;

namespace PaymentMicroservicio.Application.DTOs;

public class CreateServiceCheckoutRequest
{
    [Required(ErrorMessage = "El período es requerido")]
    public int IdPeriodo { get; set; }

    [Required(ErrorMessage = "Debe seleccionar al menos un servicio")]
    [MinLength(1, ErrorMessage = "Debe seleccionar al menos un servicio")]
    public List<int> ServiceIds { get; set; } = new();

    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}
