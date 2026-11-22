# Dependencias de migración

Relaciona las funcionalidades con las entidades, procedimientos y triggers de la base de datos, y define el orden de implementación y los riesgos de migración.

> **Nota de alcance (actualizada):** las secciones 1–3 se elaboraron cuando la fase 1 era operativa. La fase 1 es ahora administrativa (`docs/phase-1-scope.md`); ventas y reportes del empleado pasan a la fase 2 (`docs/phase-2-scope.md`). El orden vigente está en la sección 4.

---

## 1. Mapa de dependencias

```text
                         ┌──────────────────────────────┐
                         │ Inicio de sesión             │
                         │ USUARIO, TIPOUSUARIO,        │
                         │ ESTADOUSUARIO, SUCURSAL,     │
                         │ BITACORA, SECUENCIAL         │
                         └──────────────┬───────────────┘
                                        │ usuario + rol + sucursal
          ┌─────────────────────────────┼──────────────────────────────┐
          ▼                             ▼                              ▼
┌───────────────────┐       ┌────────────────────────┐      ┌───────────────────────┐
│ Consulta productos│◄──────│ Ventas                 │      │ Reporte ventas x user │
│ PRODUCTO,         │       │ VENTA, DETALLEVENTA,   │─────►│ VENTA, DETALLEVENTA,  │
│ Existencias,      │       │ CLIENTE (?), Existencias│     │ PRODUCTO (precio act.)│
│ CATEGORIA,        │       └────────────────────────┘      │ USUARIO, SUCURSAL     │
│ PROVEEDOR         │                                       └───────────────────────┘
└───────┬───────────┘
        │
        ▼
┌────────────────────────────┐        ┌───────────────────────────────┐
│ Generar traslado           │───────►│ Recibir traslado              │
│ SALXTRASLADO,              │ nSalida│ ENTXTRASLADO,                 │
│ DETSALXTRASLADO (trigger −)│Relacio-│ DETENTRADAXTRASLADOTEMP →     │
│ ENTXTRASLADO,              │ nada   │ DETENTRADAXTRASLADO (trigger +)│
│ DETENTRADAXTRASLADOTEMP    │        │ SALXTRASLADO.recibido = 'S'   │
│ SUCURSAL (origen/destino)  │        └───────────────────────────────┘
└────────────────────────────┘
```

Dependencias clave (Confirmado):
1. **Todas** las funcionalidades dependen del inicio de sesión: la sucursal y el rol del usuario determinan qué existencias se afectan y qué datos se ven.
2. **Ventas** y **traslados** dependen de la consulta de productos con existencias por sucursal.
3. **Recepción** depende de que la generación haya creado `ENTXTRASLADO` (con `nSalidaRelacionada`) y `DETENTRADAXTRASLADOTEMP`.
4. **Reporte** depende de los datos que produce ventas y del precio actual en `PRODUCTO`.
5. `Existencias` es el punto común de ventas, traslados, compras y ajustes; compras y ajustes (fuera de alcance) siguen modificándola desde el sistema actual.

---

## 2. Matriz funcionalidad × tabla

L = lectura, E = escritura (insert/update/delete), T = escrita indirectamente por trigger.

| Tabla | Login | Ventas | Generar traslado | Recibir traslado | Reporte |
|---|---|---|---|---|---|
| USUARIO | L | L | L | L | L |
| TIPOUSUARIO | L | | L (rol) | | L (rol, Pendiente) |
| ESTADOUSUARIO | L | | | | |
| SUCURSAL | L | L | L | L | L |
| BITACORA | E (Pendiente) | E (Pendiente) | E (Pendiente) | E (Pendiente) | |
| SECUENCIAL | E | | | | |
| PRODUCTO | | L | L | L | L |
| CATEGORIA, PROVEEDOR | | L | L | | |
| CLIENTE | | L / E (Pendiente) | | | |
| Existencias | | L / E | L / T | T | |
| VENTA | | E | | | L |
| DETALLEVENTA | | E | | | L |
| FACTURA, SERIEFACTURA | | Pendiente | | | |
| SALXTRASLADO | | | E | L / E | |
| DETSALXTRASLADO | | | E | | |
| ENTXTRASLADO | | | E (Pendiente) | L | |
| DETENTRADAXTRASLADOTEMP | | | E (Pendiente) | L / E (delete) | |
| DETENTRADAXTRASLADO | | | | E | |

---

## 3. Matriz funcionalidad × procedimientos / triggers

| Funcionalidad | Objetos existentes | Propuesta inicial de uso (a decidir en diseño) |
|---|---|---|
| Login | `sp_validaUsuario`, `sp_grabaBitacora`, `sp_get_sequence` | No reutilizar `sp_validaUsuario` tal cual (contraseña en claro). La validación pasa a la API con hash. `sp_grabaBitacora` reutilizable si se mantiene la bitácora. |
| Consulta productos | `sp_infoProductos`, `sp_getStoc` | Reemplazar por consulta parametrizada con búsqueda y paginación (EF Core). |
| Ventas | `grabaVenta` + tipo `DETALLESVENTA` | Candidato a **reutilizarse** (ya es transaccional y es el único que descuenta stock por venta). Revisar truncamientos de parámetros y fecha +2 h. Alternativa: reimplementar en C# replicando exactamente el efecto sobre `Existencias`. |
| Generar traslado | Ninguno (lo hacía el código VB.NET); triggers `trg_ActualizarExistencias_SalidaXTraslado`, `TR_actualizarDetalle` | Implementar en C# con transacción; **no** descontar stock manualmente (lo hace el trigger). |
| Consultar traslados | `getTraslados`, `getDetalleTraslado`, `getDetalleTrasladoE` | **No reutilizar `getTraslados`** (SQL dinámico, inyección). Reimplementar con consultas parametrizadas. |
| Recibir traslado | `sp_RecibeTraslado`, trigger `trg_ActualizarExistencias_EntradaXTraslado` | Reutilizable con validación previa en la API (sucursal destino = sucursal del usuario), o reimplementar en C# con transacción. No sumar stock manualmente (lo hace el trigger). |
| Reporte | `ReporteDeVentasPorUsuario`, `sp_ventasxuser`, `sp_ventasxusuariofecha`, `sp_cortecaja`, `sp_cortexmes*usuario` | No reutilizar `sp_cortecaja` (SQL dinámico). `ReporteDeVentasPorUsuario` tiene fecha fija; reimplementar tras confirmar reglas. |

---

## 4. Orden sugerido de implementación

### Fase 1 — Administración

1. **Base técnica** — Implementado.
2. **Seguridad**: inicio de sesión, usuarios y roles — Implementado.
3. **Sucursales** — Implementado.
4. **Configuración básica**: datos y logo de la empresa, formas de pago — Implementado. `SECUENCIAL` eliminado (IDENTITY); series de factura diferidas con la facturación.
5. **Catálogo de productos**: categorías, laboratorios, estanterías, productos, precios y consulta de existencias.
6. **Proveedores**.
7. **Compras**.
8. **Cuentas por pagar**.
9. **Ajustes de inventario**.
10. **Traslados**: generación y recepción.
11. **Reportes administrativos**.

Justificación: catálogo y existencias son la base de compras, ajustes, traslados y reportes; compras requiere proveedores; cuentas por pagar depende de compras.

### Fase 2 — Operación de sucursal

1. Búsqueda de productos con existencias de la sucursal.
2. Ventas y consulta de ventas propias.
3. Reportes del empleado / corte de caja.
4. Anulación de ventas y facturación, según se decida.

---

## 5. Riesgos de migración

| # | Riesgo | Causa | Mitigación propuesta |
|---|---|---|---|
| R-01 | Doble actualización de stock | Los triggers ya actualizan `Existencias` en traslados; si la API también lo hace, se duplica. | La API no modifica `Existencias` en traslados; documentar y probar con integración. |
| R-02 | Convivencia con el sistema VB.NET | Ambos sistemas escribirían en las mismas tablas. | Confirmar si habrá convivencia (P-71). Mantener compatibilidad de datos (mismos formatos, estados `'N'/'S'`, numeración). |
| R-03 | Migración de contraseñas | Texto plano en `contraUsuario` (varchar(50)); un hash no cabe y el VB.NET dejaría de autenticar. | Requiere decisión: columna nueva para hash, migración en primer login, o corte total. Implica cambio de esquema controlado (script versionado). |
| R-04 | Integridad referencial inexistente | Sin FKs en tablas operativas. | Validar referencias en la API (producto, sucursal, usuario existen). Evaluar agregar FKs más adelante tras revisar datos huérfanos. |
| R-05 | Fechas incoherentes | Desfases +2/−6/−1/0 h. | Definir zona horaria oficial (P-70) y centralizar el cálculo de fecha en la API. |
| R-06 | Stock negativo | Permitido por SP y triggers. | Decidir si se valida en la nueva API (RN-12, RN-32) sin romper el sistema actual. |
| R-07 | Recepción sin control de sucursal | `sp_RecibeTraslado` no valida destino. | Validar en la API que el usuario pertenezca a la sucursal destino (requiere confirmación, RN-45). |
| R-08 | Trigger `TR_actualizarDetalle` | Asume una fila y nEntrada = nSalida. | No actualizar `DETSALXTRASLADO` desde la web hasta confirmar P-32. |
| R-09 | Rendimiento | Sin índices secundarios; catálogo completo en `sp_infoProductos`. | Búsqueda paginada; proponer índices (sin aplicar) en una fase posterior. |
| R-10 | Hosting compartido | Base `db_acf2eb_*` sugiere hosting compartido (SmarterASP/similar). | Verificar permisos (crear tipos/SP, migraciones) antes de definir estrategia de despliegue. |
| R-11 | Correlativos compartidos | `IDENTITY` y `SECUENCIAL` usados por ambos sistemas. | Decidido: el sistema nuevo usa solo IDENTITY; `SECUENCIAL`/`sp_get_sequence` no se migran. |

---

## 6. Decisiones requeridas antes de codificar

1. Convivencia con el sistema VB.NET (sí/no, duración).
2. Estrategia de migración de contraseñas (implica cambio de esquema).
3. Reutilizar o reimplementar `grabaVenta` y `sp_RecibeTraslado`.
4. Validar o no existencias al vender y trasladar.
5. Zona horaria oficial.
6. Flujo exacto de generación de traslados (P-30) y de recepción (P-40–P-42).
7. Alcance exacto del reporte de ventas por usuario (P-50–P-52).
8. Anulación de ventas: ¿empleado (fase 2) o administrador? (P-11). Facturación: no se emite por ahora; solo se deja configurable `SERIEFACTURA` en la fase 1 (P-10).
