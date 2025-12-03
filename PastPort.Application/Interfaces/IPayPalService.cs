using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PastPort.Application.Interfaces
{
    public interface IPayPalService
    {
        Task<string> CreateOrder(decimal amount);
        Task<string> CaptureOrder(string orderId);
    }
}
