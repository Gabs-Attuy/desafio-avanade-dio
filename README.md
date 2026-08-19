# E-Commerce Microservices DIO

Sistema backend desenvolvido com arquitetura de microserviços para gerenciamento de estoque, autenticação de usuários e processamento de vendas.

O projeto foi desenvolvido como desafio técnico do bootcamp Avanade - Back-end com .NET e IA, com o objetivo de aplicar conceitos de arquitetura distribuída utilizando **.NET 8**, **ASP.NET Core**, **Entity Framework Core**, **SQL Server**, **RabbitMQ**, **JWT**, **API Gateway**, **Docker** e **Docker Compose**.

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
                         |       :8080         |
                         └──────────┬──────────┘
                                    │
                 ┌──────────────────┼──────────────────┐
                 │                  │                  │
                 ▼                  ▼                  ▼
        ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
        │   AuthService   │ │InventoryService │ │  SalesService   │
        |      :8080      | |      :8080      | |      :8080      |
        │                 │ │                 │ │                 │
        │ Users           │ │ Products        │ │  Orders         │
        │ Authentication  │ │                 │ │                 │
        │ JWT             │ │                 │ │                 │
        └───────┬─────────┘ └────────▲────────┘ └────────┬────────┘
                |                    │                   │
                |             ┌──────┴────┐              │
                │             │  RabbitMQ │◄─────────────┘
                │             │   :5672   │
                │             └───────────┘
                │
                └────────────────┬─────────────────────────────┐
                                 │                             │
                                 ▼                             ▼
                           ┌────────────────────────────────────────────┐
                           │                 SQL Server                 │
                           │                                            │
                           │   ┌──────────┐ ┌──────────┐ ┌───────────┐  │
                           │   │  AuthDb  │ │ SalesDb  │ │InventoryDb│  │
                           │   │          │ │          │ │           |  │
                           │   └──────────┘ └──────────┘ └───────────┘  │
                           └────────────────────────────────────────────┘
```
Cada microserviço possui seu próprio banco de dados, mantendo o isolamento dos dados entre os serviços:

```text
AuthService       → AuthDb
SalesService      → SalesDb
InventoryService  → InventoryDb
```

O **API Gateway** atua como ponto central de entrada da aplicação e utiliza o YARP para encaminhar as requisições ao microserviço responsável.

A comunicação entre `SalesService` e `InventoryService` combina comunicação síncrona via HTTP e comunicação assíncrona através do RabbitMQ.

Após a criação de um pedido, um evento é publicado para que a atualização do estoque seja processada de maneira assíncrona.

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
* Docker
* Docker Compose
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

O serviço utiliza o `AuthDb` para persistência dos usuários.

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

O serviço utiliza o `InventoryDb` para persistência dos produtos e estoques.

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

O serviço utiliza o `SalesDb` para persistência dos pedidos.

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

## 🐳 Containerização

A aplicação foi completamente containerizada utilizando Docker e Docker Compose.

O ambiente de execução é composto por:

```text
┌──────────────────────────────────────────────┐
│              Docker Compose                  │
│                                              │
│   ┌─────────────┐     ┌─────────────┐        │
│   │ API Gateway │     │  RabbitMQ   │        │
│   │    :8080    │     │    :5672    │        │
│   └──────┬──────┘     └──────▲──────┘        │
│          │                   │               │
│     ┌────┼────────┐          │               │
│     ▼    ▼        ▼          │               │
│   Auth  Sales  Inventory ────┘               │
│                                              │
│              ┌──────────────┐                │
│              │ SQL Server   │                │
│              │    :1433     │                │
│              └──────────────┘                │
│                                              │
└──────────────────────────────────────────────┘
```

O docker-compose.yml é responsável por orquestrar:

* AuthService;
* InventoryService;
* SalesService;
* API Gateway;
* RabbitMQ;
* SQL Server.
  
**Comunicação entre containers**

Os serviços utilizam os nomes dos containers para comunicação interna.

Por exemplo:

```text
SalesService
     │
     └──→ http://inventory-service:8080
```

e:

```text
InventoryService
     │
     └──→ rabbitmq:5672
```

O SQL Server também é acessado através do nome do serviço:

```text
Server=sqlserver,1433
```

Isso evita dependências de `localhost` entre containers.

**Bancos independentes**

Embora os bancos utilizem a mesma instância do SQL Server, cada microserviço possui seu próprio database:

```text
SQL Server
│
├── AuthDb
├── InventoryDb
└── SalesDb
```

Essa separação mantém o isolamento lógico dos dados entre os microserviços.

**Persistência**

Os dados do SQL Server são armazenados através de um volume Docker:

```text
volumes:
  sqlserver-data:
```

Dessa forma, a recriação dos containers não remove os dados persistidos.

**Migrations**

Os microserviços aplicam automaticamente as migrations do Entity Framework Core durante a inicialização.

O `AuthService`, além de aplicar as migrations, executa o processo de seed para garantir a existência do usuário administrador inicial.

Isso permite iniciar o ambiente sem a necessidade de executar manualmente:

```bash
dotnet ef database update
```

---

## 🔐 Variáveis de ambiente

Informações sensíveis não são armazenadas diretamente no docker-compose.yml.

O projeto utiliza um arquivo .env para configurar:

```text
MSSQL_SA_PASSWORD
JWT_KEY
RABBITMQ_USER
RABBITMQ_PASSWORD
SEED_ADMIN_PASSWORD
```

Um arquivo `.env.example` é disponibilizado como referência.

Para configurar o ambiente:

```bash
cp .env.example .env
```

Depois, preencha os valores necessários no arquivo `.env`.

> ⚠️ **Importante:** o arquivo `.env` não deve ser versionado no Git.

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
│   │   ├── Dockerfile
│   │   └── ...
│   │
│   ├── AuthService/
│   │   ├── Dockerfile
│   │   └── ...
│   │
│   ├── InventoryService/
│   │   ├── Dockerfile
│   │   └── ...
│   │
│   └── SalesService/
│       ├── Dockerfile
│       └── ...
│
├── tests/
│   ├── AuthService.Tests/
│   ├── InventoryService.Tests/
│   └── SalesService.Tests/
│
├── .env.example
├── .gitignore
├── docker-compose.yml
├── ECommerceMicroservices.sln
└── README.md
```

Cada microserviço possui suas próprias responsabilidades, configurações, Dockerfile e acesso aos recursos necessários para seu funcionamento.

---

## ▶️ Executando o projeto

### Opção 1 — Docker Compose

A forma recomendada de executar a aplicação é utilizando Docker Compose.

**Pré-requisitos**
* Docker;
* Docker Compose;
* Git.
  
**1. Clone o repositório**

```bash
git clone https://github.com/Gabs-Attuy/desafio-avanade-dio.git
```

Acesse o diretório:

```bash
cd desafio-avanade-dio
```

**2. Configure as variáveis de ambiente**

Crie o `.env` a partir do arquivo de exemplo:

```bash
cp .env.example .env
```

Configure as variáveis:

```env
MSSQL_SA_PASSWORD=SuaSenha
JWT_KEY=SuaChaveJWT
RABBITMQ_USER=SeuUsuario
RABBITMQ_PASSWORD=SuaSenha
SEED_ADMIN_PASSWORD=SuaSenha
```

**3. Inicie a aplicação**

```bash
docker compose up -d --build
```

O Docker Compose irá iniciar:

```text
SQL Server
RabbitMQ
AuthService
InventoryService
SalesService
API Gateway
```

**4. Verifique os containers**

```bash
docker compose ps
```

Para acompanhar os logs:

```bash
docker compose logs -f
```

**5. Acesse os serviços**

**API Gateway**

```http
http://localhost:8080
```

**InventoryService**

```http
http://localhost:8081/swagger
```

**SalesService**

```http
http://localhost:8082/swagger
```

**AuthService**

```http
http://localhost:8083/swagger
```

**RabbitMQ Management**

```http
http://localhost:15672
```

O Gateway deve ser utilizado como principal ponto de entrada da aplicação:

```text
/api/auth/*
/api/inventory/*
/api/sales/*
```

**Credenciais Iniciais**

O `AuthService` cria automaticamente um usuário administrador durante a inicialização.

```text
E-mail:
admin@ecommerce.com

Senha:
valor configurado em SEED_ADMIN_PASSWORD
```

A senha não é armazenada em texto puro. O projeto utiliza `PasswordHasher<User>` para gerar o hash antes da persistência.

**Parando a aplicação**

Para parar e remover os containers:

```bash
docker compose down
```

O volume do SQL Server não é removido, portanto os dados permanecem disponíveis para a próxima inicialização.

Para remover também os volumes e recriar completamente o ambiente:

```bash
docker compose down -v
```

> ⚠️ O comando acima remove os dados persistidos do SQL Server.

---

### Opção 2 — Executando sem Docker

Também é possível executar os projetos diretamente utilizando o .NET SDK.

**Pré-requisitos**

Para executar a aplicação, é necessário possuir:

* .NET 8 SDK;
* SQL Server;
* RabbitMQ;
* Git.

**1. Clone o repositório**

```bash
git clone https://github.com/Gabs-Attuy/desafio-avanade-dio.git
```

Acesse o diretório:

```bash
cd desafio-avanade-dio
```

**2. Configure os bancos de dados**

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

**3. Configure o JWT**

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

**4. Inicie o RabbitMQ**

Certifique-se de que o servidor RabbitMQ esteja disponível de acordo com as configurações utilizadas pelos serviços.
No meu caso, criei um container com a imagem do RabbitMQ com o seguinte comando:

```bash
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:4-management
```

**5. Execute os microserviços**

Inicie:

```text
AuthService
InventoryService
SalesService
ApiGateway
```

As portas utilizadas podem ser consultadas nos respectivos arquivos `launchSettings.json`.

**6. Acesse a aplicação**

Utilize o endereço do API Gateway como ponto principal de entrada para realizar as requisições.

Cada microserviço também disponibiliza sua própria documentação através do Swagger.

---

## 💡 Decisões de arquitetura

Durante o desenvolvimento, algumas decisões foram tomadas para melhorar a estrutura da solução:

**Soft delete de produtos:** produtos são desativados em vez de removidos fisicamente, preservando informações históricas.

**Identificação do usuário através do JWT:** pedidos são associados ao ID presente no token autenticado, evitando que o cliente informe arbitrariamente o proprietário do pedido.

**Autorização distribuída:** cada microserviço valida o JWT e controla o acesso aos seus próprios recursos.

**API Gateway focado em roteamento:** o Gateway utiliza YARP como ponto central de entrada sem concentrar regras de negócio ou autorização.

**Separação dos bancos:** cada microserviço possui seu próprio database, reduzindo o acoplamento entre os serviços.

**Comunicação híbrida:** comunicação síncrona via HTTP é utilizada quando o `SalesService` precisa consultar informações do `InventoryService`, enquanto eventos RabbitMQ são utilizados para atualização assíncrona do estoque.

**Containerização:** Docker Compose foi utilizado para reproduzir todo o ambiente da aplicação, incluindo microserviços e infraestrutura necessária.

**Migrations automáticas:** os serviços aplicam migrations durante a inicialização, reduzindo a necessidade de configuração manual do banco de dados.

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
* Containerização com Docker;
* Orquestração com Docker Compose;
* Comunicação entre containers;
* Persistência através de volumes;

---

## 🔮 Melhorias futuras

Como possíveis evoluções do projeto:

* Implementação do padrão Outbox para garantir maior consistência na publicação de eventos;
* Dead Letter Queue para tratamento de mensagens que falharem;
* Retry policies e resiliência na comunicação entre serviços;
* Observabilidade centralizada com logs, métricas e tracing distribuído;
* Testes de integração e end-to-end;
* CI/CD para execução automática dos testes.

---

## 👨‍💻 Autor

**Gabriel Santos Attuy**

Projeto desenvolvido como parte de um desafio técnico de bootcamp com foco em arquitetura de microserviços com .NET.
