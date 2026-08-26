using Fulfillment.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fulfillment.Tests.Application;

/// <summary>
/// Builds a FulfillmentDbContext over a private SQLite in-memory database.
///
/// SQLite is a real relational provider, so LINQ here goes through the same
/// translation pipeline the SQL Server provider uses; a query that cannot be
/// translated fails here too. It is still not SQL Server: dialect differences and
/// collation behaviour are not reproduced, and rowversion concurrency is switched
/// off below. A passing test here proves the C# and the query shape are wired
/// correctly, not that the statement behaves identically against SQL Server.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        // The in-memory database lives exactly as long as the connection.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, RowVersionFreeModelCustomizer>()
            .Options;

        Context = new FulfillmentDbContext(options);
        Context.Database.EnsureCreated();
    }

    public FulfillmentDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// SQL Server fills rowversion columns itself. SQLite has no equivalent, and the
    /// mapped column is NOT NULL, so every insert would fail. This drops the
    /// store-generated/concurrency-token flags for the test model only.
    /// Consequence: these tests do not exercise optimistic concurrency — that lives
    /// in the real provider and is verified against SQL Server, not here.
    /// </summary>
    private sealed class RowVersionFreeModelCustomizer : RelationalModelCustomizer
    {
        public RowVersionFreeModelCustomizer(ModelCustomizerDependencies dependencies)
            : base(dependencies) { }

        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");

                if (rowVersion is null || rowVersion.ClrType != typeof(byte[]))
                    continue;

                rowVersion.ValueGenerated = ValueGenerated.Never;
                rowVersion.IsConcurrencyToken = false;
                rowVersion.IsNullable = true;
            }
        }
    }
}
