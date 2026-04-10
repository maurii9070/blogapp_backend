using Blog.Api.Data;

using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Features.Categories;

public class GetAllCategories
{
    public record Response(int Id, string Name);

    public class Handler(BlogDbContext dbContext)
    {
        public async Task<List<Response>> HandleAsync()
        {
            var categories = await dbContext.Categories
                .Select(c => new Response(c.Id, c.Name))
                .ToListAsync();

            return categories;
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", async (Handler handler) =>
        {
            var categories = await handler.HandleAsync();
            return Results.Ok(categories);
        });
    }

}
