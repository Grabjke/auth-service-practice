using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Abstractions;

// Каждая фича реализует IEndpoint и сама регистрирует свой маршрут
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
