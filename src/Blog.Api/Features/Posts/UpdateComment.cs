using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;
using Blog.Api.Shared.Constants;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Posts;

public class UpdateComment
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
        public async Task<Result<Response>> HandleAsync(int postId, int commentId, Request request)
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
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId);

            if (comment is null)
            {
                return Result<Response>.Failure("Comment not found.");
            }

            var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (comment.UserId != user.Id && !isAdmin)
            {
                return Result<Response>.Failure("You are not authorized to edit this comment.");
            }

            comment.Content = request.Content.Trim();

            await dbContext.SaveChangesAsync();

            return Result<Response>.Success(new Response(
                comment.Id,
                comment.PostId,
                comment.Content,
                comment.CreatedAt,
                comment.UserId,
                comment.User.FullName));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/posts/{postId:int}/comments/{commentId:int}", async (
            int postId,
            int commentId,
            Request request,
            IValidator<Request> validator,
            Handler handler) =>
        {
            if (postId <= 0 || commentId <= 0)
            {
                return Results.BadRequest("Invalid identifiers.");
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(postId, commentId, request);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return result.Error switch
            {
                "Post not found." => Results.NotFound(result.Error),
                "Comment not found." => Results.NotFound(result.Error),
                "You are not authorized to edit this comment." => Results.Forbid(),
                _ => Results.BadRequest(result.Error)
            };
        }).RequireAuthorization();
    }
}
