using System;

using Blog.Api.Data;
using Blog.Api.Shared;

using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class GetPostById
{
    public record Response(
        int Id,
        string Title,
        string Content,
        string Slug,
        string AuthorShortId,
        DateTime CreatedAt,
        DateTime? PublishedAt,
        string CategoryName,
        string[] TagNames
    );

    public class Handler(
        BlogDbContext dbContext
    )
    {
        public async Task<Result<Response>> HandleAsync(int postId)
        {
            var post = await dbContext.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post is null || !post.IsPublished)
            {
                return Result<Response>.Failure("Post not found.");
            }

            var authorShortId = post.AuthorId.Split('-')[0];

            return Result<Response>.Success(new Response(
                post.Id,
                post.Title,
                post.Content,
                post.Slug,
                authorShortId,
                post.CreatedAt,
                post.PublishedAt,
                post.Category.Name,
                post.Tags.Select(t => t.Name).ToArray()
            ));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/posts/{postId:int}", async (int postId, Handler handler) =>
        {
            var result = await handler.HandleAsync(postId);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        });
    }
}
