using FcgCatalogApi.Exceptions;

namespace FcgCatalogApi.Models;

public sealed class Jogo
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public string Genero { get; private set; } = string.Empty;
    public string? Desenvolvedora { get; private set; }
    public DateTime? DataLancamento { get; private set; }
    public decimal? Preco { get; private set; }
    public StatusJogo Status { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public bool FichaPreenchida => !string.IsNullOrWhiteSpace(Desenvolvedora) && DataLancamento.HasValue;

    private Jogo() { }

    public static Jogo CadastrarJogo(string titulo, string descricao, string genero)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("TITULO_INVALIDO", "O título do jogo não pode ser vazio.");

        return new Jogo
        {
            Id = Guid.NewGuid(),
            Titulo = titulo.Trim(),
            Descricao = descricao ?? string.Empty,
            Genero = genero ?? string.Empty,
            Status = StatusJogo.Rascunho,
            CriadoEm = DateTime.UtcNow
        };
    }

    public void PreencherFicha(string genero, string desenvolvedora, DateTime lancamento)
    {
        if (string.IsNullOrWhiteSpace(desenvolvedora))
            throw new DomainException("FICHA_INVALIDA", "A desenvolvedora não pode ser vazia.");

        Genero = genero ?? string.Empty;
        Desenvolvedora = desenvolvedora.Trim();
        DataLancamento = lancamento;
    }

    public void DefinirPreco(decimal preco)
    {
        if (preco < 0)
            throw new DomainException("PRECO_INVALIDO", "O preço deve ser maior ou igual a zero.");

        Preco = preco;
    }

    public void AtualizarDescricao(string descricao)
    {
        Descricao = descricao ?? string.Empty;
    }

    public void PublicarJogo()
    {
        if (!FichaPreenchida || Preco is null)
            throw new RegraDeNegocioException("FICHA_OU_PRECO_AUSENTE", 
                "O jogo precisa ter a ficha preenchida e o preço definido para ser publicado.");

        Status = StatusJogo.Publicado;
    }

    public void DespublicarJogo()
    {
        Status = StatusJogo.Despublicado;
    }
}
