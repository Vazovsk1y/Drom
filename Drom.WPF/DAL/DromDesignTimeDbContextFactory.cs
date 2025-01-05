using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Drom.WPF.DAL;

public class DromDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DromDbContext>
{
    private const string ConnectionString = "User ID=postgres;Password=12345678;Host=localhost;Port=5432;Database=DromDb;";
    
    public DromDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DromDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString);
        return new DromDbContext(optionsBuilder.Options);
    }
}