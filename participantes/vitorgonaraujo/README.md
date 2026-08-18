# Entrega

## 1. Resumo da entrega

Completei o módulo de Beneficiários no backend, corrigindo os problemas que existiam no código base e implementando o que faltava na especificação.

Foram implementados consulta por id, atualização, exclusão lógica, paginação, filtros combináveis, validações de CPF e tratamento de concorrência para impedir CPF duplicado.

No frontend, implementei a parte de Beneficiários seguindo o padrão que já existia em Planos. A tela permite listar, filtrar, paginar, cadastrar, editar, alterar status e excluir beneficiários. Também tratei carregamento, lista vazia e os erros retornados pela API.

Mantive a estrutura existente do projeto e evitei adicionar bibliotecas que não eram necessárias para o desafio.

Também publiquei as imagens da API e do frontend no Docker Hub para `linux/amd64` e `linux/arm64` e testei a aplicação completa usando o `docker-compose.yml` da pasta da entrega.

Não ficou nenhuma funcionalidade obrigatória da especificação de fora.

---

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

**1. Criação de beneficiário não respeitava corretamente o contrato HTTP**

- **Onde:** fluxo de `POST /beneficiarios` no módulo de Beneficiários.
- **O que estava errado:** alguns cenários retornavam códigos HTTP diferentes dos definidos na especificação. CPF duplicado precisava ser `409`, plano inexistente precisava ser `422` e uma criação válida precisava retornar `201`.
- **Como percebi:** pelos testes que estavam falhando e comparando o comportamento com a `SPEC.md`.
- **Como corrigi:** ajustei o fluxo de criação e o tratamento das exceções para separar erro de validação, conflito de CPF e plano inexistente.
- **O que quebraria em produção:** quem consumisse a API teria dificuldade para saber o motivo real do erro e poderia mostrar mensagens erradas para o usuário.

**2. Listagem de beneficiários não tinha paginação e filtros completos**

- **Onde:** fluxo de `GET /beneficiarios`.
- **O que estava errado:** a listagem não seguia o envelope com `dados`, `pagina`, `tamanho` e `total` e também não tinha todos os filtros previstos.
- **Como percebi:** pelos testes e pela parte da especificação que define paginação e filtros.
- **Como corrigi:** implementei os parâmetros `pagina`, `tamanho`, `status` e `plano_id`, o total filtrado e uma ordenação antes do `Skip` e `Take`.
- **O que quebraria em produção:** o frontend não conseguiria navegar nas páginas de forma confiável e também não teria como saber o total de registros encontrados.

**3. Atualização de beneficiário estava incompleta**

- **Onde:** fluxo de `PUT /beneficiarios/{id}`.
- **O que estava errado:** faltavam regras de atualização, como CPF não editável, validação do plano e comportamento de beneficiário inativo.
- **Como percebi:** pela leitura da especificação e pelos cenários dos testes.
- **Como corrigi:** implementei a atualização respeitando os campos permitidos e mantive a regra de que um beneficiário inativo só pode ter o status alterado.
- **O que quebraria em produção:** dados poderiam ser alterados fora da regra de negócio ou uma reativação válida poderia ser bloqueada.

**4. Unicidade de CPF não estava protegida contra concorrência**

- **Onde:** criação de beneficiários e configuração do banco.
- **O que estava errado:** consultar antes se o CPF existia não era suficiente para duas requisições chegando ao mesmo tempo.
- **Como percebi:** pela regra explícita da especificação e pelo teste manual de concorrência.
- **Como corrigi:** mantive a consulta para detectar o caso comum, mas coloquei a garantia final no banco com índice único e tratei a violação de unicidade como `409 Conflict`.
- **O que quebraria em produção:** duas requisições poderiam passar pela verificação inicial ao mesmo tempo e criar dois registros com o mesmo CPF.

### 2.2 Pontos em que a especificação não definiu o comportamento

**1. Ordem padrão da listagem paginada**

- **O que a spec não define:** qual campo deveria ser usado para ordenar os beneficiários.
- **O que decidi:** ordenar por `Id` antes de aplicar `Skip` e `Take`.
- **Por quê:** precisava de uma ordem estável para evitar registros repetidos ou perdidos entre páginas.
- **O que eu consideraria se fosse decidir diferente:** em um sistema real provavelmente faria mais sentido ordenar por nome ou data de cadastro, dependendo do uso da tela.

**2. Nome do plano quando ele foi excluído depois do cadastro do beneficiário**

- **O que a spec não define:** como o frontend deve mostrar o nome de um plano que foi excluído logicamente, já que `GET /planos` não retorna mais esse plano.
- **O que decidi:** mostrar `Plano indisponível` quando o `plano_id` do beneficiário não for encontrado na lista de planos.
- **Por quê:** o beneficiário continua válido e vinculado ao plano antigo, mas a interface não tem outra informação disponível para recuperar o nome.
- **O que eu consideraria se fosse decidir diferente:** a API poderia devolver também o nome do plano junto com o beneficiário ou existir uma rota que permitisse consultar planos excluídos nesse contexto.

### 2.3 Inconsistências que percebi

Não encontrei uma divergência que exigisse ignorar a especificação para seguir os testes.

Os principais problemas que encontrei eram funcionalidades incompletas ou comportamentos do código base que ainda não estavam de acordo com a `SPEC.md`. Quando os testes falharam, usei a especificação como referência para entender o comportamento esperado.

Também percebi alguns pontos que a especificação deixava em aberto, como a ordenação da paginação. Nesses casos tomei uma decisão e registrei na seção anterior.

### 2.4 Decisões técnicas

Mantive a organização de camadas e o padrão do módulo de Planos em vez de criar uma estrutura nova para Beneficiários.

No backend, deixei a regra de negócio fora do controller e mantive o acesso ao banco de forma assíncrona usando Entity Framework.

Para CPF duplicado, usei duas verificações: uma consulta antes do insert para retornar o erro de forma simples no caso normal, e uma restrição única no banco para garantir o comportamento quando existem requisições concorrentes.

Na listagem, usei uma consulta para contar o total e outra para buscar somente os dados da página, sem fazer consultas adicionais para cada item.

No frontend, mantive modelo tipado, serviço separado e componente sem acesso direto ao `HttpClient`, seguindo a estrutura do módulo de Planos.

Não adicionei biblioteca de componentes ou de estado porque o escopo da interface era pequeno e não achei que traria benefício para a entrega.

### 2.5 O que ficou de fora

Não ficou nenhuma funcionalidade obrigatória da especificação de fora.

Não investi em um design mais elaborado no frontend porque a própria especificação deixa claro que o foco é o funcionamento e a integração correta com a API. Fiz apenas ajustes básicos de layout e usabilidade.

---

## 3. Uso de IA

**Nível de uso:** moderado

### 3.1 Ferramentas

- **ChatGPT:** usei como apoio principalmente no backend, pois este foi meu primeiro contato prático com .NET.
- Usei para entender alguns conceitos do ASP.NET Core e Entity Framework, analisar mensagens de erro e revisar caminhos de implementação.
- Também usei para gerar alguns dados de teste e como apoio pontual no frontend, principalmente em ajustes de comportamento e estilização.

### 3.2 Os 3 prompts que mais influenciaram o resultado

**Prompt 1**

```text
Estou tendo contato com .NET pela primeira vez. Analise este trecho e me explique como ele funciona seguindo o padrão que já existe no módulo de Planos.
```

- **O que aceitei:** usei a explicação para entender melhor a separação entre domínio, serviço, persistência e tratamento de erros antes de fazer as alterações no módulo de Beneficiários.
- **O que descartei e por quê:** descartei sugestões que mudavam demais a estrutura existente. Preferi manter o padrão do projeto em vez de criar uma abordagem diferente.

**Prompt 2**

```text
Analise esta mensagem de erro e me ajude a entender a causa antes de eu alterar o código.
```

- **O que aceitei:** usei a análise principalmente para localizar problemas de implementação e entender erros do .NET, Entity Framework e Angular.
- **O que descartei e por quê:** não apliquei correções sem testar. Depois de cada alteração, rodei novamente os testes ou reproduzi o fluxo manualmente para confirmar se o problema tinha sido resolvido.

**Prompt 3**

```text
Gere alguns dados válidos para eu testar cadastro, paginação, filtros e erros da aplicação.
```

- **O que aceitei:** usei os dados gerados como apoio para testar os fluxos do backend e do frontend.
- **O que descartei e por quê:** os dados gerados não foram usados para definir regra de negócio. As validações e os comportamentos continuaram sendo conferidos pela `SPEC.md`, pelos testes e pelas respostas reais da API.

### 3.3 O que fiz sem IA

Implementei e validei o frontend de Beneficiários, incluindo listagem, filtros combináveis, paginação, alteração da quantidade de itens por página, cadastro, edição, alteração de status e exclusão. O principal apoio de IA nessa parte foi em alguns ajustes e na estilização da interface.

Também fiz as validações manuais da aplicação, conferindo respostas da API e comportamento da tela em cenários como CPF inválido, CPF duplicado, filtros combinados, paginação, beneficiário inativo, reativação e exclusão.

Executei os testes automatizados do backend e também fiz testes manuais, incluindo o teste de concorrência com duas requisições tentando cadastrar o mesmo CPF.

Na parte de entrega, fiz os builds, publiquei as imagens no Docker Hub, validei `linux/amd64` e `linux/arm64`, montei o `docker-compose.yml` da entrega a partir do modelo fornecido e testei a aplicação usando somente as imagens publicadas.

### 3.4 O que ainda não domino

Como este foi meu primeiro contato prático com .NET, ainda não tenho domínio aprofundado de todas as partes do ASP.NET Core e do Entity Framework.

Consigo explicar os fluxos que implementei e as decisões tomadas neste projeto, mas em recursos menos comuns do framework ainda precisaria consultar documentação.

Quero aprofundar principalmente meu entendimento de Entity Framework, migrations e alguns detalhes de configuração do ASP.NET Core.

---

## 4. Perguntas de compreensão

### 4.1 Concorrência

Se duas requisições chegarem ao mesmo tempo com o mesmo CPF, as duas podem passar pela consulta que verifica se ele já existe. Por isso essa consulta não é a garantia final de unicidade.

A garantia real está no banco, com um índice único no campo `Cpf` configurado no `AppDbContext`.

Quando as duas tentam salvar, uma consegue fazer o insert e a outra recebe a violação de unicidade do PostgreSQL.

No `BeneficiarioServico`, esse erro é identificado pelo código `23505` e convertido em uma `ConflitoException`, que depois vira uma resposta `409 Conflict`.

Dessa forma, mesmo com duas requisições ao mesmo tempo, só um registro é criado. Testei esse cenário manualmente e recebi `201` em uma requisição e `409` na outra.

### 4.2 Um defeito que você corrigiu

Um dos defeitos que corrigi foi a validação de CPF em `Beneficiario.cs`.

Não era suficiente verificar apenas se o valor tinha 11 números, porque ainda seria possível aceitar um CPF com os dígitos verificadores errados.

Também precisava rejeitar sequências repetidas, como `11111111111`.

A validação passou a verificar o formato, rejeitar números repetidos e calcular os dois dígitos verificadores antes de considerar o CPF válido.

Se isso continuasse errado em produção, poderiam ser gravados CPFs inválidos no banco. Como o CPF também é único no sistema, esse dado incorreto ficaria associado ao beneficiário e poderia causar problema em integrações que dependessem dele.

### 4.3 O trecho mais complexo

O trecho que achei mais complicado foi o tratamento de CPF duplicado no momento de salvar o beneficiário, em `BeneficiarioServico.cs`.

```csharp
private async Task SalvarAsync(
    CancellationToken cancellationToken)
{
    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException excecao)
        when (EhViolacaoDeUnicidade(excecao))
    {
        throw new ConflitoException(
            "Já existe beneficiário cadastrado com esse CPF",
            [
                new DetalheErro(
                    "cpf",
                    "duplicado")
            ]);
    }
}

private static bool EhViolacaoDeUnicidade(
    DbUpdateException excecao) =>
    excecao.InnerException is PostgresException postgres &&
    postgres.SqlState == CodigoViolacaoDeUnicidade;
```

O `SalvarAsync` tenta gravar as alterações no banco usando `SaveChangesAsync`.

Se o banco rejeitar a operação, o Entity Framework lança uma `DbUpdateException`.

O `catch` tem uma condição e só entra nesse tratamento se `EhViolacaoDeUnicidade` identificar que o erro foi causado por uma restrição de unicidade.

Nesse método eu verifico se a exceção veio do PostgreSQL e se o código é `23505`, que é o código usado para violação de unicidade.

Quando isso acontece, transformo o erro do banco em uma `ConflitoException` dizendo que o CPF está duplicado.

Depois o tratamento de erros da API devolve isso como `409 Conflict`.

Esse tratamento é importante porque a consulta feita antes do insert não resolve sozinha o caso de duas requisições chegando ao mesmo tempo. A garantia final continua sendo o banco.
