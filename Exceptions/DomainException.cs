namespace FcgCatalogApi.Exceptions;

/// <summary>Invariante de agregado ou objeto de valor violada. Mapeia para HTTP 400.</summary>
public class DomainException : FcgException
{
    public DomainException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}
