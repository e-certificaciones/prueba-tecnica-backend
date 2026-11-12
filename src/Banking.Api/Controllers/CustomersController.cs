using Banking.Application.Contracts;
using Banking.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Tags("Clientes")]
[Route("api/clientes")]
public sealed class CustomersController(ICustomerService customers) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListarClientes")]
    [EndpointSummary("Lista los clientes")]
    [EndpointDescription("Devuelve los perfiles registrados, del más antiguo al más reciente.")]
    [ProducesResponseType<IReadOnlyList<CustomerResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CustomerResponse>> List(CancellationToken cancellationToken) =>
        await customers.List(cancellationToken);

    [HttpGet("{id}")]
    [EndpointName("ConsultarCliente")]
    [EndpointSummary("Consulta un cliente")]
    [EndpointDescription("Devuelve el perfil del cliente a partir de su id.")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CustomerResponse> Get(Guid id, CancellationToken cancellationToken) =>
        await customers.Get(id, cancellationToken);

    [HttpPut("{id}")]
    [EndpointName("EditarCliente")]
    [EndpointSummary("Edita un cliente")]
    [EndpointDescription("Reemplaza nombre, fecha de nacimiento, género e ingresos. El id y la fecha de registro no cambian.")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CustomerResponse> Update(Guid id, CreateCustomerRequest request, CancellationToken cancellationToken) =>
        await customers.Update(id, request, cancellationToken);

    [HttpPost]
    [EndpointName("CrearCliente")]
    [EndpointSummary("Crea el perfil de un cliente")]
    [EndpointDescription("Registra nombre completo, fecha de nacimiento, género e ingresos mensuales.")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.Create(request, cancellationToken);
        return Created($"/api/clientes/{customer.Id}", customer);
    }
}
