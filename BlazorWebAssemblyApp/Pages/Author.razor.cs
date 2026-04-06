using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using SharedClassLibrary.DTOs;

namespace BlazorWebAssemblyApp.Pages
{
    [Authorize(Roles = "Admin")]
    public partial class Author
    {
        [Parameter]
        public int idEdit { get; set; } = 0;

        private AuthorDTO? author;
        private string headerTitle = "Create New Author";

        protected override async Task OnParametersSetAsync()
        {
            if (idEdit != 0)
            {
                headerTitle = "Edit Author Details";
                author = await AuthorsService.GetAuthor(idEdit);
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
            {
                return;
            }

            int result = idEdit != 0
                ? await AuthorsService.UpdateAuthor(author.Id, author)
                : await AuthorsService.AddAuthor(author);

            if (result == 0)
            {
                await Swal.FireAsync(new SweetAlertOptions
                {
                    Title = idEdit != 0 ? "Error updating Author" : "Error adding Author",
                    Text = "Error",
                    Icon = SweetAlertIcon.Warning
                });

                return;
            }

            navigation.NavigateTo("/authors");
        }
    }
}