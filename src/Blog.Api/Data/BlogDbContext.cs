using Blog.Api.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Data;

public class BlogDbContext : IdentityDbContext<ApplicationUser>
{

    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options) { }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(eb =>
        {
            eb.Property(p => p.FullName).HasColumnType("varchar(100)");
        });

        modelBuilder.Entity<Post>(eb =>
        {
            eb.Property(p => p.Title).HasMaxLength(200);
            eb.Property(p => p.Content).HasColumnType("text");
            eb.Property(p => p.Slug).HasMaxLength(200);
        });

        modelBuilder.Entity<Category>(eb =>
        {
            eb.Property(c => c.Name).HasMaxLength(50);
            eb.Property(c => c.Slug).HasMaxLength(50);
        });

        modelBuilder.Entity<Tag>(eb =>
        {
            eb.Property(t => t.Name).HasMaxLength(50);
            eb.Property(t => t.Slug).HasMaxLength(50);
        });

        modelBuilder.Entity<Comment>(eb =>
        {
            eb.Property(c => c.Content).HasColumnType("text");

        });

        modelBuilder.Entity<Post>()
            .HasIndex(p => new { p.AuthorId, p.Slug })
            .IsUnique();
        modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Slug).IsUnique();
    }
}