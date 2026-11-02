using FcgCatalogApi.Data;
using FcgCatalogApi.DTOs;
using FcgCatalogApi.Events;
using FcgCatalogApi.Exceptions;
using FcgCatalogApi.Models;
using FcgCatalogApi.Repositories;

namespace FcgCatalogApi.Services;

public sealed class PromocaoService
{
    private readonly IPromocaoRepository _promocaoRepository;
    private readonly IJogoRepository _jogoRepository;
    private readonly ApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PromocaoService> _logger;

    public PromocaoService(
        IPromocaoRepository promocaoRepository,
        IJogoRepository jogoRepository,
        ApplicationDbContext context,
        IEventPublisher eventPublisher,
        ILogger<PromocaoService> logger)
    {
        _promocaoRepository = promocaoRepository;
        _jogoRepository = jogoRepository;
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CriarPromocaoResponse> CriarAsync(CriarPromocaoRequest request, CancellationToken cancellationToken = default)
    {
        var promocao = Promocao.CriarPromocao(request.Nome, request.Percentual, request.Inicio, request.Fim);

        await _promocaoRepository.AdicionarAsync(promocao, cancellationToken);
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new PromocaoCriadaEvent(promocao.Id, promocao.Nome, DateTime.UtcNow));
        _logger.LogInformation("Promoção criada: {PromocaoId} - {Nome}", promocao.Id, promocao.Nome);

        return new CriarPromocaoResponse(promocao.Id);
    }

    public async Task AplicarAJogosAsync(Guid promocaoId, AplicarPromocaoAJogosRequest request, CancellationToken cancellationToken = default)
    {
        var promocao = await _promocaoRepository.ObterPorIdAsync(promocaoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("PROMOCAO_NAO_ENCONTRADA", "Promoção não encontrada.");

        var agora = DateTime.UtcNow;

        foreach (var jogoId in request.JogoIds)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(jogoId, cancellationToken)
                ?? throw new RecursoNaoEncontradoException("JOGO_NAO_ENCONTRADO", $"Jogo {jogoId} não encontrado.");

            if (jogo.Status != StatusJogo.Publicado)
                throw new RegraDeNegocioException("JOGO_NAO_PUBLICADO", "Somente jogos publicados podem receber promoção.");

            if (await _promocaoRepository.ExistePromocaoVigenteParaJogoAsync(jogoId, agora, cancellationToken))
                throw new RecursoDuplicadoException("PROMOCAO_VIGENTE_EXISTENTE", "Este jogo já possui uma promoção vigente.");

            promocao.AplicarAoJogo(jogoId);
            await _eventPublisher.PublishAsync(new PromocaoAplicadaAoJogoEvent(promocaoId, jogoId, agora));
        }

        await _context.SalvarAlteracoesAsync(cancellationToken);
        _logger.LogInformation("Promoção {PromocaoId} aplicada a {Count} jogos", promocaoId, request.JogoIds.Count);
    }

    public async Task EncerrarAsync(Guid promocaoId, CancellationToken cancellationToken = default)
    {
        var promocao = await _promocaoRepository.ObterPorIdAsync(promocaoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("PROMOCAO_NAO_ENCONTRADA", "Promoção não encontrada.");

        promocao.EncerrarPromocao();
        await _context.SalvarAlteracoesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new PromocaoEncerradaEvent(promocaoId, DateTime.UtcNow));
        _logger.LogInformation("Promoção encerrada: {PromocaoId}", promocaoId);
    }

    public async Task<IReadOnlyList<PromocaoResponse>> ListarVigentesAsync(CancellationToken cancellationToken = default)
    {
        var promocoes = await _promocaoRepository.ListarVigentesAsync(DateTime.UtcNow, cancellationToken);
        return promocoes.Select(PromocaoResponse.DePromocao).ToList();
    }
}
