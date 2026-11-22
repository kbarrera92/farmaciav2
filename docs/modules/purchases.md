# Módulo de compras — análisis

Paso 7 de la fase 1 (`docs/migration-dependencies.md` §4). Este documento recoge el análisis del esquema
original (`docs/database-schema.sql`) antes de implementar. Igual que en los módulos anteriores, la base
nueva usa **tablas propias con FKs reales**; las tablas originales sirven solo como referencia.

Estado: **análisis**. Nada implementado. Las decisiones marcadas como Pendiente se confirman antes de implementar.

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
| Proveedores y productos inactivos | Un proveedor inactivo no se ofrece al crear; un producto inactivo no se ofrece en compras (a confirmar en P-85). | `catalog.md` §4 |
| Datos existentes | Se importan después mediante script, con Id nuevos. | Usuario (P-80) |

No hay decisiones nuevas todavía.

## 5. Preguntas pendientes

- **P-81 — Contado y crédito:** `COMPRA.formaPago` apunta al catálogo de formas de pago (efectivo, tarjeta...) y `CUENTAXPAGAR.idCompra` enlaza una cuenta con la compra. ¿Una compra al crédito es la que tiene cuenta por pagar? ¿Cómo se distingue (forma de pago especial, `fechaPago` nula)? ¿Qué significa `fechaPago` en la compra?
- **P-82 — Documento:** `documento` es obligatorio y numérico (bigint). ¿Es el número de factura del proveedor? ¿Debe ser único por proveedor?
- **P-83 — Editar o anular:** el original solo ajusta el stock al insertar líneas. ¿Se permite editar o eliminar una compra registrada? Si sí, ¿se revierte el stock? ¿O solo se anula y se corrige con un ajuste de inventario?
- **P-84 — Costo del producto:** ¿registrar una compra actualiza el costo del producto (`Products.Cost`)? ¿Qué pasa si el precio de compra es mayor o igual al precio de venta (el catálogo exige precio > costo)?
- **P-85 — Productos elegibles:** ¿se pueden comprar productos de cualquier proveedor en una compra o solo los del proveedor elegido? ¿Y productos inactivos?
- **P-86 — Sucursal y permisos:** ¿quién registra compras (administración o cada sucursal) y para cuál sucursal? ¿Permisos nuevos `purchases.view` y `purchases.manage`?
- **P-87 — Stock negativo:** la tabla nueva de existencias, ¿admite negativos (RN-62, R-06) o se valida en ventas, ajustes y traslados?
- **P-88 — Consulta de existencias:** ¿qué columnas y filtros necesita (sucursal, categoría, proveedor, solo bajo stock mínimo)? ¿Es parte de este módulo o va con reportes?
- **P-74** (ya registrada): productos sin fila en `Existencias` o con stock negativo en los datos actuales.
