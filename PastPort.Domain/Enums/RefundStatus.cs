using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Enums
{

    public enum RefundStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }
}
