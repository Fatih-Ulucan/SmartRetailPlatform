using Microsoft.AspNetCore.Mvc;
using SmartRetailPlatform.Data;
using SmartRetailPlatform.Models;

namespace SmartRetailPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]

public class ProductsController : ControllerBase
{
    private readonly ProductRepository _productRepository;

    public ProductsController()
    {
        _productRepository = new ProductRepository();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<Product> products = await _productRepository.GetAllAsync();
        var response = products.Select(ProductResponse.FromProduct).ToList();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Product? product = await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound($"Product with id {id} not found.");
        }

        return Ok(ProductResponse.FromProduct(product));
    }
}
