using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Shared;
using Blog.Api.Shared.Services;

using FluentValidation;

namespace Blog.Api.Features.Categories;

public class CreateCategory
{
    public record Request(string Name);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(50)
                .WithMessage("Name cannot exceed 50 characters.");
        }
    }

    public class Handler(BlogDbContext dbContext, ISlugService slugService)
    {
        public async Task<Result<string>> HandleAsync(Request request)
        {
            var slug = slugService.Generate(request.Name);
            var category = new Category
            {
                Name = request.Name,
                Slug = slug
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            return Result<string>.Success("Category created successfully.");
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/categories", async (Request request, Handler handler) =>
        {
            var result = await handler.HandleAsync(request);
            return result.IsSuccess ? Results.Created($"/api/categories/{result.Value}", result.Value) : Results.BadRequest("todo mal");
        }).RequireAuthorization("RequireAdminRole");
    }

}
