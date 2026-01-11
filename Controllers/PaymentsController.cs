using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using System.Security.Claims;

namespace PaymentMicroservicio.Controllers;

/// <summary>
/// Controller para gestión de pagos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una sesión de checkout para pagar matrícula
    /// </summary>
    [HttpPost("checkout/matricula")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateMatriculaCheckout([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var idEstudiante = GetEstudianteId();
            if (idEstudiante == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al estudiante" });

            request.TipoPago = "matricula";

            var response = await _paymentService.CreateCheckoutSessionAsync(idEstudiante.Value, request);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida al crear checkout de matrícula");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al crear checkout de matrícula");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear checkout de matrícula");
            return StatusCode(500, new { mensaje = "Error interno al procesar el pago" });
        }
    }

    /// <summary>
    /// Crea una sesión de checkout para pagar cursos
    /// </summary>
    [HttpPost("checkout/cursos")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCursosCheckout([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var idEstudiante = GetEstudianteId();
            if (idEstudiante == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al estudiante" });

            request.TipoPago = "cursos";

            if (request.Cursos == null || !request.Cursos.Any())
                return BadRequest(new { mensaje = "Debe especificar al menos un curso" });

            var response = await _paymentService.CreateCheckoutSessionAsync(idEstudiante.Value, request);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida al crear checkout de cursos");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al crear checkout de cursos");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear checkout de cursos");
            return StatusCode(500, new { mensaje = "Error interno al procesar el pago" });
        }
    }

    /// <summary>
    /// Obtiene el estado de un pago por ID
    /// </summary>
    [HttpGet("status/{paymentId}")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(int paymentId)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatusAsync(paymentId);
            if (status == null)
                return NotFound(new { mensaje = "Pago no encontrado" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado del pago {PaymentId}", paymentId);
            return StatusCode(500, new { mensaje = "Error al obtener estado del pago" });
        }
    }

    /// <summary>
    /// Obtiene el estado de un pago por Session ID de Stripe
    /// </summary>
    [HttpGet("status/session/{sessionId}")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatusBySession(string sessionId)
    {
        try
        {
            var status = await _paymentService.GetPaymentStatusBySessionAsync(sessionId);
            if (status == null)
                return NotFound(new { mensaje = "Pago no encontrado" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado del pago por session {SessionId}", sessionId);
            return StatusCode(500, new { mensaje = "Error al obtener estado del pago" });
        }
    }

    /// <summary>
    /// Verifica si el estudiante ha pagado la matrícula (endpoint público para backend principal)
    /// </summary>
    [HttpGet("verificar-matricula-pagada/{idEstudiante}/{idPeriodo}")]
    [AllowAnonymous]
    public async Task<ActionResult<VerificarPagoResponse>> VerificarMatriculaPagada(int idEstudiante, int idPeriodo)
    {
        try
        {
            _logger.LogInformation("[API] Verificando matrícula pagada: estudiante={IdEstudiante}, periodo={IdPeriodo}",
                idEstudiante, idPeriodo);

            var response = await _paymentService.VerificarMatriculaPagadaAsync(idEstudiante, idPeriodo);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar matrícula pagada");
            return StatusCode(500, new { mensaje = "Error al verificar pago de matrícula", pagado = false });
        }
    }

    /// <summary>
    /// Verifica si el estudiante autenticado ha pagado la matrícula
    /// </summary>
    [HttpGet("verificar-matricula-pagada/{idPeriodo}")]
    [Authorize]
    public async Task<ActionResult<VerificarPagoResponse>> VerificarMiMatriculaPagada(int idPeriodo)
    {
        try
        {
            var idEstudiante = GetEstudianteId();
            if (idEstudiante == null)
                return Ok(new VerificarPagoResponse { Pagado = false });

            var response = await _paymentService.VerificarMatriculaPagadaAsync(idEstudiante.Value, idPeriodo);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar mi matrícula pagada");
            return Ok(new VerificarPagoResponse { Pagado = false });
        }
    }

    /// <summary>
    /// Obtiene el historial de pagos del estudiante autenticado
    /// </summary>
    [HttpGet("historial")]
    [Authorize]
    public async Task<ActionResult<List<PaymentStatusResponse>>> GetHistorial()
    {
        try
        {
            var idEstudiante = GetEstudianteId();
            if (idEstudiante == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al estudiante" });

            var historial = await _paymentService.GetHistorialPagosAsync(idEstudiante.Value);

            return Ok(historial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de pagos");
            return StatusCode(500, new { mensaje = "Error al obtener historial de pagos" });
        }
    }

    private int? GetEstudianteId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        // TODO: Aquí deberías obtener el ID del estudiante desde el backend principal
        // Por ahora asumimos que el userId es el idEstudiante
        return userId;
    }
}
