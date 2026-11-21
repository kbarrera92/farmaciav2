# Módulo de usuarios, roles, permisos y sucursales

Primer módulo del sistema nuevo. Usa una **base de datos nueva** (migraciones de EF Core). El sistema
anterior (`USUARIO`, `TIPOUSUARIO`, `ESTADOUSUARIO`, `SUCURSAL`) sirvió solo como referencia.

## Decisiones

| Tema | Decisión | Origen |
|---|---|---|
| Base de datos | Nueva, con FKs reales y migraciones versionadas | Usuario |
| Registro de usuarios | Solo administradores (permiso `users.manage`); primer admin creado por seed | Usuario |
| Sesión | JWT (HS256, 8 h) en cookie `HttpOnly`, `Secure`, `SameSite=Strict` | Usuario |
| Roles | Dinámicos, creados por el administrador, con permisos asignables | Usuario |
| Sucursales | Catálogo mínimo con CRUD (nombre, dirección, teléfono, estado) | Usuario |
| Usuario ↔ sucursal / rol | Un usuario tiene **un** rol y **una** sucursal (igual que el sistema original) | Sistema original |
| Contraseñas | Hash PBKDF2 (`PasswordHasher<T>` de ASP.NET Core); política mínima de 8 caracteres con letras y números (configurable) | Seguridad (CLAUDE.md §8) |
| Bloqueo | 5 intentos fallidos bloquean 15 min (configurable en `Security:Lockout`) | Seguridad |
| Borrado de usuarios | No se borran: se desactivan (preserva historial para ventas/traslados futuros) | Diseño |

## Modelo de datos

```text
Branches (Id, Name único, Address, Phone, IsActive, CreatedAtUtc, UpdatedAtUtc)
Roles (Id, Name único, Description, IsSystem, IsActive, timestamps)
Permissions (Code PK, Name, Module, Description)          ← sembrada desde código
RolePermissions (RoleId FK, PermissionCode FK)  PK compuesta
Users (Id, FullName, Username, NormalizedUsername único, PasswordHash,
       RoleId FK, BranchId FK, IsActive, MustChangePassword,
       FailedLoginAttempts, LockoutEndUtc, LastLoginAtUtc, SecurityStamp, timestamps)
```

Migración: `src/backend/Pharmacy.Infrastructure/Persistence/Migrations/*_InitialUsers.cs`.
Script SQL idempotente: `database/migrations/0001_InitialUsers.sql`.

## Permisos

El catálogo vive en `src/backend/Pharmacy.Core/Auth/Permissions.cs` (y su espejo en
`src/frontend/src/modules/auth/types.ts`). Para agregar permisos de un módulo nuevo (por ejemplo
ventas o traslados para el rol *Bodeguero*): añadirlos al catálogo, crear una migración y asignarlos a
los roles desde la pantalla **Roles y permisos**.

| Código | Descripción |
|---|---|
| `users.view` | Consultar usuarios |
| `users.manage` | Crear/editar usuarios, activar/desactivar, restablecer contraseña |
| `roles.view` | Consultar roles y permisos |
| `roles.manage` | Crear/editar/activar/desactivar/eliminar roles |
| `branches.view` | Consultar sucursales |
| `branches.manage` | Crear/editar/activar/desactivar sucursales |

El rol de sistema **Administrador** (`IsSystem`) tiene implícitamente todos los permisos (incluidos los
que se agreguen en el futuro) y no se puede editar, desactivar ni eliminar.

## Flujo de autenticación

1. `POST /api/auth/login` valida usuario (sin distinguir mayúsculas), estado, bloqueo y contraseña.
   Cualquier fallo devuelve 401 con un mensaje genérico. El endpoint tiene límite de 10 intentos por
   minuto por IP.
2. Si es correcto, se emite el JWT en la cookie `pharmacy_auth` con los claims `sub`, `stamp`
   (SecurityStamp) y `mcp` (cambio de contraseña pendiente).
3. En cada petición se valida que el usuario siga activo y que su `SecurityStamp` coincida (caché en
   memoria de 5 min, invalidada al modificar el usuario). Desactivar o restablecer la contraseña de un
   usuario **cierra sus sesiones**.
4. Los permisos se resuelven por rol en cada petición (caché invalidada al editar el rol), así los
   cambios de permisos aplican sin volver a iniciar sesión.
5. Mientras `mcp = true` solo se permiten `GET /api/auth/me`, `POST /api/auth/logout` y
   `POST /api/auth/change-password`; el resto responde 403.

## Endpoints

| Método | Ruta | Permiso |
|---|---|---|
| POST | `/api/auth/login` | Anónimo |
| POST | `/api/auth/logout` | Anónimo |
| GET | `/api/auth/me` | Autenticado |
| POST | `/api/auth/change-password` | Autenticado |
| GET | `/api/users` (search, branchId, roleId, isActive, page, pageSize) | `users.view` |
| GET | `/api/users/{id}` | `users.view` |
| POST | `/api/users` | `users.manage` |
| PUT | `/api/users/{id}` | `users.manage` |
| PATCH | `/api/users/{id}/status` | `users.manage` |
| POST | `/api/users/{id}/reset-password` | `users.manage` |
| GET | `/api/roles`, `/api/roles/{id}` | `roles.view` |
| POST/PUT/PATCH status/DELETE | `/api/roles[...]` | `roles.manage` |
| GET | `/api/permissions` | `roles.view` |
| GET | `/api/branches` (includeInactive), `/api/branches/{id}` | `branches.view` |
| POST/PUT/PATCH status | `/api/branches[...]` | `branches.manage` |
| GET | `/api/lookups/branches` | Autenticado |
| GET | `/api/lookups/roles` | `users.manage` |

Errores en formato Problem Details: 400 validación, 401 no autenticado, 403 sin permiso,
404 no encontrado, 409 duplicado, 422 regla de negocio.

## Reglas de negocio implementadas

- Usuario único sin distinguir mayúsculas/minúsculas; solo letras, números, `.`, `-`, `_`.
- Un usuario nuevo o con contraseña restablecida debe cambiarla al ingresar.
- La nueva contraseña debe ser distinta de la actual.
- Solo se asignan roles y sucursales activos.
- Un usuario no puede desactivarse a sí mismo.
- Siempre debe quedar al menos un administrador activo (no se puede desactivar ni cambiar de rol al último).
- No se puede desactivar un rol o una sucursal con usuarios activos.
- No se puede eliminar un rol asignado a usuarios (se puede desactivar).
- Restablecer la contraseña desbloquea la cuenta.

## Pendiente

- Migración de usuarios del sistema anterior (sus contraseñas están en texto plano; se recomienda
  crearlos de nuevo o importarlos con contraseña temporal y cambio obligatorio).
- Bitácora de auditoría (equivalente a `BITACORA`), a definir como módulo transversal.
- Recuperación de contraseña por correo: no incluida; la restablece un administrador.
