using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using System.Security.Claims;

namespace PaymentMicroservicio.Controllers;

/// <summary>
/// Controller para gestión de recibos de pago
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly IPaymentReceiptService _receiptService;
    private readonly ILogger<ReceiptsController> _logger;

    public ReceiptsController(
        IPaymentReceiptService receiptService,
        ILogger<ReceiptsController> logger)
    {
        _receiptService = receiptService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un recibo por session ID de Stripe
    /// Requiere autenticación y verifica que el recibo pertenece al estudiante autenticado
    /// </summary>
    [HttpGet("by-session/{sessionId}")]
    [Authorize]
    public async Task<ActionResult<PaymentReceiptResponse>> GetReceiptBySession(string sessionId)
    {
        try
        {
            var studentId = GetEstudianteId();
            if (studentId == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al estudiante" });

            var receipt = await _receiptService.GetReceiptBySessionIdAsync(sessionId, studentId.Value);

            if (receipt == null)
                return NotFound(new { mensaje = "Recibo no encontrado o no tienes permiso para acceder a este recibo" });

            return Ok(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recibo por session {SessionId}", sessionId);
            return StatusCode(500, new { mensaje = "Error al obtener el recibo" });
        }
    }

    private int? GetEstudianteId()
    {
        var estudianteIdClaim = User.FindFirst("EstudianteId")?.Value;

        if (string.IsNullOrEmpty(estudianteIdClaim) || !int.TryParse(estudianteIdClaim, out var estudianteId))
        {
            _logger.LogWarning("EstudianteId claim no encontrado o inválido en el token.");
            return null;
        }

        return estudianteId;
    }
}
