using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Enums
{
    public enum InvoiceStatus
    {
        Draft = 0,
        Open = 1,
        Paid = 2,
        Void = 3,
        Uncollectible = 4
    }
}
