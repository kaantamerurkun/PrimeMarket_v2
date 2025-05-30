﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PrimeMarket.Models.Enum;
using PrimeMarket.Models.Products;
using Monitor = PrimeMarket.Models.Products.Monitor;

namespace PrimeMarket.Models
{
    public class Listing : BaseEntity
    {
        [Required]
        public int SellerId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Condition { get; set; } 

        public int? Stock { get; set; }

        [Required]
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public string DetailCategory { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public ListingStatus Status { get; set; } = ListingStatus.Pending;

        public string? RejectionReason { get; set; }
        public int? ViewCount { get; set; } = 0;

        [ForeignKey("SellerId")]
        public virtual User Seller { get; set; }
        public virtual ICollection<ListingImage> Images { get; set; }
        public virtual ICollection<Bookmark> Bookmarks { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
        public virtual ICollection<Offer> Offers { get; set; }
        public virtual ICollection<Purchase> Purchases { get; set; }
        public virtual ICollection<ProductReview> Reviews { get; set; }

        public virtual BaseProduct Product { get; set; }

        public virtual IOSPhone IOSPhone { get; set; }
        public virtual AndroidPhone AndroidPhone { get; set; }
        public virtual OtherPhone OtherPhone { get; set; }

        public virtual IOSTablet IOSTablet { get; set; }
        public virtual AndroidTablet AndroidTablet { get; set; }
        public virtual OtherTablet OtherTablet { get; set; }

        public virtual Laptop Laptop { get; set; }
        public virtual Desktop Desktop { get; set; }
        public virtual ComputerComponent ComputerComponent { get; set; }
        public virtual Monitor Monitor { get; set; }
        public virtual Keyboard Keyboard { get; set; }


        public virtual Washer Washer { get; set; }
        public virtual Dishwasher Dishwasher { get; set; }
        public virtual Fridge Fridge { get; set; }
        public virtual Oven Oven { get; set; }


        public virtual VacuumCleaner VacuumCleaner { get; set; }
        public virtual MicrowaveOven MicrowaveOven { get; set; }
        public virtual Stove Stove { get; set; }
        public virtual BeveragePreparation BeveragePreparation { get; set; }
        public virtual FoodPreparation FoodPreparation { get; set; }
        public virtual Iron Iron { get; set; }
        public virtual SewingMachine SewingMachine { get; set; }


        public virtual Television Television { get; set; }

        public virtual PhoneAccessory PhoneAccessory { get; set; }
        public virtual TabletAccessory TabletAccessory { get; set; }
        public virtual ComputerAccessory ComputerAccessory { get; set; }
        public virtual HeadphoneEarphone HeadphoneEarphone { get; set; }
        public virtual Speaker Speaker { get; set; }
        public virtual Webcam Webcam { get; set; }
        public virtual Microphone Microphone { get; set; }
        public virtual Mouse Mouse { get; set; }
        public virtual ComputerBag ComputerBag { get; set; }
        public virtual SparePart SparePart { get; set; }
        public virtual HeatingCooling HeatingCooling { get; set; }
        public virtual Camera Camera { get; set; }

        public virtual Other Other { get; set; }
    }
}