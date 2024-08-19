using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;

namespace BlazorWebAssemblyApp.Pages
{
    public partial class Author
    {
        [Parameter]
        public int idEdit { get; set; } = 0;
        private SharedClassLibrary.DTOs.AuthorDTO author = new();

        protected override async Task OnInitializedAsync()
        {
            if (idEdit != 0)
            {
                author = await AuthorsService.GetAuthor(idEdit);
            }
            else
            {
                author = new();
            }
        }

        protected async void OnValidSubmit()
        {
            int returnId = 0;

            if (idEdit != 0)
            {
                var result = await AuthorsService.UpdateAuthor(author.Id, author);
                if (result == 0)
                {
                    //await JSRuntime.InvokeVoidAsync("alert", result.FirstError.Description);
                    await Swal.FireAsync(new SweetAlertOptions
                    {
                        Title = "Error updating Author",
                        Text = "Error",
                        Icon = SweetAlertIcon.Warning
                    });
                }
                else
                {
                    returnId = result;
                    navigation.NavigateTo("/authors");
                }
            }
            else
            {
                var result = await AuthorsService.AddAuthor(author);


                if (result == 0)
                {
                    //await JSRuntime.InvokeVoidAsync("alert", "Error");
                    await Swal.FireAsync(new SweetAlertOptions
                    {
                        Title = "Error adding Author",
                        Text = "Error",
                        Icon = SweetAlertIcon.Warning
                    });

                    //navigation.NavigateTo("/authors");
                }
                else
                {
                    returnId = result;
                    navigation.NavigateTo("/authors");
                    //returnId = 1;
                };

                //returnId = 1;// await AuthorsService.AddAuthor(author);
            }
        }
        // if(returnId != 0)
        // {
        //     navigation.NavigateTo("/authors");
        // }
        // else
        // {
        //     await JSRuntime.InvokeVoidAsync("alert", "Unauthorized");
        //     navigation.NavigateTo("/authors");
        // }
    }
}