using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class CreateComment
{
    public record Request(string Content);

    public record Response(int Id, int PostId, string Content, DateTime CreatedAt, string UserId, string AuthorName);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Content)
                .NotEmpty()
                .WithMessage("Comment content is required.")
                .MaximumLength(1000)
                .WithMessage("Comment cannot exceed 1000 characters.");
        }
    }

    public class Handler(
        BlogDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        public async Task<Result<Response>> HandleAsync(int postId, Request request)
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

            var comment = new Comment
            {
                Content = request.Content.Trim(),
                PostId = postId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true
            };

            dbContext.Comments.Add(comment);
            await dbContext.SaveChangesAsync();

            return Result<Response>.Success(new Response(
                comment.Id,
                comment.PostId,
                comment.Content,
                comment.CreatedAt,
                user.Id,
                user.FullName));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/posts/{postId:int}/comments", async (
            int postId,
            Request request,
            IValidator<Request> validator,
            Handler handler) =>
        {
            if (postId <= 0)
            {
                return Results.BadRequest("Invalid post ID.");
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(postId, request);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization();
    }
}
