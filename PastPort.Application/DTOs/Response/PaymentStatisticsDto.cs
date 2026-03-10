public class PaymentStatisticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }

    public int TotalPayments { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public int RefundedPayments { get; set; }

    public decimal AveragePaymentAmount { get; set; }

    public Dictionary<string, int> PaymentsByMonth { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
}