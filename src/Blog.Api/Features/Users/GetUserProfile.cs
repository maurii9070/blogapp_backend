using System.Security.Claims;
using Blog.Api.Entities;
using Blog.Api.Shared;
using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Features.Users;

public class GetUserProfile
{
    public record Response(string Id, string Email, string FullName, string UserName);
    
    public class Handler(UserManager<ApplicationUser> userManager)
    {
        public async Task<Result<Response>> HandleAsync(ClaimsPrincipal user)
        {
            var applicationUser = await userManager.GetUserAsync(user);
            
            if (applicationUser == null)
            {
                return Result<Response>.Failure("User not found.");
            }
            
            var response = new Response(
                applicationUser.Id, 
                applicationUser.Email ?? string.Empty, 
                applicationUser.FullName, 
                applicationUser.UserName ?? string.Empty);
                
            return Result<Response>.Success(response);
        }
    }
    
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", async (
            ClaimsPrincipal user,
            Handler handler) =>
        {
            var result = await handler.HandleAsync(user);
            
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization();
    }
}

