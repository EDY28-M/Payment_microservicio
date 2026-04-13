namespace PaymentMicroservicio.Application.DTOs;

public class ServiceCatalogResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string TipoPago { get; set; } = string.Empty;
}
