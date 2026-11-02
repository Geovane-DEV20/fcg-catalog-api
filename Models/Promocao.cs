using FcgCatalogApi.Exceptions;

namespace FcgCatalogApi.Models;

public sealed class Promocao
{
    private readonly List<Guid> _jogoIds = new();

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal PercentualDesconto { get; private set; }
    public DateTime VigenciaInicio { get; private set; }
    public DateTime VigenciaFim { get; private set; }
    public bool EncerradaManualmente { get; private set; }
    public IReadOnlyCollection<Guid> JogoIds => _jogoIds.AsReadOnly();

    // Para EF Core
    public List<Guid> JogoIdsInterno
    {
        get => _jogoIds;
        set
        {
            _jogoIds.Clear();
            if (value != null) _jogoIds.AddRange(value);
        }
    }

    private Promocao() { }

    public static Promocao CriarPromocao(string nome, decimal percentualDesconto, DateTime inicio, DateTime fim)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("NOME_INVALIDO", "O nome da promoção não pode ser vazio.");

        if (percentualDesconto < 1 || percentualDesconto > 90)
            throw new DomainException("PERCENTUAL_DESCONTO_INVALIDO", 
                "O percentual de desconto deve estar entre 1% e 90%.");

        if (fim <= inicio)
            throw new DomainException("VIGENCIA_INVALIDA", 
                "A data final da vigência deve ser posterior à data inicial.");

        return new Promocao
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            PercentualDesconto = percentualDesconto,
            VigenciaInicio = inicio,
            VigenciaFim = fim,
            EncerradaManualmente = false
        };
    }

    public bool EstaVigente(DateTime agora) => 
        !EncerradaManualmente && agora >= VigenciaInicio && agora <= VigenciaFim;

    public void AplicarAoJogo(Guid jogoId)
    {
        if (EncerradaManualmente)
            throw new RegraDeNegocioException("PROMOCAO_ENCERRADA", 
                "Não é possível aplicar jogos a uma promoção encerrada.");

        if (!_jogoIds.Contains(jogoId))
            _jogoIds.Add(jogoId);
    }

    public void EncerrarPromocao()
    {
        EncerradaManualmente = true;
    }
}
