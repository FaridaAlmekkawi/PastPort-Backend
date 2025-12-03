using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PastPort.Application.DTOs.Payment
{
    public class CaptureResponseDTO
    {
        public JsonElement PayPalResponse { get; set; }
    }
}
