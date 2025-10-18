using System;
using System.IO;
using System.Linq;
using FastFoodApp.Core.Models;

namespace FastFoodApp.Core.Data
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

            // Seed Categories (ADAPTADAS PARA FAST FOOD)
            if (!ctx.Categories.Any())
            {
                ctx.Categories.AddRange(
                    new Category { Name = "Hamburguesas", IsActive = true },
                    new Category { Name = "Pizzas", IsActive = true },
                    new Category { Name = "Papas y Acompañamientos", IsActive = true },
                    new Category { Name = "Bebidas", IsActive = true },
                    new Category { Name = "Postres", IsActive = true },
                    new Category { Name = "Salsas", IsActive = true },
                    new Category { Name = "Combos", IsActive = true },
                    new Category { Name = "Ensaladas", IsActive = true },
                    new Category { Name = "Sandwiches", IsActive = true },
                    new Category { Name = "Extras", IsActive = true },
                    // NUEVAS: Categorías para Materias Primas
                    new Category { Name = "Carnes", IsActive = true },
                    new Category { Name = "Vegetales", IsActive = true },
                    new Category { Name = "Lácteos", IsActive = true },
                    new Category { Name = "Panadería", IsActive = true },
                    new Category { Name = "Condimentos", IsActive = true },
                    new Category { Name = "Packaging", IsActive = true }
                );
                ctx.SaveChanges();
            }

            // Seed Price Lists
            if (!ctx.PriceLists.Any())
            {
                ctx.PriceLists.AddRange(
                    new PriceList { Name = "Lista General", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true },
                    new PriceList { Name = "Delivery", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true },
                    new PriceList { Name = "Salón", IsActive = true, CreatedAt = DateTime.Now, IsProtected = true }
                );
                ctx.SaveChanges();
            }

            // Seed Suppliers
            if (!ctx.Suppliers.Any())
            {
                ctx.Suppliers.AddRange(
                    new Supplier
                    {
                        Name = "Distribuidora de Carnes El Gaucho",
                        ContactPerson = "Juan Pérez",
                        Phone = "011-4567-8900",
                        Email = "ventas@elgaucho.com",
                        Address = "Av. Corrientes 1234, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Verduras Frescas del Mercado",
                        ContactPerson = "María González",
                        Phone = "011-4567-8901",
                        Email = "info@verdurasfrescas.com",
                        Address = "Av. Rivadavia 5678, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Panadería y Panificados La Esperanza",
                        ContactPerson = "Carlos Rodríguez",
                        Phone = "011-4567-8902",
                        Email = "contacto@laesperanza.com",
                        Address = "San Martín 910, CABA",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Distribuidora de Bebidas Express",
                        ContactPerson = "Ana Martínez",
                        Phone = "011-4567-8903",
                        Email = "ventas@bebidasexpress.com",
                        Address = "Belgrano 456, CABA",
                        IsActive = true
                    }
                );
                ctx.SaveChanges();
            }

            // ============================================================
            // SEED RAWMATERIALS (Materias Primas)
            // ============================================================
            if (!ctx.RawMaterials.Any())
            {
                var carnesCat = ctx.Categories.First(c => c.Name == "Carnes");
                var vegetalesCat = ctx.Categories.First(c => c.Name == "Vegetales");
                var lacteosCat = ctx.Categories.First(c => c.Name == "Lácteos");
                var panaderiaCat = ctx.Categories.First(c => c.Name == "Panadería");
                var condimentosCat = ctx.Categories.First(c => c.Name == "Condimentos");
                var packagingCat = ctx.Categories.First(c => c.Name == "Packaging");

                var provCarnes = ctx.Suppliers.First(s => s.Name.Contains("Carnes"));
                var provVerduras = ctx.Suppliers.First(s => s.Name.Contains("Verduras"));
                var provPanaderia = ctx.Suppliers.First(s => s.Name.Contains("Panadería"));

                ctx.RawMaterials.AddRange(
                    // INGREDIENTES (IsIngredient = true) - SE DESCUENTAN EN VENTAS
                    new RawMaterial
                    {
                        Code = "ING-001",
                        Name = "Carne Picada",
                        Description = "Carne picada especial para hamburguesas",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 50.00m,
                        MinimumQuantity = 10.00m,
                        MaximumQuantity = 100.00m,
                        UnitCost = 3500m,
                        CategoryId = carnesCat.Id,
                        SupplierId = provCarnes.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-002",
                        Name = "Lechuga",
                        Description = "Lechuga fresca",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 15.00m,
                        MinimumQuantity = 3.00m,
                        MaximumQuantity = 30.00m,
                        UnitCost = 800m,
                        CategoryId = vegetalesCat.Id,
                        SupplierId = provVerduras.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-003",
                        Name = "Tomate",
                        Description = "Tomate perita",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 20.00m,
                        MinimumQuantity = 5.00m,
                        MaximumQuantity = 40.00m,
                        UnitCost = 1200m,
                        CategoryId = vegetalesCat.Id,
                        SupplierId = provVerduras.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-004",
                        Name = "Pan de Hamburguesa",
                        Description = "Pan fresco para hamburguesa",
                        IsIngredient = true,
                        Unit = "UN",
                        AvailableQuantity = 200.00m,
                        MinimumQuantity = 40.00m,
                        MaximumQuantity = 400.00m,
                        UnitCost = 350m,
                        CategoryId = panaderiaCat.Id,
                        SupplierId = provPanaderia.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-005",
                        Name = "Queso Cheddar",
                        Description = "Queso cheddar en fetas",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 10.00m,
                        MinimumQuantity = 2.00m,
                        MaximumQuantity = 20.00m,
                        UnitCost = 4500m,
                        CategoryId = lacteosCat.Id,
                        SupplierId = provCarnes.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-006",
                        Name = "Cebolla",
                        Description = "Cebolla blanca",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 12.00m,
                        MinimumQuantity = 3.00m,
                        MaximumQuantity = 25.00m,
                        UnitCost = 600m,
                        CategoryId = vegetalesCat.Id,
                        SupplierId = provVerduras.Id,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "ING-007",
                        Name = "Panceta",
                        Description = "Panceta ahumada",
                        IsIngredient = true,
                        Unit = "KG",
                        AvailableQuantity = 8.00m,
                        MinimumQuantity = 2.00m,
                        MaximumQuantity = 15.00m,
                        UnitCost = 5000m,
                        CategoryId = carnesCat.Id,
                        SupplierId = provCarnes.Id,
                        IsActive = true
                    },

                    // INSUMOS NO INGREDIENTES (IsIngredient = false) - SOLO PARA COMPRAS
                    new RawMaterial
                    {
                        Code = "PKG-001",
                        Name = "Papel Manteca",
                        Description = "Papel para envolver hamburguesas",
                        IsIngredient = false,
                        Unit = "UN",
                        AvailableQuantity = 0m,
                        MinimumQuantity = null,
                        MaximumQuantity = null,
                        UnitCost = 150m,
                        CategoryId = packagingCat.Id,
                        SupplierId = null,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "PKG-002",
                        Name = "Cajas de Cartón",
                        Description = "Cajas para delivery",
                        IsIngredient = false,
                        Unit = "UN",
                        AvailableQuantity = 0m,
                        MinimumQuantity = null,
                        MaximumQuantity = null,
                        UnitCost = 250m,
                        CategoryId = packagingCat.Id,
                        SupplierId = null,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "PKG-003",
                        Name = "Vasos Descartables",
                        Description = "Vasos 500ml",
                        IsIngredient = false,
                        Unit = "UN",
                        AvailableQuantity = 0m,
                        MinimumQuantity = null,
                        MaximumQuantity = null,
                        UnitCost = 80m,
                        CategoryId = packagingCat.Id,
                        SupplierId = null,
                        IsActive = true
                    },
                    new RawMaterial
                    {
                        Code = "PKG-004",
                        Name = "Servilletas",
                        Description = "Servilletas de papel",
                        IsIngredient = false,
                        Unit = "UN",
                        AvailableQuantity = 0m,
                        MinimumQuantity = null,
                        MaximumQuantity = null,
                        UnitCost = 30m,
                        CategoryId = packagingCat.Id,
                        SupplierId = null,
                        IsActive = true
                    }
                );
                ctx.SaveChanges();
            }

            // Seed Products (PRODUCTOS DE FAST FOOD)
            if (!ctx.Products.Any())
            {
                var hamburguesasCat = ctx.Categories.First(c => c.Name == "Hamburguesas");
                var pizzasCat = ctx.Categories.First(c => c.Name == "Pizzas");
                var papasCat = ctx.Categories.First(c => c.Name == "Papas y Acompañamientos");
                var bebidasCat = ctx.Categories.First(c => c.Name == "Bebidas");
                var postresCat = ctx.Categories.First(c => c.Name == "Postres");
                var salsasCat = ctx.Categories.First(c => c.Name == "Salsas");

                ctx.Products.AddRange(
                    // HAMBURGUESAS - NO llevan stock, SÍ usan receta
                    new Product
                    {
                        Code = "H001",
                        Name = "Hamburguesa Clásica",
                        ShortDescription = "Carne, lechuga, tomate, cebolla",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = hamburguesasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "H002",
                        Name = "Hamburguesa con Queso",
                        ShortDescription = "Carne, queso cheddar, lechuga, tomate",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = hamburguesasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "H003",
                        Name = "Hamburguesa Completa",
                        ShortDescription = "Doble carne, queso, huevo, panceta",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = hamburguesasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "H004",
                        Name = "Hamburguesa Bacon",
                        ShortDescription = "Carne, panceta, queso, cebolla caramelizada",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = hamburguesasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },

                    // PIZZAS - NO llevan stock, SÍ usan receta
                    new Product
                    {
                        Code = "P001",
                        Name = "Pizza Muzzarella",
                        ShortDescription = "Muzza y aceitunas",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = pizzasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "P002",
                        Name = "Pizza Napolitana",
                        ShortDescription = "Muzza, tomate, ajo y orégano",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = true,  // ✅ USA RECETA
                        CategoryId = pizzasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },

                    // PAPAS Y ACOMPAÑAMIENTOS - SÍ llevan stock, NO usan receta
                    new Product
                    {
                        Code = "A001",
                        Name = "Papas Fritas Chicas",
                        ShortDescription = "Porción individual",
                        TracksStock = true,
                        Stock = 200,
                        MinimumStock = 40,
                        MaximumStock = 300,
                        HasRecipe = false,
                        CategoryId = papasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "A002",
                        Name = "Papas Fritas Grandes",
                        ShortDescription = "Porción grande",
                        TracksStock = true,
                        Stock = 150,
                        MinimumStock = 30,
                        MaximumStock = 250,
                        HasRecipe = false,
                        CategoryId = papasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "A003",
                        Name = "Aros de Cebolla",
                        ShortDescription = "6 unidades",
                        TracksStock = true,
                        Stock = 100,
                        MinimumStock = 20,
                        MaximumStock = 150,
                        HasRecipe = false,
                        CategoryId = papasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },

                    // BEBIDAS - SÍ llevan stock, NO usan receta
                    new Product
                    {
                        Code = "B001",
                        Name = "Coca Cola 500ml",
                        ShortDescription = "Botella descartable",
                        TracksStock = true,
                        Stock = 150,
                        MinimumStock = 30,
                        MaximumStock = 250,
                        HasRecipe = false,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "B002",
                        Name = "Agua Mineral 500ml",
                        ShortDescription = "Botella descartable",
                        TracksStock = true,
                        Stock = 120,
                        MinimumStock = 25,
                        MaximumStock = 200,
                        HasRecipe = false,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "B003",
                        Name = "Cerveza Quilmes 1L",
                        ShortDescription = "Botella de vidrio retornable",
                        TracksStock = true,
                        Stock = 80,
                        MinimumStock = 15,
                        MaximumStock = 150,
                        HasRecipe = false,
                        CategoryId = bebidasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },

                    // POSTRES - SÍ llevan stock, NO usan receta
                    new Product
                    {
                        Code = "D001",
                        Name = "Helado Americana",
                        ShortDescription = "Sabor a elección",
                        TracksStock = true,
                        Stock = 60,
                        MinimumStock = 10,
                        MaximumStock = 100,
                        HasRecipe = false,
                        CategoryId = postresCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "D002",
                        Name = "Flan con Crema",
                        ShortDescription = "Casero",
                        TracksStock = true,
                        Stock = 40,
                        MinimumStock = 8,
                        MaximumStock = 70,
                        HasRecipe = false,
                        CategoryId = postresCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },

                    // SALSAS - NO llevan stock, NO usan receta
                    new Product
                    {
                        Code = "S001",
                        Name = "Salsa Ketchup",
                        ShortDescription = "Sobrecito individual",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = false,
                        CategoryId = salsasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "S002",
                        Name = "Salsa Mayonesa",
                        ShortDescription = "Sobrecito individual",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = false,
                        CategoryId = salsasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    },
                    new Product
                    {
                        Code = "S003",
                        Name = "Salsa Mostaza",
                        ShortDescription = "Sobrecito individual",
                        TracksStock = false,
                        Stock = null,
                        MinimumStock = null,
                        MaximumStock = null,
                        HasRecipe = false,
                        CategoryId = salsasCat.Id,
                        UnitOfSale = "UN",
                        IsActive = true
                    }
                );
                ctx.SaveChanges();
            }

            // ============================================================
            // SEED PRODUCT// ============================================================
            // SEED PRODUCTRECIPES (Recetas de productos)
            // ============================================================
            if (!ctx.ProductRecipes.Any())
            {
                var hamburguesaClasica = ctx.Products.FirstOrDefault(p => p.Code == "H001");
                var hamburguesaQueso = ctx.Products.FirstOrDefault(p => p.Code == "H002");
                var hamburguesaCompleta = ctx.Products.FirstOrDefault(p => p.Code == "H003");
                var hamburguesaBacon = ctx.Products.FirstOrDefault(p => p.Code == "H004");

                var carnePicada = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-001");
                var lechuga = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-002");
                var tomate = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-003");
                var pan = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-004");
                var queso = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-005");
                var cebolla = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-006");
                var panceta = ctx.RawMaterials.FirstOrDefault(rm => rm.Code == "ING-007");

                // RECETA: Hamburguesa Clásica
                if (hamburguesaClasica != null && carnePicada != null && lechuga != null &&
                    tomate != null && pan != null && cebolla != null)
                {
                    ctx.ProductRecipes.AddRange(
                        new ProductRecipe
                        {
                            ProductId = hamburguesaClasica.Id,
                            RawMaterialId = carnePicada.Id,
                            Quantity = 0.150m  // 150 gramos de carne
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaClasica.Id,
                            RawMaterialId = lechuga.Id,
                            Quantity = 0.020m  // 20 gramos de lechuga
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaClasica.Id,
                            RawMaterialId = tomate.Id,
                            Quantity = 0.030m  // 30 gramos de tomate
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaClasica.Id,
                            RawMaterialId = pan.Id,
                            Quantity = 1.00m   // 1 pan
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaClasica.Id,
                            RawMaterialId = cebolla.Id,
                            Quantity = 0.015m  // 15 gramos de cebolla
                        }
                    );
                }

                // RECETA: Hamburguesa con Queso
                if (hamburguesaQueso != null && carnePicada != null && lechuga != null &&
                    tomate != null && pan != null && queso != null)
                {
                    ctx.ProductRecipes.AddRange(
                        new ProductRecipe
                        {
                            ProductId = hamburguesaQueso.Id,
                            RawMaterialId = carnePicada.Id,
                            Quantity = 0.150m  // 150 gramos de carne
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaQueso.Id,
                            RawMaterialId = lechuga.Id,
                            Quantity = 0.020m  // 20 gramos de lechuga
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaQueso.Id,
                            RawMaterialId = tomate.Id,
                            Quantity = 0.030m  // 30 gramos de tomate
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaQueso.Id,
                            RawMaterialId = pan.Id,
                            Quantity = 1.00m   // 1 pan
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaQueso.Id,
                            RawMaterialId = queso.Id,
                            Quantity = 0.030m  // 30 gramos de queso
                        }
                    );
                }

                // RECETA: Hamburguesa Completa
                if (hamburguesaCompleta != null && carnePicada != null && lechuga != null &&
                    tomate != null && pan != null && queso != null && panceta != null)
                {
                    ctx.ProductRecipes.AddRange(
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = carnePicada.Id,
                            Quantity = 0.300m  // 300 gramos de carne (doble)
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = lechuga.Id,
                            Quantity = 0.025m  // 25 gramos de lechuga
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = tomate.Id,
                            Quantity = 0.040m  // 40 gramos de tomate
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = pan.Id,
                            Quantity = 1.00m   // 1 pan
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = queso.Id,
                            Quantity = 0.040m  // 40 gramos de queso
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaCompleta.Id,
                            RawMaterialId = panceta.Id,
                            Quantity = 0.050m  // 50 gramos de panceta
                        }
                    );
                }

                // RECETA: Hamburguesa Bacon
                if (hamburguesaBacon != null && carnePicada != null && pan != null &&
                    queso != null && cebolla != null && panceta != null)
                {
                    ctx.ProductRecipes.AddRange(
                        new ProductRecipe
                        {
                            ProductId = hamburguesaBacon.Id,
                            RawMaterialId = carnePicada.Id,
                            Quantity = 0.150m  // 150 gramos de carne
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaBacon.Id,
                            RawMaterialId = pan.Id,
                            Quantity = 1.00m   // 1 pan
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaBacon.Id,
                            RawMaterialId = queso.Id,
                            Quantity = 0.030m  // 30 gramos de queso
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaBacon.Id,
                            RawMaterialId = cebolla.Id,
                            Quantity = 0.050m  // 50 gramos de cebolla caramelizada
                        },
                        new ProductRecipe
                        {
                            ProductId = hamburguesaBacon.Id,
                            RawMaterialId = panceta.Id,
                            Quantity = 0.060m  // 60 gramos de panceta
                        }
                    );
                }

                ctx.SaveChanges();
            }

            // Seed Combos (DESPUÉS de tener productos)
            if (!ctx.Combos.Any())
            {
                var combosCat = ctx.Categories.FirstOrDefault(c => c.Name == "Combos");
                if (combosCat == null)
                {
                    combosCat = new Category { Name = "Combos", IsActive = true };
                    ctx.Categories.Add(combosCat);
                    ctx.SaveChanges();
                }

                // Obtener productos para los combos
                var hamburguesaClasica = ctx.Products.FirstOrDefault(p => p.Code == "H001");
                var hamburguesaQueso = ctx.Products.FirstOrDefault(p => p.Code == "H002");
                var papasChicas = ctx.Products.FirstOrDefault(p => p.Code == "A001");
                var papasGrandes = ctx.Products.FirstOrDefault(p => p.Code == "A002");
                var coca = ctx.Products.FirstOrDefault(p => p.Code == "B001");
                var agua = ctx.Products.FirstOrDefault(p => p.Code == "B002");

                if (hamburguesaClasica != null && papasChicas != null && coca != null)
                {
                    // COMBO 1: Combo Clásico
                    var comboClasico = new Combo
                    {
                        Name = "Combo Clásico",
                        Description = "Hamburguesa Clásica + Papas Chicas + Bebida a elección",
                        Price = 7500m,
                        IsActive = true,
                        CategoryId = combosCat.Id
                    };
                    ctx.Combos.Add(comboClasico);
                    ctx.SaveChanges();

                    var comboItem1 = new ComboItem
                    {
                        ComboId = comboClasico.Id,
                        ProductId = hamburguesaClasica.Id,
                        Quantity = 1,
                        AllowsSubstitution = false
                    };
                    ctx.ComboItems.Add(comboItem1);

                    var comboItem2 = new ComboItem
                    {
                        ComboId = comboClasico.Id,
                        ProductId = papasChicas.Id,
                        Quantity = 1,
                        AllowsSubstitution = false
                    };
                    ctx.ComboItems.Add(comboItem2);

                    var comboItem3 = new ComboItem
                    {
                        ComboId = comboClasico.Id,
                        ProductId = coca.Id,
                        Quantity = 1,
                        AllowsSubstitution = true,
                        SubstitutionGroup = "Bebida"
                    };
                    ctx.ComboItems.Add(comboItem3);
                    ctx.SaveChanges();

                    if (agua != null)
                    {
                        ctx.ComboSubstitutionOptions.Add(new ComboSubstitutionOption
                        {
                            ComboItemId = comboItem3.Id,
                            AlternativeProductId = agua.Id
                        });
                    }
                    ctx.SaveChanges();
                }

                if (hamburguesaQueso != null && papasGrandes != null && coca != null)
                {
                    // COMBO 2: Combo Grande
                    var comboGrande = new Combo
                    {
                        Name = "Combo Grande",
                        Description = "Hamburguesa con Queso + Papas Grandes + Bebida a elección",
                        Price = 9000m,
                        IsActive = true,
                        CategoryId = combosCat.Id
                    };
                    ctx.Combos.Add(comboGrande);
                    ctx.SaveChanges();

                    ctx.ComboItems.Add(new ComboItem
                    {
                        ComboId = comboGrande.Id,
                        ProductId = hamburguesaQueso.Id,
                        Quantity = 1,
                        AllowsSubstitution = false
                    });

                    ctx.ComboItems.Add(new ComboItem
                    {
                        ComboId = comboGrande.Id,
                        ProductId = papasGrandes.Id,
                        Quantity = 1,
                        AllowsSubstitution = false
                    });

                    var comboItemBebida = new ComboItem
                    {
                        ComboId = comboGrande.Id,
                        ProductId = coca.Id,
                        Quantity = 1,
                        AllowsSubstitution = true,
                        SubstitutionGroup = "Bebida"
                    };
                    ctx.ComboItems.Add(comboItemBebida);
                    ctx.SaveChanges();

                    if (agua != null)
                    {
                        ctx.ComboSubstitutionOptions.Add(new ComboSubstitutionOption
                        {
                            ComboItemId = comboItemBebida.Id,
                            AlternativeProductId = agua.Id
                        });
                        ctx.SaveChanges();
                    }
                }
            }

            // Seed ProductPriceList
            if (!ctx.ProductPriceLists.Any())
            {
                var listaGeneral = ctx.PriceLists.First(pl => pl.Name == "Lista General");
                var delivery = ctx.PriceLists.First(pl => pl.Name == "Delivery");
                var salon = ctx.PriceLists.First(pl => pl.Name == "Salón");
                var productos = ctx.Products.ToList();

                // Precios para cada producto
                var preciosProductos = new Dictionary<string, (decimal costo, decimal precio)>
                {
                    // Hamburguesas
                    { "H001", (3000m, 5500m) },
                    { "H002", (3500m, 6500m) },
                    { "H003", (5000m, 9500m) },
                    { "H004", (4500m, 8500m) },
                    // Pizzas
                    { "P001", (4000m, 7500m) },
                    { "P002", (4500m, 8500m) },
                    // Papas
                    { "A001", (800m, 2000m) },
                    { "A002", (1200m, 3000m) },
                    { "A003", (1500m, 3500m) },
                    // Bebidas
                    { "B001", (400m, 1500m) },
                    { "B002", (300m, 1200m) },
                    { "B003", (800m, 2500m) },
                    // Postres
                    { "D001", (1000m, 2500m) },
                    { "D002", (900m, 2200m) },
                    // Salsas (gratis o bajo costo)
                    { "S001", (50m, 0m) },    // Ketchup gratis
                    { "S002", (50m, 0m) },    // Mayonesa gratis
                    { "S003", (50m, 0m) }     // Mostaza gratis
                };

                foreach (var producto in productos)
                {
                    if (preciosProductos.TryGetValue(producto.Code, out var precios))
                    {
                        var (costoBase, precioBase) = precios;

                        // Lista General
                        ctx.ProductPriceLists.Add(new ProductPriceList
                        {
                            ProductId = producto.Id,
                            PriceListId = listaGeneral.Id,
                            CostPrice = costoBase,
                            SalePrice = precioBase,
                            ProfitPercentage = precioBase > 0 ? ((precioBase - costoBase) / costoBase) * 100 : 0,
                            InternalTaxPercentage = 0,
                            IVA = "21%",
                            LastUpdate = DateTime.Now
                        });

                        // Delivery - 15% más caro
                        var precioDelivery = precioBase * 1.15m;
                        ctx.ProductPriceLists.Add(new ProductPriceList
                        {
                            ProductId = producto.Id,
                            PriceListId = delivery.Id,
                            CostPrice = costoBase,
                            SalePrice = Math.Round(precioDelivery, 0),
                            ProfitPercentage = precioDelivery > 0 ? ((precioDelivery - costoBase) / costoBase) * 100 : 0,
                            InternalTaxPercentage = 0,
                            IVA = "21%",
                            LastUpdate = DateTime.Now
                        });

                        // Salón - mismo precio
                        ctx.ProductPriceLists.Add(new ProductPriceList
                        {
                            ProductId = producto.Id,
                            PriceListId = salon.Id,
                            CostPrice = costoBase,
                            SalePrice = precioBase,
                            ProfitPercentage = precioBase > 0 ? ((precioBase - costoBase) / costoBase) * 100 : 0,
                            InternalTaxPercentage = 0,
                            IVA = "21%",
                            LastUpdate = DateTime.Now
                        });
                    }
                }
                ctx.SaveChanges();
            }

            // Seed ProductSuppliers
            if (!ctx.ProductSuppliers.Any())
            {
                var productosConStock = ctx.Products.Where(p => p.TracksStock).Take(8).ToList();
                var proveedores = ctx.Suppliers.ToList();

                for (int i = 0; i < productosConStock.Count; i++)
                {
                    var producto = productosConStock[i];
                    var proveedor = proveedores[i % proveedores.Count];

                    var precioLista = ctx.ProductPriceLists
                        .FirstOrDefault(ppl => ppl.ProductId == producto.Id && ppl.PriceListId == 1);

                    ctx.ProductSuppliers.Add(new ProductSupplier
                    {
                        ProductId = producto.Id,
                        SupplierId = proveedor.Id,
                        Cost = precioLista?.CostPrice ?? 1000m,
                        IsDefault = true,
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