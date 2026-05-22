# OsFacil — Sistema de Gestão de Ordens de Serviço

**OsFacil** é uma Web API desenvolvida em **.NET 8** para automação e gerenciamento de oficinas mecânicas. O sistema controla clientes, veículos, ordens de serviço, funcionários e itens de serviço, com integrações assíncronas, auditoria NoSQL, autenticação JWT e observabilidade completa.

> Projeto acadêmico — Análise e Desenvolvimento de Sistemas — **FIAP 2026**

---

## Integrantes

| Nome | RM |
|---|---|
| Renato Kenji Sugaki | RM-559810 |
| Gabriel Wu Castro | RM-560210 |
| Fabio Eduardo | RM-560416 |

---

## Sumário

1. [Arquitetura](#arquitetura)
2. [Tecnologias](#tecnologias)
3. [Funcionalidades](#funcionalidades)
4. [Pré-requisitos e Instalação](#pré-requisitos-e-instalação)
5. [Configuração](#configuração)
6. [Endpoints da API](#endpoints-da-api)
7. [Autenticação JWT](#autenticação-jwt)
8. [Paginação, Filtros e HATEOAS](#paginação-filtros-e-hateoas)
9. [MongoDB — Auditoria](#mongodb--auditoria)
10. [Health Checks](#health-checks)
11. [Testes](#testes)
12. [Swagger / OpenAPI](#swagger--openapi)

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        Clientes HTTP                            │
│                  (Swagger UI / Postman / App)                   │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTPS / JWT Bearer
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core 8 Web API                         │
│                                                                 │
│  ┌─────────────┐  ┌────────────────────┐  ┌─────────────────┐  │
│  │  Controllers│  │  JWT Middleware     │  │  Health Checks  │  │
│  │  (REST)     │  │  (Authentication)  │  │  /healthz       │  │
│  └──────┬──────┘  └────────────────────┘  └─────────────────┘  │
│         │                                                       │
│  ┌──────▼──────┐  ┌────────────────────┐  ┌─────────────────┐  │
│  │  AutoMapper │  │  Repository<T>      │  │  TokenService   │  │
│  │  (Profiles) │  │  (IRepository<T>)  │  │  (JWT gen.)     │  │
│  └──────┬──────┘  └──────────┬─────────┘  └─────────────────┘  │
│         │                   │                                   │
└─────────┼───────────────────┼───────────────────────────────────┘
          │                   │
          ▼                   ▼
┌──────────────────┐  ┌───────────────────┐  ┌──────────────────┐
│  Oracle Database │  │  MongoDB           │  │  RabbitMQ        │
│  (EF Core 8)     │  │  (Audit Logs)      │  │  (Mensageria)    │
│                  │  │  IMongoAuditService│  │  Producer /      │
│  OS_USUARIOS     │  │  AuditLogs coleção │  │  Consumer        │
│  OS_CARROS       │  └───────────────────┘  └──────────────────┘
│  OS_FUNCIONARIOS │
│  OS_ORDENS_SERV  │  ┌──────────────────────────────────────────┐
│  OS_ITENS_SERV   │  │  Observabilidade (Serilog + OpenTelemetry)│
└──────────────────┘  │  Logs estruturados → arquivo diário      │
                      │  Tracing / Metrics → console exporter    │
                      └──────────────────────────────────────────┘
```

### Camadas da Solução

| Camada | Responsabilidade |
|---|---|
| **Controllers** | Recebem requests HTTP, aplicam autorização, retornam respostas com HATEOAS |
| **Services / TokenService** | Geração de tokens JWT |
| **Repositories** | Abstração de acesso a dados com `IRepository<T>` genérico |
| **Models** | Entidades de domínio mapeadas para Oracle via EF Core |
| **DTOs** | Objetos de entrada (`Request`) e saída (`Response`) imutáveis (records) |
| **AutoMapper Profiles** | Mapeamento bidirecional DTO ↔ Model |
| **MongoDB** | Serviço de auditoria assíncrona com degradação graciosa |
| **Messaging** | `RabbitMqProducer` e `RabbitMqConsumer` para eventos de domínio |
| **HealthChecks** | Verificação de disponibilidade do Oracle (EF Core) e MongoDB |

---

## Tecnologias

| Categoria | Tecnologia | Versão |
|---|---|---|
| Framework | .NET / ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0.11 |
| Banco Relacional | Oracle Database | oracle.fiap.com.br |
| Banco NoSQL | MongoDB | 2.28.0 (driver) |
| Mensageria | RabbitMQ.Client | 6.8.1 |
| Autenticação | JWT Bearer | 8.0.0 |
| Documentação | Swashbuckle / OpenAPI | 6.6.2 |
| Mapeamento | AutoMapper | 16.1.1 |
| Hash de Senha | BCrypt.Net-Next | 4.1.0 |
| Logging | Serilog + Sinks.File | 8.0.0 |
| Tracing | OpenTelemetry | 1.15.1 |
| Testes | xUnit + Moq + TestHost | 2.9.x |

---

## Funcionalidades

| Módulo | Operações |
|---|---|
| **Usuários** | CRUD completo (cadastro público, demais protegidos) |
| **Veículos (Carros)** | CRUD com validação de proprietário |
| **Funcionários** | CRUD com campos de cargo e salário |
| **Ordens de Serviço** | CRUD + atualização de status (Aberta → EmAndamento → Concluída → Cancelada) |
| **Itens de Serviço** | CRUD vinculado a OS, cálculo automático de total |
| **Auditoria** | Log de criação/atualização/remoção persistido no MongoDB |
| **Auth** | Login com e-mail/senha retornando JWT Bearer |
| **Health Checks** | `/healthz` para Oracle e MongoDB |

---

## Pré-requisitos e Instalação

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Acesso à rede FIAP (para Oracle) ou conexão VPN
- MongoDB local ou Atlas (opcional — auditoria degrada graciosamente se indisponível)

### 1. Clonar o repositório

```bash
git clone https://github.com/seu-usuario/OsFacil.git
cd OsFacil
```

### 2. Subir RabbitMQ via Docker

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

Painel de gerenciamento: [http://localhost:15672](http://localhost:15672) — `guest` / `guest`

### 3. Subir MongoDB via Docker (opcional)

```bash
docker run -d --name mongodb \
  -p 27017:27017 \
  mongo:7
```

### 4. Restaurar pacotes e executar

```bash
dotnet restore
dotnet run --project OsFacil
```

A API estará disponível em `http://localhost:5066` (ou a porta configurada).  
Swagger UI: `http://localhost:5066/swagger`

---

## Configuração

Arquivo: `OsFacil/appsettings.json`

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=<RM>;Password=<senha>;Data Source=oracle.fiap.com.br:1521/ORCL"
  },
  "Jwt": {
    "Key": "OsFacil@SuperSecretKey#2026$FIAP!NetCore8",
    "Issuer": "OsFacilAPI",
    "Audience": "OsFacilClients",
    "ExpiracaoHoras": "8"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "OsFacilDB",
    "AuditLogsCollection": "AuditLogs"
  }
}
```


---

## Endpoints da API

### Autenticação

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | Público | Login — retorna token JWT |

**Exemplo de login:**
```json
POST /api/auth/login
{
  "email": "usuario@email.com",
  "senha": "minhasenha"
}
```
**Resposta:**
```json
{
  "token": "eyJhbGci...",
  "expiracao": "2026-05-21T18:00:00Z",
  "nome": "Nome do Usuário"
}
```

---

### Usuários

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/usuarios` | Público | Cadastra novo usuário |
| `GET` | `/api/usuarios` | JWT | Lista com paginação |
| `GET` | `/api/usuarios/{id}` | JWT | Obtém por ID |
| `PUT` | `/api/usuarios/{id}` | JWT | Atualiza dados |
| `DELETE` | `/api/usuarios/{id}` | JWT | Remove usuário |

---

### Veículos (Carros)

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/carros` | JWT | Cadastra veículo |
| `GET` | `/api/carros` | JWT | Lista com paginação |
| `GET` | `/api/carros/{id}` | JWT | Obtém por ID |
| `PUT` | `/api/carros/{id}` | JWT | Atualiza dados |
| `DELETE` | `/api/carros/{id}` | JWT | Remove veículo |

**Formato da placa:** `^[A-Z]{3}\d[A-Z\d]\d{2}$` (Mercosul e padrão antigo)

---

### Funcionários

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/funcionarios` | JWT | Cadastra funcionário |
| `GET` | `/api/funcionarios` | JWT | Lista com paginação |
| `GET` | `/api/funcionarios/{id}` | JWT | Obtém por ID |
| `PUT` | `/api/funcionarios/{id}` | JWT | Atualiza dados |
| `DELETE` | `/api/funcionarios/{id}` | JWT | Remove funcionário |

---

### Ordens de Serviço

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/ordemservicos` | JWT | Cria ordem de serviço |
| `GET` | `/api/ordemservicos` | JWT | Lista com paginação |
| `GET` | `/api/ordemservicos/{id}` | JWT | Obtém por ID |
| `PUT` | `/api/ordemservicos/{id}` | JWT | Atualiza dados |
| `PATCH` | `/api/ordemservicos/{id}/status` | JWT | Atualiza status |
| `DELETE` | `/api/ordemservicos/{id}` | JWT | Remove OS |

**Status válidos:** `Aberta`, `EmAndamento`, `Concluida`, `Cancelada`

---

### Itens de Serviço

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/itemservicos` | JWT | Adiciona item a uma OS |
| `GET` | `/api/itemservicos/ordem/{ordemId}` | JWT | Lista itens de uma OS |
| `GET` | `/api/itemservicos/{id}` | JWT | Obtém item por ID |
| `PUT` | `/api/itemservicos/{id}` | JWT | Atualiza item |
| `DELETE` | `/api/itemservicos/{id}` | JWT | Remove item |

---

### Auditoria

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `GET` | `/api/audit` | JWT | Lista logs do MongoDB |
| `GET` | `/api/audit?entidade=Carro` | JWT | Filtra por entidade |

---

### Monitoramento

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `GET` | `/healthz` | Público | Status geral da API |
| `GET` | `/healthz/ready` | Público | Oracle + MongoDB |
| `GET` | `/healthz/live` | Público | Liveness da aplicação |

---

## Autenticação JWT

Todos os endpoints (exceto `POST /api/usuarios` e `POST /api/auth/login`) exigem um token JWT no cabeçalho:

```
Authorization: Bearer <token>
```

**Fluxo:**
1. `POST /api/usuarios` — cria sua conta
2. `POST /api/auth/login` — obtém o token JWT
3. Use o token nos demais requests (válido por 8 horas)

No **Swagger UI**, clique no botão **Authorize** (cadeado) e insira `Bearer <seu_token>`.

---

## Paginação, Filtros e HATEOAS

### Parâmetros de paginação (query string)

| Parâmetro | Tipo | Padrão | Descrição |
|---|---|---|---|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx. 50) |
| `orderBy` | string | — | Campo para ordenar |
| `orderDir` | string | `asc` | Direção: `asc` ou `desc` |
| `filter` | string | — | Filtro de texto livre |

**Exemplo:**
```
GET /api/carros?page=1&pageSize=5&orderBy=placa&filter=ABC
```

**Resposta paginada:**
```json
{
  "data": [
    {
      "data": { "id": 1, "marca": "Honda", "modelo": "Civic", "placa": "ABC1D23" },
      "links": [
        { "href": "/api/carros/1", "rel": "self", "method": "GET" },
        { "href": "/api/carros/1", "rel": "update", "method": "PUT" },
        { "href": "/api/carros/1", "rel": "delete", "method": "DELETE" }
      ]
    }
  ],
  "page": 1,
  "pageSize": 5,
  "totalCount": 1,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

---

## MongoDB — Auditoria

Todas as operações de criação, atualização e remoção são registradas automaticamente no MongoDB na coleção `AuditLogs`.

**Estrutura de um log:**
```json
{
  "_id": "ObjectId",
  "entidade": "Carro",
  "operacao": "CRIACAO",
  "entidadeId": 42,
  "usuarioEmail": "cliente@email.com",
  "detalhes": "Placa: ABC1D23",
  "timestamp": "2026-05-21T10:30:00Z"
}
```

> Se o MongoDB estiver indisponível a API continua funcionando normalmente — o erro é logado via Serilog mas não propaga para o cliente.

---

## Health Checks

```
GET /healthz
```

```json
{
  "status": "Healthy",
  "checks": {
    "oracle-db": "Healthy",
    "mongodb": "Healthy"
  }
}
```

---

## Testes

### Executar todos os testes

```bash
dotnet test
```

### Executar apenas testes unitários

```bash
dotnet test --filter "Unit"
```

### Executar apenas testes de integração

```bash
dotnet test --filter "Integration"
```

### Cobertura atual: **42 testes — 100% aprovados**

| Tipo | Quantidade | Camadas cobertas |
|---|---|---|
| Unitários | 22 | Controllers (Create, Delete, GetById, GetAll) |
| Integração | 20 | Fluxo completo HTTP: paginação, HATEOAS, CRUD, erro 404/400 |

### Arquitetura dos testes

```
OsFacil.Tests/
├── Unit/
│   ├── CarrosControllerTests.cs
│   ├── FuncionariosControllerTests.cs
│   ├── ItemServicosControllerTests.cs
│   ├── OrdemServicosControllerTests.cs
│   └── UsuariosControllerTests.cs
├── Integration/
│   ├── CarrosIntegrationTests.cs
│   ├── FuncionariosIntegrationTests.cs
│   ├── ItemServicoIntegrationTests.cs
│   ├── OrdemServicoIntegrationTests.cs
│   └── UsuariosIntegrationTests.cs
├── Helpers/
│   ├── JwtTestHelper.cs        ← gera tokens JWT válidos para os testes
│   └── JsonOptions.cs          ← configuração de deserialização
└── CustomWebApplicationFactory.cs  ← TestServer com InMemory DB isolado por classe
```

**Padrões aplicados:**
- **AAA** (Arrange / Act / Assert) em todos os testes
- **IClassFixture** com `CustomWebApplicationFactory` para isolar banco por classe de teste
- In-memory database com nome único por instância (`Guid.NewGuid()`)
- Mocks de `RabbitMqProducer` e `IMongoAuditService` para isolamento total
- JWT válido gerado em `JwtTestHelper` com a mesma chave do servidor de teste

---

## Swagger / OpenAPI

Disponível em `http://localhost:5066/swagger` com:

- Descrição XML dos endpoints e modelos
- Suporte a autenticação JWT Bearer (botão **Authorize**)
- Exemplos de request/response para todos os endpoints
- Agrupamento por controller

Para exportar a especificação OpenAPI (JSON):
```
GET /swagger/v1/swagger.json
```

---

## Logging Estruturado

Logs são gravados em `Logs/osfacil_log-{data}.txt` com rotação diária via **Serilog**. Exemplos:

```
[INF] Iniciando a API OsFacil...
[INF] Carro ABC1D23 cadastrado.
[INF] Ordem de Serviço 15 criada para o carro ID 3.
[WRN] UsuarioId inexistente: 9999
[ERR] Erro ao excluir carro 5. <exception>
```

Rastreamento distribuído via **OpenTelemetry** (ASP.NET Core + EF Core instrumentation, exporter console).

---

## Mensageria (RabbitMQ)

Eventos publicados automaticamente ao criar/atualizar/remover entidades:

| Evento | Formato |
|---|---|
| Usuário criado | `USUARIO_CADASTRADO\|Id:N\|Nome:X\|Email:Y` |
| Carro cadastrado | `CARRO_CADASTRADO\|Id:N\|Placa:X\|Dono:Y` |
| OS criada | `OS_CRIADA\|Id:N\|Descricao:X\|Valor:Y` |
| Item adicionado | `ITEM_ADICIONADO\|Id:N\|Desc:X\|Total:Y` |

O `RabbitMqConsumer` processa as mensagens de forma assíncrona em background.
