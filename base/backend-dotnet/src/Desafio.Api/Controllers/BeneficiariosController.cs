using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController(BeneficiarioServico servico)
    : ControllerBase
{
    // POST
    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
        [FromBody] BeneficiarioRequest requisicao,
        CancellationToken cancellationToken)
    {
        var dados = new BeneficiarioRequestDados(
            requisicao.NomeCompleto,
            requisicao.Cpf,
            requisicao.DataNascimento,
            requisicao.PlanoId ?? Guid.Empty);

        var beneficiario = await servico.CriarAsync(
            dados,
            cancellationToken);

        return CreatedAtAction(
            nameof(Obter),
            new { id = beneficiario.Id },
            BeneficiarioResponse.De(beneficiario));
    }
    // GET
    [HttpGet]
    [ProducesResponseType<BeneficiarioListaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        [FromQuery] string? status = null,
        [FromQuery(Name = "plano_id")] Guid? planoId = null,
        CancellationToken cancellationToken = default)
    {
        StatusBeneficiario? statusConvertido = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusBeneficiario>(
                    status,
                    ignoreCase: false,
                    out var valor))
            {
                throw new ValidacaoException(
                    "Parâmetros inválidos",
                    [
                        new DetalheErro("status", "invalido")
                    ]);
            }

            statusConvertido = valor;
        }

        var resultado = await servico.ListarAsync(
            pagina,
            tamanho,
            statusConvertido,
            planoId,
            cancellationToken);

        return Ok(
        new BeneficiarioListaResponse(
            resultado.Dados
                .Select(item => new BeneficiarioResponse(
                    item.Id,
                    item.NomeCompleto,
                    item.Cpf,
                    item.DataNascimento,
                    item.Status.ToString(),
                    item.PlanoId,
                    item.DataCadastro))
                .ToList(),
            resultado.Pagina,
            resultado.Tamanho,
            resultado.Total));
    }

    // GET {id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(
        Guid id,
        CancellationToken cancellationToken)
    {
        var beneficiario = await servico.ObterAsync(
            id,
            cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    // PUT 
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
    Guid id,
    [FromBody] BeneficiarioAtualizacaoRequest requisicao,
    CancellationToken cancellationToken)
    {
        var dados = new BeneficiarioAtualizacaoDados(
            requisicao.NomeCompleto,
            requisicao.Cpf,
            requisicao.DataNascimento,
            requisicao.PlanoId ?? Guid.Empty,
            requisicao.Status);

        var beneficiario = await servico.AtualizarAsync(
            id,
            dados,
            cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    // Delete {id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(
    Guid id,
    CancellationToken cancellationToken)
    {
        await servico.ExcluirAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
