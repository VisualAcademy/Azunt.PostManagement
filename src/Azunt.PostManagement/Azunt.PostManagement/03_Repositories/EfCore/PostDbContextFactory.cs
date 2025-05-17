using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.PostManagement;

public class PostDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public PostDbContextFactory() { }

    public PostDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public PostDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PostDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new PostDbContext(options);
    }

    public PostDbContext CreateDbContext(DbContextOptions<PostDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new PostDbContext(options);
    }

    public PostDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}