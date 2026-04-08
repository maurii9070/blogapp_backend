using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class GetAuthorPosts
{
    public record Request(int Page = 1, int PageSize = 10);

    public record Response(
        int Id,
        string Title,
        string Slug,
        bool IsPublished,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? PublishedAt,
        string CategoryName,
        string[] TagNames
    );

    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor
    )
    {
        public async Task<Result<PaginatedList<Response>>> HandleAsync(string authorId, Request request)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user == null)
                return Result<PaginatedList<Response>>.Failure("User not authenticated.");

            if (user.Id != authorId)
                return Result<PaginatedList<Response>>.Failure("You are not authorized to view these posts.");

            var query = dbContext.Posts
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new Response(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.IsPublished,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.IsPublished ? p.PublishedAt : null,
                    p.Category.Name,
                    p.Tags.Select(tag => tag.Name).ToArray()
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
        app.MapGet("/api/users/{authorId}/posts", async (string authorId, [AsParameters] Request request, Handler handler) =>
        {
            if (request.Page < 1) request = request with { Page = 1 };
            if (request.PageSize > 50) request = request with { PageSize = 50 };

            var result = await handler.HandleAsync(authorId, request);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireEditorRole");
    }
}