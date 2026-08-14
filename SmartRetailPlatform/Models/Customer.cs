namespace SmartRetailPlatform.Models;

public abstract class Customer
{
    public string Name { get;  }
    public string Email { get;  }

    protected Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }
    public abstract decimal CalculateDiscount(decimal orderTotal);
}