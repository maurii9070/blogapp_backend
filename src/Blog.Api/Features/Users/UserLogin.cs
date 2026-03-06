using Blog.Api.Entities;
using Blog.Api.Shared;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Features.Users;

public class UserLogin
{
    public record Request(string Email, string Password, bool RememberMe);
    
    public class RequestValidator : AbstractValidator<Request>
    {
        public  RequestValidator()
        {
            RuleFor(request => request.Email).NotEmpty()
                                             .EmailAddress()
                                             .WithMessage("A valid email address is required.");
            RuleFor(request => request.Password).NotEmpty().WithMessage("Password is required.");
        }
    }
    
    public class Handler(SignInManager<ApplicationUser> signInManager)
    {
        public async Task<Result<string>> HandleAsync(Request request)
        {
            var result = await signInManager.PasswordSignInAsync(
                userName: request.Email,
                password: request.Password,
                isPersistent: request.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return Result<string>.Success("User successfully logged in.");
            }

            if (result.IsLockedOut)
            {
                return Result<string>.Failure("User account locked out.");
            }
            
            return Result<string>.Failure("Invalid login attempt.");
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/login", async (
            Request request,
            IValidator<Request> validator,
            Handler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.ToDictionary());
            }
            
            var loginResult = await handler.HandleAsync(request);
            
            return loginResult.IsSuccess ? Results.Ok() : Results.Unauthorized();
        });
    }
}