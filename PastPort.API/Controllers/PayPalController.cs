using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Payment;
using PastPort.Application.Interfaces;
[ApiController]
[Route("api/paypal")]
public class PayPalController(IPayPalService paypalService) : ControllerBase // Updated to use primary constructor  
{
    private readonly IPayPalService _paypalService = paypalService;

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO dto)
    {
        var orderId = await _paypalService.CreateOrder(dto.Amount);
        var response = new OrderResponseDTO { OrderId = orderId };
        return Ok(response);
    }

    [HttpPost("capture-order")]
    public async Task<IActionResult> CaptureOrder([FromBody] CaptureOrderDTO dto)
    {
        var result = await _paypalService.CaptureOrder(dto.OrderId);
        var response = new CaptureResponseDTO
        {
            PayPalResponse = System.Text.Json.JsonDocument.Parse(result).RootElement
        };
        return Ok(response);
    }
}



