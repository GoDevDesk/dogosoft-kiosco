using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using FastFoodApp.Core.Models;

namespace FastFoodApp.Core.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<ProductPriceList> ProductPriceLists { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ProductSupplier> ProductSuppliers { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboItem> ComboItems { get; set; }
        public DbSet<ComboSubstitutionOption> ComboSubstitutionOptions { get; set; }

        // NUEVOS: Materias Primas y Recetas
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<ProductRecipe> ProductRecipes { get; set; }

        // Tablas para pedidos
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public static string AppDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FastFoodApp");

        public static string DbPath => Path.Combine(AppDataFolder, "fastfood.db");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(AppDataFolder);
            var conn = $"Data Source={DbPath}";
            optionsBuilder.UseSqlite(conn);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ============================================================
            // CONFIGURACIÓN DE PRODUCTS
            // ============================================================

            // TracksStock: por defecto true
            modelBuilder.Entity<Product>()
                .Property(p => p.TracksStock)
                .HasDefaultValue(true);

            // Stock: nullable, por defecto null
            modelBuilder.Entity<Product>()
                .Property(p => p.Stock)
                .IsRequired(false); // Explícitamente nullable

            // MinimumStock: nullable
            modelBuilder.Entity<Product>()
                .Property(p => p.MinimumStock)
                .IsRequired(false);

            // MaximumStock: nullable
            modelBuilder.Entity<Product>()
                .Property(p => p.MaximumStock)
                .IsRequired(false);

            // HasRecipe: por defecto false
            modelBuilder.Entity<Product>()
                .Property(p => p.HasRecipe)
                .HasDefaultValue(false);

            // ============================================================
            // CONFIGURACIÓN DE RAWMATERIALS (Materias Primas)
            // ============================================================

            modelBuilder.Entity<RawMaterial>()
                .HasIndex(rm => rm.Code)
                .IsUnique();

            modelBuilder.Entity<RawMaterial>()
                .Property(rm => rm.IsIngredient)
                .HasDefaultValue(false);

            modelBuilder.Entity<RawMaterial>()
                .Property(rm => rm.AvailableQuantity)
                .HasDefaultValue(0);

            // ============================================================
            // CONFIGURACIÓN DE PRODUCTRECIPES (Recetas)
            // ============================================================

            modelBuilder.Entity<ProductRecipe>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Recipe)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductRecipe>()
                .HasOne(pr => pr.RawMaterial)
                .WithMany()
                .HasForeignKey(pr => pr.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict); // No eliminar materias primas si están en uso

            // ============================================================
            // RELACIONES DE SALES (historial)
            // ============================================================

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // No borrar productos si tienen ventas

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra venta, borrar items

            // ============================================================
            // RELACIONES DE ORDERS (pedidos)
            // ============================================================

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // No borrar productos si tienen pedidos

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra pedido, borrar items

            // Índices para mejorar performance en Orders
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Date);

            // ============================================================
            // RELACIONES DE COMBOS
            // ============================================================

            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Combo)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.ComboId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra combo, borrar items

            modelBuilder.Entity<ComboItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // No borrar productos si están en combos

            modelBuilder.Entity<ComboSubstitutionOption>()
                .HasOne(cso => cso.ComboItem)
                .WithMany()
                .HasForeignKey(cso => cso.ComboItemId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra item, borrar opciones

            modelBuilder.Entity<ComboSubstitutionOption>()
                .HasOne(cso => cso.AlternativeProduct)
                .WithMany()
                .HasForeignKey(cso => cso.AlternativeProductId)
                .OnDelete(DeleteBehavior.Restrict); // No borrar productos alternativos

            // ============================================================
            // ÍNDICES ADICIONALES
            // ============================================================

            // Índice en Product.IsActive para filtros rápidos
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);

            // Índice en Product.TracksStock
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.TracksStock);

            // Índice en Product.HasRecipe
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.HasRecipe);

            // Índice compuesto para búsquedas de productos activos con stock
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.IsActive, p.TracksStock, p.Stock });

            // Índice en RawMaterial.IsIngredient
            modelBuilder.Entity<RawMaterial>()
                .HasIndex(rm => rm.IsIngredient);

            // Índice en RawMaterial.IsActive
            modelBuilder.Entity<RawMaterial>()
                .HasIndex(rm => rm.IsActive);
        }
    }
}