using AuthPractice.Contracts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Ping;

// GET /api/ping — публичный endpoint, проверка что бэк жив
public sealed class PingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("ping", async (PingHandler handler, CancellationToken ct) =>
            // ResultResponse.Ok оборачивает ответ в Envelope { result, errors, timeGenerated }
            ResultResponse.Ok(await handler.Handle(new PingQuery(), ct)));
}

// Query — входные данные фичи (здесь пусто)
public sealed record PingQuery : IQuery;

// Handler — сама логика фичи (endpoint только принимает HTTP и отдаёт результат)
public sealed class PingHandler : IQueryHandler<PingResponse, PingQuery>
{
    public Task<PingResponse> Handle(PingQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PingResponse("pong", DateTime.UtcNow));
}
