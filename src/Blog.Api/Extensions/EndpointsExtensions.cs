using Blog.Api.Features.Categories;
using Blog.Api.Features.Posts;
using Blog.Api.Features.Users;

namespace Blog.Api.Extensions;

public static class EndpointsExtensions
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        UserRegistration.MapEndpoint(app);
        UserLogin.MapEndpoint(app);
        UserLogout.MapEndpoint(app);
        GetUserProfile.MapEndpoint(app);
        UpdateUserProfile.MapEndpoint(app);
        GetPublicUserProfile.MapEndpoint(app);
    }

    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        CreatePost.MapEndpoint(app);
        GetAuthorPosts.MapEndpoint(app);
        UpdatePost.MapEndpoint(app);
        DeletePost.MapEndpoint(app);
        PublishPost.MapEndpoint(app);
        GetPublishedPosts.MapEndpoint(app);
        GetPostById.MapEndpoint(app);
        CreateComment.MapEndpoint(app);
        GetPostComments.MapEndpoint(app);
    }

    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        CreateCategory.MapEndpoint(app);
        GetAllCategories.MapEndpoint(app);
    }
}