# MiPos

Sistema de punto de venta (POS) diseñado para restaurantes y establecimientos de comidas rápidas.

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![C#](https://img.shields.io/badge/C%23-14-239120)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-336791)
![Status](https://img.shields.io/badge/Status-En%20desarrollo-orange)

## Acerca del proyecto

MiPos busca simplificar la operación diaria de los restaurantes mediante una plataforma que centraliza la gestión de pedidos, mesas, ventas, productos, pagos y caja.

El sistema está diseñado para operar en una red local sin depender permanentemente de una conexión a internet, manteniendo una arquitectura preparada para futuras modalidades de despliegue.

## Funcionalidades previstas

- Gestión de negocios y establecimientos.
- Usuarios, roles y permisos.
- Catálogo de productos, categorías y combos.
- Gestión de mesas y pedidos.
- Consumo en local, para llevar y domicilios.
- Ventas y métodos de pago.
- Apertura, movimientos y cierre de caja.
- Historial de ventas y reportes.
- Facturación electrónica.
- Inventario y control de existencias.

## Stack tecnológico

| Tecnología | Propósito |
|---|---|
| .NET 10 LTS | Plataforma de desarrollo |
| C# 14 | Lenguaje de programación |
| ASP.NET Core | API REST |
| Entity Framework Core | Persistencia y migraciones |
| PostgreSQL | Base de datos |
| OpenAPI / Scalar | Documentación de la API |
| xUnit | Pruebas automatizadas |

## Arquitectura

El backend utiliza una arquitectura de monolito modular basada en principios de Clean Architecture, priorizando la simplicidad, separación de responsabilidades y mantenibilidad.

```text
MiPos/
├── src/
│   ├── MiPos.Api
│   ├── MiPos.Application
│   ├── MiPos.Domain
│   └── MiPos.Infrastructure
├── tests/
│   ├── MiPos.UnitTests
│   └── MiPos.IntegrationTests
├── docs/
├── AGENTS.md
├── README.md
└── MiPos.slnx
```

| Proyecto | Responsabilidad |
|---|---|
| MiPos.Api | Controladores, configuración y exposición HTTP. |
| MiPos.Application | Servicios y lógica de aplicación. |
| MiPos.Domain | Entidades y conceptos del dominio. |
| MiPos.Infrastructure | Persistencia e integraciones externas. |

## Desarrollo local

### Requisitos

- .NET 10 SDK.
- PostgreSQL.
- Git.

### Restaurar dependencias

```bash
dotnet restore MiPos.slnx
```

### Configurar la base de datos

Crea una base de datos PostgreSQL y configura la cadena de conexión mediante User Secrets en desarrollo.


```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project src/MiPos.Api
```

Las credenciales y configuraciones sensibles no deben almacenarse en el repositorio.

### Configurar autenticación

Configura `Jwt:SigningKey` con 32 bytes aleatorios codificados en Base64, mediante
User Secrets en desarrollo o `Jwt__SigningKey` en la instalación. Usa una
clave distinta por instalación. Ajusta `Jwt:AccessTokenHours` si necesitas
cambiar la vigencia predeterminada de 12 horas. `Jwt:Issuer` y `Jwt:Audience`
configuran el emisor y la audiencia.

Antes de aplicar `UpdateUserProfileAndGlobalUsername` a una base existente,
revisa si hay usernames normalizados repetidos o correos pendientes:

```sql
SELECT normalized_username, count(*)
FROM users
GROUP BY normalized_username
HAVING count(*) > 1;

SELECT id, business_id, username
FROM users
WHERE email IS NULL OR email !~ '^[^@[:space:]]+@[^@[:space:]]+$';
```

Si alguna consulta devuelve filas, resuelve los conflictos y completa los
correos reales antes de migrar. La migración se detiene si quedan casos
pendientes. Conserva el nombre existente completo en `first_name` y deja
`last_name` vacío a nivel de base de datos para esos perfiles históricos.

Aplica las migraciones sobre la base que hayas configurado para la instalación:

```bash
dotnet ef database update --project src/MiPos.Infrastructure --startup-project src/MiPos.Api
```

El login requiere un negocio, un rol y un usuario activos. La creación de
un negocio desde `POST /api/v1/businesses` requiere un JWT y el objeto
`administrator` con `username`, `password`, `firstName`, `lastName` y
`email`. El username es único en toda la base de datos. El servidor crea el
usuario inicial con el rol `ADMINISTRATOR` y guarda su contraseña como hash.

Con la API disponible por HTTPS, envía `POST /api/v1/auth/login` con
`Content-Type: application/json` y el cuerpo
`{"username":"<usuario>","password":"<contraseña>"}`. Usa el token de
`data.accessToken` como `Authorization: Bearer <token>` para consultar, por
ejemplo, `GET /api/v1/typologies`.

### Compilar

```bash
dotnet build MiPos.slnx
```

### Ejecutar la API

```bash
dotnet run --project src/MiPos.Api --launch-profile https
```

En el entorno de desarrollo están disponibles:

- `/health` — Estado de la API.
- `/openapi/v1.json` — Especificación OpenAPI.
- `/scalar/v1` — Documentación interactiva.

### Ejecutar pruebas

```bash
dotnet test MiPos.slnx
```

Para ejecutar las pruebas con PostgreSQL, define
`MIPOS_IDENTITY_TEST_CONNECTION`, `MIPOS_ROLES_TEST_CONNECTION`,
`MIPOS_ORGANIZATION_TEST_CONNECTION` y `MIPOS_USERS_TEST_CONNECTION` con bases temporales vacías y distintas,
con nombres que comiencen por `mipos_test_`.

## Documentación

- [Arquitectura](docs/architecture.md)
- [Reglas de negocio](docs/business-rules.md)
- [Convenciones de la API](docs/api-conventions.md)
- [Modelo de base de datos](docs/database.dbml)
- [Instrucciones para agentes de IA](AGENTS.md)

## Flujo de desarrollo

- `main`: versiones estables.
- `develop`: integración de funcionalidades.
- `feature/*`: desarrollo de funcionalidades individuales.

Los cambios se integran mediante Pull Requests y los commits siguen la convención Conventional Commits.

## Estado del proyecto

Actualmente en desarrollo del MVP. Las funcionalidades se implementan progresivamente.

---

**MiPos — Proyecto privado.**
