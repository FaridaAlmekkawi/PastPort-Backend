using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Enums
{
    public enum RefundReason
    {
        RequestedByCustomer = 1,
        Duplicate = 2,
        Fraudulent = 3,
        ServiceNotProvided = 4,
        Other = 5
    }
}
