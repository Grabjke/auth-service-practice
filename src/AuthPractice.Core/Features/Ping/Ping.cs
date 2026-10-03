using AuthPractice.Contracts;
using AuthPractice.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Ping;

// GET /api/ping — публичный endpoint, проверка что бэк жив
public sealed class PingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("ping", (PingHandler handler) => Results.Ok(handler.Handle()));
}

// Handler — сама логика фичи (endpoint только принимает HTTP и отдаёт результат)
public sealed class PingHandler
{
    public PingResponse Handle() => new("pong", DateTime.UtcNow);
}
