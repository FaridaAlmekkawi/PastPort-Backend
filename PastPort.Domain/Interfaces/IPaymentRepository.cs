using PastPort.Domain.Entities;
using PastPort.Domain.Enums;

namespace PastPort.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetPaymentByPayPalOrderIdAsync(string payPalOrderId);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
}
