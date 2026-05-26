using CurrieTechnologies.Razor.SweetAlert2;

namespace BlazorWebAssemblyApp.Services;

public interface INotificationService
{
    Task ShowSuccessAsync(string message, string title = "Success");
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowWarningAsync(string message, string title = "Warning");
    Task<bool> ConfirmAsync(string title, string message, SweetAlertIcon? icon = null);
}