# Fase 1 — Alcance funcional (administración)

## 1. Objetivo

Migrar a la nueva aplicación web (ASP.NET Core + Vue 3) la **administración general** del sistema: seguridad, sucursales, catálogo, compras, cuentas por pagar, inventario (ajustes y traslados), configuración y reportes administrativos. Se conserva el comportamiento del sistema original y se parte del esquema de la base de datos SQL Server existente (`db_acf2eb_farmaciahorro`).

La operación diaria de los empleados de sucursal (ventas, caja) queda para la **fase 2** (`docs/phase-2-scope.md`).

En producción, la administración se publica en **`https://admin.farmaciahorro.com`** (ver `docs/deployment.md`).

## 2. Fuentes analizadas

| Fuente | Disponible | Observación |
|---|---|---|
| `docs/database-schema.sql` | Sí | Script completo: 37 tablas, 1 tipo tabla, 36 procedimientos, 8 triggers. |
| Código fuente VB.NET / Windows Forms | **No** | Todo comportamiento de pantalla, validaciones de UI y consultas embebidas queda **Pendiente de confirmar**. |
| Datos reales (catálogos, `SECUENCIAL`, etc.) | No | Solo se conoce la estructura, no los valores cargados. |

`docs/database-analysis.md` y `docs/business-rules.md` cubren a fondo seguridad, traslados e inventario. Las tablas de catálogo, compras, cuentas por pagar, ajustes y configuración se analizarán en detalle **al iniciar cada módulo** (no se inventan reglas antes de ese análisis).

## 3. Funcionalidades incluidas

### 3.1 Seguridad — Implementado
- Inicio y cierre de sesión, cambio obligatorio de contraseña, bloqueo por intentos fallidos.
- Usuarios, roles con permisos y sucursal asignada (`docs/modules/users.md`).
- Tablas de referencia: `USUARIO`, `TIPOUSUARIO`, `ESTADOUSUARIO`, `BITACORA`.

### 3.2 Sucursales — Implementado
- Mantenimiento de sucursales (`SUCURSAL`).

### 3.3 Catálogo de productos — Implementado (la consulta de existencias pasa a Compras, §3.5)
- Productos, categorías y estanterías: `PRODUCTO`, `CATEGORIA`, `ESTANTERIA`. El laboratorio es un campo de texto del producto (decisión del usuario; la tabla `LABORATORIO` no la usa ningún procedimiento). Análisis: `docs/modules/catalog.md`.
- Precios del producto: precio general y precio opcional por sucursal (no existe historial de precios — RN-63, Confirmado).
- Existencias: no se crean al crear el producto; se crean en el primer movimiento (P-77, resuelta).

### 3.4 Proveedores — Implementado
- Mantenimiento de `PROVEEDOR`. Se incluye porque compras y cuentas por pagar dependen de él.

### 3.5 Compras — En análisis (`docs/modules/purchases.md`)
- Registro y consulta de compras: `COMPRA`, `DETALLECOMPRA` (`getCompras`, `getDetalleCompra`, `sp_infoProdCompras` como referencia).
- Suma de existencias por trigger `trg_ActualizarExistencias_Compra` (la API no debe duplicarla); totales mantenidos por `TR_updateTotalActualizarC` y `TR_actualizartotalBorrarC`.
- Tabla de existencias y su consulta por sucursal (movida desde el catálogo; `Existencias`, `sp_infoProdCompras`, `sp_getStoc` como referencia).
- Pendiente de confirmar: relación exacta entre compra y cuenta por pagar (compra al crédito / contado).

### 3.6 Cuentas por pagar
- Consulta y gestión de `CUENTAXPAGAR`, `DETALLECXP`, `ESTADOCXP` (`sp_vercxp`, `sp_vercxpxmes`, `sp_verDetallesCxP` como referencia).
- Pendiente de confirmar: registro de abonos/pagos y cambios de estado.

### 3.7 Ajustes de inventario
- Registro y consulta de ajustes: `AJUSTE`, `DETAJUSTE`, `TIPOAJUSTE` (`getAjustes`, `detAjustes` como referencia).
- Existencias actualizadas por triggers `trg_ActualizarExistencias_Ajuste` y `trg_ActualizarExistencias_AjusteEliminaDetalle`.

### 3.8 Traslados entre sucursales
- Generación: `SALXTRASLADO` + `DETSALXTRASLADO`, entrada pendiente en destino `ENTXTRASLADO` + `DETENTRADAXTRASLADOTEMP` (flujo exacto Pendiente, P-30). Descuento en origen por trigger `trg_ActualizarExistencias_SalidaXTraslado`.
- Recepción total (todo o nada) con `sp_RecibeTraslado` (opciones `B` y `R`); suma en destino por trigger `trg_ActualizarExistencias_EntradaXTraslado`.
- Consulta de traslados y su detalle (`getTraslados`, `getDetalleTraslado`, `getDetalleTrasladoE`).
- Reglas detalladas: `docs/business-rules.md` secciones 3 y 4.
- Pendiente de confirmar: si en la fase 2 los empleados de sucursal también generarán/recibirán traslados desde el área operativa.

### 3.9 Configuración — Implementado (configuración básica)
- Datos de la empresa y **logo** (nuevo; no existe en el sistema original): un solo registro por empresa, logo PNG/JPEG de hasta 512 KB guardado en la base de datos.
- Formas de pago (`FORMAPAGO`), sin valores iniciales.
- Correlativos: **se elimina `SECUENCIAL`**; el sistema nuevo usa IDENTITY (decisión del usuario).
- Series de factura (`SERIEFACTURA`): diferidas a la fase en que se active la facturación.
- Detalle: `docs/modules/configuration.md`.

### 3.10 Reportes administrativos
- Inventario por sucursal y por categoría (`verInventarioxsuc`, `verInventarioxsucfecha`, `verinventarioxcat`).
- Productos bajo stock mínimo (`verprodsstockmin`).
- Ventas por sucursal y por producto (`sp_ventasSuc`, `ventasdiariassuc`, `sp_ventasxproducto`, `sp_cortexmes`, `sp_cortexmesintervalo`).
- Cuentas por pagar (`sp_vercxp`, `sp_vercxpxmes`).
- Pendiente de confirmar: filtros y columnas que usa realmente el administrador. Los reportes de ventas leen datos que hoy genera el sistema VB.NET.

## 4. Facturación — preparada, no implementada

El negocio **no factura actualmente**. Las tablas `FACTURA` y `SERIEFACTURA` existen pero ningún procedimiento las usa. En fase 1:
- `SERIEFACTURA` y los correlativos de factura se configurarán cuando se active la facturación (decisión del usuario).
- No se emiten facturas ni se integra con ningún servicio externo.
- Pendiente de confirmar al activarla: requisitos fiscales/electrónicos aplicables.

## 5. Datos de consulta (solo lectura)

| Entidad | Uso en fase 1 |
|---|---|
| `VENTA`, `DETALLEVENTA` | Reportes de ventas administrativos. |
| `VENTASELIMINADAS`, `DETALLEVENTASELIMINADOS` | Pendiente de confirmar si hay reporte administrativo de ventas eliminadas (`verVentasEliminadas`, `getDetalleVentaElim`). |
| `CLIENTE` | Solo como dato descriptivo en reportes, si aplica. |

## 6. Excluido (fase 2 u otra)

- Registro de ventas, búsqueda de productos en caja, clientes en venta, corte de caja y reportes del empleado → fase 2.
- Anulación de ventas → Pendiente (fase 2 o administración).
- Emisión de facturas → cuando se active la facturación.
- Trasiegos legados (`TRASIEGOE`, `TRASIEGOSSAL`, `DETENTRADAXT`, `DETSALIDAXT`) — Pendiente confirmar si siguen en uso (P-72).

## 7. Supuestos de la fase

1. La nueva aplicación podría convivir temporalmente con el sistema VB.NET (Pendiente de confirmar, P-71).
2. Los triggers de existencias se mantienen; la nueva aplicación no duplica su lógica.
3. Cualquier cambio de estructura se hace con migraciones y scripts versionados.

## 8. Orden sugerido

Ver `docs/migration-dependencies.md` sección 4.
