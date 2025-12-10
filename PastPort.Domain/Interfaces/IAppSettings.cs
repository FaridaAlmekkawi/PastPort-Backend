using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Interfaces;

public interface IAppSettings
{
    // Define app settings properties here
    string DatabaseConnectionString { get; }
    string StripeSecretKey { get; }
    string PayPalClientId { get; }
    string PayPalClientSecret { get; }
    // Add other settings as needed
}