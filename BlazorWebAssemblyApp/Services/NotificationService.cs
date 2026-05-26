using CurrieTechnologies.Razor.SweetAlert2;

namespace BlazorWebAssemblyApp.Services;

public class NotificationService(SweetAlertService swal) : INotificationService
{
    public Task ShowSuccessAsync(string message, string title = "Success")
    {
        return ShowMessageAsync(title, message, SweetAlertIcon.Success);
    }

    public Task ShowErrorAsync(string message, string title = "Error")
    {
        return ShowMessageAsync(title, message, SweetAlertIcon.Error);
    }

    public Task ShowWarningAsync(string message, string title = "Warning")
    {
        return ShowMessageAsync(title, message, SweetAlertIcon.Warning);
    }

    public async Task<bool> ConfirmAsync(string title, string message, SweetAlertIcon? icon = null)
    {
        SweetAlertResult result = await swal.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Text = message,
            //Icon = icon,
            ShowCancelButton = true,
            ConfirmButtonText = "Yes",
            CancelButtonText = "Cancel",
            HeightAuto = false,
            Icon = icon ?? SweetAlertIcon.Question
        });

        return result.IsConfirmed;
    }

    private Task ShowMessageAsync(string title, string message, SweetAlertIcon icon)
    {
        return swal.FireAsync(new SweetAlertOptions
        {
            Title = title,
            Text = message,
            Icon = icon,
            ShowCancelButton = false,
            HeightAuto = false
        });
    }
}