# Módulo de compras — análisis

Paso 7 de la fase 1 (`docs/migration-dependencies.md` §4). Este documento recoge el análisis del esquema
original (`docs/database-schema.sql`) antes de implementar. Igual que en los módulos anteriores, la base
nueva usa **tablas propias con FKs reales**; las tablas originales sirven solo como referencia.

Estado: **análisis cerrado**. Backend en curso: tablas, permisos y servicio de compras hechos; consulta de existencias y frontend pendientes. P-81 a P-88 respondidas.

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
| Compra al crédito | La compra tiene un tipo **Contado** o **Crédito**. La compra al crédito es la que tiene cuenta por pagar y la crea automáticamente, con fecha límite, calculada con **días de crédito**, que el usuario ingresa en cada compra (valor por defecto 15, editable). La forma de pago se usa solo para contado. | Usuario (P-81) |
| Fecha de pago | `fechaPago`: en contado es la fecha de pago de la compra; en crédito es la fecha en que se cancela la cuenta por pagar (vacía hasta entonces). Esa actualización pertenece al módulo de cuentas por pagar. | Usuario (P-81) |
| Documento | `documento` es el número de factura entregado por el proveedor. **Único por proveedor**: no se repite la misma factura para el mismo proveedor. | Usuario (P-82) |
| Editar y anular | Solo el administrador. Puede **editar** líneas y cantidades (el stock se ajusta por la diferencia) y **anular** la compra completa. La compra anulada queda marcada, no se borra, y su stock se resta. Ambas acciones dejan auditoría. Una compra al crédito cuya cuenta por pagar ya tiene abonos **no se puede editar ni anular**. Sin abonos, se puede cambiar el tipo: de crédito a contado se descarta la cuenta por pagar; de contado a crédito se crea. | Usuario (P-83) |
| Costo del producto | Registrar una compra **no** actualiza `Products.Cost`. Si el precio de compra es mayor o igual al precio de venta, se permite guardar con una **advertencia**. | Usuario (P-84) |
| Productos elegibles | Productos de cualquier proveedor, **solo productos activos**. | Usuario (P-85) |
| Quién registra | El administrador registra compras; el permiso se puede asignar a otros roles (p. ej. bodeguero). El usuario **elige la sucursal** de la compra en el formulario. Permisos: `purchases.view` (consultar), `purchases.create` (registrar) y `purchases.manage` (editar y anular, solo administrador). | Usuario (P-86) |
| Stock negativo | La tabla de existencias admite stock negativo, como el original (RN-62). | Usuario (P-87) |
| Consulta de existencias | Va en el módulo de compras, con filtros por sucursal y categoría. Columnas: producto, categoría, existencia y stock mínimo de la sucursal. Sin filtro de bajo stock. | Usuario (P-88) |
| Número de documento | Se guarda como **texto** (máx. 30), no como `bigint`: admite letras, guiones y ceros a la izquierda. La unicidad por proveedor no distingue mayúsculas ni tildes e **ignora las compras anuladas**, de modo que una factura anulada puede registrarse de nuevo. | Usuario |
| Fechas | El usuario elige la fecha de compra (hoy por defecto, no futura). En contado, la fecha de pago es la fecha de compra. | Usuario |
| Anulación | Exige un **motivo** (máx. 250). Queda registrado quién y cuándo anuló. | Usuario |
| Cuenta por pagar | Este paso crea una tabla mínima `AccountsPayable` (compra, proveedor, fecha de inicio, fecha límite, total, saldo, estado Pendiente/Pagada). No hay tabla de abonos todavía: hasta que exista el módulo de cuentas por pagar, "tiene abonos" significa saldo distinto del total. | Usuario |

| Fecha "de hoy" | La regla "no futura" usa la hora local del negocio: opción `Business:UtcOffsetHours` (por defecto −6, Guatemala sin horario de verano, igual que el desfase del original). | Decisión técnica |
| Edición | La sucursal de una compra registrada no se puede cambiar (el stock ya se aplicó a esa sucursal). Se pueden editar fecha, proveedor, documento, tipo, forma de pago, días de crédito y líneas. | Decisión técnica (P-83) |

API: `GET/POST /api/purchases`, `GET/PUT /api/purchases/{id}`, `POST /api/purchases/{id}/cancel`, `GET /api/purchases/products` (productos elegibles con existencia y precio de la sucursal).

Tablas nuevas (migración `0007_Purchases.sql`): `Purchases`, `PurchaseItems`, `Stocks` y `AccountsPayable`.

## 5. Preguntas pendientes

Resueltas: P-81 a P-88 (ver §4). Sin puntos sueltos.

- **P-74** (ya registrada): productos sin fila en `Existencias` o con stock negativo en los datos actuales.
