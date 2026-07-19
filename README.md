# E-Commerce Microservices DIO

Sistema backend desenvolvido com arquitetura de microserviços para gerenciamento de estoque, autenticação de usuários e processamento de vendas.

O projeto foi desenvolvido como desafio técnico do bootcamp Avanade - Back-end com .NET e IA, com o objetivo de aplicar conceitos de arquitetura distribuída utilizando **.NET 8**, **ASP.NET Core**, **Entity Framework Core**, **SQL Server**, **RabbitMQ**, **JWT** e **API Gateway**.

---

## 📐 Arquitetura

A aplicação é composta por três microserviços independentes e um API Gateway:

```text
                         ┌─────────────────────┐
                         │       Cliente       │
                         └──────────┬──────────┘
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │     API Gateway     │
                         │        YARP         │
                         └──────────┬──────────┘
                                    │
                 ┌──────────────────┼──────────────────┐
                 │                  │                  │
                 ▼                  ▼                  ▼
        ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
        │   AuthService   │ │ InventoryService│ │   SalesService  │
        │                 │ │                 │ │                 │
        │ Users           │ │ Products        │ │ Orders          │
        │ Authentication  │ │                 │ │                 │
        │ JWT             │ │                 │ │                 │
        └─────────────────┘ └────────▲────────┘ └────────┬────────┘
                                     │                   │
                                     │    RabbitMQ       │
                                     └───────────────────┘
```

O **API Gateway** atua como ponto central de entrada da aplicação e utiliza o YARP para encaminhar as requisições ao microserviço responsável.

A comunicação entre `SalesService` e `InventoryService` utiliza RabbitMQ. Após a criação de um pedido, um evento é publicado para que a atualização do estoque seja processada de maneira assíncrona.

---

## 🚀 Tecnologias utilizadas

* C#
* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* RabbitMQ
* JWT Bearer Authentication
* YARP Reverse Proxy
* Swagger / OpenAPI
* MSTest
* Moq

---

## 🧩 Microserviços

### 🔐 AuthService

Responsável pelo gerenciamento de usuários e autenticação da aplicação.

Principais funcionalidades:

* Cadastro de usuários;
* Cadastro protegido de administradores;
* Autenticação por e-mail e senha;
* Hash seguro de senhas;
* Geração de tokens JWT;
* Autorização baseada em roles;

Os usuários possuem dois possíveis perfis:

```text
User
Admin
```

As credenciais autenticadas são utilizadas para acessar os endpoints protegidos dos demais serviços.

---

### 📦 InventoryService

Responsável pelo gerenciamento dos produtos e controle de estoque.

Principais funcionalidades:

* Cadastro de produtos;
* Consulta de produtos;
* Consulta individual por ID;
* Consulta de estoque;
* Atualização de produtos;
* Ativação e desativação de produtos;
* Atualização assíncrona do estoque.

Produtos não são excluídos fisicamente do banco de dados. Em vez disso, podem ser desativados, preservando seu histórico e impedindo novas vendas enquanto estiverem inativos.

A atualização do estoque ocorre após o recebimento de eventos publicados pelo `SalesService` através do RabbitMQ.

---

### 🛒 SalesService

Responsável pelo gerenciamento e processamento dos pedidos.

Principais funcionalidades:

* Criação de pedidos;
* Consulta de pedido por ID;
* Consulta dos pedidos do usuário autenticado;
* Consulta de todos os pedidos por administradores;
* Validação da disponibilidade dos produtos;
* Validação do status ativo do produto;
* Validação da quantidade disponível em estoque;
* Publicação de eventos no RabbitMQ.

Durante a criação de um pedido, o `SalesService` consulta o `InventoryService` para obter os dados atuais dos produtos e validar a operação.

Após o pedido ser persistido, é publicado um evento contendo os produtos e quantidades adquiridas.

---

## 📨 Comunicação assíncrona com RabbitMQ

A atualização do estoque utiliza uma arquitetura orientada a eventos.

O fluxo ocorre da seguinte maneira:

```text
1. Cliente cria um pedido
            │
            ▼
2. SalesService valida os produtos
            │
            ▼
3. Pedido é persistido
            │
            ▼
4. Evento OrderCreated é publicado
            │
            ▼
5. RabbitMQ recebe a mensagem
            │
            ▼
6. InventoryService consome o evento
            │
            ▼
7. Estoque dos produtos é atualizado
```

Essa abordagem reduz o acoplamento direto entre os serviços durante a atualização do estoque e permite que o processamento ocorra de forma assíncrona.

---

## 🔑 Autenticação e autorização

A aplicação utiliza JWT para autenticação.

Após o login, o `AuthService` gera um token contendo informações como:

* ID do usuário;
* E-mail;
* Nome;
* Role.

O token deve ser enviado nas requisições protegidas:

```http
Authorization: Bearer <token>
```

Cada microserviço é responsável por validar o JWT e aplicar suas regras de autorização.

Endpoints administrativos utilizam:

```csharp
[Authorize(Roles = "Admin")]
```

Enquanto endpoints destinados a qualquer usuário autenticado utilizam:

```csharp
[Authorize]
```

O API Gateway encaminha o header `Authorization` aos microserviços responsáveis.

---

## 🌐 API Gateway

O projeto utiliza YARP como API Gateway e Reverse Proxy.

O Gateway fornece um único ponto de entrada para os serviços:

```text
/api/auth/*       → AuthService
/api/inventory/*  → InventoryService
/api/sales/*      → SalesService
```

As rotas externas são transformadas antes de serem encaminhadas para os respectivos microserviços.

O Gateway é responsável pelo roteamento, enquanto autenticação, autorização e regras de negócio permanecem sob responsabilidade de cada serviço.

---

## 🧪 Testes automatizados

A aplicação possui testes automatizados utilizando MSTest e Moq.

A estrutura de testes é separada por microserviço:

```text
tests/
├── AuthService.Tests/
├── InventoryService.Tests/
└── SalesService.Tests/
```

### InventoryService

Os testes cobrem cenários como:

* Cadastro de produtos;
* Validação de preço;
* Validação de estoque;
* Consulta de produtos;
* Atualização de produtos;
* Ativação e desativação.

### SalesService

Os testes validam:

* Criação de pedidos;
* Pedidos sem itens;
* Produtos inexistentes;
* Estoque insuficiente;
* Produtos inativos;
* Cálculo do valor total;
* Associação do usuário ao pedido;
* Publicação de eventos;
* Consulta de pedidos;
* Atualização de status.

### AuthService

Os testes cobrem:

* Cadastro de usuários;
* Cadastro de administradores;
* E-mails duplicados;
* Hash de senha;
* Login válido;
* Credenciais inválidas;
* Geração de JWT;
* Claims do token;
* Configurações de issuer e audience;
* Expiração do token.

Para executar todos os testes:

```bash
dotnet test
```

---

## 📂 Estrutura do projeto

```text
.
├── src/
│   ├── ApiGateway/
│   ├── AuthService/
│   ├── InventoryService/
│   └── SalesService/
│
├── tests/
│   ├── AuthService.Tests/
│   ├── InventoryService.Tests/
│   └── SalesService.Tests/
│
└── ECommerceMicroservices.sln
```

Cada microserviço possui suas próprias responsabilidades, configurações e acesso aos recursos necessários para seu funcionamento.

---

## ▶️ Executando o projeto

### Pré-requisitos

Para executar a aplicação, é necessário possuir:

* .NET 8 SDK;
* SQL Server;
* RabbitMQ;
* Git.

### 1. Clone o repositório

```bash
git clone <URL_DO_REPOSITORIO>
```

Acesse o diretório:

```bash
cd <NOME_DO_REPOSITORIO>
```

### 2. Configure os bancos de dados

Configure as connection strings nos arquivos `appsettings.json` dos serviços que utilizam persistência.

Depois, aplique as migrations:

```bash
dotnet ef database update --project src/AuthService
```

```bash
dotnet ef database update --project src/InventoryService
```

```bash
dotnet ef database update --project src/SalesService
```

### 3. Configure o JWT

No `AuthService`, configure:

```json
"Jwt": {
  "Key": "SUA_CHAVE_SECRETA",
  "Issuer": "SEU_ISSUER",
  "Audience": "SUA_AUDIENCE",
  "ExpirationMinutes": 60
}
```

Os serviços que validam os tokens devem utilizar configurações compatíveis de assinatura, issuer e audience.

### 4. Inicie o RabbitMQ

Certifique-se de que o servidor RabbitMQ esteja disponível de acordo com as configurações utilizadas pelos serviços.
No meu caso, criei um container com a imagem do RabbitMQ com o seguinte comando:

```bash
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:4-management
```

### 5. Execute os microserviços

Inicie:

```text
AuthService
InventoryService
SalesService
ApiGateway
```

As portas utilizadas podem ser consultadas nos respectivos arquivos `launchSettings.json`.

### 6. Acesse a aplicação

Utilize o endereço do API Gateway como ponto principal de entrada para realizar as requisições.

Cada microserviço também disponibiliza sua própria documentação através do Swagger.

---

## 💡 Decisões de arquitetura

Durante o desenvolvimento, algumas decisões foram tomadas para melhorar a estrutura da solução:

**Soft delete de produtos:** produtos são desativados em vez de removidos fisicamente, preservando informações históricas.

**Identificação do usuário através do JWT:** pedidos são associados ao ID presente no token autenticado, evitando que o cliente informe arbitrariamente o proprietário do pedido.

**Autorização distribuída:** cada microserviço valida o JWT e controla o acesso aos seus próprios recursos.

**API Gateway focado em roteamento:** o Gateway utiliza YARP como ponto central de entrada sem concentrar regras de negócio ou autorização.

**Separação dos projetos de testes:** cada microserviço possui seu próprio projeto de testes, mantendo isolamento e organização.

---

## 📚 Aprendizados

O desenvolvimento deste projeto permitiu aplicar na prática conceitos importantes de sistemas distribuídos e desenvolvimento backend, incluindo:

* Arquitetura de microserviços;
* Separação de responsabilidades;
* Comunicação síncrona entre APIs;
* Comunicação assíncrona orientada a eventos;
* Mensageria com RabbitMQ;
* API Gateway e Reverse Proxy;
* Autenticação e autorização com JWT;
* Entity Framework Core e migrations;
* Injeção de dependências;
* Repository Pattern;
* Tratamento global de exceções;
* Testes unitários;
* Mock de dependências;
* Segurança e armazenamento de senhas.

---

## 🔮 Melhorias futuras

Como possíveis evoluções do projeto:

* Implementação do padrão Outbox para garantir maior consistência na publicação de eventos;
* Dead Letter Queue para tratamento de mensagens que falharem;
* Retry policies e resiliência na comunicação entre serviços;
* Containerização com Docker e Docker Compose;
* Observabilidade centralizada com logs, métricas e tracing distribuído;
* Testes de integração e end-to-end;
* CI/CD para execução automática dos testes.

---

## 👨‍💻 Autor

**Gabriel Santos Attuy**

Projeto desenvolvido como parte de um desafio técnico de bootcamp com foco em arquitetura de microserviços com .NET.
