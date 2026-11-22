# Módulo de configuración básica

Paso 4 de la fase 1 (`docs/migration-dependencies.md` §4). Incluye los datos y el logo de la empresa, el
catálogo de formas de pago y los **campos de auditoría** transversales a todas las entidades.

## Decisiones

| Tema | Decisión | Origen |
|---|---|---|
| Datos de la empresa | Un único registro (nombre comercial, NIT, dirección, teléfono, correo) | Usuario |
| Logo | Uno por empresa, en la base de datos (`varbinary`), PNG o JPEG, máx. 512 KB. Evita depender de permisos de escritura del hosting compartido y se respalda con la base | Usuario |
| Formas de pago | Catálogo equivalente a `FORMAPAGO` (lo usa `COMPRA`), **sin valores iniciales** | Usuario |
| Correlativos | Se elimina `SECUENCIAL`/`sp_get_sequence`; todo usa IDENTITY | Usuario |
| Series de factura | `SERIEFACTURA` y numeración de `FACTURA` diferidas hasta activar la facturación | Usuario |
| Auditoría | Quién y cuándo creó y modificó cada registro; sin bitácora de cambios por ahora | Usuario |

## Modelo de datos

```text
CompanySettings (Id = 1 [CHECK], Name, TaxId, Address, Phone, Email,
                 Logo varbinary(max), LogoContentType, LogoUpdatedAtUtc, auditoría)
PaymentMethods (Id, Name único, IsActive, auditoría)
```

Migración: `src/backend/Pharmacy.Infrastructure/Persistence/Migrations/*_BasicConfiguration.cs`.
Script SQL idempotente: `database/migrations/0002_BasicConfiguration.sql` (solo agrega columnas
nullable, tablas, índices, FKs y permisos; no es destructivo).

## Auditoría

Toda entidad que hereda `AuditableEntity` (`Pharmacy.Core/Common/AuditableEntity.cs`) tiene:

| Campo | Contenido |
|---|---|
| `CreatedAtUtc`, `CreatedByUserId` | Alta del registro. **Inmutables**: aunque el código los cambie, no se guardan. |
| `UpdatedAtUtc`, `UpdatedByUserId` | Última modificación. |

- Los llena `AppDbContext` al guardar, con el reloj (`TimeProvider`) y el usuario de la petición
  (`ICurrentUser`, implementado en `Pharmacy.Api/Auth/HttpCurrentUser.cs`). Los servicios no los asignan.
- Usuario `NULL` = cambio del sistema (semilla inicial, registros previos a esta migración).
- `CreatedByUserId`/`UpdatedByUserId` tienen FK a `Users` sin cascada (los usuarios no se borran).
- Las propiedades marcadas `[NotAudited]` (intentos fallidos, bloqueo y último ingreso del usuario) no
  cuentan como modificación: iniciar sesión no altera la auditoría.
- Agregar o quitar filas dependientes no auditables (p. ej. permisos de un rol) marca como modificado al
  registro padre.
- Las respuestas de detalle exponen `audit: { createdAtUtc, createdBy, updatedAtUtc, updatedBy }` con el
  nombre completo del usuario; el frontend lo muestra con `components/AuditInfo.vue`.

## Permisos

| Código | Descripción |
|---|---|
| `settings.view` | Consultar las formas de pago (y ver las pantallas de configuración) |
| `settings.manage` | Editar datos y logo de la empresa; crear/editar/activar/desactivar formas de pago |

Los datos y el logo de la empresa se pueden **leer con cualquier sesión válida**, porque los usarán tickets
(fase 2) y reportes.

## Endpoints

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/settings/company` | Autenticado |
| PUT | `/api/settings/company` | `settings.manage` |
| GET | `/api/settings/company/logo` | Autenticado (imagen; 404 si no hay logo) |
| PUT | `/api/settings/company/logo` (multipart, campo `logo`) | `settings.manage` |
| DELETE | `/api/settings/company/logo` | `settings.manage` |
| GET | `/api/payment-methods` (includeInactive), `/api/payment-methods/{id}` | `settings.view` |
| POST/PUT/PATCH status | `/api/payment-methods[...]` | `settings.manage` |

## Reglas implementadas

- Solo existe un registro de empresa; la primera vez que se guarda se crea.
- El logo exige haber guardado antes los datos de la empresa.
- El tipo del logo se detecta por el **contenido** del archivo (firma PNG/JPEG); el nombre y el
  content-type enviados por el cliente no se usan. Se rechazan otros formatos (incluido SVG) y archivos
  de más de 512 KB.
- Nombre de forma de pago único sin distinguir mayúsculas/minúsculas; no se eliminan, se desactivan
  (las compras las referenciarán).

## Pendiente

- Series de factura y numeración de facturas, al activar la facturación.
- Bitácora de cambios (equivalente a `BITACORA`), como módulo transversal.
- Lookup de formas de pago activas para el módulo de compras (se agregará con ese módulo).
