using Banking.Application.Accounts;
using Banking.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Tags("Transferencias")]
[Route("api/transferencias")]
public sealed class TransfersController(IAccountService accounts) : ControllerBase
{
    [HttpPost]
    [EndpointName("RegistrarTransferencia")]
    [EndpointSummary("Transfiere entre dos cuentas")]
    [EndpointDescription("Resta el monto de la cuenta origen y lo suma en la destino. Si no hay fondos, ninguna de las dos cambia.")]
    [ProducesResponseType<TransferResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<TransferResultResponse> Create(TransferRequest request, CancellationToken cancellationToken) =>
        await accounts.Transfer(request, cancellationToken);

    [HttpGet("{numeroCuenta}")]
    [EndpointName("ConsultarTransferencias")]
    [EndpointSummary("Consulta las transferencias de una cuenta")]
    [EndpointDescription("Devuelve las transferencias enviadas y recibidas de esa cuenta, en orden cronológico, con el nombre del otro cliente.")]
    [ProducesResponseType<IReadOnlyList<TransferMovementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<TransferMovementResponse>> Get(string numeroCuenta, CancellationToken cancellationToken) =>
        await accounts.GetTransfers(numeroCuenta, cancellationToken);
}
