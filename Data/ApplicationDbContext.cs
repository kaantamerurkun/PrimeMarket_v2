using Microsoft.EntityFrameworkCore;
using PrimeMarket.Models;
using PrimeMarket.Models.Products;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PrimeMarket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Main entities
        public DbSet<User> Users { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<VerificationDocument> VerificationDocuments { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminAction> AdminActions { get; set; }

        // Product entities
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
        public DbSet<PhoneAccessory> PhoneAccessories { get; set; }
        public DbSet<TabletAccessory> TabletAccessories { get; set; }
        public DbSet<ComputerAccessory> ComputerAccessories { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships

            // User relationships
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

            // Listing relationships
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
                .HasOne(l => l.Purchase)
                .WithOne(p => p.Listing)
                .HasForeignKey<Purchase>(p => p.ListingId);

            // Configure unique constraints
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


            modelBuilder.Entity<UserRating>()
                .HasIndex(r => new { r.RaterId, r.RatedUserId })
                .IsUnique();

            // Configure one-to-one relationships between Listing and product types
            // This ensures that a Listing can only have one specific product type

            // Phone product types
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

            // Tablet product types
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

            // Computer product types
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Laptop)
                .WithOne(p => p.Listing)
                .HasForeignKey<Laptop>(p => p.ListingId);

            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Desktop)
                .WithOne(p => p.Listing)
                .HasForeignKey<Desktop>(p => p.ListingId);

            // White Goods product types
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

            // Electrical Domestic Appliances product types
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.VacuumCleaner)
                .WithOne(p => p.Listing)
                .HasForeignKey<VacuumCleaner>(p => p.ListingId);

            // Television product types
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Television)
                .WithOne(p => p.Listing)
                .HasForeignKey<Television>(p => p.ListingId);

            // Accessories product types
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

            modelBuilder.Entity<Offer>()
    .HasOne(o => o.Buyer)
    .WithMany(u => u.Offers) // You'll need to add this navigation property to User
    .HasForeignKey(o => o.BuyerId)
    .OnDelete(DeleteBehavior.NoAction); // Change to NoAction instead of Cascade

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