using System.Reflection;
using Drom.WPF.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Drom.WPF.DAL;

public class DromDbContext(DbContextOptions<DromDbContext> options) : DbContext(options)
{
    internal const string CaseInsensitiveCollation = "case_insensitive_collation";
    
    public DbSet<User> Users { get; init; }
    
    public DbSet<Ad> Ads { get; init; }
    
    public DbSet<AdImage> AdImages { get; init; }
    
    public DbSet<FavoriteAd> FavoriteAds { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level1", provider: "icu", deterministic: false);
    }
}