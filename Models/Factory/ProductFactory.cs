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
        public static BaseProduct CreateProduct(string category, string subcategory, string detailcategory)
        {
            string? normalizedSubcategory = subcategory?.Trim();
            string? normalizedDetailcategory = detailcategory?.Trim();
            if (category == "Other")
            {
                switch (normalizedSubcategory)
                {
                    case "Other":
                    case "Others":
                        return new Other();
                }
            }
            if (category == "Phone")
            {
                normalizedDetailcategory = null;
                switch (normalizedSubcategory)
                {
                    case "IOS Phone":
                    case "IOS Phones":
                        return new IOSPhone();
                    case "Android Phone":
                    case "Android Phones":
                        return new AndroidPhone();
                    case "Other Phones":
                    case "Other Phone":
                        return new OtherPhone();
                    case "Phone Accessories":
                    case "Phone Accessory":
                        return new PhoneAccessory();
                    case "Spare Parts":
                    case "Spare Part":
                        return new SparePart();
                }
            }
            if (category == "Tablets")
            {
                normalizedDetailcategory = null;
                switch (normalizedSubcategory)
                {
                    case "IOS Tablet":
                    case "IOS Tablets":
                        return new IOSTablet();
                    case "Android Tablet":
                    case "Android Tablets":
                        return new AndroidTablet();
                    case "Other Tablets":
                    case "Other Tablet":
                        return new OtherTablet();
                    case "Tablet Accessories":
                    case "Tablet Accessory":
                        return new TabletAccessory();
                }
            }

            if (category == "Electronics")
            {
                if (normalizedSubcategory == "Computer")
                {
                    switch (normalizedDetailcategory)
                    {
                        case "Laptop":
                        case "Laptops":
                            return new Laptop();
                        case "Desktops":
                        case "Desktop":
                            return new Desktop();
                        case "Computer Accessory":
                        case "Computer Accessories":
                            return new ComputerAccessory();
                        case "Computer Components":
                        case "Computer Component":
                            return new ComputerComponent();
                        case "Monitors":
                        case "Monitor":
                            return new Monitor();
                    }
                }
                if (normalizedSubcategory == "White Goods")
                {
                    switch (normalizedDetailcategory)
                    {
                        case "Fridge":
                        case "Fridges":
                            return new Fridge();
                        case "Washers":
                        case "Washer":
                            return new Washer();
                        case "Dishwashers":
                        case "Dishwasher":
                            return new Dishwasher();
                        case "Stoves":
                        case "Stove":
                            return new Stove();
                        case "Ovens":
                        case "Oven":
                            return new Oven();
                        case "Microwave Ovens":
                        case "Microwave Oven":
                            return new MicrowaveOven();
                    }
                }
                if (normalizedSubcategory == "Electrical Domestic Appliances")
                {
                    switch (normalizedDetailcategory)
                    {
                        case "Vacuum Cleaner":
                        case "Vacuum Cleaners":
                            return new VacuumCleaner();
                        case "Beverage Preparation":
                        case "Beverage Preparations":
                            return new BeveragePreparation();
                        case "Food Preparation":
                        case "Food Preparations":
                            return new FoodPreparation();
                        case "Iron":
                        case "Irons":
                            return new Iron();
                        case "Sewing Machine":
                        case "Sewing Machines":
                            return new SewingMachine();
                    }
                }
                if (normalizedSubcategory == "Televisions")
                {
                    normalizedDetailcategory = null;
                    return new Television();
                }
                if (normalizedSubcategory == "Heating & Cooling")
                {
                    normalizedDetailcategory = null;
                    return new HeatingCooling();
                }
                if (normalizedSubcategory == "Cameras")
                {
                    normalizedDetailcategory = null;
                    return new Camera();
                }
                if (normalizedSubcategory == "Computer Accessories")
                {
                    switch (normalizedDetailcategory)
                    {
                        case "Keyboard":
                        case "Keyboards":
                            return new Keyboard();
                        case "Speaker":
                        case "Speakers":
                            return new Speaker();
                        case "Headphones & Earphones":
                        case "Headphones & Earphone":
                            return new HeadphoneEarphone();
                        case "Webcam":
                        case "Webcams":
                            return new Webcam();
                        case "Microphone":
                        case "Microphones":
                            return new Microphone();
                        case "Mouse":
                        case "Mouses":
                            return new Mouse();
                        case "Computer Bag":
                        case "Computer Bags":
                            return new ComputerBag();
                    }
                }
            }

            throw new ArgumentException("Invalid category, subcategory, or detailcategory provided.");
        }
    }
}