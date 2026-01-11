using System.Text.Json;
using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using PaymentMicroservicio.Domain.Entities;
using PaymentMicroservicio.Domain.Enums;

namespace PaymentMicroservicio.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de pagos
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripeService _stripeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    // Monto fijo de matrícula en PEN
    private const decimal MATRICULA_AMOUNT = 5.00m;
    private const string DEFAULT_CURRENCY = "pen";

    public PaymentService(
        IPaymentRepository paymentRepository,
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _stripeService = stripeService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(int idEstudiante, CreateCheckoutSessionRequest request)
    {
        // Validaciones
        if (idEstudiante <= 0)
            throw new ArgumentException("ID de estudiante inválido");

        if (request.IdPeriodo <= 0)
            throw new ArgumentException("ID de período inválido");

        // Verificar si ya pagó matrícula (solo para tipo matricula)
        if (request.TipoPago == PaymentType.Matricula)
        {
            var yaPage = await _paymentRepository.HasPaidMatriculaAsync(idEstudiante, request.IdPeriodo);
            if (yaPage)
            {
                throw new InvalidOperationException("Ya has pagado la matrícula para este período");
            }
        }

        // Calcular monto
        decimal amount;
        if (request.TipoPago == PaymentType.Matricula)
        {
            amount = MATRICULA_AMOUNT;
        }
        else if (request.Cursos != null && request.Cursos.Any())
        {
            amount = request.Cursos.Sum(c => c.Precio * c.Cantidad);
        }
        else
        {
            throw new ArgumentException("Debe especificar cursos o tipo de pago matrícula");
        }

        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0");

        // URLs de redirección
        var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
        var successUrl = request.SuccessUrl ?? $"{frontendUrl}/estudiante/pago-exitoso";
        var cancelUrl = request.CancelUrl ?? $"{frontendUrl}/estudiante/pago-cancelado";

        // Crear sesión de Stripe
        var (sessionId, checkoutUrl) = await _stripeService.CreateCheckoutSessionAsync(
            idEstudiante,
            request.IdPeriodo,
            request.TipoPago,
            amount,
            DEFAULT_CURRENCY,
            request.Cursos,
            successUrl,
            cancelUrl);

        // Crear registro de pago
        var payment = new Payment
        {
            IdEstudiante = idEstudiante,
            IdPeriodo = request.IdPeriodo,
            StripeSessionId = sessionId, // Guardamos el session ID
            Amount = amount,
            Currency = DEFAULT_CURRENCY.ToUpper(),
            Status = PaymentStatus.Pending.ToString(),
            PaymentType = request.TipoPago == "matricula" ? "Enrollment" : "Course",
            MetadataJson = JsonSerializer.Serialize(new { tipo = request.TipoPago, cursos = request.Cursos }),
            FechaCreacion = DateTime.UtcNow
        };

        // Agregar items si hay cursos
        if (request.Cursos != null)
        {
            foreach (var curso in request.Cursos)
            {
                payment.PaymentItems.Add(new PaymentItem
                {
                    IdCurso = curso.IdCurso,
                    NombreCurso = curso.Nombre ?? $"Curso {curso.IdCurso}",
                    Cantidad = curso.Cantidad,
                    PrecioUnitario = curso.Precio,
                    Subtotal = curso.Precio * curso.Cantidad
                });
            }
        }
        else if (request.TipoPago == "matricula")
        {
            // Agregar un item genérico para matrícula
            payment.PaymentItems.Add(new PaymentItem
            {
                IdCurso = 0,
                NombreCurso = "Matrícula",
                Cantidad = 1,
                PrecioUnitario = amount,
                Subtotal = amount
            });
        }

        await _paymentRepository.CreateAsync(payment);

        _logger.LogInformation("Sesión de checkout creada para estudiante {IdEstudiante}, tipo {TipoPago}, monto {Amount}",
            idEstudiante, request.TipoPago, amount);

        return new CheckoutSessionResponse
        {
            SessionId = sessionId,
            CheckoutUrl = checkoutUrl,
            PaymentId = payment.Id,
            Amount = amount,
            Currency = DEFAULT_CURRENCY.ToUpper()
        };
    }

    public async Task ProcessPaymentCompletedAsync(string sessionId)
    {
        var payment = await _paymentRepository.GetBySessionIdAsync(sessionId);
        if (payment == null)
        {
            _logger.LogWarning("Payment no encontrado para session: {SessionId}", sessionId);
            return;
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            _logger.LogInformation("Payment ya procesado: {PaymentId}", payment.Id);
            return;
        }

        // Obtener detalles de la sesión de Stripe
        var session = await _stripeService.GetSessionAsync(sessionId);

        payment.MarkAsSucceeded();
        // Note: PaymentMethod no está implementado en este modelo simplificado

        // Si es pago de matrícula, marcar como procesado automáticamente
        if (payment.IsMatriculaPayment())
        {
            payment.MarkAsProcessed();
            _logger.LogInformation("Pago de matrícula procesado automáticamente: {PaymentId}", payment.Id);
        }

        await _paymentRepository.UpdateAsync(payment);

        _logger.LogInformation("Pago completado: {PaymentId}, Estudiante: {IdEstudiante}", 
            payment.Id, payment.IdEstudiante);
    }

    public async Task ProcessPaymentFailedAsync(string sessionId, string errorMessage)
    {
        var payment = await _paymentRepository.GetBySessionIdAsync(sessionId);
        if (payment == null)
        {
            _logger.LogWarning("Payment no encontrado para session fallida: {SessionId}", sessionId);
            return;
        }

        payment.MarkAsFailed(errorMessage);
        await _paymentRepository.UpdateAsync(payment);

        _logger.LogWarning("Pago fallido: {PaymentId}, Error: {Error}", payment.Id, errorMessage);
    }

    public async Task<PaymentStatusResponse?> GetPaymentStatusAsync(int paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        return payment == null ? null : MapToResponse(payment);
    }

    public async Task<PaymentStatusResponse?> GetPaymentStatusBySessionAsync(string sessionId)
    {
        var payment = await _paymentRepository.GetBySessionIdAsync(sessionId);
        return payment == null ? null : MapToResponse(payment);
    }

    public async Task<VerificarPagoResponse> VerificarMatriculaPagadaAsync(int idEstudiante, int idPeriodo)
    {
        _logger.LogInformation("[VERIFICAR] Verificando matrícula pagada: estudiante={IdEstudiante}, periodo={IdPeriodo}",
            idEstudiante, idPeriodo);

        var payment = await _paymentRepository.GetMatriculaPaymentAsync(idEstudiante, idPeriodo);

        var response = new VerificarPagoResponse
        {
            Pagado = payment != null,
            PaymentId = payment?.Id,
            FechaPago = payment?.FechaPagoExitoso,
            Monto = payment?.Amount
        };

        _logger.LogInformation("[VERIFICAR] Resultado: pagado={Pagado}", response.Pagado);

        return response;
    }

    public async Task<List<PaymentStatusResponse>> GetHistorialPagosAsync(int idEstudiante)
    {
        var payments = await _paymentRepository.GetByEstudianteAsync(idEstudiante);
        return payments.Select(MapToResponse).ToList();
    }

    private static PaymentStatusResponse MapToResponse(Payment payment)
    {
        string? tipoPago = null;
        try
        {
            if (payment.MetadataJson != null)
            {
                var metadata = JsonSerializer.Deserialize<JsonElement>(payment.MetadataJson);
                if (metadata.TryGetProperty("tipo", out var tipo))
                {
                    tipoPago = tipo.GetString();
                }
            }
        }
        catch { /* Ignorar errores de parsing */ }

        return new PaymentStatusResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            Amount = payment.Amount,
            Currency = payment.Currency,
            FechaCreacion = payment.FechaCreacion,
            FechaPagoExitoso = payment.FechaPagoExitoso,
            Procesado = payment.Procesado,
            ErrorMessage = payment.ErrorMessage,
            TipoPago = tipoPago,
            Items = payment.PaymentItems.Select(i => new PaymentItemDto
            {
                IdCurso = i.IdCurso,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }
}
