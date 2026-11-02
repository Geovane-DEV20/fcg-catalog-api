namespace FcgCatalogApi.Exceptions;

/// <summary>Recurso solicitado não existe. Mapeia para HTTP 404.</summary>
public class RecursoNaoEncontradoException : FcgException
{
    public RecursoNaoEncontradoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}
