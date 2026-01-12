using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Application.Interfaces;
using PaymentMicroservicio.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace PaymentMicroservicio.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de recibos de pago
/// </summary>
public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _receiptRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository receiptRepository,
        IPaymentRepository paymentRepository,
        IConfiguration configuration,
        ILogger<PaymentReceiptService> logger)
    {
        _receiptRepository = receiptRepository;
        _paymentRepository = paymentRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentReceipt?> CreateReceiptFromSessionAsync(Session session, Event stripeEvent)
    {
        // Verificar idempotencia: si ya existe un receipt para este stripe_event_id, no crear otro
        var existingReceipt = await _receiptRepository.GetByStripeEventIdAsync(stripeEvent.Id);
        if (existingReceipt != null)
        {
            _logger.LogInformation("Receipt ya existe para evento Stripe: {StripeEventId}, ReceiptId: {ReceiptId}", 
                stripeEvent.Id, existingReceipt.Id);
            return existingReceipt;
        }

        // Verificar idempotencia por session_id también
        var existingBySession = await _receiptRepository.GetBySessionIdAsync(session.Id);
        if (existingBySession != null)
        {
            _logger.LogInformation("Receipt ya existe para sesión: {SessionId}, ReceiptId: {ReceiptId}", 
                session.Id, existingBySession.Id);
            return existingBySession;
        }

        // Obtener datos desde metadata de Stripe o valores por defecto
        var studentId = int.Parse(session.Metadata.GetValueOrDefault("idEstudiante", "0"));
        var studentCode = session.Metadata.GetValueOrDefault("studentCode", "");
        var studentName = session.Metadata.GetValueOrDefault("studentName", "");
        var universityName = session.Metadata.GetValueOrDefault("universityName", 
            _configuration["AppSettings:UniversityName"] ?? "Universidad Nacional de San Agustín");
        var facultyName = session.Metadata.GetValueOrDefault("facultyName",
            _configuration["AppSettings:FacultyName"] ?? "Facultad de Ingeniería de Producción y Servicios");
        var period = session.Metadata.GetValueOrDefault("period", "");
        var academicYearStr = session.Metadata.GetValueOrDefault("academicYear", DateTime.UtcNow.Year.ToString());
        var academicYear = int.Parse(academicYearStr);
        var concept = session.Metadata.GetValueOrDefault("concept", "Matrícula Académica");

        // Si no hay datos en metadata, intentar obtenerlos desde Payment
        if (string.IsNullOrEmpty(studentCode) || string.IsNullOrEmpty(studentName) || string.IsNullOrEmpty(period))
        {
            var payment = await _paymentRepository.GetBySessionIdAsync(session.Id);
            if (payment != null)
            {
                // Aquí podríamos hacer una llamada al backend principal, pero como el microservicio es independiente,
                // usamos los valores que tengamos o valores por defecto
                if (string.IsNullOrEmpty(studentCode))
                    studentCode = $"EST{payment.IdEstudiante:D6}";
                if (string.IsNullOrEmpty(studentName))
                    studentName = "Estudiante";
                if (string.IsNullOrEmpty(period))
                    period = $"Periodo {payment.IdPeriodo}";
            }
        }

        // Obtener amount desde session (en centavos) o desde metadata
        var amountDecimal = session.AmountTotal.HasValue 
            ? (decimal)session.AmountTotal.Value / 100m 
            : decimal.Parse(session.Metadata.GetValueOrDefault("amount", "0"));
        var currency = session.Currency?.ToUpper() ?? "PEN";

        // Obtener payment_intent_id si está disponible
        var paymentIntentId = session.PaymentIntentId ?? session.Metadata.GetValueOrDefault("paymentIntentId", null);

        // Generar código de recibo
        var receiptCode = await _receiptRepository.GenerateReceiptCodeAsync(academicYear);

        // Crear recibo
        var receipt = new PaymentReceipt
        {
            ReceiptCode = receiptCode,
            StripeSessionId = session.Id,
            PaymentIntentId = paymentIntentId,
            StudentId = studentId,
            StudentCode = studentCode,
            StudentName = studentName,
            UniversityName = universityName,
            FacultyName = facultyName,
            Concept = concept,
            Period = period,
            AcademicYear = academicYear,
            Amount = amountDecimal,
            Currency = currency,
            Status = "PAID",
            PaidAt = DateTime.UtcNow,
            StripeEventId = stripeEvent.Id,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _receiptRepository.CreateAsync(receipt);
            _logger.LogInformation("Receipt creado exitosamente: ReceiptCode={ReceiptCode}, SessionId={SessionId}, StudentId={StudentId}",
                receiptCode, session.Id, studentId);
            return receipt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear receipt: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<PaymentReceiptResponse?> GetReceiptBySessionIdAsync(string sessionId, int studentId)
    {
        var receipt = await _receiptRepository.GetBySessionIdAsync(sessionId);
        
        if (receipt == null)
            return null;

        // Verificar que el receipt pertenece al estudiante autenticado
        if (receipt.StudentId != studentId)
        {
            _logger.LogWarning("Intento de acceso a receipt no autorizado: SessionId={SessionId}, RequestedStudentId={RequestedStudentId}, ReceiptStudentId={ReceiptStudentId}",
                sessionId, studentId, receipt.StudentId);
            return null;
        }

        return MapToResponse(receipt);
    }

    public async Task<string> GenerateReceiptCodeAsync(int year)
    {
        return await _receiptRepository.GenerateReceiptCodeAsync(year);
    }

    private static PaymentReceiptResponse MapToResponse(PaymentReceipt receipt)
    {
        return new PaymentReceiptResponse
        {
            Id = receipt.Id,
            ReceiptCode = receipt.ReceiptCode,
            StripeSessionId = receipt.StripeSessionId,
            PaymentIntentId = receipt.PaymentIntentId,
            StudentId = receipt.StudentId,
            StudentCode = receipt.StudentCode,
            StudentName = receipt.StudentName,
            UniversityName = receipt.UniversityName,
            FacultyName = receipt.FacultyName,
            Concept = receipt.Concept,
            Period = receipt.Period,
            AcademicYear = receipt.AcademicYear,
            Amount = receipt.Amount,
            Currency = receipt.Currency,
            Status = receipt.Status,
            PaidAt = receipt.PaidAt,
            StripeEventId = receipt.StripeEventId,
            CreatedAt = receipt.CreatedAt
        };
    }
}
