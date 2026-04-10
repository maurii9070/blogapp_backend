using Blog.Api.Entities;
using Blog.Api.Extensions;
using Blog.Api.Shared;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Features.Users;

public class UpdateUserProfile
{
    public record Request(string FullName, string Email);

    public record Response(string Id, string Email, string FullName, string UserName);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(request => request.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Invalid email address format.");
        }
    }

    public class Handler(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        public async Task<Result<Response>> HandleAsync(Request request)
        {
            var currentUser = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            if (currentUser is null)
            {
                return Result<Response>.Failure("User not authenticated.");
            }

            var normalizedEmail = request.Email.Trim();
            var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser is not null && existingUser.Id != currentUser.Id)
            {
                return Result<Response>.Failure("Email is already in use.");
            }

            currentUser.FullName = request.FullName.Trim();
            currentUser.Email = normalizedEmail;
            currentUser.UserName = normalizedEmail;

            var updateResult = await userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                return Result<Response>.ValidationFailure(updateResult.ToErrorDictionary());
            }

            return Result<Response>.Success(new Response(
                currentUser.Id,
                currentUser.Email ?? string.Empty,
                currentUser.FullName,
                currentUser.UserName ?? string.Empty));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/me", async (
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

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            if (result.ValidationErrors is not null)
            {
                return Results.ValidationProblem(result.ValidationErrors);
            }

            return Results.BadRequest(result.Error);
        }).RequireAuthorization();
    }
}
