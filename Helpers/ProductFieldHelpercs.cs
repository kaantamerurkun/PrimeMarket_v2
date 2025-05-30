using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using PrimeMarket.Models.Products;

namespace PrimeMarket.Helpers
{
    public static class ProductFieldHelper
    {
        public static List<ProductField> GetProductFields(string category, string subcategory, string detailCategory)
        {
            var productType = GetProductType(category, subcategory, detailCategory);
            if (productType == null) return new List<ProductField>();

            var fields = new List<ProductField>();
            var properties = productType.GetProperties();

            foreach (var property in properties)
            {
                if (property.Name == "Id" || property.Name == "ListingId" || property.Name == "Listing")
                    continue;

                var field = new ProductField
                {
                    Name = property.Name,
                    Label = GetDisplayName(property),
                    Type = GetInputType(property),
                    Placeholder = GetPlaceholder(property),
                    Options = GetSelectOptions(property),
                    IsRequired = IsRequired(property)
                };

                fields.Add(field);
            }

            return fields;
        }

        private static Type GetProductType(string category, string subcategory, string detailCategory)
        {
            if (string.Equals(category, "Others", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Other", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(Other);
            }

            if (string.Equals(category, "Phone", StringComparison.OrdinalIgnoreCase))
            {
                return subcategory?.ToLower() switch
                {
                    "ios phone" or "ios phones" or "ıos phone" or "ıos phones" => typeof(IOSPhone),
                    "android phone" or "android phones" => typeof(AndroidPhone),
                    "other phone" or "other phones" => typeof(OtherPhone),
                    "phone accessories" or "phone accessory" => typeof(PhoneAccessory),
                    "spare parts" or "spare part" => typeof(SparePart),
                    _ => null
                };
            }

            if (string.Equals(category, "Tablets", StringComparison.OrdinalIgnoreCase))
            {
                return subcategory?.ToLower() switch
                {
                    "ios tablet" or "ios tablets" or "ıos tablet" or "ıos tablets" => typeof(IOSTablet),
                    "android tablet" or "android tablets" => typeof(AndroidTablet),
                    "other tablet" or "other tablets" => typeof(OtherTablet),
                    "tablet accessories" or "tablet accessory" => typeof(TabletAccessory),
                    _ => null
                };
            }

            if (string.Equals(category, "Electronics", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subcategory, "Computer", StringComparison.OrdinalIgnoreCase))
                {
                    return detailCategory?.ToLower() switch
                    {
                        "laptop" or "laptops" => typeof(Laptop),
                        "desktop" or "desktops" => typeof(Desktop),
                        "computer accessory" or "computer accessories" => typeof(ComputerAccessory),
                        "computer component" or "computer components" => typeof(ComputerComponent),
                        "monitor" or "monitors" => typeof(Models.Products.Monitor),
                        _ => null
                    };
                }
                else if (string.Equals(subcategory, "White Goods", StringComparison.OrdinalIgnoreCase))
                {
                    return detailCategory?.ToLower() switch
                    {
                        "fridge" or "fridges" => typeof(Fridge),
                        "washer" or "washers" => typeof(Washer),
                        "dishwasher" or "dishwashers" => typeof(Dishwasher),
                        "stove" or "stoves" => typeof(Stove),
                        "oven" or "ovens" => typeof(Oven),
                        "microwave oven" or "microwave ovens" => typeof(MicrowaveOven),
                        _ => null
                    };
                }
                else if (string.Equals(subcategory, "Electrical Domestic Appliances", StringComparison.OrdinalIgnoreCase))
                {
                    return detailCategory?.ToLower() switch
                    {
                        "vacuum cleaner" or "vacuum cleaners" => typeof(VacuumCleaner),
                        "beverage preparation" or "beverage preparations" => typeof(BeveragePreparation),
                        "food preparation" or "food preparations" => typeof(FoodPreparation),
                        "iron" or "irons" => typeof(Iron),
                        "sewing machine" or "sewing machines" => typeof(SewingMachine),
                        _ => null
                    };
                }
                else if (string.Equals(subcategory, "Televisions", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(Television);
                }
                else if (string.Equals(subcategory, "Heating & Cooling", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(HeatingCooling);
                }
                else if (string.Equals(subcategory, "Cameras", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(Camera);
                }
                else if (string.Equals(subcategory, "Computer Accessories", StringComparison.OrdinalIgnoreCase))
                {
                    return detailCategory?.ToLower() switch
                    {
                        "keyboard" or "keyboards" => typeof(Keyboard),
                        "speaker" or "speakers" => typeof(Speaker),
                        "headphones & earphones" or "headphone & earphone" => typeof(HeadphoneEarphone),
                        "webcam" or "webcams" => typeof(Webcam),
                        "microphone" or "microphones" => typeof(Microphone),
                        "mouse" or "mouses" => typeof(Mouse),
                        "computer bag" or "computer bags" => typeof(ComputerBag),
                        _ => null
                    };
                }
            }

            return null;
        }

        private static string GetDisplayName(PropertyInfo property)
        {
            var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? SplitCamelCase(property.Name);
        }

        private static string GetInputType(PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                return "select"; 
            }
            else if (IsNumericType(property.PropertyType))
            {
                return "number";
            }
            else
            {
                return "text";
            }
        }

        private static bool IsNumericType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType == typeof(int) ||
                   underlyingType == typeof(decimal) ||
                   underlyingType == typeof(double) ||
                   underlyingType == typeof(float) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(short);
        }

        private static string GetPlaceholder(PropertyInfo property)
        {
            string name = property.Name.ToLower();

            var placeholderMap = new Dictionary<string, string>
            {
                { "ram", "e.g. 16GB" },
                { "ramtype", "e.g. DDR4" },
                { "storage", "e.g. 512GB SSD" },
                { "processor", "e.g. Intel i7-12700H" },
                { "gpu", "e.g. NVIDIA GTX 1650" },
                { "graphics", "e.g. NVIDIA GTX 1650" },
                { "screensize", "e.g. 15.6 inch" },
                { "battery", "e.g. 5000mAh" },
                { "batterypower", "e.g. 5000mAh" },
                { "camera", "e.g. 12MP triple camera" },
                { "warranty", "e.g. 2 years" },
                { "color", "e.g. Black, White, Silver" },
                { "weight", "e.g. 2.5 kg" },
                { "power", "e.g. 900W" },
                { "energy", "e.g. A++" },
                { "energyclass", "e.g. A++" },
                { "capacity", "e.g. 8 kg" },
                { "volume", "e.g. 350L" },
                { "dimensions", "e.g. 60x85x60 cm" },
                { "resolution", "e.g. 4K UHD" },
                { "port", "e.g. USB-C, Lightning" },
                { "chargingport", "e.g. USB-C, Lightning" },
                { "operatingsystem", "e.g. Windows 11, macOS" },
                { "keyboard", "e.g. Backlit, Mechanical" },
                { "memoryspeed", "e.g. 3200MHz" },
                { "gpumemory", "e.g. 4GB" },
                { "hdmi", "HDMI support" },
                { "displaytechnology", "e.g. OLED, LCD" },
                { "cablelength", "e.g. 7m" },
                { "dustcontainer", "e.g. 2L" }
            };

            foreach (var mapping in placeholderMap)
            {
                if (name.Contains(mapping.Key))
                {
                    return mapping.Value;
                }
            }

            return $"Enter {GetDisplayName(property)}";
        }

        private static List<string> GetSelectOptions(PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                return new List<string> { "Yes", "No" };
            }

            string name = property.Name.ToLower();

            if (name.Contains("energy"))
                return new List<string> { "A+++", "A++", "A+", "A", "B", "C", "D" };
            else if (name.Contains("color"))
                return new List<string> { "Black", "White", "Silver", "Gray", "Blue", "Red", "Gold", "Other" };
            else if (name.Contains("operatingsystem") || name.Contains("os"))
                return new List<string> { "Windows 11", "Windows 10", "macOS", "Linux", "Android", "iOS", "Other" };
            else if (name.Contains("chargingport") || name.Contains("port"))
                return new List<string> { "USB-C", "Lightning", "Micro USB", "USB-A", "Other" };

            return null; 
        }

        private static bool IsRequired(PropertyInfo property)
        {
            return property.GetCustomAttribute<RequiredAttribute>() != null;
        }

        private static string SplitCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && char.IsUpper(input[i]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }
            return result.ToString();
        }
    }

    public class ProductField
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public string Placeholder { get; set; }
        public List<string> Options { get; set; }
        public bool IsRequired { get; set; }
    }
}