# Funcionalidades extra correspondiente al enfasis del entrevistador

El entrevistador pidio demostrar criterio, proactividad, cosas que se podrian mejorar y de que manera los desarrolladores deben adaptarse a las herramientas de IA.

Estas operaciones no están en la prueba técnica. Siguen el flujo de una banca pequeña: primero existe el cliente, después su cuenta, y solo entonces puede mover dinero.

La prueba pide alta de cliente, alta de cuenta, saldo, depósito, retiro e historial. Lo de abajo se agregó encima de eso.

## Cómo se trabajó

La solución se armó en Cursor, con Grok 4.7. El modo plan sirvió para ordenar el paso a paso usando hitos. Ese plan se corrigió al comprobar el SDK de .NET 10: la solución se genera como `.slnx`, no como `.sln`. Las dudas de diseño se contrastaron con la documentación y con búsquedas en la web. 

Se trabajó de esta manera ya que los LLM son una herramienta, el responsable de los resultado es el desarrollador es por ello que se deben marcar los límites de lo que se quiere construir en otras palabras el alcance de un proyecto, además como desarrollador se debe tener la capacidad de analizar situaciones que los enunciados no mencionan.

## Criterio
Ejemplos de criterio aplicados en la prueba técnica:

- El saldo solo cambia dentro de la cuenta. Un retiro sin fondos no guarda el movimiento.
- Cada cliente tiene una sola cuenta. Intentarlo de nuevo responde 400 y la base lo impide con un índice único sobre el cliente.
- Una transferencia mueve dinero entre las cuentas de dos clientes distintos. Si no hay fondos, ninguno de los dos saldos cambia.
- Los controladores no conocen la base de datos. Un error de negocio sale como 400 o 404, sin el detalle interno de un fallo inesperado.

## Proactividad:

El entrevistado notó que se podían agregar más funcionalidades que van deacuerdo al flujo de una app bancaria como: 
Consultar la informacion de todos los clientes, consultar información de un cliente por su identificador, actualizar información del cliente, mostrar información de la cuenta usando el id del cleinte, hacer transferencias entre cuentas (una cuenta por cliente) y listar las transferencias de una cuenta en específico.


## Clientes

Un cliente se registra una vez y después se puede consultar y corregir sin abrir otra ficha.

| Operación | Ruta |
|---|---|
| Listar | `GET /api/clientes` |
| Consultar | `GET /api/clientes/{id}` |
| Editar | `PUT /api/clientes/{id}` |

El listado va por fecha de registro y, si coinciden, por nombre. La edición reemplaza nombre, fecha de nacimiento, género e ingresos. El `id` y la `fechaCreacion` no cambian. Un id que no existe responde `404`.

## Una cuenta por cliente

Al abrir una cuenta, si ese cliente ya tiene una, la API responde `400`:

```json
{
  "title": "Solicitud no válida",
  "status": 400,
  "detail": "El cliente ya tiene una cuenta."
}
```

La prueba no impone esta regla. Aquí cada cliente opera con una sola cuenta, así que una transferencia siempre une a dos clientes distintos.

Para ver las cuentas de un cliente:

`GET /api/cuentas/cliente/{idCliente}`

`200` devuelve una lista. Cada elemento trae `idCliente`, `numeroCuenta`, `saldo` y `fechaCreacion`, de la más antigua a la más reciente. Si el cliente no existe, o no tiene cuenta, la respuesta es `404`.

## Transferencias

El cliente ordena pasar un monto de su cuenta a la de otro cliente. El banco hace las dos patas en la misma operación: resta en el origen y suma en el destino. Si el origen no alcanza, no se mueve nada.

`POST /api/transferencias`

```json
{
  "cuentaOrigen": "ACC-20261112-0001",
  "cuentaDestino": "ACC-20261112-0002",
  "monto": 40
}
```

`200`

```json
{
  "enviada": {
    "id": "e0a89052-b5bb-4cd5-9162-0c0bde31ea67",
    "numeroCuenta": "ACC-20261112-0001",
    "cuentaContraparte": "ACC-20261112-0002",
    "nombreClienteContraparte": "Luis Gómez",
    "tipo": "Transferencia enviada",
    "monto": 40,
    "fecha": "2026-11-12T02:00:00Z",
    "saldoPosterior": 60
  },
  "recibida": {
    "id": "9b86c55b-55a3-41ed-8d8a-9f309c39ed48",
    "numeroCuenta": "ACC-20261112-0002",
    "cuentaContraparte": "ACC-20261112-0001",
    "nombreClienteContraparte": "Ana Pérez",
    "tipo": "Transferencia recibida",
    "monto": 40,
    "fecha": "2026-11-12T02:00:00Z",
    "saldoPosterior": 50
  }
}
```

Reglas:

- El monto tiene que ser mayor que cero.
- Origen y destino tienen que ser cuentas distintas. Si son la misma, `400` con detalle `La cuenta origen y la destino deben ser distintas.`
- Si el origen no tiene fondos, `400` con detalle `Fondos insuficientes.` Los dos saldos quedan igual.
- Si alguna cuenta no existe, `404`.

Cada cuenta guarda su propio movimiento, con el saldo que le quedó. El historial general (`GET /api/cuentas/{numeroCuenta}/movimientos`) también los incluye, junto con depósitos y retiros.

Para ver solo las transferencias de una cuenta:

`GET /api/transferencias/{numeroCuenta}`

`200`

```json
[
  {
    "id": "e0a89052-b5bb-4cd5-9162-0c0bde31ea67",
    "numeroCuenta": "ACC-20261112-0001",
    "cuentaContraparte": "ACC-20261112-0002",
    "nombreClienteContraparte": "Luis Gómez",
    "tipo": "Transferencia enviada",
    "monto": 40,
    "fecha": "2026-11-12T02:00:00Z",
    "saldoPosterior": 60
  }
]
```

El orden es cronológico. Cada fila dice si esa cuenta envió o recibió, cuál fue la otra cuenta y el nombre de su cliente. Una cuenta que no existe responde `404`.

## Pruebas extra

`dotnet test` también ejecuta estas pruebas que fueron añadidas como un extra.

`tests/Banking.UnitTests/CustomerServiceTests.cs` cubre el alta del cliente y las operaciones de más: nombre vacío, nombre de más de 150 caracteres, fecha de nacimiento futura, ingresos negativos, listado ordenado, consulta de un id que no existe, edición del perfil y edición de un cliente que no existe.

`tests/Banking.UnitTests/AccountServiceTests.cs` cubre la regla de una sola cuenta por cliente, la consulta de la cuenta por id de cliente y la transferencia: mueve el saldo entre dos cuentas, no mueve nada si faltan fondos, rechaza origen y destino iguales, y el listado solo devuelve transferencias, en orden, con el nombre del otro cliente.
