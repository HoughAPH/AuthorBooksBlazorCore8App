using BlazorWebAssemblyApp;
using BlazorWebAssemblyApp.Authentication;
using BlazorWebAssemblyApp.Services;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SharedClassLibrary.Contracts;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("Missing configuration: ApiSettings:BaseUrl");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

builder.Services.AddScoped<IUserAccount, AccountService>();
builder.Services.AddScoped<IBooks, BookService>();
builder.Services.AddScoped<IAuthors, AuthorsService>();
builder.Services.AddScoped<IManageUsers, ManageUsersService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSweetAlert2();

await builder.Build().RunAsync();
