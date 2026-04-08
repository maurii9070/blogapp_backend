using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;
using Blog.Api.Shared.Services;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class UpdatePost
{
    public record Request(string Title, string Content, int CategoryId, string[] TagNames);
    public record Response(int Id, string Slug, DateTime UpdatedAt);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters.");

            RuleFor(request => request.Content)
                .NotEmpty()
                .WithMessage("Content is required.");

            RuleFor(request => request.CategoryId)
                .GreaterThan(0)
                .WithMessage("CategoryId must be greater than 0.");

            RuleFor(request => request.TagNames)
                .NotNull()
                .WithMessage("Tag names list cannot be null.")
                .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage("Tag names cannot contain empty or whitespace-only values.");
        }
    }

    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ISlugService slugService)
    {
        public async Task<Result<Response>> HandleAsync(int postId, Request request)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user == null)
                return Result<Response>.Failure("User not authenticated.");

            var post = await dbContext.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return Result<Response>.Failure("Post not found.");

            if (post.AuthorId != user.Id)
                return Result<Response>.Failure("Post not found or you are not the author.");

            var slug = slugService.Generate(request.Title);

            var existingPost = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.AuthorId == user.Id && p.Slug == slug && p.Id != postId);

            if (existingPost != null)
            {
                return Result<Response>.Failure("Ya existe un post con este título para este autor. Por favor, elige un título diferente.");
            }

            post.Title = request.Title;
            post.Slug = slug;
            post.Content = request.Content;
            post.CategoryId = request.CategoryId;
            post.UpdatedAt = DateTime.UtcNow;

            await UpdateTagsAsync(post, request.TagNames);

            await dbContext.SaveChangesAsync();

            return Result<Response>.Success(new Response(post.Id, post.Slug, post.UpdatedAt));
        }

        private async Task UpdateTagsAsync(Post post, string[] tagNames)
        {
            post.Tags.Clear();

            foreach (var tagName in tagNames)
            {
                var tagSlug = slugService.Generate(tagName.Trim());
                var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug);

                if (tag == null)
                {
                    tag = new Tag
                    {
                        Name = tagName.Trim().ToLower(),
                        Slug = tagSlug
                    };
                    dbContext.Tags.Add(tag);
                }

                post.Tags.Add(tag);
            }
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/posts/{postId:int}", async (
            int postId,
            Request request,
            IValidator<Request> validator,
            Handler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(postId, request);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireEditorRole");
    }
}