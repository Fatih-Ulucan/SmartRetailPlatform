using SmartRetailPlatform.Models;
using Npgsql;
using SmartRetailPlatform.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

Customer customer1 = new RegularCustomer("Anna Kowalska", "anna@example.com");
Customer customer2 = new VIPCustomer("Piotr Nowak", "piotr@example.com");

decimal orderTotal = 250m;

Console.WriteLine($"{customer1.Name} discount: {customer1.CalculateDiscount(orderTotal)} PLN");
Console.WriteLine($"{customer2.Name} discount: {customer2.CalculateDiscount(orderTotal)} PLN");

Order order = new Order(customer2);

Product laptop = new Product("Laptop", 3500m, 10);
Product mouse = new Product("Mouse", 120m, 50);

order.AddItem(laptop, 1);
order.AddItem(mouse, 2);

Console.WriteLine($"Subtotal: {order.GetSubtotal()} PLN");
Console.WriteLine($"Total after discount: {order.GetTotalAfterDiscount()} PLN");
Console.WriteLine($"Remaining laptop stock: {laptop.GetStock()}");

Console.WriteLine("\n--- Testing PostgreSQL Connection ---");

await using var connection = new NpgsqlConnection(DbConfig.ConnectionString);
await connection.OpenAsync();

Console.WriteLine("Connected to PostgreSQL successfully!");

await connection.CloseAsync();
Console.WriteLine("\n--- Testing ProductRepository ---");

var productRepository = new ProductRepository();
Product? dbLaptop = await productRepository.GetByIdAsync(1);

if (dbLaptop != null)
{
    Console.WriteLine($"Fetched from DB -> Name: {dbLaptop.Name}, Price: {dbLaptop.Price} PLN, Stock: {dbLaptop.GetStock()}");
}
else
{
    Console.WriteLine("Product not found.");
}

Console.WriteLine("\n--- Testing OrderRepository (Real DB Write) ---");

var orderRepository = new OrderRepository();

int newOrderId = await orderRepository.CreateOrderAsync(customerId: 2); // Piotr Nowak
Console.WriteLine($"Created order with id: {newOrderId}");

await orderRepository.AddOrderItemAsync(newOrderId, productId: 2, quantity: 1, unitPrice: 120.00m);
Console.WriteLine("Order item added successfully!");

Product? updatedMouse = await productRepository.GetByIdAsync(2);
Console.WriteLine($"Mouse stock after order: {updatedMouse?.GetStock()}");


Console.WriteLine("\n--- Testing Binary Search ---");

var testProducts = new List<Product>
{
    new Product("Keyboard", 250m, 20),
    new Product("Laptop", 4200m, 7),
    new Product("Mouse", 120m, 44),
    new Product("Monitor", 900m, 15),
    new Product("Webcam", 350m, 30)
};

var catalog = new ProductCatalog(testProducts);

Product? found = catalog.FindByExactPrice(900m);
Console.WriteLine(found != null
    ? $"Found: {found.Name} at {found.Price} PLN"
    : "Not found");

Product? notFound = catalog.FindByExactPrice(999m);
Console.WriteLine(notFound != null
    ? $"Found: {notFound.Name}"
    : "Not found (correctly)");

Console.WriteLine("\n--- Testing OrderAnalytics (Co-Occurrence) ---");

var pastOrders = new List<List<string>>
{
    new List<string> { "Laptop", "Mouse" },
    new List<string> { "Laptop", "Keyboard", "Mouse" },
    new List<string> { "Laptop", "Mouse", "Webcam" },
    new List<string> { "Monitor", "Keyboard" },
    new List<string> { "Laptop", "Mouse", "Keyboard" }
};

var analytics = new OrderAnalytics();
var coOccurrenceMap = analytics.BuildCoOccurrenceMap(pastOrders);

var recommendations = analytics.GetTopRecommendations(coOccurrenceMap, "Laptop");

Console.WriteLine("Customers who bought Laptop also bought:");
foreach (var item in recommendations)
{
    Console.WriteLine($"- {item}");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();