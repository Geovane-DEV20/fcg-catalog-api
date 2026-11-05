# FCG Catalog API

Microsserviço responsável pelo gerenciamento do catálogo de jogos e biblioteca de usuários da plataforma FIAP Cloud Games.

## Funcionalidades

- CRUD de jogos (Create, Read, Update, Delete)
- Listagem de jogos públicos
- Iniciar fluxo de compra de jogos (publica `OrderPlacedEvent`)
- Consumir evento `PaymentProcessedEvent` e adicionar jogo à biblioteca quando aprovado
- Gerenciar biblioteca de jogos dos usuários

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core (InMemory Database)
- JWT Bearer Authentication
- RabbitMQ.Client
- Swagger/OpenAPI

## Variáveis de Ambiente

- `Jwt__Secret`: Chave secreta para validação do JWT
- `RabbitMQ__Host`: Hostname do servidor RabbitMQ (padrão: localhost)
- `RabbitMQ__Port`: Porta do RabbitMQ (padrão: 5672)
- `RabbitMQ__Username`: Usuário do RabbitMQ (padrão: guest)
- `RabbitMQ__Password`: Senha do RabbitMQ (padrão: guest)

## Como Executar

### Com .NET CLI

```bash
dotnet restore
dotnet build
dotnet run
```

### Com Docker

```bash
docker build -t fcg-catalog-api .
docker run -p 8080:8080 fcg-catalog-api
```

## Endpoints

- `GET /api/games` - Listar jogos publicados
- `GET /api/games/{id}` - Obter detalhes de um jogo
- `POST /api/games` - Criar novo jogo (requer autenticação Admin)
- `PUT /api/games/{id}` - Atualizar jogo (requer autenticação Admin)
- `DELETE /api/games/{id}` - Remover jogo (requer autenticação Admin)
- `POST /api/games/{id}/purchase` - Iniciar compra de um jogo (requer autenticação)
- `GET /api/games/library` - Obter biblioteca do usuário (requer autenticação)

## Eventos Publicados

### OrderPlacedEvent
**Routing Key:** `order.placed`

```json
{
  "userId": "guid",
  "gameId": "guid",
  "price": 299.90,
  "orderId": "guid",
  "timestamp": "datetime"
}
```

## Eventos Consumidos

### PaymentProcessedEvent
**Routing Key:** `payment.processed`

```json
{
  "orderId": "guid",
  "userId": "guid",
  "gameId": "guid",
  "price": 299.90,
  "status": "Approved|Rejected",
  "timestamp": "datetime"
}
```

Quando o status é "Approved", o jogo é automaticamente adicionado à biblioteca do usuário.
