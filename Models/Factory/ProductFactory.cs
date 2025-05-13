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
                // Standardize the subcategory names to match DB models
                switch (normalizedSubcategory)
                {
                    case "white goods/ovens":
                    case "Oven":
                    case "Ovens":
                        return new Oven();
                    case "Desktops":
                    case "desktop":
                    case "computer/desktop":
                        return new Desktop();
                    case "tablet/ios":
                    case "IOS Tablets":
                        return new IOSTablet();
                    case "tablet/Android":
                    case "Android Tablets":
                        return new AndroidTablet();
                    case "tablet/OtherTablets":
                    case "Other Tablets":
                        return new OtherTablet();
                    // Check for other Electronics subcategories
                    case "Laptops": return new Laptop();
                    case "Computer Accessories": return new ComputerAccessory();
                    case "Computer Components": return new ComputerComponent();
                    case "Monitors": return new Monitor();
                    case "Fridges": return new Fridge();
                    case "Washers": return new Washer();
                    case "Dishwashers": return new Dishwasher();
                    case "Stoves": return new Stove();
                    case "Microwave Ovens": return new MicrowaveOven();
                    case "Vacuum Cleaners": return new VacuumCleaner();
                    case "Beverage Preparations": return new BeveragePreparation();
                    case "Food Preparations": return new FoodPreparation();
                    case "Irons": return new Iron();
                    case "Sewing Machines": return new SewingMachine();
                    case "Keyboards": return new Keyboard();
                    case "Speakers": return new Speaker();
                    case "HeadPhones&EarPhones": return new HeadphoneEarphone();
                    case "Webcams": return new Webcam();
                    case "Microphones": return new Microphone();
                    case "Mouses": return new Mouse();
                    case "Computer Bags": return new ComputerBag();
                    case "Televisions": return new Television();
                    default:
                        throw new ArgumentException($"Unsupported subcategory in Electronics: {normalizedSubcategory}");
                }
            }

            // Main switch for standard category/subcategory combinations
            switch (normalizedSubcategory)
            {
                case "IOS Phone": return new IOSPhone();
                case "Android Phone": return new AndroidPhone();
                case "Other Phones": return new OtherPhone();
                case "Phone Accessories": return new PhoneAccessory();
                case "IOS Tablets": return new IOSTablet();
                case "Android Tablets": return new AndroidTablet();
                case "Other Tablets": return new OtherTablet();
                case "Tablet Accessories": return new TabletAccessory();
                // Special case for just "Tablets" (might come from selection)
                case "Tablets": return new OtherTablet();
                default:
                    throw new ArgumentException($"Unsupported product type: {category} - {normalizedSubcategory}");
            }
        }
    }
}