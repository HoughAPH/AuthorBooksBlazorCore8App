using IdentityManagerServerApi.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace IdentityManagerServerApi.Data;

public class AppDbContext(DbContextOptions options) : IdentityDbContext<ApplicationUser>(options)
{
    // Book is a child of the Author aggregate and is intentionally not exposed as a DbSet.
    public virtual DbSet<Author> Authors { get; set; }
    public virtual DbSet<Logs> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Author>(author =>
        {
            author.HasKey(a => a.Id);
            author.Property(a => a.Name).IsRequired().HasMaxLength(Author.NameMaxLength);

            author.HasMany(a => a.Books)
                  .WithOne(b => b.Author)
                  .HasForeignKey(b => b.AuthorId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);

            author.Navigation(a => a.Books).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Book>(book =>
        {
            book.ToTable("Books");
            book.HasKey(b => b.Id);
            book.Property(b => b.Title).IsRequired().HasMaxLength(Book.TitleMaxLength);
        });
    }
}
