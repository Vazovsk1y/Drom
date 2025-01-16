using System.Reflection;
using Drom.WPF.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Drom.WPF.DAL;

public class DromDbContext(DbContextOptions<DromDbContext> options) : DbContext(options)
{
    internal const string CaseInsensitiveCollation = "case_insensitive_collation";
    
    private const string FirstUserPwd = "Admin_123#";
    
    private static readonly User FirstUser = new()
    {
        Id = Guid.Parse("3ee9159b-2673-4a0c-8a94-fc6384e64565"),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(FirstUserPwd),
        Role = Role.Admin,
        Username = "Admin",
        PhoneNumber = "+7 999 999 9999"
    };
    
    public DbSet<User> Users { get; init; }
    
    public DbSet<Ad> Ads { get; init; }
    
    public DbSet<AdImage> AdImages { get; init; }
    
    public DbSet<FavoriteAd> FavoriteAds { get; init; }
    
    public DbSet<NewsItem> News { get; init; }
    
    public DbSet<NewsItemComment> NewsItemComments { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.Entity<User>().HasData(FirstUser);
        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level1", provider: "icu", deterministic: false);
    }
}