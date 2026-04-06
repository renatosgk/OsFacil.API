#  OsFacil — Sistema de Gestão de Ordens de Serviço

**OsFacil** é uma Web API desenvolvida em **.NET 8** focada na automação e gerenciamento de oficinas mecânicas. O sistema permite controlar clientes, veículos, ordens de serviço e integrações assíncronas com mensageria.

---

##  Sobre o Projeto

Desenvolvido como parte do curso de **Análise e Desenvolvimento de Sistemas (ADS)** na **FIAP**, com arquitetura projetada com foco em:

- Escalabilidade
- Organização em camadas
- Boas práticas de desenvolvimento
- Testabilidade

---

##  Funcionalidades

| Módulo | Descrição |
|---|---|
|  **Usuários** | Cadastro e manutenção de clientes |
|  **Veículos** | Associação de veículos a proprietários |
|  **Ordens de Serviço** | Abertura, edição, acompanhamento e controle de status |
|  **Itens de Serviço** | Peças, mão de obra e cálculo automático de valores |
|  **Mensageria** | Integração com RabbitMQ e processamento assíncrono de eventos |

---

##  Tecnologias Utilizadas

| Categoria | Tecnologias |
|---|---|
| **Linguagem / Framework** | .NET 8, C# |
| **Banco de Dados** | Oracle Database, Entity Framework Core |
| **Mensageria** | RabbitMQ |
| **Infraestrutura** | Docker |
| **Testes** | xUnit, Moq |

---

##  Como Rodar o Projeto

###  Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Visual Studio 2022 ou VS Code

---

###  1. Subir o RabbitMQ via Docker
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

Acesse o painel de gerenciamento em: [http://localhost:15672](http://localhost:15672)

> **Credenciais padrão:** `guest` / `guest`

---

###  2. Restaurar pacotes e executar a API
```bash
# Restaurar pacotes NuGet
dotnet restore

# Compilar e executar a API
dotnet run --project OsFacil
```

---

###  3. Executar os testes
```bash
dotnet test
```

---

##  Endpoints Principais

| Recurso | Método | Endpoint |
|---|---|---|
| Usuários | `POST` | `/api/usuarios` |
| Carros | `POST` | `/api/carros` |
| Funcionários | `PUT` | `/api/funcionarios/{id}` |
| Ordem de Serviço | `POST` | `/api/ordemservico` |
| Status da OS | `PATCH` | `/api/ordemservico/{id}/status` |
| Itens da OS | `GET` | `/api/itemservico/ordem/{id}` |

##  Integrantes

| Nome | RM |
|---|---|
| Renato Kenji Sugaki | RM-559810 |
| Gabriel Wu Castro | RM-560210 |
| Fabio Eduardo | RM-560416 |
