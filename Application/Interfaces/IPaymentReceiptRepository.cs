using PaymentMicroservicio.Domain.Entities;

namespace PaymentMicroservicio.Application.Interfaces;

/// <summary>
/// Interfaz para el repositorio de recibos de pago
/// </summary>
public interface IPaymentReceiptRepository
{
    Task<PaymentReceipt?> GetByIdAsync(int id);
    Task<PaymentReceipt?> GetBySessionIdAsync(string sessionId);
    Task<PaymentReceipt?> GetByReceiptCodeAsync(string receiptCode);
    Task<PaymentReceipt?> GetByStripeEventIdAsync(string stripeEventId);
    Task<List<PaymentReceipt>> GetByStudentIdAsync(int studentId);
    Task<PaymentReceipt> CreateAsync(PaymentReceipt receipt);
    Task UpdateAsync(PaymentReceipt receipt);
    Task SaveChangesAsync();
    Task<string> GenerateReceiptCodeAsync(int year);
}
