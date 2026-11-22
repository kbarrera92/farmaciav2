# Módulo de compras — análisis

Paso 7 de la fase 1 (`docs/migration-dependencies.md` §4). Este documento recoge el análisis del esquema
original (`docs/database-schema.sql`) antes de implementar. Igual que en los módulos anteriores, la base
nueva usa **tablas propias con FKs reales**; las tablas originales sirven solo como referencia.

Estado: **análisis**. Nada implementado. P-81 a P-88 respondidas, con puntos sueltos en §5. Las decisiones marcadas como Pendiente se confirman antes de implementar.

Este módulo incluye también la **tabla de existencias** y su **consulta por sucursal**, que quedaron fuera del
catálogo: la compra es el primer movimiento que genera stock (P-77, `docs/modules/catalog.md` §4).

## 1. Tablas originales

### `COMPRA`
| Columna | Tipo | Observación |
|---|---|---|
| nCompra | int, PK, identity | |
| fechaC | date NOT NULL | Fecha de la compra (sin hora). |
| usuario | int NOT NULL | FK a `USUARIO`. |
| total | decimal(10,2) NOT NULL | Suma de los subtotales del detalle (ver §2). |
| documento | bigint NOT NULL | Número del documento del proveedor (factura o similar). Significado exacto Pendiente. |
| sucursal | int NOT NULL | FK a `SUCURSAL`. Sucursal que recibe el producto. |
| proveedor | int NOT NULL | FK a `PROVEEDOR`. |
| formaPago | int NOT NULL | FK a `FORMAPAGO`. |
| fechaPago | date NULL | Cuándo se pagó. Nulo = no pagada, o no aplica (Pendiente). |

- FKs reales: `FK_COMPRA_FORMAPAGO`, `FK_COMPRA_PROVEEDOR`, `FK_COMPRA_SUCURSAL`, `FK_COMPRA_USUARIO`.
- No hay restricción de unicidad sobre `documento`.

### `DETALLECOMPRA`
| Columna | Tipo | Observación |
|---|---|---|
| nDetalleC, nCompra | smallint + int, PK compuesta | `nDetalleC` es el número de línea dentro de la compra, sin identity (lo asigna el código VB.NET). |
| producto | int NOT NULL | Relación lógica a `PRODUCTO` (sin FK). |
| cantidad | int NOT NULL | |
| precio | decimal(10,2) NOT NULL | Precio de compra por unidad (costo). |
| subtotal | decimal(10,2) NOT NULL | Calculado por el código; sin columna calculada ni validación. |

- No tiene FK a `COMPRA` ni a `PRODUCTO`: se pueden dejar detalles huérfanos.

### Tablas relacionadas (se analizan con cuentas por pagar)
- `CUENTAXPAGAR`: `idCuenta` (identity desde 1000), concepto, fechaInicio, fechaLimite, totalCuenta, saldoCuenta, estado (`ESTADOCXP`), `idProveedor`, **`idCompra`**. Sin FKs. Es el vínculo compra → cuenta por pagar.
- `DETALLECXP`: abonos (`fechaPago`, `abono`, `nreciboProveedor`).
- `FORMAPAGO`: `idFormaPago` (sin identity), `foraPago` (sic). Es el mismo catálogo que usan las ventas.

## 2. Triggers sobre `DETALLECOMPRA`

| Trigger | Evento | Efecto |
|---|---|---|
| `trg_ActualizarExistencias_Compra` | AFTER INSERT | `UPDATE Existencias SET Stock = Stock + cantidad` para (sucursal de la compra, producto). **Solo si la fila ya existe** (no hay INSERT, RN-64). Fecha de actualización con desfase de −6 h. |
| `TR_updateTotalActualizarC` | AFTER UPDATE | Recalcula `COMPRA.total` como suma de subtotales. |
| `TR_actualizartotalBorrarC` | AFTER DELETE | Recalcula `COMPRA.total` (0 si no quedan líneas). |

Observaciones (sin código fuente VB.NET, por eso son Pendiente de confirmar con el usuario):
- **No hay trigger de total en INSERT**: el total de la compra lo escribe el código al registrarla.
- **El stock solo se ajusta en INSERT**. Modificar la cantidad de una línea o borrarla **no revierte ni corrige el stock**, aunque el total sí se recalcula. El sistema original queda inconsistente si se edita una compra.
- Si el producto no tiene fila en `Existencias` para esa sucursal, la compra **no suma nada** y no avisa. Es lo que P-77 corrige: la compra nueva crea la fila.
- `Stock` admite negativos y es `NULL`-able (RN-62); `NULL + cantidad` da `NULL`.

## 3. Consultas del sistema original

| Procedimiento | Uso | Columnas |
|---|---|---|
| `getCompras` | Listado de compras. Sin filtros ni paginación. Orden: `fechaC` ascendente, luego `nCompra` descendente. | nCompra, fechaC, nombreUsuario, total, documento, nombreSuc, rzProveedor, forma de pago, fechaPago (`''` si es nulo) |
| `getDetalleCompra @suc` | Detalle de una compra. **El parámetro se llama `@suc` pero recibe `nCompra`**. | nDetalleC, nombre del producto, cantidad, precio, subtotal |
| `sp_infoProdCompras @suc` | Catálogo para elegir productos al comprar. Todos los productos con su proveedor y la existencia de la sucursal (`LEFT JOIN`, sin fila = 0). | idProducto, nombre, existencia, laboratorio, presentación, costo, proveedor, medida |

Observación: `sp_infoProdCompras` usa `INNER JOIN` con proveedor, así que el producto sin proveedor no aparece. En la base nueva el proveedor del producto es obligatorio (`catalog.md` §4), por lo que no aplica.

## 4. Decisiones

Heredadas del catálogo (ya resueltas):

| Tema | Decisión | Origen |
|---|---|---|
| Existencias | La fila de existencia se crea con el primer movimiento, incluida la compra; la ausencia cuenta como 0. | Usuario (P-77) |
| Proveedores inactivos | Un proveedor inactivo no se ofrece al crear una compra. | `catalog.md` §4 |
| Datos existentes | Se importan después mediante script, con Id nuevos. | Usuario (P-80) |

De este módulo:

| Tema | Decisión | Origen |
|---|---|---|
| Compra al crédito | La compra al crédito es la que tiene cuenta por pagar. | Usuario (P-81) |
| Documento | `documento` es el número de factura entregado por el proveedor. | Usuario (P-82) |
| Editar y anular | Solo el administrador, dejando auditoría, y el stock se ajusta en consecuencia. | Usuario (P-83) |
| Costo del producto | Registrar una compra **no** actualiza `Products.Cost`. | Usuario (P-84) |
| Productos elegibles | Se pueden comprar productos de cualquier proveedor. | Usuario (P-85) |
| Quién registra | El administrador registra compras; el permiso se puede asignar a otros roles (p. ej. bodeguero). | Usuario (P-86) |
| Stock negativo | La tabla de existencias admite stock negativo, como el original (RN-62). | Usuario (P-87) |
| Consulta de existencias | Va en el módulo de compras, con filtros por sucursal y categoría. | Usuario (P-88) |

## 5. Preguntas pendientes

Resueltas: P-81 a P-88 (ver §4). Quedan estos puntos sueltos de esas preguntas:

- **P-81 (resto):** ¿cómo se distingue la compra al crédito (forma de pago "crédito" o campo contado/crédito)? ¿Qué significa `fechaPago` en la compra? ¿La cuenta por pagar se crea automáticamente al registrar la compra?
- **P-82 (resto):** ¿`documento` debe ser único por proveedor?
- **P-83 (resto):** ¿se permiten editar y anular, o solo una? Si hay anulación, ¿la compra queda marcada como anulada o se borra? ¿Qué pasa si la cuenta por pagar ya tiene abonos? (depende de P-81)
- **P-84 (resto):** si el precio de compra es mayor o igual al precio de venta, ¿se permite, se avisa o se bloquea?
- **P-85 (resto):** ¿se pueden comprar productos inactivos?
- **P-86 (resto):** permisos propuestos: `purchases.view`, `purchases.create` y `purchases.manage` (editar y anular, solo administrador). ¿Sucursal de la compra: la del usuario o la elige el usuario?
- **P-88 (resto):** columnas de la consulta de existencias (propuesta: producto, categoría, existencia, stock mínimo de la sucursal) y si incluye un filtro de bajo stock mínimo.
- **P-74** (ya registrada): productos sin fila en `Existencias` o con stock negativo en los datos actuales.
