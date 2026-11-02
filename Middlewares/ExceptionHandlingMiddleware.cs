using System.Net;
using System.Text.Json;
using FcgCatalogApi.Exceptions;

namespace FcgCatalogApi.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var rastreio = context.TraceIdentifier;
        var (status, titulo, codigo) = ClassificarExcecao(exception);

        _logger.LogWarning(
            exception,
            "Falha tratada: {Rota} {Verbo} {Codigo} {Rastreio}",
            context.Request.Path,
            context.Request.Method,
            codigo,
            rastreio);

        var detalhe = exception is FcgException
            ? exception.Message
            : "Ocorreu um erro inesperado. Consulte o suporte com o identificador de rastreio.";

        var errorResponse = new ErrorResponse(titulo, detalhe, codigo, rastreio);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static (int Status, string Titulo, string Codigo) ClassificarExcecao(Exception exception) => exception switch
    {
        RecursoNaoEncontradoException e => ((int)HttpStatusCode.NotFound, "Recurso não encontrado", e.Codigo),
        RecursoDuplicadoException e => ((int)HttpStatusCode.Conflict, "Recurso duplicado", e.Codigo),
        RegraDeNegocioException e => (422, "Pré-condição não satisfeita", e.Codigo),
        DomainException e => ((int)HttpStatusCode.BadRequest, "Requisição inválida", e.Codigo),
        _ => ((int)HttpStatusCode.InternalServerError, "Erro interno", "ERRO_INTERNO"),
    };
}

/// <summary>Resposta padronizada de erro da API.</summary>
public sealed record ErrorResponse(string Titulo, string Detalhe, string Codigo, string Rastreio);
