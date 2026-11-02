namespace FcgCatalogApi.Exceptions;

/// <summary>Recurso único já existe (ex.: título duplicado). Mapeia para HTTP 409.</summary>
public class RecursoDuplicadoException : FcgException
{
    public RecursoDuplicadoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}
