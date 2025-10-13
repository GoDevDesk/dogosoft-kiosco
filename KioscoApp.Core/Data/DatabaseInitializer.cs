using System;
using System.IO;
using System.Linq;
using KioscoApp.Core.Models;

namespace KioscoApp.Core.Data
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseAndSeed()
        {
            using var ctx = new AppDbContext();
            ctx.Database.EnsureCreated();

            // Seed demo license
            if (!ctx.Licenses.Any())
            {
                var now = DateTime.UtcNow;
                ctx.Licenses.Add(new License
                {
                    Key = Guid.NewGuid().ToString(),
                    Type = "Demo",
                    InstallDate = now,
                    ExpiryDate = now.AddDays(30),
                    HardwareHash = GetHardwareHash()
                });
                ctx.SaveChanges();
            }

            // Seed Categories (Rubros)
            if (!ctx.Categories.Any())
            {
                ctx.Categories.AddRange(
                    new Category { Name = "Bebidas", IsActive = true },
                    new Category { Name = "Golosinas", IsActive = true },
                    new Category { Name = "Cigarrillos", IsActive = true },
                    new Category { Name = "Snacks", IsActive = true },
                    new Category { Name = "Lácteos", IsActive = true },
                    new Category { Name = "Panadería", IsActive = true },
                    new Category { Name = "Almacén", IsActive = true },
                    new Category { Name = "Limpieza", IsActive = true },
                    new Category { Name = "Perfumería", IsActive = true },
                    new Category { Name = "Otros", IsActive = true }
                );
                ctx.SaveChanges();
            }

            // Seed Price Lists
            if (!ctx.PriceLists.Any())
            {
                ctx.PriceLists.AddRange(
                    new PriceList { Name = "Lista General", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true },
                    new PriceList { Name = "Minorista", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true },
                    new PriceList { Name = "Mayorista", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true }
                );
                ctx.SaveChanges();
            }

            // Seed Suppliers (Proveedores)
            if (!ctx.Suppliers.Any())
            {
                ctx.Suppliers.AddRange(
                    new Supplier
                    {
                        Name = "Distribuidora Central",
                        ContactPerson = "Juan Pérez",
                        Phone = "011-4567-8900",
                        Email = "ventas@distribuidoracentral.com",
                        Address = "Av. Corrientes 1234, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Mayorista del Sur",
                        ContactPerson = "María González",
                        Phone = "011-4567-8901",
                        Email = "info@mayoristasdelsur.com",
                        Address = "Av. Rivadavia 5678, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Alimentos y Bebidas SA",
                        ContactPerson = "Carlos Rodríguez",
                        Phone = "011-4567-8902",
                        Email = "contacto@alimentosybebidas.com",
                        Address = "San Martín 910, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Proveedor Express",
                        ContactPerson = "Ana Martínez",
                        Phone = "011-4567-8903",
                        Email = "ventas@proveedorexpress.com",
                        Address = "Belgrano 456, CABA",
                        IsActive = true
                    }
                );
                ctx.SaveChanges();
            }

            // Seed Products (SIN Price ni Cost)
            if (!ctx.Products.Any())
            {
                var bebidasCat = ctx.Categories.First(c => c.Name == "Bebidas");
                var golosinasCat = ctx.Categories.First(c => c.Name == "Golosinas");
                var panCat = ctx.Categories.First(c => c.Name == "Panadería");
                var snacksCat = ctx.Categories.First(c => c.Name == "Snacks");
                var lacteosCat = ctx.Categories.First(c => c.Name == "Lácteos");

                ctx.Products.AddRange(
                    new Product
                    {
                        Code = "0001",
                        Name = "Coca Cola 500ml",
                        ShortDescription = "COCA COLA X 500 ML",
                        Stock = 48,
                        MinimumStock = 10,
                        MaximumStock = 100,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0002",
                        Name = "Chocolate Milka 100g",
                        ShortDescription = "MILKA X 100 GR",
                        Stock = 30,
                        MinimumStock = 5,
                        MaximumStock = 50,
                        CategoryId = golosinasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0003",
                        Name = "Pan Lactal Bimbo",
                        ShortDescription = "PAN LACTAL BIMBO",
                        Stock = 15,
                        MinimumStock = 5,
                        MaximumStock = 30,
                        CategoryId = panCat.Id,
                        UnitOfSale = "UN",
                        HasExpiry = true,
                        ExpiryDate = DateTime.Now.AddDays(7),
                        ExpiryAlertDays = 2,
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0004",
                        Name = "Agua Mineral Villavicencio 1.5L",
                        ShortDescription = "AGUA VILLAVICENCIO X 1.5 LTS",
                        Stock = 60,
                        MinimumStock = 20,
                        MaximumStock = 120,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0005",
                        Name = "Galletitas Oreo",
                        ShortDescription = "OREO X 118 GR",
                        Stock = 24,
                        MinimumStock = 6,
                        MaximumStock = 50,
                        CategoryId = golosinasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0006",
                        Name = "Papas Fritas Lays 140g",
                        ShortDescription = "LAYS X 140 GR",
                        Stock = 36,
                        MinimumStock = 10,
                        MaximumStock = 60,
                        CategoryId = snacksCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0007",
                        Name = "Yogur Ser Frutilla 190g",
                        ShortDescription = "YOGUR SER FRUTILLA X 190 GR",
                        Stock = 20,
                        MinimumStock = 8,
                        MaximumStock = 40,
                        CategoryId = lacteosCat.Id,
                        UnitOfSale = "UN",
                        HasExpiry = true,
                        ExpiryDate = DateTime.Now.AddDays(5),
                        ExpiryAlertDays = 1,
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0008",
                        Name = "Cerveza Quilmes 1L",
                        ShortDescription = "QUILMES X 1 LTS",
                        Stock = 42,
                        MinimumStock = 12,
                        MaximumStock = 72,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0009",
                        Name = "Alfajor Jorgito Triple",
                        ShortDescription = "JORGITO TRIPLE",
                        Stock = 50,
                        MinimumStock = 15,
                        MaximumStock = 100,
                        CategoryId = golosinasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "0010",
                        Name = "Leche La Serenísima 1L",
                        ShortDescription = "LECHE SERENISIMA X 1 LTS",
                        Stock = 25,
                        MinimumStock = 10,
                        MaximumStock = 50,
                        CategoryId = lacteosCat.Id,
                        UnitOfSale = "LTS",
                        HasExpiry = true,
                        ExpiryDate = DateTime.Now.AddDays(4),
                        ExpiryAlertDays = 1,
                        IsActive = true
                    }
                );
                ctx.SaveChanges();
            }

            // Seed ProductPriceList (Precios por lista)
            if (!ctx.ProductPriceLists.Any())
            {
                var listaGeneral = ctx.PriceLists.First(pl => pl.Name == "Lista General");
                var minorista = ctx.PriceLists.First(pl => pl.Name == "Minorista");
                var mayorista = ctx.PriceLists.First(pl => pl.Name == "Mayorista");
                var productos = ctx.Products.ToList();

                // Precios base de ejemplo para cada producto (índice basado)
                decimal[] costosBase = { 350m, 600m, 850m, 300m, 650m, 750m, 380m, 650m, 450m, 550m };
                decimal[] preciosBase = { 500m, 850m, 1200m, 450m, 950m, 1100m, 550m, 900m, 650m, 800m };

                for (int i = 0; i < productos.Count; i++)
                {
                    var producto = productos[i];
                    var costoBase = i < costosBase.Length ? costosBase[i] : 100m;
                    var precioBase = i < preciosBase.Length ? preciosBase[i] : 150m;

                    // Lista General - precio normal
                    ctx.ProductPriceLists.Add(new ProductPriceList
                    {
                        ProductId = producto.Id,
                        PriceListId = listaGeneral.Id,
                        CostPrice = costoBase,
                        SalePrice = precioBase,
                        ProfitPercentage = ((precioBase - costoBase) / costoBase) * 100,
                        InternalTaxPercentage = 0,
                        IVA = "21%",
                        LastUpdate = DateTime.Now
                    });

                    // Minorista - 5% más caro
                    var precioMinorista = precioBase * 1.05m;
                    ctx.ProductPriceLists.Add(new ProductPriceList
                    {
                        ProductId = producto.Id,
                        PriceListId = minorista.Id,
                        CostPrice = costoBase,
                        SalePrice = Math.Round(precioMinorista, 2),
                        ProfitPercentage = ((precioMinorista - costoBase) / costoBase) * 100,
                        InternalTaxPercentage = 0,
                        IVA = "21%",
                        LastUpdate = DateTime.Now
                    });

                    // Mayorista - 10% más barato
                    var precioMayorista = precioBase * 0.90m;
                    ctx.ProductPriceLists.Add(new ProductPriceList
                    {
                        ProductId = producto.Id,
                        PriceListId = mayorista.Id,
                        CostPrice = costoBase,
                        SalePrice = Math.Round(precioMayorista, 2),
                        ProfitPercentage = ((precioMayorista - costoBase) / costoBase) * 100,
                        InternalTaxPercentage = 0,
                        IVA = "21%",
                        LastUpdate = DateTime.Now
                    });
                }
                ctx.SaveChanges();
            }

            // Seed ProductSuppliers (Proveedores por producto)
            if (!ctx.ProductSuppliers.Any())
            {
                var productos = ctx.Products.Take(5).ToList();
                var proveedores = ctx.Suppliers.ToList();

                // Costos base para proveedores
                decimal[] costosProveedor = { 350m, 600m, 850m, 300m, 650m };

                // Asignar 2-3 proveedores a cada producto
                for (int i = 0; i < productos.Count; i++)
                {
                    var producto = productos[i];
                    var proveedor1 = proveedores[i % proveedores.Count];
                    var proveedor2 = proveedores[(i + 1) % proveedores.Count];
                    var costoBase = i < costosProveedor.Length ? costosProveedor[i] : 100m;

                    ctx.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = producto.Id,
                        SupplierId = proveedor1.Id,
                        Cost = costoBase,
                        IsDefault = true,
                        LastUpdate = DateTime.Now
                    });

                    ctx.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = producto.Id,
                        SupplierId = proveedor2.Id,
                        Cost = costoBase * 1.1m, // 10% más caro
                        IsDefault = false,
                        LastUpdate = DateTime.Now
                    });
                }
                ctx.SaveChanges();
            }
        }

        private static string GetHardwareHash()
        {
            var hwPath = Path.Combine(AppDbContext.AppDataFolder, "hwid.txt");
            if (!File.Exists(hwPath))
                File.WriteAllText(hwPath, Guid.NewGuid().ToString());
            return File.ReadAllText(hwPath);
        }
    }
}