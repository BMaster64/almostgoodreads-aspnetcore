using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Models;

public partial class AlmostGoodReadsContext : DbContext
{
    public AlmostGoodReadsContext()
    {
    }

    public AlmostGoodReadsContext(DbContextOptions<AlmostGoodReadsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<MyBook> MyBooks { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<BookGenre> BookGenres { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        if (!optionsBuilder.IsConfigured) { optionsBuilder.UseSqlServer(config.GetConnectionString("MyCnn")); }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Books__3214EC0759C86CBA");

            entity.Property(e => e.Author).HasMaxLength(255);
            entity.Property(e => e.CoverImageUrl).HasMaxLength(2083);
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.GenreId).HasName("PK__Genres__0385057ECC938080");

            entity.HasIndex(e => e.GenreName, "UQ__Genres__BBE1C33958F9AB95").IsUnique();

            entity.Property(e => e.GenreName).HasMaxLength(100);
        });

        modelBuilder.Entity<BookGenre>(entity =>
        {
            entity.HasKey(e => new { e.BookId, e.GenreId });

            entity.HasOne(d => d.Book)
                .WithMany()
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Genre)
                .WithMany()
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Genres)
            .WithMany(g => g.Books)
            .UsingEntity<BookGenre>();

        modelBuilder.Entity<MyBook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MyBooks__3214EC0703FAEE72");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "UQ_UserBook").IsUnique();

            entity.Property(e => e.DateAdded).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Book).WithMany(p => p.MyBooks)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__MyBooks__BookId__71D1E811");

            entity.HasOne(d => d.User).WithMany(p => p.MyBooks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__MyBooks__UserId__70DDC3D8");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC07471EC0B0");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Book).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__Reviews__BookId__4F7CD00D");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__UserId__5070F446");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC071BB64F85");

            entity.HasIndex(e => e.UserName, "UQ__Users__C9F28456D11EA7F2").IsUnique();

            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(256);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
