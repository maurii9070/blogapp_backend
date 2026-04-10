using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;
using Blog.Api.Shared.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class DeleteComment
{
    public record Response(string Message);

    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        public async Task<Result<Response>> HandleAsync(int postId, int commentId)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user is null)
            {
                return Result<Response>.Failure("User not authenticated.");
            }

            var postExists = await dbContext.Posts.AnyAsync(post => post.Id == postId && post.IsPublished);
            if (!postExists)
            {
                return Result<Response>.Failure("Post not found.");
            }

            var comment = await dbContext.Comments
                .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.PostId == postId);

            if (comment is null)
            {
                return Result<Response>.Failure("Comment not found.");
            }

            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (comment.UserId != user.Id && !isAdmin)
            {
                return Result<Response>.Failure("You are not authorized to delete this comment.");
            }

            dbContext.Comments.Remove(comment);
            await dbContext.SaveChangesAsync();

            return Result<Response>.Success(new Response("Comment deleted successfully."));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/posts/{postId:int}/comments/{commentId:int}", async (
            int postId,
            int commentId,
            Handler handler) =>
        {
            if (postId <= 0 || commentId <= 0)
            {
                return Results.BadRequest("Invalid identifiers.");
            }

            var result = await handler.HandleAsync(postId, commentId);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return result.Error switch
            {
                "Post not found." => Results.NotFound(result.Error),
                "Comment not found." => Results.NotFound(result.Error),
                "You are not authorized to delete this comment." => Results.Forbid(),
                _ => Results.BadRequest(result.Error)
            };
        }).RequireAuthorization();
    }
}
