using Blazored.LocalStorage;
using BlazorWebAssemblyApp;
using BlazorWebAssemblyApp.Authentication;
using BlazorWebAssemblyApp.Services;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SharedClassLibrary.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7202/") });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IUserAccount, AccountService>();
builder.Services.AddScoped<IBooks, BookService>();
builder.Services.AddScoped<IAuthors, AuthorsService>();
builder.Services.AddScoped<IWeather, AccountService>();
builder.Services.AddSweetAlert2();

await builder.Build().RunAsync();
