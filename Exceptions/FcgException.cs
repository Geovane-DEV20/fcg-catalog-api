namespace FcgCatalogApi.Exceptions;

public abstract class FcgException : Exception
{
    public string Codigo { get; }

    protected FcgException(string codigo, string mensagem) : base(mensagem)
    {
        Codigo = codigo;
    }
}
