using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Users;

public class GetPublicUserProfile
{
    public record Response(string Id, string FullName, int PublishedPostsCount);

    public class Handler(
        UserManager<ApplicationUser> userManager,
        BlogDbContext dbContext)
    {
        public async Task<Result<Response>> HandleAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Result<Response>.Failure("User not found.");
            }

            var publishedPostsCount = await dbContext.Posts
                .CountAsync(post => post.AuthorId == userId && post.IsPublished);

            return Result<Response>.Success(new Response(
                user.Id,
                user.FullName,
                publishedPostsCount));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/{userId}/profile", async (string userId, Handler handler) =>
        {
            var result = await handler.HandleAsync(userId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        });
    }
}
