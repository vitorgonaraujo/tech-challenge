using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<Beneficiario> CriarAsync(
        BeneficiarioRequestDados dados,
        CancellationToken cancellationToken)
    {
        var dataNascimento = ParseDataNascimento(dados.DataNascimento);

        var beneficiario = new Beneficiario(
            Guid.NewGuid(),
            dados.NomeCompleto,
            dados.Cpf,
            dataNascimento,
            dados.PlanoId);

        await GarantirPlanoExistenteAsync(
            dados.PlanoId,
            cancellationToken);

        await GarantirCpfDisponivelAsync(
            dados.Cpf,
            cancellationToken);

        db.Beneficiarios.Add(beneficiario);

        await SalvarAsync(cancellationToken);

        return beneficiario;
    }

    public async Task<Beneficiario> ObterAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await db.Beneficiarios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == id,
                cancellationToken)
            ?? throw new NaoEncontradoException(
                "Beneficiário não encontrado");
    }

    public async Task ExcluirAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        var beneficiario = await db.Beneficiarios
            .FirstOrDefaultAsync(
                b => b.Id == id,
                cancellationToken)
            ?? throw new NaoEncontradoException(
                "Beneficiário não encontrado");

        beneficiario.Excluir();

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Beneficiario> AtualizarAsync(
    Guid id,
    BeneficiarioAtualizacaoDados dados,
    CancellationToken cancellationToken)
    {
        var beneficiario = await db.Beneficiarios
            .FirstOrDefaultAsync(
                b => b.Id == id,
                cancellationToken)
            ?? throw new NaoEncontradoException(
                "Beneficiário não encontrado");

        var dataNascimento =
            ParseDataNascimento(dados.DataNascimento);

        var status = ParseStatus(dados.Status);

        await GarantirPlanoExistenteAsync(
            dados.PlanoId,
            cancellationToken);

        beneficiario.AtualizarDados(
            dados.NomeCompleto,
            dataNascimento,
            dados.PlanoId,
            status);

        await db.SaveChangesAsync(cancellationToken);

        return beneficiario;
    }

    public async Task<BeneficiarioPagina> ListarAsync(
    int pagina,
    int tamanho,
    StatusBeneficiario? status,
    Guid? planoId,
    CancellationToken cancellationToken)
    {
        if (pagina < 1)
        {
            throw new ValidacaoException(
                "Parâmetros de paginação inválidos",
                [
                    new DetalheErro("pagina", "minimo_1")
                ]);
        }

        if (tamanho < 1 || tamanho > 100)
        {
            throw new ValidacaoException(
                "Parâmetros de paginação inválidos",
                [
                    new DetalheErro("tamanho", "deve_estar_entre_1_e_100")
                ]);
        }

        var consulta = db.Beneficiarios
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            consulta = consulta.Where(
                b => b.Status == status.Value);
        }

        if (planoId.HasValue)
        {
            consulta = consulta.Where(
                b => b.PlanoId == planoId.Value);
        }

        var total = await consulta.CountAsync(
            cancellationToken);

        var dados = await consulta
            .OrderBy(b => b.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(b => new BeneficiarioListaItem(
                b.Id,
                b.NomeCompleto,
                b.Cpf,
                b.DataNascimento,
                b.Status,
                b.PlanoId,
                b.DataCadastro))
            .ToListAsync(cancellationToken);

        return new BeneficiarioPagina(
            dados,
            pagina,
            tamanho,
            total);
    }

    private async Task GarantirPlanoExistenteAsync(
        Guid planoId,
        CancellationToken cancellationToken)
    {
        var existe = await db.Planos
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == planoId,
                cancellationToken);

        if (!existe)
        {
            throw new NaoProcessavelException(
                "O plano informado não existe",
                [
                    new DetalheErro(
                        "plano_id",
                        "nao_encontrado")
                ]);
        }
    }

    private async Task GarantirCpfDisponivelAsync(
        string? cpf,
        CancellationToken cancellationToken)
    {
        var cpfNormalizado = cpf?.Trim() ?? string.Empty;

        var existe = await db.Beneficiarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                b => b.Cpf == cpfNormalizado,
                cancellationToken);

        if (existe)
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

    private static DateOnly ParseDataNascimento(
        string? valor)
    {
        if (!DateOnly.TryParseExact(
                valor,
                "yyyy-MM-dd",
                out var data))
        {
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                [
                    new DetalheErro(
                        "data_nascimento",
                        "formato_invalido")
                ]);
        }

        return data;
    }

    private static StatusBeneficiario ParseStatus(string? valor)
    {
        if (!Enum.TryParse<StatusBeneficiario>(
                valor,
                ignoreCase: false,
                out var status))
        {
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                [
                    new DetalheErro(
                    "status",
                    "invalido")
                ]);
        }

        return status;
    }

    private static bool EhViolacaoDeUnicidade(
        DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;
}

public sealed record BeneficiarioRequestDados(
    string? NomeCompleto,
    string? Cpf,
    string? DataNascimento,
    Guid PlanoId);

public sealed record BeneficiarioListaItem(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    StatusBeneficiario Status,
    Guid PlanoId,
    DateTime DataCadastro);

public sealed record BeneficiarioPagina(
    IReadOnlyList<BeneficiarioListaItem> Dados,
    int Pagina,
    int Tamanho,
    int Total);

public sealed record BeneficiarioAtualizacaoDados(
    string? NomeCompleto,
    string? Cpf,
    string? DataNascimento,
    Guid PlanoId,
    string? Status);
