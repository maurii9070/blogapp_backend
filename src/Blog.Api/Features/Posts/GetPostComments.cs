using Blog.Api.Data;
using Blog.Api.Shared;

using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class GetPostComments
{
    public record Response(int Id, int PostId, string Content, DateTime CreatedAt, string UserId, string AuthorName);

    public class Handler(BlogDbContext dbContext)
    {
        public async Task<Result<List<Response>>> HandleAsync(int postId)
        {
            var postExists = await dbContext.Posts.AnyAsync(post => post.Id == postId && post.IsPublished);
            if (!postExists)
            {
                return Result<List<Response>>.Failure("Post not found.");
            }

            var comments = await dbContext.Comments
                .Where(comment => comment.PostId == postId && comment.IsApproved)
                .OrderByDescending(comment => comment.CreatedAt)
                .Select(comment => new Response(
                    comment.Id,
                    comment.PostId,
                    comment.Content,
                    comment.CreatedAt,
                    comment.UserId,
                    comment.User.FullName
                ))
                .ToListAsync();

            return Result<List<Response>>.Success(comments);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/posts/{postId:int}/comments", async (int postId, Handler handler) =>
        {
            if (postId <= 0)
            {
                return Results.BadRequest("Invalid post ID.");
            }

            var result = await handler.HandleAsync(postId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        });
    }
}
