using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Exceptions;
using IdentityManagerServerApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
//using Microsoft.OpenApi.Models;
using Serilog;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.Repositories;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["https://localhost:7254"];

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//var levelSwitch = new LoggingLevelSwitch();
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.ControlledBy(levelSwitch)
//    .Enrich.WithProperty("InstanceId", Guid.NewGuid().ToString("n"))
//    //.WriteTo.BrowserHttp(endpointUrl: $"{builder.HostEnvironment.BaseAddress}ingest", controlLevelSwitch: levelSwitch)
//    .CreateLogger();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

//Starting
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection String is not found"));
});


/* this is used instead of .UseSerilog to add Serilog to providers */
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
//builder.Logging.AddSerilog();

//Add Identity & JWT authentication
//Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddRoles<IdentityRole>();



builder.Services.AddScoped<IBooks, BooksRepository>();
builder.Services.AddScoped<IAuthors, AuthorsRepository>();
builder.Services.AddScoped<IUserAccount, IdentityAccountService>();
builder.Services.AddScoped<IManageUsers, ManageUsersRepository>();

// JWT 
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{


    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWebassembly", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "DELETE", "PUT", "HEAD")
              .AllowAnyHeader();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionsHandler>();
builder.Services.AddProblemDetails();

//Add authentication to Swagger UI
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();

});


var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorWebassembly");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

