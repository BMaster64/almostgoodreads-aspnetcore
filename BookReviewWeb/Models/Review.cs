using System;
using System.Collections.Generic;

namespace BookReviewWeb.Models;

public partial class Review
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
