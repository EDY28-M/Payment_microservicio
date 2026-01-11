using PaymentMicroservicio.Domain.Entities;

namespace PaymentMicroservicio.Application.Interfaces;

/// <summary>
/// Repositorio de pagos
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<Payment?> GetBySessionIdAsync(string sessionId);
    Task<Payment?> GetByPaymentIntentIdAsync(string paymentIntentId);
    Task<List<Payment>> GetByEstudianteAsync(int idEstudiante);
    Task<Payment?> GetMatriculaPaymentAsync(int idEstudiante, int idPeriodo);
    Task<bool> HasPaidMatriculaAsync(int idEstudiante, int idPeriodo);
    Task<Payment> CreateAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task SaveChangesAsync();
}
