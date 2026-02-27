using Blog.Api.Shared.Constants;

using Microsoft.AspNetCore.Identity;

namespace Blog.Api.Data;

public class DbInitializer
{
    public static async Task SeedRoles(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        string[] roleNames = [
            AppRoles.Admin, 
            AppRoles.Editor, 
            AppRoles.Reader
        ];

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}