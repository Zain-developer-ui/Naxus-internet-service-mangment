using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NEXUS.Data;

/**
 * The EF tooling builds a separate copy of the app to read the model. Running
 * the real startup path for that is slow and drags in auth and logging, so the
 * tool is given a bare context pointed at the same connection string instead.
 */
public sealed class NexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
{
    public NexusDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("NexusDb")
            ?? throw new InvalidOperationException("Connection string 'NexusDb' is not configured.");

        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NexusDbContext(options);
    }
}
