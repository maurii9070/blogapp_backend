using Blog.Api.Entities;
using Blog.Api.Shared;

using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Features.Users;

public class UserLogout
{
    public class Handler(SignInManager<ApplicationUser> signInManager)
    {
        public async Task<Result<string>> HandleAsync()
        {
            await signInManager.SignOutAsync();
            return Result<string>.Success("User successfully logged out.");
        }
    }
    
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/logout", async (
            Handler handler) =>
        {
            var result = await handler.HandleAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();
    }
}