using  SmartRetailPlatform.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
