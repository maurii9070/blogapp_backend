using Blog.Api.Data;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

using Blog.Api.Entities;
using Blog.Api.Shared;
using Blog.Api.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class CreatePost
{

    public record Request(string Title, string Content, int CategoryId, string[] TagNames);

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
        public async Task<Result<string>> HandleAsync(Request request)
        {
            var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (user == null)
                return Result<string>.Failure("User not authenticated.");


            var slug = slugService.Generate(request.Title);

            var existingPost = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.AuthorId == user.Id && p.Slug == slug);

            if (existingPost != null)
            {
                return Result<string>.Failure("Ya existe un post con este título para este autor. Por favor, elige un título diferente.");
            }

            var post = new Post
            {
                Title = request.Title,
                Slug = slug,
                Content = request.Content,
                AuthorId = user.Id,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            await AddTagsToPost(post, request.TagNames);

            dbContext.Posts.Add(post);
            await dbContext.SaveChangesAsync();

            return Result<string>.Success("Post created successfully.");
        }

        private async Task AddTagsToPost(Post post, string[] tagNames)
        {
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
        app.MapPost("/api/posts", async (
            Request request,
            IValidator<Request> validator,
            Handler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(request);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireEditorRole");
    }
}
