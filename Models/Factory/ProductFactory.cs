using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using PrimeMarket.Models.Products;


namespace PrimeMarket.Models.Factory
{
    public static class ProductFactory
    {
        public static BaseProduct CreateProduct(string category, string subcategory)
        {
            return (category, subcategory) switch
            {
                ("Phone", "IOS Phone") => new IOSPhone(),
                ("Phone", "Android Phone") => new AndroidPhone(),
                ("Phone", "Other Phones") => new OtherPhone(),
                ("Phone", "Phone Accessories") => new PhoneAccessory(),

                ("Tablets", "IOS Tablets") => new IOSTablet(),
                ("Tablets", "Android Tablets") => new AndroidTablet(),
                ("Tablets", "Other Tablets") => new OtherTablet(),
                ("Tablets", "Tablet Accessories") => new TabletAccessory(),

                ("Electronics", "Laptops") => new Laptop(),
                ("Electronics", "Desktops") => new Desktop(),
                ("Electronics", "Computer Accessories") => new ComputerAccessory(),

                ("Electronics", "Fridges") => new Fridge(),
                ("Electronics", "Washers") => new Washer(),
                ("Electronics", "Dishwashers") => new Dishwasher(),
                ("Electronics", "Ovens") => new Oven(),

                ("Electronics", "Vacuum Cleaner") => new VacuumCleaner(),

                ("Electronics", "Televisions") => new Television(),

                _ => throw new ArgumentException($"Unsupported product type: {category} - {subcategory}")
            };
        }
    }
}