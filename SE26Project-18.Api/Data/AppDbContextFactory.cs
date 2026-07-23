using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SE26Project_18.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var version = new MariaDbServerVersion(new Version(11, 4, 5));
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3307;Database=playmate_dev;User=root;Password=playmate123;SslMode=None;",
            version
        );
        return new AppDbContext(optionsBuilder.Options);
    }
}
