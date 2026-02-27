using Blog.Api.Features.Users;

namespace Blog.Api.Extensions;

public static class EndpointsExtensions
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        UserRegistration.MapEndpoint(app);
    }
}