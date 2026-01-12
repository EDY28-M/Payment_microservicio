using PaymentMicroservicio.Application.DTOs;
using PaymentMicroservicio.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace PaymentMicroservicio.Application.Interfaces;

/// <summary>
/// Interfaz para el servicio de recibos de pago
/// </summary>
public interface IPaymentReceiptService
{
    Task<PaymentReceipt?> CreateReceiptFromSessionAsync(Session session, Event stripeEvent);
    Task<PaymentReceiptResponse?> GetReceiptBySessionIdAsync(string sessionId, int studentId);
    Task<string> GenerateReceiptCodeAsync(int year);
}
