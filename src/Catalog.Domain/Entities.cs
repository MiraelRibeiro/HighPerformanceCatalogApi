namespace Catalog.Domain;

public sealed class Product
{
    private Product()
    {
    }

    public Product(
        string name,
        string description,
        int categoryId,
        int supplierId,
        decimal price,
        int stockQuantity,
        decimal rating,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Name = name;
        Description = description;
        CategoryId = categoryId;
        SupplierId = supplierId;
        Price = price;
        StockQuantity = stockQuantity;
        Rating = rating;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        UpdatedAtUtc = DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc);
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public int SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public decimal Rating { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}

public sealed class Category
{
    private Category()
    {
    }

    public Category(string name) => Name = name;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<Product> Products { get; } = new List<Product>();
}

public sealed class Supplier
{
    private Supplier()
    {
    }

    public Supplier(string name) => Name = name;

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<Product> Products { get; } = new List<Product>();
}
