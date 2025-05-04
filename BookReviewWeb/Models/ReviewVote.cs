using System;
using System.Collections.Generic;

namespace BookReviewWeb.Models;

public partial class ReviewVote
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public int UserId { get; set; }

    public int VoteType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
