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
    }

    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        CreatePost.MapEndpoint(app);
        PublishPost.MapEndpoint(app);
        GetPublishedPosts.MapEndpoint(app);
        GetPostById.MapEndpoint(app);
    }
}