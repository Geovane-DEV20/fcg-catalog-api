using FcgCatalogApi.Models;

namespace FcgCatalogApi.DTOs;

// Requests
public sealed record CadastrarJogoRequest(string Titulo, string Descricao, string Genero);

public sealed record PreencherFichaRequest(string Genero, string Desenvolvedora, DateTime Lancamento);

public sealed record DefinirPrecoRequest(decimal Preco);

public sealed record AtualizarDescricaoRequest(string Descricao);

// Responses
public sealed record CadastrarJogoResponse(Guid Id, string Status);

public sealed record JogoResponse(
    Guid Id,
    string Titulo,
    string Descricao,
    string Genero,
    string? Desenvolvedora,
    DateTime? DataLancamento,
    decimal? Preco,
    decimal? PrecoComDesconto,
    string Status)
{
    public static JogoResponse DeJogo(Jogo jogo, decimal? precoComDesconto = null) => new(
        jogo.Id,
        jogo.Titulo,
        jogo.Descricao,
        jogo.Genero,
        jogo.Desenvolvedora,
        jogo.DataLancamento,
        jogo.Preco,
        precoComDesconto,
        jogo.Status.ToString());
}

/// <summary>DTO para consulta de jogo para aquisição (comunicação entre microsserviços).</summary>
public sealed record JogoParaAquisicaoResponse(Guid JogoId, bool Publicado, decimal PrecoEfetivo);

// DTOs de Compra/Pedido

/// <summary>Response retornado ao iniciar uma compra.</summary>
public sealed record CompraIniciadaResponse(
    Guid OrderId,
    Guid GameId,
    string GameTitle,
    decimal Price,
    string Status,
    string Mensagem);
