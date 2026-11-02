# FCG Catalog API

API de Catálogo de Jogos - Microsserviço para gerenciamento de jogos e promoções da plataforma FIAP Cloud Games.

## Visão Geral

Este microsserviço é responsável pelo gerenciamento do catálogo de jogos, incluindo:
- Cadastro e gestão de jogos
- Publicação e despublicação de jogos
- Criação e gestão de promoções
- Consulta de catálogo (vitrine pública)
- Comunicação com outros microsserviços via RabbitMQ

## Tecnologias

- .NET 8.0
- Entity Framework Core (InMemory para dev, SQL Server para prod)
- RabbitMQ para mensageria
- JWT para autenticação
- Swagger/OpenAPI para documentação

## Estrutura do Projeto

```
fcg-catalog-api/
├── Controllers/          # Endpoints da API
│   ├── JogosController.cs
│   └── PromocoesController.cs
├── Data/                 # DbContext e configurações EF
│   ├── ApplicationDbContext.cs
│   ├── JogoConfiguration.cs
│   └── PromocaoConfiguration.cs
├── DTOs/                 # Data Transfer Objects
│   ├── JogoDtos.cs
│   └── PromocaoDtos.cs
├── Events/               # Eventos e RabbitMQ
│   ├── CatalogEvents.cs
│   ├── IEventPublisher.cs
│   ├── RabbitMQEventPublisher.cs
│   └── RabbitMQHealthCheck.cs
├── Exceptions/           # Exceções de domínio
│   ├── DomainException.cs
│   ├── FcgException.cs
│   ├── RecursoDuplicadoException.cs
│   ├── RecursoNaoEncontradoException.cs
│   └── RegraDeNegocioException.cs
├── Middlewares/          # Middleware de tratamento de erros
│   └── ExceptionHandlingMiddleware.cs
├── Models/               # Entidades de domínio
│   ├── Jogo.cs
│   ├── Promocao.cs
│   └── StatusJogo.cs
├── Repositories/         # Repositórios
│   ├── IJogoRepository.cs
│   ├── IPromocaoRepository.cs
│   ├── JogoRepository.cs
│   └── PromocaoRepository.cs
├── Services/             # Serviços de aplicação
│   ├── JogoService.cs
│   └── PromocaoService.cs
└── k8s/                  # Manifests Kubernetes
    ├── deployment.yaml
    └── service.yaml
```

## Endpoints

### Jogos (`/api/jogos`)

| Método | Endpoint | Descrição | Autenticação |
|--------|----------|-----------|--------------|
| GET | `/api/jogos` | Lista jogos publicados (catálogo) | Público |
| GET | `/api/jogos/{id}` | Obtém jogo publicado por ID | Público |
| GET | `/api/jogos/{id}/aquisicao` | Consulta para aquisição (interno) | Público |
| POST | `/api/jogos` | Cadastra novo jogo | Admin |
| PUT | `/api/jogos/{id}/ficha` | Preenche ficha técnica | Admin |
| PUT | `/api/jogos/{id}/preco` | Define preço | Admin |
| PUT | `/api/jogos/{id}/descricao` | Atualiza descrição | Admin |
| POST | `/api/jogos/{id}/publicacao` | Publica jogo | Admin |
| DELETE | `/api/jogos/{id}/publicacao` | Despublica jogo | Admin |

### Promoções (`/api/promocoes`)

| Método | Endpoint | Descrição | Autenticação |
|--------|----------|-----------|--------------|
| GET | `/api/promocoes` | Lista promoções vigentes | Público |
| POST | `/api/promocoes` | Cria nova promoção | Admin |
| POST | `/api/promocoes/{id}/jogos` | Aplica promoção a jogos | Admin |
| DELETE | `/api/promocoes/{id}` | Encerra promoção | Admin |

## Regras de Negócio

### Jogos

1. **Cadastro**: Jogo nasce com status `Rascunho`
2. **Ficha técnica**: Deve conter desenvolvedora e data de lançamento
3. **Publicação**: Requer ficha preenchida E preço definido
4. **Preço**: Deve ser >= 0 (jogos gratuitos permitidos)

### Promoções

1. **Percentual**: Entre 1% e 90%
2. **Vigência**: Data fim deve ser posterior à data início
3. **Aplicação**: Somente em jogos publicados
4. **Exclusividade**: Um jogo só pode ter uma promoção vigente por vez
5. **Encerramento**: Pode ser manual ou automático (por vigência)

## Eventos Publicados (RabbitMQ)

Exchange: `fcg.catalog.events` (Topic)

### Eventos de Jogo
- `catalog.jogocadastradoevent` - Jogo cadastrado
- `catalog.fichapreenchidaevent` - Ficha técnica preenchida
- `catalog.precodefinidoevent` - Preço definido
- `catalog.descricaoatualizadaevent` - Descrição atualizada
- `catalog.jogopublicadoevent` - Jogo publicado
- `catalog.jogodespublicadoevent` - Jogo despublicado

### Eventos de Promoção
- `catalog.promocaocriadaevent` - Promoção criada
- `catalog.promocaoaplicadaaojogoevent` - Promoção aplicada a jogo
- `catalog.promocaoencerradaevent` - Promoção encerrada

## Comunicação com Outros Microsserviços

### fcg-users-api
- Autenticação via JWT (mesmo secret compartilhado)
- Roles: `Administrador`, `Usuario`

### fcg-payments-api / fcg-library-api
- Endpoint `/api/jogos/{id}/aquisicao` para consulta de preço efetivo
- Retorna: `{ jogoId, publicado, precoEfetivo }`

## Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=FcgCatalogDb;..."
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "FCG-Users-API",
    "Audience": "FCG-Users-API"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "fcg.catalog.events"
  }
}
```

## Execução

### Desenvolvimento Local

```bash
# Restaurar dependências
dotnet restore

# Iniciar RabbitMQ (opcional)
docker-compose -f docker-compose.rabbitmq.yml up -d

# Executar API
dotnet run
```

### Docker

```bash
docker build -t fcg-catalog-api .
docker run -p 5000:8080 fcg-catalog-api
```

### Kubernetes

```bash
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

## Health Checks

- `/health` - Status da aplicação e conexão RabbitMQ

## Swagger

Disponível em `/swagger` quando em ambiente de desenvolvimento.
