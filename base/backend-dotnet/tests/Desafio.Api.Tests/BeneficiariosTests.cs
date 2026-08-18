using System.Net;
namespace Desafio.Api.Tests;

[Collection(ColecaoDaApi.Nome)]
public class BeneficiariosTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient Client => fixture.Client;

    public Task InitializeAsync() => fixture.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object CorpoDeCriacao(string cpf, Guid? planoId = null) => new
    {
        NomeCompleto = "Maria Aparecida da Silva",
        Cpf = cpf,
        DataNascimento = "1990-05-12",
        PlanoId = planoId ?? Planos.Bronze
    };

    // ------------------------------------------------------------------ criação

    [Fact]
    public async Task Criar_deve_devolver_201_com_header_location()
    {
        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("52998224725")));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);

        var corpo = await resposta.CorpoAsync();
        Assert.NotEqual(Guid.Empty, corpo.GetProperty("id").GetGuid());
        Assert.Equal("52998224725", corpo.GetProperty("cpf").GetString());
        Assert.Equal("ATIVO", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Criar_com_cpf_ja_cadastrado_deve_devolver_409()
    {
        await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao("71428793860")));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_com_plano_inexistente_deve_devolver_422()
    {
        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(CorpoDeCriacao("39053344705", Planos.Inexistente)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ consulta por id

    [Fact]
    public async Task Obter_deve_devolver_o_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(beneficiario.Id, corpo.GetProperty("id").GetGuid());
        Assert.Equal(beneficiario.Cpf, corpo.GetProperty("cpf").GetString());
        Assert.Equal(Planos.Bronze, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Obter_inexistente_deve_devolver_404()
    {
        var resposta = await Client.GetAsync($"/beneficiarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ atualização

    [Fact]
    public async Task Atualizar_deve_alterar_os_dados_do_beneficiario()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Joana Ribeiro Nunes",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal("Joana Ribeiro Nunes", corpo.GetProperty("nome_completo").GetString());
        Assert.Equal(Planos.Ouro, corpo.GetProperty("plano_id").GetGuid());
    }

    [Fact]
    public async Task Atualizar_inexistente_deve_devolver_404()
    {
        var resposta = await Client.PutAsync($"/beneficiarios/{Guid.NewGuid()}", Http.Json(new
        {
            NomeCompleto = "Nao Existe",
            DataNascimento = "1985-03-20",
            PlanoId = Planos.Ouro,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_apontando_para_plano_inexistente_deve_devolver_422()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var resposta = await Client.PutAsync($"/beneficiarios/{beneficiario.Id}", Http.Json(new
        {
            NomeCompleto = "Maria Aparecida da Silva",
            DataNascimento = "1990-05-12",
            PlanoId = Planos.Inexistente,
            Status = "ATIVO"
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ exclusão

    [Fact]
    public async Task Excluir_deve_ser_logico_e_tirar_o_beneficiario_das_consultas()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        var exclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var consulta = await Client.GetAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);

        var listagem = await (await Client.GetAsync("/beneficiarios?pagina=1&tamanho=50")).CorpoAsync();
        Assert.Equal(0, listagem.GetProperty("total").GetInt32());

        var novaExclusao = await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");
        Assert.Equal(HttpStatusCode.NotFound, novaExclusao.StatusCode);
    }

    [Fact]
    public async Task Cpf_de_beneficiario_excluido_deve_continuar_ocupado()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(1)).Single();

        await Client.DeleteAsync($"/beneficiarios/{beneficiario.Id}");

        var resposta = await Client.PostAsync("/beneficiarios", Http.Json(CorpoDeCriacao(beneficiario.Cpf)));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    // ------------------------------------------------------------------ listagem

    [Fact]
    public async Task Listar_deve_devolver_envelope_paginado()
    {
        await fixture.SemearBeneficiariosAsync(3);

        var resposta = await Client.GetAsync("/beneficiarios?pagina=1&tamanho=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();
        Assert.Equal(3, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(1, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(3, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_deve_respeitar_pagina_e_tamanho()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios?pagina=3&tamanho=10")).CorpoAsync();

        Assert.Equal(5, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(3, corpo.GetProperty("pagina").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listar_deve_combinar_os_filtros_de_status_e_plano()
    {
        await fixture.SemearBeneficiariosAsync(4, Planos.Bronze, "ATIVO", 100);
        await fixture.SemearBeneficiariosAsync(6, Planos.Bronze, "INATIVO", 200);
        await fixture.SemearBeneficiariosAsync(3, Planos.Prata, "ATIVO", 300);

        var corpo = await (await Client.GetAsync(
            $"/beneficiarios?tamanho=50&status=ATIVO&plano_id={Planos.Bronze}")).CorpoAsync();

        Assert.Equal(4, corpo.GetProperty("total").GetInt32());
        Assert.All(
            corpo.GetProperty("dados").EnumerateArray(),
            beneficiario =>
            {
                Assert.Equal("ATIVO", beneficiario.GetProperty("status").GetString());
                Assert.Equal(Planos.Bronze, beneficiario.GetProperty("plano_id").GetGuid());
            });
    }

    [Fact]
    public async Task Listar_sem_informar_tamanho_deve_devolver_10_itens_por_pagina()
    {
        await fixture.SemearBeneficiariosAsync(25);

        var corpo = await (await Client.GetAsync("/beneficiarios")).CorpoAsync();

        Assert.Equal(10, corpo.GetProperty("dados").GetArrayLength());
        Assert.Equal(10, corpo.GetProperty("tamanho").GetInt32());
        Assert.Equal(25, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Atualizar_dados_de_beneficiario_inativo_deve_devolver_409()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1,
            Planos.Bronze,
            "INATIVO",
            500)).Single();

        var resposta = await Client.PutAsync(
            $"/beneficiarios/{beneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto = "Nome Corrigido do Inativo",
                DataNascimento = "1990-05-12",
                PlanoId = Planos.Bronze,
                Status = "INATIVO"
            }));

        Assert.Equal(
            HttpStatusCode.Conflict,
            resposta.StatusCode);
    }

    [Fact]
    public async Task Reativar_beneficiario_inativo_deve_devolver_200()
    {
        var beneficiario = (await fixture.SemearBeneficiariosAsync(
            1,
            Planos.Bronze,
            "INATIVO",
            600)).Single();

        var resposta = await Client.PutAsync(
            $"/beneficiarios/{beneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto = beneficiario.NomeCompleto,
                DataNascimento = beneficiario.DataNascimento
                    .ToString("yyyy-MM-dd"),
                PlanoId = beneficiario.PlanoId,
                Status = "ATIVO"
            }));

        Assert.Equal(
            HttpStatusCode.OK,
            resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();

        Assert.Equal(
            "ATIVO",
            corpo.GetProperty("status").GetString());
    }

    // ------------------------------------------------------------------ casos de borda da especificação

    [Fact]
    public async Task Criar_deve_ignorar_id_status_e_data_cadastro_enviados_pelo_cliente()
    {
        var idEnviado = Guid.Parse(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var resposta = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(new
            {
                Id = idEnviado,
                NomeCompleto = "Teste Campos Controlados",
                Cpf = GeradorDeCpf.Gerar(7000),
                DataNascimento = "1990-01-01",
                PlanoId = Planos.Bronze,
                Status = "INATIVO",
                DataCadastro = "2000-01-01T00:00:00Z"
            }));

        Assert.Equal(
            HttpStatusCode.Created,
            resposta.StatusCode);

        var corpo = await resposta.CorpoAsync();

        Assert.NotEqual(
            idEnviado,
            corpo.GetProperty("id").GetGuid());

        Assert.Equal(
            "ATIVO",
            corpo.GetProperty("status").GetString());

        var dataCadastro =
            corpo.GetProperty("data_cadastro").GetDateTime();

        Assert.True(
            dataCadastro > new DateTime(
                2000,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public async Task Criacoes_simultaneas_com_mesmo_cpf_devem_criar_apenas_um_beneficiario()
    {
        var cpf = GeradorDeCpf.Gerar(7100);

        var corpo = CorpoDeCriacao(cpf);

        var primeiraRequisicao =
            Client.PostAsync(
                "/beneficiarios",
                Http.Json(corpo));

        var segundaRequisicao =
            Client.PostAsync(
                "/beneficiarios",
                Http.Json(corpo));

        var respostas = await Task.WhenAll(
            primeiraRequisicao,
            segundaRequisicao);

        Assert.Single(
            respostas,
            resposta =>
                resposta.StatusCode == HttpStatusCode.Created);

        Assert.Single(
            respostas,
            resposta =>
                resposta.StatusCode == HttpStatusCode.Conflict);

        var listagem = await (
            await Client.GetAsync(
                "/beneficiarios?pagina=1&tamanho=100"))
            .CorpoAsync();

        var quantidadeComMesmoCpf =
            listagem
                .GetProperty("dados")
                .EnumerateArray()
                .Count(
                    beneficiario =>
                        beneficiario
                            .GetProperty("cpf")
                            .GetString() == cpf);

        Assert.Equal(
            1,
            quantidadeComMesmoCpf);
    }

    [Fact]
    public async Task Plano_excluido_deve_preservar_vinculo_existente_e_rejeitar_novos_vinculos()
    {
        var criacaoPlano = await Client.PostAsync(
            "/planos",
            Http.Json(new
            {
                Nome = "Plano Temporario Beneficiarios",
                CodigoRegistroAns = "299999"
            }));

        Assert.Equal(
            HttpStatusCode.Created,
            criacaoPlano.StatusCode);

        var planoId = (
            await criacaoPlano.CorpoAsync())
            .GetProperty("id")
            .GetGuid();

        var criacaoBeneficiario = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(
                CorpoDeCriacao(
                    GeradorDeCpf.Gerar(7200),
                    planoId)));

        Assert.Equal(
            HttpStatusCode.Created,
            criacaoBeneficiario.StatusCode);

        var beneficiarioId = (
            await criacaoBeneficiario.CorpoAsync())
            .GetProperty("id")
            .GetGuid();

        var exclusaoPlano =
            await Client.DeleteAsync(
                $"/planos/{planoId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            exclusaoPlano.StatusCode);


        var consultaBeneficiario =
            await Client.GetAsync(
                $"/beneficiarios/{beneficiarioId}");

        Assert.Equal(
            HttpStatusCode.OK,
            consultaBeneficiario.StatusCode);


        var novoBeneficiario = await Client.PostAsync(
            "/beneficiarios",
            Http.Json(
                CorpoDeCriacao(
                    GeradorDeCpf.Gerar(7201),
                    planoId)));

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            novoBeneficiario.StatusCode);



        var outroBeneficiario =
            (await fixture.SemearBeneficiariosAsync(
                1,
                Planos.Bronze,
                "ATIVO",
                7202))
            .Single();

        var atualizacao = await Client.PutAsync(
            $"/beneficiarios/{outroBeneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto =
                    outroBeneficiario.NomeCompleto,
                DataNascimento =
                    outroBeneficiario.DataNascimento
                        .ToString("yyyy-MM-dd"),
                PlanoId = planoId,
                Status = "ATIVO"
            }));

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            atualizacao.StatusCode);
    }

    [Fact]
    public async Task Atualizar_beneficiario_excluido_deve_devolver_404()
    {
        var beneficiario =
            (await fixture.SemearBeneficiariosAsync(1))
            .Single();

        var exclusao =
            await Client.DeleteAsync(
                $"/beneficiarios/{beneficiario.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            exclusao.StatusCode);

        var resposta = await Client.PutAsync(
            $"/beneficiarios/{beneficiario.Id}",
            Http.Json(new
            {
                NomeCompleto = "Nome Atualizado",
                DataNascimento = "1990-05-12",
                PlanoId = Planos.Bronze,
                Status = "ATIVO"
            }));

        Assert.Equal(
            HttpStatusCode.NotFound,
            resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_pagina_alem_do_total_deve_devolver_lista_vazia_e_total_correto()
    {
        await fixture.SemearBeneficiariosAsync(12);

        var resposta =
            await Client.GetAsync(
                "/beneficiarios?pagina=99&tamanho=10");

        Assert.Equal(
            HttpStatusCode.OK,
            resposta.StatusCode);

        var corpo =
            await resposta.CorpoAsync();

        Assert.Equal(
            0,
            corpo.GetProperty("dados").GetArrayLength());

        Assert.Equal(
            99,
            corpo.GetProperty("pagina").GetInt32());

        Assert.Equal(
            10,
            corpo.GetProperty("tamanho").GetInt32());

        Assert.Equal(
            12,
            corpo.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("/beneficiarios?pagina=0")]
    [InlineData("/beneficiarios?tamanho=0")]
    [InlineData("/beneficiarios?tamanho=101")]
    public async Task Listar_com_parametros_de_paginacao_invalidos_deve_devolver_400(
        string url)
    {
        var resposta =
            await Client.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            resposta.StatusCode);
    }

    [Fact]
    public async Task Paginacao_deve_ser_estavel_sem_repetir_ou_perder_beneficiarios()
    {
        var semeados =
            await fixture.SemearBeneficiariosAsync(25);

        var primeiraPagina =
            await (
                await Client.GetAsync(
                    "/beneficiarios?pagina=1&tamanho=10"))
                .CorpoAsync();

        var segundaPagina =
            await (
                await Client.GetAsync(
                    "/beneficiarios?pagina=2&tamanho=10"))
                .CorpoAsync();

        var terceiraPagina =
            await (
                await Client.GetAsync(
                    "/beneficiarios?pagina=3&tamanho=10"))
                .CorpoAsync();

        var ids = primeiraPagina
            .GetProperty("dados")
            .EnumerateArray()
            .Concat(
                segundaPagina
                    .GetProperty("dados")
                    .EnumerateArray())
            .Concat(
                terceiraPagina
                    .GetProperty("dados")
                    .EnumerateArray())
            .Select(
                beneficiario =>
                    beneficiario
                        .GetProperty("id")
                        .GetGuid())
            .ToList();

        Assert.Equal(
            25,
            ids.Count);

        Assert.Equal(
            25,
            ids.Distinct().Count());

        var idsEsperados =
            semeados
                .Select(beneficiario => beneficiario.Id)
                .ToHashSet();

        Assert.True(
            idsEsperados.SetEquals(ids));
    }
}
