# FazaBoa API

FazaBoa API é uma aplicação backend desenvolvida em .NET 8.0 que fornece funcionalidades essenciais para um aplicativo móvel. A API foi projetada para gerenciar usuários, grupos, desafios, transações de moedas, recompensas e muito mais.

## Arquitetura do Projeto

O projeto é estruturado em uma arquitetura limpa e modular, facilitando a manutenção e a escalabilidade.

### Estrutura de Pastas
```
/FazaBoa_API
│
├── /Controllers
│ ├── AccountController.cs
│ ├── ChallengeController.cs
│ ├── CoinBalanceController.cs
│ ├── CoinTransactionController.cs
│ ├── GroupController.cs
│ ├── RewardController.cs
│ └── UserController.cs
│
├── /Data
│ └── ApplicationDbContext.cs
│
├── /Models
│ ├── ApplicationUser.cs
│ ├── Challenge.cs
│ ├── CoinBalance.cs
│ ├── CoinTransaction.cs
│ ├── CompletedChallenge.cs
│ ├── ForgotPassword.cs
│ ├── Group.cs
│ ├── Login.cs
│ ├── Register.cs
│ ├── ResetPassword.cs
│ ├── Response.cs
│ ├── Reward.cs
│ ├── RewardTransaction.cs
│ └── ValidateChallenge.cs
│
├── /Services
│ ├── EmailSender.cs
│ └── IEmailSender.cs
│
├── /Validation
│ ├── RegisterValidator.cs
│ └── ResetPasswordValidator.cs
│
└── Program.cs
```
## Configuração e Execução

### Pré-requisitos

- .NET 8.0 SDK
- MySQL Server
- Ferramenta para gerenciar pacotes NuGet

### Configuração do Ambiente

1. Clone o repositório:
```
   git clone https://github.com/seu-usuario/fazaboa-api.git
   cd fazaboa-api
```
2. Configure o banco de dados no appsettings.json:
```
"ConnectionStrings":
  {
    "DefaultConnection": "server=localhost;port=3306;database=fazaboaapi;user=root;password=sua-senha"
  }
```

3. Carregue as variáveis de ambiente do arquivo .env:
```
JWT_KEY=sua-chave-secreta
```

4. Migrações do Banco de Dados

Execute as migrações antes de para criar as tabelas no banco de dados:
```
dotnet ef migrations add MigracaoInicial
```
Depois:
```
dotnet ef database update
```

5. Executando a Aplicação

Inicie a aplicação:
```
dotnet run
```
### A API estará disponível em https://localhost:5019.

## Funcionalidades

### Autenticação e Autorização
Registro de Usuário: Permite que novos usuários se registrem na plataforma.\
Login: Gera um token JWT para autenticação.\
Logout: Invalida o refresh token do usuário.\
Esqueci a Senha: Envia um link de redefinição de senha para o email do usuário.\
Redefinir Senha: Permite que o usuário redefina sua senha usando um token de redefinição.

### Gestão de Desafios
Criar Desafio: Permite que usuários criem novos desafios.\
Obter Detalhes do Desafio: Retorna os detalhes de um desafio específico.\
Atualizar Desafio: Permite que o criador do desafio atualize suas informações.\
Excluir Desafio: Permite que o criador do desafio o exclua.\
Marcar Desafio como Concluído: Permite que os usuários marquem desafios como concluídos.\
Validar Conclusão de Desafio: Permite que o criador do desafio valide sua conclusão.

### Gestão de Grupos
Criar Grupo: Permite que usuários criem novos grupos.\
Obter Detalhes do Grupo: Retorna os detalhes de um grupo específico.\
Adicionar Membro ao Grupo: Permite adicionar membros a um grupo.\
Remover Membro do Grupo: Permite remover membros de um grupo.\
Marcar Membro como Dependente: Permite que o criador do grupo marque um membro como dependente.\
Adicionar Dependente ao Grupo: Permite adicionar dependentes ao grupo.\
Obter Dependentes do Grupo: Retorna uma lista de dependentes de um grupo.\
Excluir Grupo: Permite que o criador do grupo o exclua.

### Gestão de Moedas
Obter Saldo de Moedas: Retorna o saldo de moedas de um usuário em um grupo específico.\
Adicionar Saldo de Moedas: Adiciona moedas ao saldo de um usuário.\
Gastar Saldo de Moedas: Deduz moedas do saldo de um usuário.

### Gestão de Recompensas
Criar Recompensa: Permite que usuários criem novas recompensas.\
Obter Recompensas do Grupo: Retorna uma lista de recompensas de um grupo específico.\
Excluir Recompensa: Permite que o criador da recompensa a exclua.\
Resgatar Recompensa: Permite que os usuários resgatem recompensas.\
Obter Recompensas Resgatadas: Retorna uma lista de recompensas resgatadas por um usuário em um grupo específico.

### Tecnologias Utilizadas
.NET 8.0\
Entity Framework Core\
MySQL\
JWT para autenticação\
FluentValidation para validação de modelos\
Serilog para logging\
Swagger para documentação da API\
Postman para testes nos End-Points\
CORS para permitir requisições de diferentes origens

## Desenvolvido por Felipe Melo
