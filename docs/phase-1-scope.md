# Fase 1 — Alcance funcional

## 1. Objetivo

Migrar a la nueva aplicación web (ASP.NET Core + Vue 3) las operaciones diarias que realizan los **empleados de cada sucursal**, conservando el comportamiento del sistema original y reutilizando la base de datos SQL Server existente (`db_acf2eb_farmaciahorro`).

La administración general del sistema queda para fases posteriores.

## 2. Fuentes analizadas

| Fuente | Disponible | Observación |
|---|---|---|
| `docs/database-schema.sql` | Sí | Script completo: 37 tablas, 1 tipo tabla, 36 procedimientos, 8 triggers. |
| Código fuente VB.NET / Windows Forms | **No** | No está en el proyecto. Todo comportamiento de pantalla, validaciones de UI y consultas embebidas en el código queda **Pendiente de confirmar**. |
| Datos reales (catálogos `TIPOUSUARIO`, `ESTADOUSUARIO`, `SECUENCIAL`, etc.) | No | Solo se conoce la estructura, no los valores cargados. |

## 3. Funcionalidades incluidas

### 3.1 Inicio de sesión
- Autenticación del empleado por `nick` y contraseña contra `USUARIO`.
- Solo usuarios con estado `ACTIVO` (Confirmado, `sp_validaUsuario`).
- Al iniciar sesión se obtiene: id, nombre, tipo/rol y **sucursal asignada** del usuario (Confirmado).
- Registro en bitácora (`BITACORA` vía `sp_grabaBitacora`) — Pendiente de confirmar qué acciones registra el sistema original (inicio de sesión, ventas, traslados...).
- Cierre de sesión.

### 3.2 Ventas
- Búsqueda/consulta de productos con existencia de la sucursal del usuario (`sp_infoProductos`, `sp_getStoc`).
- Registro de venta: cabecera + detalle + descuento de existencias en una transacción (`grabaVenta`).
- Formas de pago: efectivo, tarjeta (con número de autorización) o combinación de ambas (Confirmado por estructura de `VENTA`).
- Cliente asociado a la venta (campo `VENTA.cliente`, probablemente NIT) — detalles Pendientes de confirmar.
- Consulta de ventas propias / detalle de una venta (`sp_ventasxuser`, `getDetallesVentas`) — Pendiente confirmar si es parte de la operación del empleado.

### 3.3 Generación de traslados entre sucursales
- Registro de salida por traslado desde la sucursal del usuario hacia otra sucursal (`SALXTRASLADO` + `DETSALXTRASLADO`).
- Creación de la entrada pendiente en la sucursal destino (`ENTXTRASLADO` + `DETENTRADAXTRASLADOTEMP`) — Pendiente confirmar el flujo exacto (no existe SP que lo haga; lo hacía el código VB.NET).
- Descuento automático de existencias en origen (trigger `trg_ActualizarExistencias_SalidaXTraslado`).
- Consulta de traslados generados y su detalle (`getTraslados`, `getDetalleTraslado`).

### 3.4 Recepción de traslados
- Consulta de traslados pendientes de recibir (`recibido = 'N'`).
- Visualización del detalle a recibir (`sp_RecibeTraslado` opción `B`).
- Confirmación de recepción (`sp_RecibeTraslado` opción `R`): mueve el detalle temporal al definitivo, suma existencias en destino (trigger) y marca el traslado como recibido.
- La recepción es **total** (todo o nada) en el sistema actual (Confirmado por el SP).

### 3.5 Reporte de ventas por usuario
- Reporte de ventas del usuario con diferencias entre precio vendido y precio vigente del producto (`ReporteDeVentasPorUsuario`).
- Posibles reportes complementarios: ventas por usuario y fecha (`sp_ventasxusuariofecha`), corte de caja por usuario (`sp_cortecaja`), por mes/intervalo (`sp_cortexmesusuario`, `sp_cortexmesintervalousuario`).
- **Pendiente de confirmar** cuál o cuáles de estos reportes usa realmente el empleado y con qué filtros.

## 4. Datos de consulta permitidos (solo lectura)

Estas entidades no se administran en la fase 1, pero sí se leen:

| Entidad | Uso en fase 1 |
|---|---|
| `USUARIO`, `TIPOUSUARIO`, `ESTADOUSUARIO` | Autenticación, rol y sucursal del usuario. |
| `SUCURSAL` | Sucursal del usuario, sucursal destino del traslado, nombres en listados. |
| `PRODUCTO` | Búsqueda, precio, descripción, código de barras. |
| `Existencias` | Stock por sucursal (lectura; la escritura ocurre a través de ventas y traslados). |
| `CATEGORIA`, `PROVEEDOR` | Datos descriptivos en búsqueda de productos (`sp_infoProductos`). |
| `CLIENTE` | Búsqueda de cliente por NIT para la venta (Pendiente de confirmar si se crea cliente desde la venta). |

## 5. Funcionalidades excluidas

- Gestión de usuarios, roles, estados.
- Gestión de sucursales.
- Gestión de productos, categorías, laboratorios, estanterías, precios.
- Gestión de proveedores y clientes (salvo lo que se confirme necesario para la venta).
- Compras (`COMPRA`, `DETALLECOMPRA`) y cuentas por pagar (`CUENTAXPAGAR`, `DETALLECXP`, `ESTADOCXP`).
- Ajustes de inventario (`AJUSTE`, `DETAJUSTE`, `TIPOAJUSTE`).
- Configuración general (`SECUENCIAL`, `SERIEFACTURA`, `FORMAPAGO`).
- Reportes administrativos: inventario por sucursal/categoría, stock mínimo, ventas por sucursal, ventas por producto, cuentas por pagar.
- Trasiegos legados (`TRASIEGOE`, `TRASIEGOSSAL`, `DETENTRADAXT`, `DETSALIDAXT`) — Pendiente confirmar si siguen en uso.

### Fuera de alcance salvo decisión explícita (Pendiente de confirmar)
- **Anulación/eliminación de ventas** (`VENTASELIMINADAS`, `DETALLEVENTASELIMINADOS`): existe estructura, pero no se sabe si la realiza el empleado o un administrador.
- **Facturación** (`FACTURA`, `SERIEFACTURA`): existen tablas, pero ningún procedimiento las usa. No se sabe si el sistema actual emite facturas.
- **Edición o cancelación de traslados** ya generados.

## 6. Supuestos de la fase

1. La nueva aplicación trabajará sobre la **misma base de datos** y podría convivir temporalmente con el sistema VB.NET (Pendiente de confirmar).
2. Los triggers de existencias se mantienen activos; la nueva aplicación no debe duplicar su lógica.
3. No se modifica la estructura de la base de datos en esta fase de análisis.

## 7. Entregables relacionados

- `docs/database-analysis.md` — tablas, relaciones, procedimientos y triggers.
- `docs/business-rules.md` — reglas de negocio confirmadas y pendientes.
- `docs/migration-dependencies.md` — dependencias, orden de implementación y riesgos.
