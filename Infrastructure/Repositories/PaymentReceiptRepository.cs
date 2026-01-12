using Microsoft.EntityFrameworkCore;
using PaymentMicroservicio.Application.Interfaces;
using PaymentMicroservicio.Domain.Entities;
using PaymentMicroservicio.Infrastructure.Data;

namespace PaymentMicroservicio.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de recibos de pago
/// </summary>
public class PaymentReceiptRepository : IPaymentReceiptRepository
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentReceiptRepository> _logger;

    public PaymentReceiptRepository(PaymentDbContext context, ILogger<PaymentReceiptRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentReceipt?> GetByIdAsync(int id)
    {
        return await _context.PaymentReceipts.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<PaymentReceipt?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.PaymentReceipts
            .FirstOrDefaultAsync(r => r.StripeSessionId == sessionId);
    }

    public async Task<PaymentReceipt?> GetByReceiptCodeAsync(string receiptCode)
    {
        return await _context.PaymentReceipts
            .FirstOrDefaultAsync(r => r.ReceiptCode == receiptCode);
    }

    public async Task<PaymentReceipt?> GetByStripeEventIdAsync(string stripeEventId)
    {
        return await _context.PaymentReceipts
            .FirstOrDefaultAsync(r => r.StripeEventId == stripeEventId);
    }

    public async Task<List<PaymentReceipt>> GetByStudentIdAsync(int studentId)
    {
        return await _context.PaymentReceipts
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentReceipt> CreateAsync(PaymentReceipt receipt)
    {
        _context.PaymentReceipts.Add(receipt);
        await _context.SaveChangesAsync();
        return receipt;
    }

    public async Task UpdateAsync(PaymentReceipt receipt)
    {
        receipt.UpdatedAt = DateTime.UtcNow;
        _context.PaymentReceipts.Update(receipt);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateReceiptCodeAsync(int year)
    {
        // Formato: REC-YYYY-000001
        var prefix = $"REC-{year}-";
        
        // Buscar el último recibo del año
        var lastReceipt = await _context.PaymentReceipts
            .Where(r => r.ReceiptCode.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptCode)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastReceipt != null)
        {
            // Extraer el número del último código
            var lastCode = lastReceipt.ReceiptCode;
            var numberPart = lastCode.Substring(prefix.Length);
            if (int.TryParse(numberPart, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D6}";
    }
}
