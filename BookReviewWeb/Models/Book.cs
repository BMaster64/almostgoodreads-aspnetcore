using System;
using System.Collections.Generic;

namespace BookReviewWeb.Models;

public partial class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string? Description { get; set; }

    public int? PublishYear { get; set; }

    public string? CoverImageUrl { get; set; }

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();

    public virtual ICollection<MyBook> MyBooks { get; set; } = new List<MyBook>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
