using System;
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

            // seed demo license
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

            // seed example products (solo 5 para demo)
            if (!ctx.Products.Any())
            {
                ctx.Products.AddRange(
                    new Product { Code = "0001", Name = "Gaseosa 500ml", Price = 220m, Stock = 20, Category = "Bebidas" },
                    new Product { Code = "0002", Name = "Chocolate", Price = 150m, Stock = 50, Category = "Golosinas" },
                    new Product { Code = "0003", Name = "Pan Bimbo", Price = 450m, Stock = 10, Category = "Panaderia" },
                    new Product { Code = "0004", Name = "Agua 1.5L", Price = 120m, Stock = 30, Category = "Bebidas" },
                    new Product { Code = "0005", Name = "Galletitas", Price = 300m, Stock = 15, Category = "Golosinas" }
                );
                ctx.SaveChanges();
            }
        }

        private static string GetHardwareHash()
        {
            // implementación simple: archivo único por instalacion
            var hwPath = Path.Combine(AppDbContext.AppDataFolder, "hwid.txt");
            if (!File.Exists(hwPath)) File.WriteAllText(hwPath, Guid.NewGuid().ToString());
            return File.ReadAllText(hwPath);
        }
    }
}
