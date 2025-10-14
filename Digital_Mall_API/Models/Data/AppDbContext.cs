using Digital_Mall_API.Models.Entities.Financials;
using Digital_Mall_API.Models.Entities.Logs;
using Digital_Mall_API.Models.Entities.Orders___Shopping;
using Digital_Mall_API.Models.Entities.PlatformSettings;
using Digital_Mall_API.Models.Entities.Product_Catalog;
using Digital_Mall_API.Models.Entities.Promotions;
using Digital_Mall_API.Models.Entities.Reels___Content;
using Digital_Mall_API.Models.Entities.T_Shirt_Customization;
using Digital_Mall_API.Models.Entities.User___Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Digital_Mall_API.Models.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<WebhookLog> WebhookLogs { get; set; }

        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<FashionModel> FashionModels { get; set; }
        public DbSet<FollowingBrand> FollowingBrands { get; set; }
        public DbSet<FollowingModel> FollowingModels { get; set; }

        public DbSet<TshirtDesigner> TshirtDesigners { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<BrandStatistics> BrandStatistics { get; set; }
        public DbSet<RefundTransaction> RefundTransactions { get; set; }
        public DbSet<Reel> Reels { get; set; }
        public DbSet<ReelLike> ReelLikes { get; set; }

        public DbSet<ReelProduct> ReelProducts { get; set; }
        public DbSet<TshirtDesignOrder> TshirtDesignOrders { get; set; }
        public DbSet<TshirtDesignOrderImage> TshirtDesignOrderImages { get; set; }
        public DbSet<TshirtOrderText> TshirtOrderTexts { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<GlobalCommission> GlobalCommission { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<ProductDiscount> ProductDiscounts { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }
        public DbSet<TshirtDesignSubmission> TshirtDesignSubmissions { get; set; }
        public DbSet<TshirtDesignSubmissionImage> TshirtDesignSubmissionImages { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<TshirtTemplate> TshirtTemplates { get; set; }
        public DbSet<TShirtSize> TShirtSizes { get; set; }
        public DbSet<TShirtStyle> TShirtStyles { get; set; }
        public DbSet<ReelCommission> ReelCommissions { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<ProductFeedback> ProductFeedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table mappings
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Brand>().ToTable("Brands");
            modelBuilder.Entity<FashionModel>().ToTable("FashionModels");
            modelBuilder.Entity<TshirtDesigner>().ToTable("TshirtDesigners");

            // One-to-one relationship between OrderItem and RefundRequest
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.RefundRequest)
                .WithOne(rr => rr.OrderItem)
                .HasForeignKey<RefundRequest>(rr => rr.OrderItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RefundRequest>()
                .HasIndex(rr => rr.OrderItemId)
                .IsUnique();

            // ReelProduct many-to-many relationship
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

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // OrderItem relationships
            modelBuilder.Entity<OrderItem>()
               .HasOne(oi => oi.Brand)
               .WithMany(b => b.OrderItems)
               .HasForeignKey(oi => oi.BrandId)
               .OnDelete(DeleteBehavior.Restrict);

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

            // Product relationships
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

            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductDiscount)
                .WithMany(pd => pd.Products)
                .HasForeignKey(p => p.ProductDiscountId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProductVariant relationships
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // ProductImage relationships
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Reel relationships
            modelBuilder.Entity<Reel>()
                .HasOne(r => r.PostedByBrand)
                .WithMany(b => b.Reels)
                .HasForeignKey(r => r.PostedByBrandId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Reel>()
                .HasOne(r => r.PostedByModel)
                .WithMany(f => f.Reels)
                .HasForeignKey(r => r.PostedByModelId)
                .OnDelete(DeleteBehavior.NoAction);

            // ReelLike relationships
            modelBuilder.Entity<ReelLike>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(rl => rl.Reel)
                      .WithMany()
                      .HasForeignKey(rl => rl.ReelId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rl => rl.Customer)
                      .WithMany()
                      .HasForeignKey(rl => rl.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rl => new { rl.CustomerId, rl.ReelId })
                      .IsUnique();
            });

            // TshirtDesignOrder relationships
            modelBuilder.Entity<TshirtDesignOrder>()
                .HasOne(t => t.CustomerUser)
                .WithMany()
                .HasForeignKey(t => t.CustomerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // TshirtDesignOrderImage relationships
            modelBuilder.Entity<TshirtDesignOrderImage>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Images)
                .HasForeignKey(i => i.TshirtDesignOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payout relationships
            modelBuilder.Entity<Payout>()
                .HasOne(p => p.PayeeUser)
                .WithMany()
                .HasForeignKey(p => p.PayeeUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Category relationships
            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId);

            // Decimal type configurations
            modelBuilder.Entity<BrandStatistics>()
                .Property(b => b.TotalRefundAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RefundRequest>()
                .Property(r => r.RefundAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RefundTransaction>()
                .Property(r => r.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PromoCodeUsage>()
                .Property(p => p.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PromoCodeUsage>()
                .Property(p => p.OrderTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.WalletBalance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TshirtDesignOrder>()
                .Property(t => t.Length)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TshirtDesignOrder>()
                .Property(t => t.Weight)
                .HasColumnType("decimal(18,2)");

            // RefundTransaction relationships
            modelBuilder.Entity<RefundTransaction>()
                .HasOne(rt => rt.RefundRequest)
                .WithMany()
                .HasForeignKey(rt => rt.RefundRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RefundTransaction>()
                .HasOne(rt => rt.Customer)
                .WithMany()
                .HasForeignKey(rt => rt.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // RefundRequest relationships
            modelBuilder.Entity<RefundRequest>()
                .HasOne(rr => rr.Order)
                .WithMany()
                .HasForeignKey(rr => rr.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RefundRequest>()
                .HasOne(rr => rr.Customer)
                .WithMany()
                .HasForeignKey(rr => rr.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // PromoCode relationships
            modelBuilder.Entity<PromoCode>()
                .HasOne(pc => pc.Brand)
                .WithMany()
                .HasForeignKey(pc => pc.BrandId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PromoCodeUsage>()
                .HasOne(pcu => pcu.PromoCode)
                .WithMany(pc => pc.Usages)
                .HasForeignKey(pcu => pcu.PromoCodeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PromoCodeUsage>()
                .HasOne(pcu => pcu.Customer)
                .WithMany()
                .HasForeignKey(pcu => pcu.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PromoCodeUsage>()
                .HasOne(pcu => pcu.Order)
                .WithMany()
                .HasForeignKey(pcu => pcu.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // BrandStatistics relationships
            modelBuilder.Entity<BrandStatistics>()
                .HasOne(bs => bs.Brand)
                .WithMany()
                .HasForeignKey(bs => bs.BrandId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WebhookLog>(entity =>
            {
                entity.HasIndex(e => e.LogDate);
                entity.HasIndex(e => e.WebhookType);
                entity.HasIndex(e => e.LogLevel);
            });
        }
    }
}