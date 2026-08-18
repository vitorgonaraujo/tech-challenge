namespace Desafio.Api.Dominio;

public enum StatusBeneficiario
{
    ATIVO,
    INATIVO
}

public class Beneficiario
{
    private Beneficiario()
    {
    }

    public Beneficiario(
        Guid id,
        string? nomeCompleto,
        string? cpf,
        DateOnly dataNascimento,
        Guid planoId)
    {
        Id = id;

        DefinirDados(
            nomeCompleto,
            cpf,
            dataNascimento,
            planoId);

        Status = StatusBeneficiario.ATIVO;
        DataCadastro = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string NomeCompleto { get; private set; } = null!;

    public string Cpf { get; private set; } = null!;

    public DateOnly DataNascimento { get; private set; }

    public StatusBeneficiario Status { get; private set; }

    public Guid PlanoId { get; private set; }

    public Plano? Plano { get; set; }

    public DateTime DataCadastro { get; private set; }

    public DateTime? ExcluidoEm { get; private set; }

    public void DefinirDados(
        string? nomeCompleto,
        string? cpf,
        DateOnly dataNascimento,
        Guid planoId)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
        cpf = cpf?.Trim() ?? string.Empty;

        var detalhes = new List<DetalheErro>();

        if (nomeCompleto.Length == 0)
        {
            detalhes.Add(
                new DetalheErro(
                    "nome_completo",
                    "obrigatorio"));
        }
        else if (nomeCompleto.Length is < 3 or > 120)
        {
            detalhes.Add(
                new DetalheErro(
                    "nome_completo",
                    "tamanho_invalido"));
        }

        if (cpf.Length == 0)
        {
            detalhes.Add(
                new DetalheErro(
                    "cpf",
                    "obrigatorio"));
        }
        else if (!CpfValido(cpf))
        {
            detalhes.Add(
                new DetalheErro(
                    "cpf",
                    "formato_invalido"));
        }

        if (dataNascimento >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(
                new DetalheErro(
                    "data_nascimento",
                    "deve_ser_passada"));
        }

        if (planoId == Guid.Empty)
        {
            detalhes.Add(
                new DetalheErro(
                    "plano_id",
                    "obrigatorio"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                detalhes);
        }

        NomeCompleto = nomeCompleto;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        PlanoId = planoId;
    }

    public void AtualizarDados(
        string? nomeCompleto,
        DateOnly dataNascimento,
        Guid planoId,
        StatusBeneficiario status)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;

        var detalhes = new List<DetalheErro>();

        if (nomeCompleto.Length == 0)
        {
            detalhes.Add(
                new DetalheErro(
                    "nome_completo",
                    "obrigatorio"));
        }
        else if (nomeCompleto.Length is < 3 or > 120)
        {
            detalhes.Add(
                new DetalheErro(
                    "nome_completo",
                    "tamanho_invalido"));
        }

        if (dataNascimento >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(
                new DetalheErro(
                    "data_nascimento",
                    "deve_ser_passada"));
        }

        if (planoId == Guid.Empty)
        {
            detalhes.Add(
                new DetalheErro(
                    "plano_id",
                    "obrigatorio"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException(
                "Dados do beneficiário inválidos",
                detalhes);
        }

        var alterouDadosCadastrais =
            NomeCompleto != nomeCompleto ||
            DataNascimento != dataNascimento ||
            PlanoId != planoId;

        if (Status == StatusBeneficiario.INATIVO &&
            alterouDadosCadastrais)
        {
            throw new ConflitoException(
                "Beneficiário inativo não pode ter seus dados cadastrais alterados",
                [
                    new DetalheErro(
                        "status",
                        "beneficiario_inativo")
                ]);
        }

        NomeCompleto = nomeCompleto;
        DataNascimento = dataNascimento;
        PlanoId = planoId;
        Status = status;
    }

    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
        {
            return false;
        }

        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var digitos = cpf
            .Select(c => c - '0')
            .ToArray();

        var primeiroDigito = CalcularDigito(
            digitos.Take(9).ToArray(),
            10);

        var segundoDigito = CalcularDigito(
            digitos.Take(10).ToArray(),
            11);

        return digitos[9] == primeiroDigito &&
               digitos[10] == segundoDigito;
    }

    private static int CalcularDigito(
        IReadOnlyList<int> digitos,
        int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < digitos.Count; i++)
        {
            soma += digitos[i] * (pesoInicial - i);
        }

        var resto = soma % 11;

        return resto < 2
            ? 0
            : 11 - resto;
    }

    public void Excluir()
    {
        ExcluidoEm = DateTime.UtcNow;
    }
}