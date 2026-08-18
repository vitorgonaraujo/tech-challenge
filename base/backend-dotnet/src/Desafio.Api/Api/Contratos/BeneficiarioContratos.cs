using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioRequest(
    string? NomeCompleto,
    string? Cpf,
    string? DataNascimento,
    Guid? PlanoId);

public sealed record BeneficiarioResponse(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    string Status,
    Guid PlanoId,
    DateTime DataCadastro)
{
    public static BeneficiarioResponse De(Beneficiario beneficiario) =>
        new(
            beneficiario.Id,
            beneficiario.NomeCompleto,
            beneficiario.Cpf,
            beneficiario.DataNascimento,
            beneficiario.Status.ToString(),
            beneficiario.PlanoId,
            beneficiario.DataCadastro);
}

public sealed record BeneficiarioListaResponse(
    IReadOnlyList<BeneficiarioResponse> Dados,
    int Pagina,
    int Tamanho,
    int Total);

public sealed record BeneficiarioAtualizacaoRequest(
    string? NomeCompleto,
    string? Cpf,
    string? DataNascimento,
    Guid? PlanoId,
    string? Status);
    