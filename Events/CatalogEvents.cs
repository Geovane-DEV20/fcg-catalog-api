namespace FcgCatalogApi.Events;

// Eventos de Jogo
public sealed record JogoCadastradoEvent(Guid JogoId, string Titulo, DateTime OcorridoEm);
public sealed record FichaPreenchidaEvent(Guid JogoId, DateTime OcorridoEm);
public sealed record PrecoDefinidoEvent(Guid JogoId, decimal Preco, DateTime OcorridoEm);
public sealed record DescricaoAtualizadaEvent(Guid JogoId, DateTime OcorridoEm);
public sealed record JogoPublicadoEvent(Guid JogoId, DateTime OcorridoEm);
public sealed record JogoDespublicadoEvent(Guid JogoId, DateTime OcorridoEm);

// Eventos de Promoção
public sealed record PromocaoCriadaEvent(Guid PromocaoId, string Nome, DateTime OcorridoEm);
public sealed record PromocaoAplicadaAoJogoEvent(Guid PromocaoId, Guid JogoId, DateTime OcorridoEm);
public sealed record PromocaoEncerradaEvent(Guid PromocaoId, DateTime OcorridoEm);
