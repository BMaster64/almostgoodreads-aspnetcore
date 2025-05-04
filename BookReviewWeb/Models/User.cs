using System;
using System.Collections.Generic;

namespace BookReviewWeb.Models;

public partial class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<MyBook> MyBooks { get; set; } = new List<MyBook>();

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
