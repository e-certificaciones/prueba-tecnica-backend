# Banca digital

API en ASP.NET Core para registrar clientes, abrir cuentas y operar depósitos, retiros y transferencias. El saldo vive en la cuenta. Cada movimiento guarda el saldo posterior. Los errores de negocio se responden fuera de los controladores.

Funcionalidades extras que entran dentro del flujo de una app bancaria: [FUNCIONALIDADES-EXTRA.md](FUNCIONALIDADES-EXTRA.md).

La solución usa arquitectura limpia en cuatro capas: Domain, Application, Infrastructure y Api. Domain guarda las reglas de negocio, Application los casos de uso, Infrastructure la base SQLite y Api los endpoints. Las dependencias apuntan hacia Domain. Ese reparto aplica SOLID: cada capa tiene su responsabilidad, los errores de negocio se extienden sin reescribir el manejador, las pruebas sustituyen los repositorios y los controladores dependen de interfaces. Hay 39 pruebas unitarias: 15 exigidas por el enunciado de la prueba y 24 extras. El detalle de cada punto está después de los endpoints.

Requiere el SDK de .NET 10. Si `dotnet --version` no empieza por `10`, instálalo con:

```powershell
winget install --id Microsoft.DotNet.SDK.10 -e
```

## Ejecutar

Clona el repositorio y entra en la carpeta de este archivo, la que contiene `Banking.slnx`.

```powershell
dotnet run --project src/Banking.Api --launch-profile http
```

Ese perfil deja el entorno en Development. La API queda en `http://localhost:5049`. Al arrancar crea `src/Banking.Api/banking.db` con SQLite y aplica las migraciones. El archivo no está en el repositorio: cada clon empieza con la base vacía. La cadena de conexión está en `src/Banking.Api/appsettings.json`:

```text
Data Source=banking.db
```

Swagger UI solo se publica en Development.

```text
http://localhost:5049/swagger
```

El documento OpenAPI que alimenta esa pantalla está en:

```text
http://localhost:5049/openapi/v1.json
```

## Pruebas

La prueba técnica pide pruebas unitarias del generador de número de cuenta y del saldo.

`tests/Banking.UnitTests/AccountNumberGeneratorTests.cs` comprueba, sobre la fecha de hoy en UTC, que el número mide 17 caracteres, empieza por `ACC-`, lleva la fecha `YYYYMMDD` y que dos sufijos distintos no coinciden.

`tests/Banking.UnitTests/AccountBalanceTests.cs` comprueba el depósito, el retiro con fondos y el retiro sin fondos. Si el monto supera el saldo, la operación se rechaza y ni el saldo ni el historial cambian.

```powershell
dotnet test
```

Las pruebas de lo que no pide el enunciado están en [Pruebas extra](FUNCIONALIDADES-EXTRA.md#pruebas-extra).

## Endpoints

Swagger, en Development, muestra estas doce operaciones en tres grupos: Clientes, Cuentas y Transferencias. El género se envía y se devuelve como `Masculino`, `Femenino` u `Otro`. El tipo de movimiento se devuelve como `Depósito`, `Retiro`, `Transferencia enviada` o `Transferencia recibida`.

### Crear cliente

`POST /api/clientes`

```json
{
  "nombreCompleto": "Ana Pérez",
  "fechaNacimiento": "1990-05-02",
  "genero": "Femenino",
  "ingresosMensuales": 1500
}
```

`201`

```json
{
  "id": "b153a10e-3318-4357-9246-4bb77bd23e74",
  "nombreCompleto": "Ana Pérez",
  "fechaNacimiento": "1990-05-02",
  "genero": "Femenino",
  "ingresosMensuales": 1500,
  "fechaCreacion": "2026-11-12T01:27:10.6720792Z"
}
```

### Listar clientes

`GET /api/clientes`

`200`

```json
[
  {
    "id": "b153a10e-3318-4357-9246-4bb77bd23e74",
    "nombreCompleto": "Ana Pérez",
    "fechaNacimiento": "1990-05-02",
    "genero": "Femenino",
    "ingresosMensuales": 1500,
    "fechaCreacion": "2026-11-12T01:27:10.6720792Z"
  }
]
```

El orden es por fecha de registro y, si coinciden, por nombre.

### Consultar cliente

`GET /api/clientes/b153a10e-3318-4357-9246-4bb77bd23e74`

`200` con el mismo perfil. Un id que no existe responde `404`.

### Editar cliente

`PUT /api/clientes/b153a10e-3318-4357-9246-4bb77bd23e74`

```json
{
  "nombreCompleto": "Luis Gómez",
  "fechaNacimiento": "1985-01-09",
  "genero": "Masculino",
  "ingresosMensuales": 2200
}
```

`200` devuelve el perfil nuevo. El `id` y `fechaCreacion` se conservan.

### Crear cuenta

`POST /api/cuentas`

```json
{
  "idCliente": "b153a10e-3318-4357-9246-4bb77bd23e74",
  "saldoInicial": 100
}
```

`201`

```json
{
  "numeroCuenta": "ACC-20261112-8669",
  "saldo": 100
}
```

El número tiene la forma `ACC-YYYYMMDD-XXXX`. El saldo inicial mayor que cero queda como primer depósito. Si el cliente ya tiene una cuenta, la respuesta es `400` con detalle `El cliente ya tiene una cuenta.`

### Consultar cuenta por cliente

`GET /api/cuentas/cliente/b153a10e-3318-4357-9246-4bb77bd23e74`

`200`

```json
[
  {
    "idCliente": "b153a10e-3318-4357-9246-4bb77bd23e74",
    "numeroCuenta": "ACC-20261112-8669",
    "saldo": 100,
    "fechaCreacion": "2026-11-12T01:27:11.5573732Z"
  }
]
```

El orden es por fecha de apertura. Si el cliente no existe, o no tiene cuenta, la respuesta es `404`.

### Consultar saldo

`GET /api/cuentas/ACC-20261112-8669/saldo`

`200`

```json
{
  "numeroCuenta": "ACC-20261112-8669",
  "saldo": 100
}
```

### Depositar

`POST /api/cuentas/ACC-20261112-8669/depositos`

```json
{
  "monto": 25.50
}
```

`200`

```json
{
  "id": "e0a89052-b5bb-4cd5-9162-0c0bde31ea67",
  "tipo": "Depósito",
  "monto": 25.50,
  "fecha": "2026-11-12T01:27:11.8439437Z",
  "saldoPosterior": 125.50
}
```

### Retirar sin fondos

`POST /api/cuentas/ACC-20261112-8669/retiros`

```json
{
  "monto": 200
}
```

`400`

```json
{
  "title": "Solicitud no válida",
  "status": 400,
  "detail": "Fondos insuficientes.",
  "traceId": "00-2c2805915b9d36a9111a0e9b9713cbb8-edaf12535d540d6d-00"
}
```

El saldo no cambia.

### Retirar con fondos

`POST /api/cuentas/ACC-20261112-8669/retiros`

```json
{
  "monto": 40
}
```

`200`

```json
{
  "id": "9b86c55b-55a3-41ed-8d8a-9f309c39ed48",
  "tipo": "Retiro",
  "monto": 40,
  "fecha": "2026-11-12T01:27:11.9191584Z",
  "saldoPosterior": 85.50
}
```

### Historial

`GET /api/cuentas/ACC-20261112-8669/movimientos`

`200`

```json
[
  {
    "id": "551826b5-d83a-4998-b91a-c06b64f64b60",
    "tipo": "Depósito",
    "monto": 100,
    "fecha": "2026-11-12T01:27:11.5573732",
    "saldoPosterior": 100
  },
  {
    "id": "e0a89052-b5bb-4cd5-9162-0c0bde31ea67",
    "tipo": "Depósito",
    "monto": 25.50,
    "fecha": "2026-11-12T01:27:11.8439437",
    "saldoPosterior": 125.50
  },
  {
    "id": "9b86c55b-55a3-41ed-8d8a-9f309c39ed48",
    "tipo": "Retiro",
    "monto": 40,
    "fecha": "2026-11-12T01:27:11.9191584",
    "saldoPosterior": 85.50
  }
]
```

El orden es cronológico. Una cuenta que no existe responde `404` con título `No encontrado`.

### Transferir

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

Origen y destino tienen que ser cuentas distintas. Si el origen no tiene fondos, la respuesta es `400` y ninguno de los dos saldos cambia. Si alguna cuenta no existe, la respuesta es `404`. Las reglas de esta operación están en [Transferencias](FUNCIONALIDADES-EXTRA.md#transferencias).

### Consultar transferencias

`GET /api/transferencias/ACC-20261112-0001`

`200` devuelve solo las transferencias de esa cuenta, en orden cronológico. Cada elemento trae `id`, `numeroCuenta`, `cuentaContraparte`, `nombreClienteContraparte`, `tipo`, `monto`, `fecha` y `saldoPosterior`. Una cuenta que no existe responde `404`.

## Arquitectura

El código sigue arquitectura limpia. El dominio no conoce HTTP ni Entity Framework. Los casos de uso tampoco. La base de datos y los controladores viven en los bordes y entran al centro a través de interfaces.

La solución `Banking.slnx` tiene cuatro proyectos de producción y uno de pruebas:

- `src/Banking.Domain` no referencia a nadie.
- `src/Banking.Application` referencia a Domain.
- `src/Banking.Infrastructure` referencia a Application.
- `src/Banking.Api` referencia a Application y a Infrastructure.
- `tests/Banking.UnitTests` referencia a Domain y a Application, y ejercita las reglas con dobles en memoria.

Api arranca la composición: registra los casos de uso con `AddApplication`, las implementaciones con `AddInfrastructure` y aplica las migraciones con `app.Services.Migrate()`. El proyecto web no nombra el contexto de base de datos.

## Capas

**Domain** contiene `Customer`, `Account` y `Transaction`. El saldo es `decimal` y vive en la cuenta. Abrir con saldo mayor que cero registra el primer depósito; cero deja el historial vacío; un saldo inicial negativo se rechaza. El número `ACC-YYYYMMDD-XXXX` lo arma `AccountNumberGenerator` a partir de una fecha y cuatro dígitos que ya vienen resueltos. También viven aquí las excepciones de negocio y los puertos `ICustomerRepository`, `IAccountRepository`, `IClock` e `IAccountNumberDigits`.

**Application** contiene `CustomerService` y `AccountService`. Validan la entrada, cargan las entidades, dejan que el dominio cambie el saldo y piden al repositorio que guarde. Los contratos JSON usan nombres en español (`nombreCompleto`, `saldoInicial`, `monto`) y los valores de enum que muestra Swagger (`Masculino`, `Depósito`, `Transferencia enviada`).

**Infrastructure** implementa esos puertos con EF Core y SQLite. Cada operación llama a `SaveChanges` una vez. `AccountNumber` tiene índice único. El reloj del sistema y los cuatro dígitos aleatorios están aquí; si el número ya existe, el caso de uso reintenta hasta diez veces con la misma fecha.

**Api** expone controladores por módulo: clientes, cuentas y transferencias. No ven el `DbContext` y no llevan `try/catch`. `GlobalExceptionHandler` traduce `CustomerNotFoundException`, `AccountNotFoundException` y `CustomerAccountNotFoundException` a `404`, el resto de `BusinessException` a `400` y cualquier otro error a `500` con un mensaje genérico. El cuerpo es `ProblemDetails` con `title`, `status`, `detail` y `traceId`. Swagger UI solo se monta en Development, sobre el documento OpenAPI existente.

## Principios SOLID

**Una sola responsabilidad.** `Account` decide si un monto entra o sale. Los servicios coordinan el caso de uso. Los repositorios persisten. Los controladores traducen HTTP. El manejador global solo convierte una excepción en respuesta. Alta, saldo, depósito, retiro, historial y transferencias comparten `AccountService` porque todas operan la misma cuenta.

**Abierto a la extensión.** Una `BusinessException` nueva responde `400` con el caso general del manejador. Un `404` nuevo se agrega al `switch` que lista las excepciones de recurso inexistente. Un tipo de movimiento nuevo se agrega al enum, a los métodos de `Account` y al mapeo que produce el texto en español.

**Sustitución.** `AccountService` y `CustomerService` reciben repositorios, reloj y generador de dígitos por interfaz. Las pruebas unitarias colocan dobles en memoria en ese mismo lugar. El caso de uso no distingue si detrás hay SQLite o un diccionario.

**Interfaces acotadas.** `IClock` e `IAccountNumberDigits` exponen un método. Los repositorios exponen el acceso que los casos de uso necesitan. `IAccountService` reúne las operaciones de cuenta y de transferencia; los controladores de cuentas y de transferencias reciben esa interfaz.

**Inversión de dependencias.** Domain define los puertos. Infrastructure los implementa. Api pide `ICustomerService` e `IAccountService` en el constructor y no instancia repositorios. El registro concreto está en `AddApplication` y `AddInfrastructure`.

## Pruebas unitarias

`dotnet test` ejecuta 39 pruebas.

Quince cubren lo que exige el enunciado. `AccountNumberGeneratorTests` aporta 5. Una comprueba longitud 17, prefijo `ACC-`, la fecha de hoy en UTC y que dos sufijos distintos no coinciden. Las otras cuatro aceptan `0` y `9999`, y rechazan `-1` y `10000`. `AccountBalanceTests` aporta 10: apertura en cero, apertura con saldo, apertura negativa, depósito, retiro con fondos, retiro sin fondos (el saldo y el historial no cambian) y montos que no son mayores que cero.

Veinticuatro cubren lo agregado fuera del enunciado. `CustomerServiceTests` aporta 10: nombre vacío o de más de 150 caracteres, fecha futura, ingresos negativos, el nombre recortado que sí se guarda, el orden del listado, la consulta y la edición de un cliente que no existe, y una edición que conserva el id y la fecha de alta. `AccountServiceTests` aporta 14: reintento cuando el número ya existe, agotamiento de los diez intentos, cliente que ya tiene cuenta, consulta de una o varias cuentas, cliente sin cuenta, cliente inexistente, depósito sobre una cuenta que no existe, orden del historial, transferencia entre cuentas distintas, transferencia sin fondos, la misma cuenta en origen y destino, y el listado de transferencias con el nombre del otro cliente.

El detalle de esas 24 está en [Pruebas extra](FUNCIONALIDADES-EXTRA.md#pruebas-extra).
