using LifeSprint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LifeSprint.Tests.Integration;

/// <summary>
/// Base class for integration tests that use a real PostgreSQL database.
/// </summary>
/// <remarks>
/// Uses the test-db service from docker-compose.yml:
/// - Host: localhost:5433
/// - Database: lifesprint_test
/// - User: lifesprint_test
/// - Password: test_password
///
/// Tests implement IAsyncLifetime to:
/// - InitializeAsync: Run migrations and seed test data
/// - DisposeAsync: Clean up test data
/// </remarks>
[Trait("Category", "Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected AppDbContext Context { get; private set; } = null!;
    protected const string TestUserId = "integration_test_user";

    /// <summary>
    /// Creates a new database context connected to the test database.
    /// </summary>
    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=lifesprint_test;Username=lifesprint_test;Password=test_password")
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Initialize runs before each test.
    /// Ensures database is ready and applies migrations.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Context = CreateContext();

        // Ensure database is created and migrations are applied
        await Context.Database.MigrateAsync();

        // Clean any existing test data
        await CleanupTestDataAsync();
    }

    /// <summary>
    /// Dispose runs after each test.
    /// Cleans up test data to ensure test isolation.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await CleanupTestDataAsync();
        await Context.DisposeAsync();
    }

    /// <summary>
    /// Removes all test data from the database.
    /// Override this in derived classes to clean up specific test data.
    /// </summary>
    protected virtual async Task CleanupTestDataAsync()
    {
        // Use raw SQL to clean up test data efficiently
        // This avoids complex EF Core queries and foreign key issues
        await Context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""ContainerActivities""
            WHERE ""ActivityTemplateId"" IN (
                SELECT ""Id"" FROM ""ActivityTemplates"" WHERE ""UserId"" = {0}
            )", TestUserId);

        await Context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""ActivityTemplates"" WHERE ""UserId"" = {0}", TestUserId);

        await Context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""Containers"" WHERE ""UserId"" = {0}", TestUserId);
    }
}
