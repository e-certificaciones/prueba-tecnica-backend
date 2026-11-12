using Banking.Application.Accounts;
using Banking.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Tags("Cuentas")]
[Route("api/cuentas")]
public sealed class AccountsController(IAccountService accounts) : ControllerBase
{
    [HttpPost]
    [EndpointName("CrearCuenta")]
    [EndpointSummary("Crea una cuenta bancaria")]
    [EndpointDescription("Abre una cuenta para un cliente registrado, con saldo inicial y número ACC-YYYYMMDD-XXXX. Un cliente solo puede tener una cuenta.")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.Create(request, cancellationToken);
        return Created($"/api/cuentas/{account.AccountNumber}", account);
    }

    [HttpGet("cliente/{idCliente}")]
    [EndpointName("ConsultarCuentaPorCliente")]
    [EndpointSummary("Consulta las cuentas de un cliente")]
    [EndpointDescription("Devuelve número, saldo y fecha de apertura de las cuentas asociadas al id del cliente, de la más antigua a la más reciente.")]
    [ProducesResponseType<IReadOnlyList<AccountInfoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<AccountInfoResponse>> GetByCustomer(Guid idCliente, CancellationToken cancellationToken) =>
        await accounts.GetByCustomer(idCliente, cancellationToken);

    [HttpGet("{numeroCuenta}/saldo")]
    [EndpointName("ConsultarSaldo")]
    [EndpointSummary("Consulta el saldo")]
    [EndpointDescription("Devuelve el saldo actual de la cuenta a partir de su número.")]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<BalanceResponse> ConsultarSaldo(string numeroCuenta, CancellationToken cancellationToken) =>
        await accounts.GetBalance(numeroCuenta, cancellationToken);

    [HttpPost("{numeroCuenta}/depositos")]
    [EndpointName("RegistrarDeposito")]
    [EndpointSummary("Registra un depósito")]
    [EndpointDescription("Suma el monto al saldo y devuelve el movimiento con el saldo posterior.")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<TransactionResponse> RegistrarDeposito(
        string numeroCuenta,
        AmountRequest request,
        CancellationToken cancellationToken) =>
        await accounts.Deposit(numeroCuenta, request, cancellationToken);

    [HttpPost("{numeroCuenta}/retiros")]
    [EndpointName("RegistrarRetiro")]
    [EndpointSummary("Registra un retiro")]
    [EndpointDescription("Resta el monto si hay fondos suficientes. Si no alcanzan, la operación se rechaza.")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<TransactionResponse> RegistrarRetiro(
        string numeroCuenta,
        AmountRequest request,
        CancellationToken cancellationToken) =>
        await accounts.Withdraw(numeroCuenta, request, cancellationToken);

    [HttpGet("{numeroCuenta}/movimientos")]
    [EndpointName("ConsultarMovimientos")]
    [EndpointSummary("Consulta el historial")]
    [EndpointDescription("Devuelve depósitos, retiros y transferencias en orden cronológico, con el saldo posterior a cada uno.")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<TransactionResponse>> ConsultarMovimientos(
        string numeroCuenta,
        CancellationToken cancellationToken) =>
        await accounts.GetTransactions(numeroCuenta, cancellationToken);
}
