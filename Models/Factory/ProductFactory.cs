using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using PrimeMarket.Models.Products;
using Monitor = PrimeMarket.Models.Products.Monitor;

namespace PrimeMarket.Models.Factory
{
    public static class ProductFactory
    {
        public static BaseProduct CreateProduct(string category, string subcategory)
        {
            // Normalize subcategory to match with switch cases
            string normalizedSubcategory = subcategory?.Trim();

            // Handle subcategories for Electronics category
            if (category == "Electronics")
            {
                // Check for known subcategories in Electronics
                return normalizedSubcategory switch
                {
                    "Laptops" => new Laptop(),
                    "Desktops" => new Desktop(),
                    "Computer Accessories" => new ComputerAccessory(),
                    "Computer Components" => new ComputerComponent(),
                    "Monitors" => new Monitor(),
                    "Fridges" => new Fridge(),
                    "Washers" => new Washer(),
                    "Dishwashers" => new Dishwasher(),
                    "Ovens" => new Oven(),
                    "Stoves" => new Stove(),
                    "Microwave Ovens" => new MicrowaveOven(),
                    "Vacuum Cleaners" => new VacuumCleaner(),
                    "Beverage Preparations" => new BeveragePreparation(),
                    "Food Preparations" => new FoodPreparation(),
                    "Irons" => new Iron(),
                    "Sewing Machines" => new SewingMachine(),
                    "Keyboards" => new Keyboard(),
                    "Speakers" => new Speaker(),
                    "HeadPhones&EarPhones" => new HeadphoneEarphone(),
                    "Webcams" => new Webcam(),
                    "Microphones" => new Microphone(),
                    "Mouses" => new Mouse(),
                    "Computer Bags" => new ComputerBag(),
                    "Televisions" => new Television(),
                    _ => throw new ArgumentException($"Unsupported subcategory in Electronics: {normalizedSubcategory}")
                };
            }

            // Main switch for standard category/subcategory combinations
            return (category, normalizedSubcategory) switch
            {
                ("Phone", "IOS Phone") => new IOSPhone(),
                ("Phone", "Android Phone") => new AndroidPhone(),
                ("Phone", "Other Phones") => new OtherPhone(),
                ("Phone", "Phone Accessories") => new PhoneAccessory(),

                ("Tablets", "IOS Tablets") => new IOSTablet(),
                ("Tablets", "Android Tablets") => new AndroidTablet(),
                ("Tablets", "Other Tablets") => new OtherTablet(),
                ("Tablets", "Tablet Accessories") => new TabletAccessory(),

                // Special case for just "Tablets" (might come from selection)
                ("Tablets", "Tablets") => new OtherTablet(),

                // Handle potential mismatches in categorization
                var (cat, sub) when cat == "Phone" && sub.Contains("Phone") => new OtherPhone(),
                var (cat, sub) when cat == "Tablets" && sub.Contains("Tablet") => new OtherTablet(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Laptop") => new Laptop(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Desktop") => new Desktop(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Fridge") => new Fridge(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Wash") => new Washer(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Dishwasher") => new Dishwasher(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Oven") => new Oven(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Vacuum") => new VacuumCleaner(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("Television") => new Television(),
                var (cat, sub) when cat == "Electronics" && sub.Contains("TV") => new Television(),

                _ => throw new ArgumentException($"Unsupported product type: {category} - {normalizedSubcategory}")
            };
        }
    }
}