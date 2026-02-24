using Microsoft.EntityFrameworkCore;
using PaymentMicroservicio.Application.Interfaces;
using PaymentMicroservicio.Domain.Entities;
using PaymentMicroservicio.Domain.Enums;
using PaymentMicroservicio.Infrastructure.Data;

namespace PaymentMicroservicio.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de pagos
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.PaymentItems)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Payments
            .Include(p => p.PaymentItems)
            .FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
    }  // Note: NO AsNoTracking here — this entity is updated after read in ProcessPaymentCompleted

    public async Task<Payment?> GetByPaymentIntentIdAsync(string paymentIntentId)
    {
        return await _context.Payments
            .Include(p => p.PaymentItems)
            .FirstOrDefaultAsync(p => p.StripeSessionId == paymentIntentId);
    }

    public async Task<List<Payment>> GetByEstudianteAsync(int idEstudiante)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.PaymentItems)
            .Where(p => p.IdEstudiante == idEstudiante)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Payment?> GetMatriculaPaymentAsync(int idEstudiante, int idPeriodo)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.IdEstudiante == idEstudiante 
                     && p.IdPeriodo == idPeriodo 
                     && p.Status == "Succeeded"
                     && (p.PaymentType == "Enrollment" || p.MetadataJson.Contains("matricula")))
            .OrderByDescending(p => p.FechaCreacion)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasPaidMatriculaAsync(int idEstudiante, int idPeriodo)
    {
        return await _context.Payments
            .AnyAsync(p => p.IdEstudiante == idEstudiante 
                        && p.IdPeriodo == idPeriodo 
                        && p.Status == "Succeeded"
                        && (p.PaymentType == "Enrollment" || p.MetadataJson.Contains("matricula")));
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
