using FcgCatalogApi.Data;
using FcgCatalogApi.DTOs;
using FcgCatalogApi.Events;
using FcgCatalogApi.Exceptions;
using FcgCatalogApi.Models;
using FcgCatalogApi.Repositories;

namespace FcgCatalogApi.Services;

public sealed class JogoService
{
    private readonly IJogoRepository _jogoRepository;
    private readonly IPromocaoRepository _promocaoRepository;
    private readonly ApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<JogoService> _logger;

    public JogoService(
        IJogoRepository jogoRepository,
        IPromocaoRepository promocaoRepository,
        ApplicationDbContext context,
        IEventPublisher eventPublisher,
        ILogger<JogoService> logger)
    {
        _jogoRepository = jogoRepository;
        _promocaoRepository = promocaoRepository;
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CadastrarJogoResponse> CadastrarAsync(CadastrarJogoRequest request, CancellationToken cancellationToken = default)
    {
        if (await _jogoRepository.ExisteComTituloAsync(request.Titulo, cancellationToken))
            throw new RecursoDuplicadoException("TITULO_DUPLICADO_DETECTADO", "Já existe um jogo cadastrado com este título.");

        var jogo = Jogo.CadastrarJogo(request.Titulo, request.Descricao, request.Genero);

        await _jogoRepository.AdicionarAsync(jogo, cancellationToken);
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new JogoCadastradoEvent(jogo.Id, jogo.Titulo, DateTime.UtcNow));
        _logger.LogInformation("Jogo cadastrado: {JogoId} - {Titulo}", jogo.Id, jogo.Titulo);

        return new CadastrarJogoResponse(jogo.Id, jogo.Status.ToString());
    }

    public async Task PreencherFichaAsync(Guid jogoId, PreencherFichaRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterOuFalharAsync(jogoId, cancellationToken);

        jogo.PreencherFicha(request.Genero, request.Desenvolvedora, request.Lancamento);
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new FichaPreenchidaEvent(jogo.Id, DateTime.UtcNow));
        _logger.LogInformation("Ficha preenchida para jogo: {JogoId}", jogo.Id);
    }

    public async Task DefinirPrecoAsync(Guid jogoId, DefinirPrecoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterOuFalharAsync(jogoId, cancellationToken);

        jogo.DefinirPreco(request.Preco);
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new PrecoDefinidoEvent(jogo.Id, request.Preco, DateTime.UtcNow));
        _logger.LogInformation("Preço definido para jogo: {JogoId} - R${Preco}", jogo.Id, request.Preco);
    }

    public async Task AtualizarDescricaoAsync(Guid jogoId, AtualizarDescricaoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterOuFalharAsync(jogoId, cancellationToken);

        jogo.AtualizarDescricao(request.Descricao);
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new DescricaoAtualizadaEvent(jogo.Id, DateTime.UtcNow));
        _logger.LogInformation("Descrição atualizada para jogo: {JogoId}", jogo.Id);
    }

    public async Task PublicarAsync(Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterOuFalharAsync(jogoId, cancellationToken);

        jogo.PublicarJogo();
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new JogoPublicadoEvent(jogo.Id, DateTime.UtcNow));
        _logger.LogInformation("Jogo publicado: {JogoId}", jogo.Id);
    }

    public async Task DespublicarAsync(Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await ObterOuFalharAsync(jogoId, cancellationToken);

        jogo.DespublicarJogo();
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new JogoDespublicadoEvent(jogo.Id, DateTime.UtcNow));
        _logger.LogInformation("Jogo despublicado: {JogoId}", jogo.Id);
    }

    public async Task<IReadOnlyList<JogoResponse>> ConsultarCatalogoAsync(string? titulo, string? genero, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var jogos = await _jogoRepository.ListarPublicadosAsync(titulo, genero, cancellationToken);
        var promocoesVigentes = await _promocaoRepository.ListarVigentesAsync(agora, cancellationToken);

        return jogos.Select(jogo =>
        {
            var promocao = promocoesVigentes.FirstOrDefault(p => p.JogoIds.Contains(jogo.Id));
            var precoComDesconto = promocao is null || jogo.Preco is null
                ? (decimal?)null
                : Math.Round(jogo.Preco.Value * (1 - promocao.PercentualDesconto / 100m), 2);

            return JogoResponse.DeJogo(jogo, precoComDesconto);
        }).ToList();
    }

    public async Task<JogoResponse> ObterPublicadoPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogoRepository.ObterPorIdAsync(id, cancellationToken);

        if (jogo is null || jogo.Status != StatusJogo.Publicado)
            throw new RecursoNaoEncontradoException("JOGO_NAO_ENCONTRADO", "Jogo não encontrado.");

        var agora = DateTime.UtcNow;
        var promocoesVigentes = await _promocaoRepository.ListarVigentesAsync(agora, cancellationToken);
        var promocao = promocoesVigentes.FirstOrDefault(p => p.JogoIds.Contains(jogo.Id));
        var precoComDesconto = promocao is null || jogo.Preco is null
            ? (decimal?)null
            : Math.Round(jogo.Preco.Value * (1 - promocao.PercentualDesconto / 100m), 2);

        return JogoResponse.DeJogo(jogo, precoComDesconto);
    }

    /// <summary>Consulta para aquisição (comunicação entre microsserviços).</summary>
    public async Task<JogoParaAquisicaoResponse?> ObterParaAquisicaoAsync(Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogoRepository.ObterPorIdAsync(jogoId, cancellationToken);

        if (jogo is null || jogo.Status != StatusJogo.Publicado || jogo.Preco is null)
            return null;

        var agora = DateTime.UtcNow;
        var promocoesVigentes = await _promocaoRepository.ListarVigentesAsync(agora, cancellationToken);
        var promocao = promocoesVigentes.FirstOrDefault(p => p.JogoIds.Contains(jogo.Id));

        var precoEfetivo = promocao is null
            ? jogo.Preco.Value
            : Math.Round(jogo.Preco.Value * (1 - promocao.PercentualDesconto / 100m), 2);

        return new JogoParaAquisicaoResponse(jogo.Id, true, precoEfetivo);
    }

    /// <summary>Lista todos os jogos (para administração).</summary>
    public async Task<IReadOnlyList<JogoResponse>> ListarTodosAsync(CancellationToken cancellationToken = default)
    {
        var jogos = await _jogoRepository.ListarTodosAsync(cancellationToken);
        return jogos.Select(j => JogoResponse.DeJogo(j)).ToList();
    }

    /// <summary>
    /// Inicia uma compra de jogo. Publica OrderPlacedEvent para processamento.
    /// O jogo NÃO é adicionado à biblioteca até que o pagamento seja aprovado.
    /// </summary>
    public async Task<CompraIniciadaResponse> IniciarCompraAsync(Guid jogoId, Guid userId, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogoRepository.ObterPorIdAsync(jogoId, cancellationToken);

        if (jogo is null || jogo.Status != StatusJogo.Publicado)
            throw new RecursoNaoEncontradoException("JOGO_NAO_ENCONTRADO", "Jogo não encontrado ou não está disponível para compra.");

        if (jogo.Preco is null)
            throw new RegraDeNegocioException("JOGO_SEM_PRECO", "Este jogo não possui preço definido.");

        // Calcular preço efetivo (com promoção se houver)
        var agora = DateTime.UtcNow;
        var promocoesVigentes = await _promocaoRepository.ListarVigentesAsync(agora, cancellationToken);
        var promocao = promocoesVigentes.FirstOrDefault(p => p.JogoIds.Contains(jogo.Id));

        var precoEfetivo = promocao is null
            ? jogo.Preco.Value
            : Math.Round(jogo.Preco.Value * (1 - promocao.PercentualDesconto / 100m), 2);

        // Gerar OrderId para rastreio
        var orderId = Guid.NewGuid();

        // Publicar evento de pedido - o jogo só será adicionado à biblioteca após aprovação do pagamento
        var orderEvent = new OrderPlacedEvent(
            OrderId: orderId,
            UserId: userId,
            GameId: jogo.Id,
            GameTitle: jogo.Titulo,
            Price: precoEfetivo,
            OcorridoEm: agora);

        await _eventPublisher.PublishAsync(orderEvent);

        _logger.LogInformation(
            "Compra iniciada - OrderId: {OrderId}, UserId: {UserId}, GameId: {GameId}, Price: {Price}",
            orderId, userId, jogo.Id, precoEfetivo);

        return new CompraIniciadaResponse(
            OrderId: orderId,
            GameId: jogo.Id,
            GameTitle: jogo.Titulo,
            Price: precoEfetivo,
            Status: "PendenteAprovacao",
            Mensagem: "Compra iniciada com sucesso. Aguardando aprovação do pagamento.");
    }

    private async Task<Jogo> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await _jogoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("JOGO_NAO_ENCONTRADO", "Jogo não encontrado.");
}
