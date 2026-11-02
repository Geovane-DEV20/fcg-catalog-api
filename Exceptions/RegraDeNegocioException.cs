namespace FcgCatalogApi.Exceptions;

/// <summary>Pré-condição de estado não satisfeita (ex.: publicar jogo sem preço). Mapeia para HTTP 422.</summary>
public class RegraDeNegocioException : FcgException
{
    public RegraDeNegocioException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}
