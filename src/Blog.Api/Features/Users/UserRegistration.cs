using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Features.Users;

public class UserRegistration
{
    public record Request(string FullName, string Email, string Password, string PasswordConfirmation);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.FullName).NotEmpty()
                                                .WithMessage("Full name is required.")
                                                .MaximumLength(100);
            RuleFor(request => request.Email).EmailAddress().WithMessage("Invalid email address format.");
            RuleFor(request => request.Password).MinimumLength(6).WithMessage("Password is too short.");

            RuleFor(request => request.Password).Equal(request => request.PasswordConfirmation)
                                                .WithMessage("Passwords do not match.");

        }
    }

    public class Handler
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<string>> HandleAsync(Request request)
        {
            var user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, nameof(Roles.Reader));
                return Result<string>.Success("User registered successfully.");
            }

            var errors = result.Errors
                               .GroupBy(e => e.Code)
                               .ToDictionary(
                                   g => g.Key,
                                   g => g.Select(e => e.Description).ToArray()
                               );

            return Result<string>.ValidationFailure(errors);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users/register", async (
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

            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(result.ValidationErrors!);
            }

            return Results.Ok(result);
        });
    }
}