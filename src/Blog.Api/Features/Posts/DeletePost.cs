using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class DeletePost
{
    public record Response(string Message);

    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        public async Task<Result<Response>> HandleAsync(int postId)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user == null)
                return Result<Response>.Failure("User not authenticated.");

            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return Result<Response>.Failure("Post not found.");

            if (post.AuthorId != user.Id)
                return Result<Response>.Failure("Post not found or you are not the author.");

            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();

            return Result<Response>.Success(new Response("Post deleted successfully"));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/posts/{postId:int}", async (int postId, Handler handler) =>
        {
            if (postId <= 0) return Results.BadRequest("Invalid ID");

            var result = await handler.HandleAsync(postId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireEditorRole");
    }
}