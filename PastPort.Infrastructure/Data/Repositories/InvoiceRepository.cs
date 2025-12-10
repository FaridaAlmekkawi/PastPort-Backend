using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;
using PastPort.Infrastructure.Data.Repositories;
using PastPort.Infrastructure.Data;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Invoice>> GetUserInvoicesAsync(string userId)
    {
        return await _dbSet
            .Where(i => i.UserId == userId)
            .Include(i => i.Items)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        return await _dbSet
            .Include(i => i.Items)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        var lastInvoice = await _dbSet
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-{month:D2}"))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;

        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"INV-{year}-{month:D2}-{nextNumber:D4}";
    }
}