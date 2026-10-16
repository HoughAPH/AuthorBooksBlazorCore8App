using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;
using System.Net.Http.Headers;

namespace AuthorBooksBlazorCore.Test;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbConnection>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton(_connection);
            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                DbConnection connection = serviceProvider.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }

    public HttpClient CreateAnonymousClient()
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        EnsureDatabaseInitialized();

        return client;
    }

    public HttpClient CreateAuthenticatedClient(string role, string? userId = null)
    {
        HttpClient client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);

        if (userId is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, userId);

        return client;
    }

    private void EnsureDatabaseInitialized()
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        if (!db.Authors.Any())
        {
            SeedDatabase(db);
        }
    }

    private static void SeedDatabase(AppDbContext db)
    {
        // Ids are database-generated (1, 2, 3 for authors; 1, 2 for books on a fresh database).
        Author author1 = Author.Create("Eric Evans");
        author1.AddBook("Domain Driven Design");

        Author author2 = Author.Create("Robert Martin");
        author2.AddBook("Clean Code");

        Author author3 = Author.Create("Removable Author");

        db.Authors.AddRange(author1, author2, author3);

        db.SaveChanges();
    }
}
