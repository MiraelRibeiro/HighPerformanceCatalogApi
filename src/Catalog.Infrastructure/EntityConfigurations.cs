using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_products_price", "price >= 0");
            tableBuilder.HasCheckConstraint("ck_products_stock_quantity", "stock_quantity >= 0");
            tableBuilder.HasCheckConstraint("ck_products_rating", "rating >= 0 AND rating <= 5");
        });

        builder.HasKey(product => product.Id).HasName("pk_products");
        builder.Property(product => product.Id).HasColumnName("id");
        builder.Property(product => product.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(product => product.CategoryId).HasColumnName("category_id");
        builder.Property(product => product.SupplierId).HasColumnName("supplier_id");
        builder.Property(product => product.Price).HasColumnName("price").HasPrecision(12, 2);
        builder.Property(product => product.StockQuantity).HasColumnName("stock_quantity");
        builder.Property(product => product.Rating).HasColumnName("rating").HasPrecision(3, 2);
        builder.Property(product => product.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(product => product.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_categories_category_id");

        builder.HasOne(product => product.Supplier)
            .WithMany(supplier => supplier.Products)
            .HasForeignKey(product => product.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_suppliers_supplier_id");

        builder.HasIndex(product => new { product.CategoryId, product.Price, product.Id })
            .HasDatabaseName("ix_products_category_price_id");

        builder.HasIndex(product => new { product.Rating, product.Id })
            .IsDescending(true, false)
            .HasDatabaseName("ix_products_rating_id");
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id).HasName("pk_categories");
        builder.Property(category => category.Id).HasColumnName("id");
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.HasIndex(category => category.Name).IsUnique().HasDatabaseName("ux_categories_name");
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(supplier => supplier.Id).HasName("pk_suppliers");
        builder.Property(supplier => supplier.Id).HasColumnName("id");
        builder.Property(supplier => supplier.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.HasIndex(supplier => supplier.Name).IsUnique().HasDatabaseName("ux_suppliers_name");
    }
}
