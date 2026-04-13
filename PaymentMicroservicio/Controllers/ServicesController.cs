using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using PaymentMicroservicio.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace PaymentMicroservicio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IStripeService _stripeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        PaymentDbContext context,
        IPaymentService paymentService,
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<ServicesController> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _stripeService = stripeService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos los servicios disponibles para pago
    /// </summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceCatalogResponse>>> GetCatalog()
    {
        var services = await _context.ServiceCatalogs
            .Where(s => s.Activo)
            .OrderBy(s => s.Orden)
            .Select(s => new ServiceCatalogResponse
            {
                Id = s.Id,
                Code = s.Code,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                Detalle = s.Detalle,
                Monto = s.Monto,
                Categoria = s.Categoria,
                TipoPago = s.TipoPago,
            })
            .ToListAsync();

        return Ok(services);
    }

    /// <summary>
    /// Crea checkout de Stripe para servicios seleccionados
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateServiceCheckout(
        [FromBody] CreateServiceCheckoutRequest request)
    {
        try
        {
            var idEstudiante = GetEstudianteId();
            if (idEstudiante == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al estudiante" });

            if (request.ServiceIds == null || request.ServiceIds.Count == 0)
                return BadRequest(new { mensaje = "Debe seleccionar al menos un servicio" });

            // Obtener servicios seleccionados de la DB
            var servicios = await _context.ServiceCatalogs
                .Where(s => request.ServiceIds.Contains(s.Id) && s.Activo)
                .ToListAsync();

            if (servicios.Count == 0)
                return BadRequest(new { mensaje = "Ninguno de los servicios seleccionados está disponible" });

            // Verificar si incluye matrícula y ya pagó
            var matriculaService = servicios.FirstOrDefault(s => s.TipoPago == "matricula");
            if (matriculaService != null)
            {
                var verificacion = await _paymentService.VerificarMatriculaPagadaAsync(
                    idEstudiante.Value, request.IdPeriodo);
                if (verificacion.Pagado)
                {
                    return BadRequest(new { mensaje = "Ya has pagado la matrícula para este período" });
                }
            }

            var total = servicios.Sum(s => s.Monto);

            // Si solo es matrícula, usar el flujo existente
            if (servicios.Count == 1 && matriculaService != null)
            {
                var matriculaRequest = new CreateCheckoutSessionRequest
                {
                    IdPeriodo = request.IdPeriodo,
                    TipoPago = "matricula",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                };
                var result = await _paymentService.CreateCheckoutSessionAsync(
                    idEstudiante.Value, matriculaRequest);
                return Ok(result);
            }

            // Para servicios mixtos o solo servicios, crear checkout con line items
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
            var successUrl = request.SuccessUrl ?? $"{frontendUrl}/estudiante/pago-exitoso";
            var cancelUrl = request.CancelUrl ?? $"{frontendUrl}/estudiante/pago-cancelado";

            var cursoItems = servicios.Select(s => new CursoItem
            {
                IdCurso = 0,
                Nombre = s.Nombre,
                Precio = s.Monto,
                Cantidad = 1,
            }).ToList();

            var tipoPago = matriculaService != null ? "matricula" : "servicio";

            var (sessionId, checkoutUrl) = await _stripeService.CreateCheckoutSessionAsync(
                idEstudiante.Value,
                request.IdPeriodo,
                tipoPago,
                total,
                "pen",
                cursoItems,
                successUrl,
                cancelUrl,
                new Dictionary<string, string>
                {
                    { "service_ids", string.Join(",", servicios.Select(s => s.Id)) },
                    { "service_codes", string.Join(",", servicios.Select(s => s.Code)) },
                });

            // Registrar pago
            var payment = new Domain.Entities.Payment
            {
                IdEstudiante = idEstudiante.Value,
                IdPeriodo = request.IdPeriodo,
                StripeSessionId = sessionId,
                Amount = total,
                Currency = "PEN",
                Status = "Pending",
                PaymentType = matriculaService != null ? "Enrollment" : "Service",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    tipo = tipoPago,
                    servicios = servicios.Select(s => new { s.Id, s.Code, s.Nombre, s.Monto })
                }),
                FechaCreacion = DateTime.UtcNow,
            };

            foreach (var servicio in servicios)
            {
                payment.PaymentItems.Add(new Domain.Entities.PaymentItem
                {
                    IdCurso = 0,
                    NombreCurso = servicio.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = servicio.Monto,
                    Subtotal = servicio.Monto,
                });
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Checkout de servicios creado: {SessionId}, estudiante={IdEstudiante}, servicios={Count}, total={Total}",
                sessionId, idEstudiante.Value, servicios.Count, total);

            return Ok(new CheckoutSessionResponse
            {
                SessionId = sessionId,
                CheckoutUrl = checkoutUrl,
                PaymentId = payment.Id,
                Amount = total,
                Currency = "PEN",
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear checkout de servicios");
            return StatusCode(500, new { mensaje = "Error interno al procesar el pago" });
        }
    }

    private int? GetEstudianteId()
    {
        // Usar "EstudianteId" (consistente con PaymentsController y el backend principal)
        var claim = User.FindFirst("EstudianteId");

        if (claim != null && int.TryParse(claim.Value, out var id))
            return id;

        // Fallback: NameIdentifier (IdUsuario)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("EstudianteId claim no encontrado, usando NameIdentifier: {UserId}", userId);
            return userId;
        }

        return null;
    }
}
