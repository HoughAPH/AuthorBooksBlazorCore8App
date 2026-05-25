using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedClassLibrary.Models;
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
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
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

    public HttpClient CreateAuthenticatedClient(string role)
    {
        HttpClient client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);
        return client;
    }

    private void EnsureDatabaseInitialized()
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        if (!db.Authors.Any() && !db.Books.Any())
        {
            SeedDatabase(db);
        }
    }

    private static void SeedDatabase(AppDbContext db)
    {
        Author author1 = new() { Id = 1, Name = "Eric Evans" };
        Author author2 = new() { Id = 2, Name = "Robert Martin" };
        Author author3 = new() { Id = 3, Name = "Removable Author" };

        db.Authors.AddRange(author1, author2, author3);

        db.Books.AddRange(
            new Book { Id = 1, Title = "Domain Driven Design", AuthorId = 1 },
            new Book { Id = 2, Title = "Clean Code", AuthorId = 2 });

        db.SaveChanges();
    }
}
