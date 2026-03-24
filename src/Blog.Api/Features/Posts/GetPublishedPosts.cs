using System;

using Blog.Api.Data;
using Blog.Api.Shared;

using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class GetPublishedPosts
{
    public record Request(int Page = 1, int PageSize = 10);

    public record Response(
        int Id,
        string Title,
        DateTime PublishedAt,
        string AuthorName,
        string slug
    );

    public class Handler(
        BlogDbContext dbContext
    )
    {
        public async Task<Result<PaginatedList<Response>>> HandleAsync(Request request)
        {
            var query = dbContext.Posts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new Response(
                    p.Id,
                    p.Title,
                    p.PublishedAt,
                    p.Author.FullName,
                    p.Slug
                ));

            var totalCount = await query.CountAsync();

            var posts = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<Response>(posts, totalCount, request.Page, request.PageSize);

            return Result<PaginatedList<Response>>.Success(paginatedList);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/posts", async ([AsParameters] Request request, Handler handler) =>
        {
            if (request.Page < 1) request = request with { Page = 1 };
            if (request.PageSize > 50) request = request with { PageSize = 50 };

            var result = await handler.HandleAsync(request);

            return result.IsSuccess
                 ? Results.Ok(result.Value)
                 : Results.BadRequest(result.Error);
        });
    }
}
