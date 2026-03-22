using System;

using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class PublishPost
{
    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor
    )
    {
        public async Task<Result<string>> HandleAsync(int postId)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user == null)
                return Result<string>.Failure("User not authenticated.");

            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.AuthorId == user.Id);

            if (post == null)
            {
                return Result<string>.Failure("Post not found or you are not the author.");
            }

            if (post.IsPublished)
            {
                return Result<string>.Failure("Post is already published.");
            }

            post.IsPublished = true;
            post.PublishedAt = DateTime.UtcNow;

            dbContext.Posts.Update(post);
            await dbContext.SaveChangesAsync();

            return Result<string>.Success("Post published successfully");
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/posts/{postId}/publish", async (int postId, Handler handler) =>
        {
            if (postId <= 0) Results.BadRequest("Invalid ID");

            var result = await handler.HandleAsync(postId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireEditorRole");
    }
}
