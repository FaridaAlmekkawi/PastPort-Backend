using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Enums
{
    public enum PaymentTransactionType
    {
        Charge = 1,
        Refund = 2,
        Capture = 3,
        Cancel = 4
    }
}
