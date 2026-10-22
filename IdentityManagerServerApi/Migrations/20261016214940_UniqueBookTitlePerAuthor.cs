using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityManagerServerApi.Migrations;

/// <inheritdoc />
public partial class UniqueBookTitlePerAuthor : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Books_AuthorId",
            table: "Books");

        migrationBuilder.CreateIndex(
            name: "IX_Books_AuthorId_Title",
            table: "Books",
            columns: new[] { "AuthorId", "Title" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Books_AuthorId_Title",
            table: "Books");

        migrationBuilder.CreateIndex(
            name: "IX_Books_AuthorId",
            table: "Books",
            column: "AuthorId");
    }
}
