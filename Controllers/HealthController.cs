using Microsoft.AspNetCore.Mvc;
using PaymentMicroservicio.Infrastructure.Data;

namespace PaymentMicroservicio.Controllers;

/// <summary>
/// Controller para health checks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(PaymentDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Health check básico
    /// </summary>
    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "PaymentMicroservicio",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Health check con verificación de base de datos
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return Ok(new
                {
                    status = "ready",
                    database = "connected",
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = "not ready",
                database = "disconnected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en health check de base de datos");
            return StatusCode(503, new
            {
                status = "not ready",
                database = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
