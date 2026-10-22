using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace BlazorWebAssemblyApp.Pages.Authors;

[Authorize(Roles = "Admin")]
public partial class Author
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int IdEdit { get; set; }

    private AuthorDTO? author;
    private string headerTitle = "Create New Author";

    protected override async Task OnParametersSetAsync()
    {
        if (IdEdit != 0)
        {
            headerTitle = "Edit Author Details";
            author = await AuthorsService.GetAuthor(IdEdit);
        }
        else
        {
            headerTitle = "Create New Author";
            author = new();
        }
    }

    protected async Task OnValidSubmit()
    {
        if (author is null)
            return;

        Result response = IdEdit != 0
            ? await AuthorsService.UpdateAuthor(author.Id, author)
            : await AuthorsService.AddAuthor(author);

        if (!response.Succeeded)
        {
            await NotificationService.ShowWarningAsync(
                response.Message,
                IdEdit != 0 ? "Error Updating Author" : "Error Adding Author");

            return;
        }

        await NotificationService.ShowSuccessAsync(response.Message);
        Navigation.NavigateTo("/authors");
    }

    private void NavigateBackToList()
    {
        Navigation.NavigateTo("/authors");
    }
}