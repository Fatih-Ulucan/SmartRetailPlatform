using Microsoft.AspNetCore.Mvc;
using SmartRetailPlatform.Data;
using SmartRetailPlatform.Models;

namespace  SmartRetailPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderRepository _orderRepository;

    public OrderController()
    {
        _orderRepository = new OrderRepository();
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("Order must contain at least one item.");
        }

        int newOrderId = await _orderRepository.CreateOrderAsync(request.CustomerId);

        foreach (var item in request.Items)
        {
            await _orderRepository.AddOrderItemAsync(
                newOrderId,
                item.ProductId,
                item.Quantity,
                item.UnitPrice
            );
        }
        return CreatedAtAction(nameof(CreateOrder), new {id = newOrderId}, new {orderId = newOrderId});
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        OrderDetailsResponse? detail = await _orderRepository.GetOrderDetailsAsync(id);

        if (detail == null)
        {
            return NotFound($"Order with id {id} not found.");
        }
        return Ok(detail);
    }
}

