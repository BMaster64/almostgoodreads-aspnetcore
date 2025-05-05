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

    public virtual DbSet<ReviewVote> ReviewVotes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        if (!optionsBuilder.IsConfigured) { optionsBuilder.UseSqlServer(config.GetConnectionString("MyCnn")); }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Books__3214EC07E4CDA612");

            entity.Property(e => e.Author).HasMaxLength(255);
            entity.Property(e => e.CoverImageUrl).HasMaxLength(2083);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasMany(d => d.Genres).WithMany(p => p.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .HasConstraintName("FK__BookGenre__Genre__4AB81AF0"),
                    l => l.HasOne<Book>().WithMany()
                        .HasForeignKey("BookId")
                        .HasConstraintName("FK__BookGenre__BookI__49C3F6B7"),
                    j =>
                    {
                        j.HasKey("BookId", "GenreId").HasName("PK__BookGenr__CDD89250D2520271");
                        j.ToTable("BookGenres");
                    });
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.GenreId).HasName("PK__Genres__0385057E87F729D8");

            entity.HasIndex(e => e.GenreName, "UQ__Genres__BBE1C3391F5F8085").IsUnique();

            entity.Property(e => e.GenreName).HasMaxLength(100);
        });

        modelBuilder.Entity<MyBook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MyBooks__3214EC07946B65EE");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "UQ_UserBook").IsUnique();

            entity.Property(e => e.DateAdded).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Book).WithMany(p => p.MyBooks)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__MyBooks__BookId__693CA210");

            entity.HasOne(d => d.User).WithMany(p => p.MyBooks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__MyBooks__UserId__68487DD7");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC0786838469");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Book).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__Reviews__BookId__2E1BDC42");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__UserId__2F10007B");
        });

        modelBuilder.Entity<ReviewVote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReviewVo__3214EC0772C68039");

            entity.HasIndex(e => new { e.UserId, e.ReviewId }, "UQ_UserReviewVote").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK__ReviewVot__Revie__571DF1D5");

            entity.HasOne(d => d.User).WithMany(p => p.ReviewVotes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReviewVot__UserI__5812160E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC078AA537CD");

            entity.HasIndex(e => e.UserName, "UQ__Users__C9F2845627167D6A").IsUnique();

            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.UserName).HasMaxLength(256);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
