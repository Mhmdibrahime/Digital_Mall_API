using Digital_Mall_API.Models.Entities.User___Authentication;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Digital_Mall_API.Models.Entities.Financials;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Digital_Mall_API.Models.Entities.Promotions;
using Digital_Mall_API.Models.Entities.PlatformSettings;

namespace Digital_Mall_API.Models.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<FashionModel> FashionModels { get; set; }
        public DbSet<TshirtDesigner> TshirtDesigners { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<Reel> Reels { get; set; }
        public DbSet<ReelProduct> ReelProducts { get; set; }
        public DbSet<TshirtDesignOrder> TshirtDesignOrders { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<GlobalCommission> GlobalCommission { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<ProductDiscount> ProductDiscounts { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }
        public DbSet<TshirtDesignSubmission> TshirtDesignSubmissions { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<TshirtTemplate> TshirtTemplates { get; set; }
        public DbSet<TShirtSize> TShirtSizes { get; set; }
        public DbSet<TShirtStyle> TShirtStyles { get; set; }
        public DbSet<ReelCommission> ReelCommissions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Brand>().ToTable("Brands");
            modelBuilder.Entity<FashionModel>().ToTable("FashionModels");
            modelBuilder.Entity<TshirtDesigner>().ToTable("TshirtDesigners");

           

            modelBuilder.Entity<ReelProduct>()
                .HasKey(rp => new { rp.ReelId, rp.ProductId });

            modelBuilder.Entity<ReelProduct>()
                .HasOne(rp => rp.Reel)
                .WithMany(r => r.LinkedProducts)
                .HasForeignKey(rp => rp.ReelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReelProduct>()
                .HasOne(rp => rp.Product)
                .WithMany(p => p.ReelProducts)
                .HasForeignKey(rp => rp.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OrderItem>()
               .HasOne(oi => oi.Brand)
               .WithMany(b => b.OrderItems)
               .HasForeignKey(oi => oi.BrandId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

           

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.ProductVariant)
                .WithMany(pv => pv.OrderItems)
                .HasForeignKey(oi => oi.ProductVariantId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Reel>()
                   .HasOne(r => r.PostedByBrand)
                   .WithMany(b => b.Reels)
                   .HasForeignKey(r => r.PostedByUserId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_Reels_Brands_PostedByUserId");

            modelBuilder.Entity<Reel>()
                .HasOne(r => r.PostedByFashionModel)
                .WithMany(f => f.Reels)
                .HasForeignKey(r => r.PostedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Reels_FashionModels_PostedByUserId");

            modelBuilder.Entity<TshirtDesignOrder>()
                .HasOne(t => t.CustomerUser)
                .WithMany()
                .HasForeignKey(t => t.CustomerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Payout>()
                .HasOne(p => p.PayeeUser)
                .WithMany()
                .HasForeignKey(p => p.PayeeUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId);
        }
    }
}