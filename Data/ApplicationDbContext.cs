using Microsoft.EntityFrameworkCore;
using PrimeMarket.Models;
using PrimeMarket.Models.Products;
using System.Collections.Generic;
using System.Reflection.Emit;
using Monitor = PrimeMarket.Models.Products.Monitor;

namespace PrimeMarket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<VerificationDocument> VerificationDocuments { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseConfirmation> PurchaseConfirmations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<SellerRating> SellerRatings { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminAction> AdminActions { get; set; }

        public DbSet<IOSPhone> IOSPhones { get; set; }
        public DbSet<AndroidPhone> AndroidPhones { get; set; }
        public DbSet<OtherPhone> OtherPhones { get; set; }
        public DbSet<IOSTablet> IOSTablets { get; set; }
        public DbSet<AndroidTablet> AndroidTablets { get; set; }
        public DbSet<OtherTablet> OtherTablets { get; set; }
        public DbSet<Laptop> Laptops { get; set; }
        public DbSet<Desktop> Desktops { get; set; }
        public DbSet<Washer> Washers { get; set; }
        public DbSet<Dishwasher> Dishwashers { get; set; }
        public DbSet<Fridge> Fridges { get; set; }
        public DbSet<Oven> Ovens { get; set; }
        public DbSet<VacuumCleaner> VacuumCleaners { get; set; }
        public DbSet<Television> Televisions { get; set; }
        public DbSet<PhoneAccessory> PhoneAccessorys { get; set; }
        public DbSet<TabletAccessory> TabletAccessorys { get; set; }
        public DbSet<ComputerAccessory> ComputerAccessorys { get; set; }
        public DbSet<ComputerComponent> ComputerComponents { get; set; }
        public DbSet<Monitor> Monitors { get; set; }
        public DbSet<Stove> Stoves { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }

        public DbSet<MicrowaveOven> MicrowaveOvens { get; set; }
        public DbSet<BeveragePreparation> BeveragePreparations { get; set; }
        public DbSet<FoodPreparation> FoodPreparations { get; set; }
        public DbSet<Iron> Irons { get; set; }
        public DbSet<SewingMachine> SewingMachines { get; set; }
        public DbSet<Keyboard> Keyboards { get; set; }
        public DbSet<Speaker> Speakers { get; set; }
        public DbSet<HeadphoneEarphone> HeadphoneEarphones { get; set; }
        public DbSet<HeatingCooling> HeatingCoolings { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Other> Others { get; set; }

        public DbSet<Webcam> Webcams { get; set; }
        public DbSet<Microphone> Microphones { get; set; }
        public DbSet<Mouse> Mouses { get; set; }
        public DbSet<ComputerBag> ComputerBags { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<User>()
                .HasMany(u => u.Listings)
                .WithOne(l => l.Seller)
                .HasForeignKey(l => l.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SentMessages)
                .WithOne(m => m.Sender)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReceivedMessages)
                .WithOne(m => m.Receiver)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Bookmarks)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Purchases)
                .WithOne(p => p.Buyer)
                .HasForeignKey(p => p.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RatingsGiven)
                .WithOne(r => r.Rater)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RatingsReceived)
                .WithOne(r => r.RatedUser)
                .HasForeignKey(r => r.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.VerificationDocument)
                .WithOne(v => v.User)
                .HasForeignKey<VerificationDocument>(v => v.UserId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Images)
                .WithOne(i => i.Listing)
                .HasForeignKey(i => i.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Bookmarks)
                .WithOne(b => b.Listing)
                .HasForeignKey(b => b.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Messages)
                .WithOne(m => m.Listing)
                .HasForeignKey(m => m.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Offers)
                .WithOne(o => o.Listing)
                .HasForeignKey(o => o.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Purchases)
                .WithOne(p => p.Listing)
                .HasForeignKey(p => p.ListingId);


            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Bookmark>()
                .HasIndex(b => new { b.UserId, b.ListingId })
                .IsUnique();

            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Confirmation)
                .WithOne(c => c.Purchase)
                .HasForeignKey<PurchaseConfirmation>(c => c.PurchaseId);

            modelBuilder.Entity<Offer>()
                .HasOne(o => o.Messages)
                .WithOne()
                .HasForeignKey<Offer>(o => o.MessageId)
                .IsRequired(false);

            modelBuilder.Entity<Offer>()
                .HasOne(o => o.OriginalOffer)
                .WithMany(o => o.CounterOffers)
                .HasForeignKey(o => o.OriginalOfferId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRating>()
                .HasIndex(r => new { r.RaterId, r.RatedUserId })
                .IsUnique();

            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Buyer)
                .WithMany(u => u.RatingsGivenAsBuyer)
                .HasForeignKey(sr => sr.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Seller)
                .WithMany(u => u.RatingsReceivedAsSeller)
                .HasForeignKey(sr => sr.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SellerRating>()
                .HasOne(sr => sr.Purchase)
                .WithMany()
                .HasForeignKey(sr => sr.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerRating>()
                .HasIndex(sr => new { sr.BuyerId, sr.SellerId, sr.PurchaseId })
                .IsUnique();


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.IOSPhone)
                .WithOne(p => p.Listing)
                .HasForeignKey<IOSPhone>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.AndroidPhone)
                .WithOne(p => p.Listing)
                .HasForeignKey<AndroidPhone>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.OtherPhone)
                .WithOne(p => p.Listing)
                .HasForeignKey<OtherPhone>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.IOSTablet)
                .WithOne(p => p.Listing)
                .HasForeignKey<IOSTablet>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.AndroidTablet)
                .WithOne(p => p.Listing)
                .HasForeignKey<AndroidTablet>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.OtherTablet)
                .WithOne(p => p.Listing)
                .HasForeignKey<OtherTablet>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Laptop)
                .WithOne(p => p.Listing)
                .HasForeignKey<Laptop>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Desktop)
                .WithOne(p => p.Listing)
                .HasForeignKey<Desktop>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Washer)
                .WithOne(p => p.Listing)
                .HasForeignKey<Washer>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Dishwasher)
                .WithOne(p => p.Listing)
                .HasForeignKey<Dishwasher>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Fridge)
                .WithOne(p => p.Listing)
                .HasForeignKey<Fridge>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Oven)
                .WithOne(p => p.Listing)
                .HasForeignKey<Oven>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.VacuumCleaner)
                .WithOne(p => p.Listing)
                .HasForeignKey<VacuumCleaner>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Television)
                .WithOne(p => p.Listing)
                .HasForeignKey<Television>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.PhoneAccessory)
                .WithOne(p => p.Listing)
                .HasForeignKey<PhoneAccessory>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.TabletAccessory)
                .WithOne(p => p.Listing)
                .HasForeignKey<TabletAccessory>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.ComputerAccessory)
                .WithOne(p => p.Listing)
                .HasForeignKey<ComputerAccessory>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.ComputerComponent)
                .WithOne(p => p.Listing)
                .HasForeignKey<ComputerComponent>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Monitor)
                .WithOne(p => p.Listing)
                .HasForeignKey<Monitor>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Stove)
                .WithOne(p => p.Listing)
                .HasForeignKey<Stove>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.MicrowaveOven)
                .WithOne(p => p.Listing)
                .HasForeignKey<MicrowaveOven>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.BeveragePreparation)
                .WithOne(p => p.Listing)
                .HasForeignKey<BeveragePreparation>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.FoodPreparation)
                .WithOne(p => p.Listing)
                .HasForeignKey<FoodPreparation>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Iron)
                .WithOne(p => p.Listing)
                .HasForeignKey<Iron>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.SewingMachine)
                .WithOne(p => p.Listing)
                .HasForeignKey<SewingMachine>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Keyboard)
                .WithOne(p => p.Listing)
                .HasForeignKey<Keyboard>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Speaker)
                .WithOne(p => p.Listing)
                .HasForeignKey<Speaker>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.HeadphoneEarphone)
                .WithOne(p => p.Listing)
                .HasForeignKey<HeadphoneEarphone>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Webcam)
                .WithOne(p => p.Listing)
                .HasForeignKey<Webcam>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Microphone)
                .WithOne(p => p.Listing)
                .HasForeignKey<Microphone>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Mouse)
                .WithOne(p => p.Listing)
                .HasForeignKey<Mouse>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.ComputerBag)
                .WithOne(p => p.Listing)
                .HasForeignKey<ComputerBag>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.SparePart)
                .WithOne(p => p.Listing)
                .HasForeignKey<SparePart>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.HeatingCooling)
                .WithOne(p => p.Listing)
                .HasForeignKey<HeatingCooling>(p => p.ListingId);


            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Camera)
                .WithOne(p => p.Listing)
                .HasForeignKey<Camera>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Other)
                .WithOne(p => p.Listing)
                .HasForeignKey<Other>(p => p.ListingId);


            modelBuilder.Entity<ProductReview>()
                .HasIndex(pr => new { pr.UserId, pr.ListingId })
                .IsUnique();

            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.ProductReviews)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict); 
            
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Listing)
                .WithMany(l => l.Reviews)
                .HasForeignKey(pr => pr.ListingId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<Offer>()
                .HasOne(o => o.Buyer)
                .WithMany(u => u.Offers)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Username)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .HasMany(a => a.AdminActions)
                .WithOne(aa => aa.Admin)
                .HasForeignKey(aa => aa.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}