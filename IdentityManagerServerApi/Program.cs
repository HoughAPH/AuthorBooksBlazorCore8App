using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Exceptions;
using IdentityManagerServerApi.Features.Account;
using IdentityManagerServerApi.Features.Account.Commands;
using IdentityManagerServerApi.Features.Account.Queries;
using IdentityManagerServerApi.Features.Authors.Commands;
using IdentityManagerServerApi.Features.Authors.Queries;
using IdentityManagerServerApi.Features.Books.Commands;
using IdentityManagerServerApi.Features.Books.Queries;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Text;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsEnvironment("IntegrationTesting"))
    {
        loggerConfiguration
            .MinimumLevel.Warning()
            .WriteTo.Debug();

        return;
    }

    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["https://localhost:7254"];

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddHealthChecks();

if (!builder.Environment.IsEnvironment("IntegrationTesting"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(defaultConnection);
    });
}

// ========================================================
// FIXED STATELESS IDENTITY CONFIGURATION (JWT BEARER)
// ========================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

// ========================================================
// JWT BEARER MIDDLEWARE SETUP
// ========================================================
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key configuration is missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "IdentityManagerServerApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BlazorWasmClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

builder.Services.AddScoped<ICommandHandler<CreateAuthorCommand>, CreateAuthorHandler>();
builder.Services.AddScoped<ICommandHandler<RenameAuthorCommand>, RenameAuthorHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteAuthorCommand>, DeleteAuthorHandler>();
builder.Services.AddScoped<IQueryHandler<GetAuthorsQuery, List<AuthorDTO>>, GetAuthorsHandler>();
builder.Services.AddScoped<IQueryHandler<GetAuthorQuery, AuthorDTO?>, GetAuthorHandler>();

builder.Services.AddScoped<ICommandHandler<AddBookCommand>, AddBookHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateBookCommand>, UpdateBookHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteBookCommand>, DeleteBookHandler>();
builder.Services.AddScoped<IQueryHandler<GetBooksQuery, IEnumerable<BookDTO>>, GetBooksHandler>();
builder.Services.AddScoped<IQueryHandler<GetAuthorBooksQuery, IEnumerable<BookDTO>>, GetAuthorBooksHandler>();
builder.Services.AddScoped<IQueryHandler<GetBookQuery, BookDTO?>, GetBookHandler>();

builder.Services.AddScoped<ICommandHandler<RegisterAccountCommand>, RegisterAccountHandler>();
builder.Services.AddScoped<ICommandHandler<ResetPasswordCommand>, ResetPasswordHandler>();
builder.Services.AddScoped<IQueryHandler<LoginQuery, Result<string>>, LoginHandler>();
builder.Services.AddScoped<ICommandHandler<ForgotPasswordCommand>, ForgotPasswordHandler>();
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IResetTokenSender, LoggingResetTokenSender>();
else
    builder.Services.AddScoped<IResetTokenSender, NoResetTokenSender>();
builder.Services.AddScoped<IQueryHandler<GetCurrentUserQuery, CurrentUserDto?>, GetCurrentUserHandler>();

builder.Services.AddScoped<IManageUsers, ManageUsersRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWebassembly", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "DELETE", "PUT", "HEAD")
              .AllowAnyHeader()
              .AllowCredentials(); // Required: the Blazor client sends requests with credentials mode 'include'
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionsHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiRateLimiting(builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandler();

// Tests build their own database (see CustomWebApplicationFactory) and replace auth.
if (!app.Environment.IsEnvironment("IntegrationTesting"))
{
    await app.SeedAdminAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseCors("BlazorWebassembly");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program
{
}
