using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace PastPort.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISavedPaymentMethodRepository _savedPaymentMethodRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPayPalPaymentService _paypalService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppSettings _settings;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IInvoiceRepository invoiceRepository,
            ISavedPaymentMethodRepository savedPaymentMethodRepository,
            ISubscriptionRepository subscriptionRepository,
            IPayPalPaymentService paypalService,
            UserManager<ApplicationUser> userManager,
            IAppSettings settings,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _invoiceRepository = invoiceRepository;
            _savedPaymentMethodRepository = savedPaymentMethodRepository;
            _subscriptionRepository = subscriptionRepository;
            _paypalService = paypalService;
            _userManager = userManager;
            _settings = settings;
            _logger = logger;
        }

        // ===================================================
        // ================ PayPal Methods ====================
        // ===================================================

        public async Task<PayPalOrderResponseDto> CreatePayPalOrderAsync(
            string userId,
            CreatePaymentIntentRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            var returnUrl = $"{_settings.FrontendUrl}/payment/paypal/success";
            var cancelUrl = $"{_settings.FrontendUrl}/payment/paypal/cancel";

            var order = await _paypalService.CreateOrderAsync(
                request.Amount,
                request.Currency,
                returnUrl,
                cancelUrl);

            if (order == null || string.IsNullOrEmpty(order.Id))
                throw new Exception("Failed to create PayPal order");

            var payment = new Payment
            {
                UserId = userId,
                Amount = request.Amount,
                Currency = request.Currency,
                Provider = PaymentProvider.PayPal,
                ProviderPaymentId = order.Id,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            return new PayPalOrderResponseDto
            {
                OrderId = order.Id,
                ApproveUrl = order.ApprovalLink
            };
        }

        // ===================================================
        // ================ General Payments ==================
        // ===================================================

        public async Task<Payment> GetPaymentByIdAsync(int id)
        {
            return await _paymentRepository.GetByIdAsync(id)
                ?? throw new Exception("Payment not found");
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId)
        {
            return await _paymentRepository.GetUserPaymentsAsync(userId);
        }

        public async Task DeletePaymentAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id)
                ?? throw new Exception("Payment not found");

            await _paymentRepository.DeleteAsync(payment);
        }

        // ===================================================
        // ================= Invoices =========================
        // ===================================================

        public async Task<Invoice> CreateInvoiceAsync(CreateInvoiceDto request)
        {
            var invoice = new Invoice
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = InvoiceStatus.Unpaid,
                CreatedAt = DateTime.UtcNow
            };

            await _invoiceRepository.AddAsync(invoice);

            return invoice;
        }

        public async Task<Invoice> GetInvoiceAsync(int id)
        {
            return await _invoiceRepository.GetByIdAsync(id)
                ?? throw new Exception("Invoice not found");
        }

        // ===================================================
        // ================= Helper Mapping ==================
        // ===================================================

        private InvoiceDto MapToInvoiceDto(Invoice invoice)
        {
            return new InvoiceDto
            {
                Id = invoice.Id,
                UserId = invoice.UserId,
                Amount = invoice.Amount,
                Currency = invoice.Currency,
                Status = invoice.Status.ToString(),
                CreatedAt = invoice.CreatedAt
            };
        }
    }
}
