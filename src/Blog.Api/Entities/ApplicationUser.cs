using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<Post> Posts { get; set; } = [];
    public  ICollection<Comment> Comments { get; set; } = [];
}