# Módulo de catálogo de productos — análisis

Paso 5 de la fase 1 (`docs/migration-dependencies.md` §4). Este documento recoge el análisis del esquema
original (`docs/database-schema.sql`) antes de implementar. Igual que en los módulos anteriores, la base
nueva usa **tablas propias con FKs reales**; las tablas originales sirven solo como referencia.

Estado: **implementado**. Categorías, proveedores y productos. La consulta de existencias por sucursal pasa al módulo de compras (`docs/modules/purchases.md`). Las decisiones marcadas como Pendiente se confirman antes de implementar.

## 1. Tablas originales

### `CATEGORIA`
| Columna | Tipo | Observación |
|---|---|---|
| idCategoria | int IDENTITY(1000,1) PK | |
| categoria | varchar(50) NOT NULL | Sin índice único: se permiten nombres repetidos. |

Usada por `sp_infoProductos` y `verinventarioxcat` (filtro), `verInventarioxsuc`, `verInventarioxsucfecha`
(INNER JOIN: un producto sin categoría válida no aparece en esos reportes).

### `LABORATORIO`
| Columna | Tipo | Observación |
|---|---|---|
| idLab | int IDENTITY(1000,1) PK | |
| laboratorio | varchar(100) NOT NULL | |

**Ningún procedimiento ni trigger la usa.** `PRODUCTO.laboratorio` es `varchar(100)` de texto libre y no
referencia a esta tabla (Confirmado). Si la tabla se usaba, era solo como lista de sugerencias en la
interfaz VB.NET (Pendiente, sin código fuente).

### `ESTANTERIA`
| Columna | Tipo | Observación |
|---|---|---|
| idEstanteria | int IDENTITY(100,1) PK | **Única columna**: no tiene nombre ni descripción. |

`PRODUCTO.estanteria` (int NULL) es el número de estantería. `sp_infoProductos` lo devuelve tal cual
(`ISNULL(P.estanteria, '')`). La estantería es un número, no un registro con datos propios (Confirmado por
estructura). No indica sucursal: la ubicación es la misma en todas las sucursales.

### `PRODUCTO`
| Columna | Tipo | Observación |
|---|---|---|
| idProducto | int IDENTITY(10000,1) PK | |
| dProducto | varchar(150) NOT NULL | Nombre/descripción. |
| composicion | varchar(150) NULL | Principio activo. |
| presentacion | varchar(100) NOT NULL | |
| aterapeutica | varchar(150) NULL | Acción terapéutica. |
| indicaciones | varchar(150) NULL | |
| contraindicaciones | varchar(150) NULL | |
| observaciones | varchar(250) NULL | |
| proveedor | int NOT NULL | → `PROVEEDOR` (lógica). **Obligatorio.** |
| medida | varchar(75) NULL | |
| categoria | int NOT NULL | → `CATEGORIA` (lógica). **Obligatoria.** |
| laboratorio | varchar(100) NULL | Texto libre. |
| precio | decimal(10,2) NOT NULL | Precio de venta vigente. Sin historial (RN-63). |
| costo | decimal(10,2) NOT NULL | Costo vigente. Las compras no lo actualizan (ningún trigger lo hace). |
| fechaRegistro | date NOT NULL | Fecha de alta. |
| estanteria | int NULL | Número de estantería. |
| barcode | varchar(25) NULL | Sin índice ni restricción de unicidad. |
| stockmin | int NULL | Stock mínimo, **único para todas las sucursales**; `verprodsstockmin` lo compara con la existencia de cada sucursal. |

- No hay FKs ni índices aparte de la PK.
- No hay campo de estado: no se sabe cómo se descontinuaba un producto (Pendiente).
- No hay procedimiento de alta/edición: el mantenimiento lo hacía el código VB.NET (Pendiente).

### `Existencias`
| Columna | Tipo | Observación |
|---|---|---|
| IdSucursal, IdProducto | int, PK compuesta | Relaciones lógicas a `SUCURSAL` y `PRODUCTO`. |
| Stock | int NULL | Admite negativos (RN-62). |
| FechaActualizacion | datetime NULL, default `getdate()` | Los triggers la ajustan con desfases fijos de −1 h o −6 h (zona horaria improvisada). |

## 2. Cómo se crean las filas de `Existencias` (RN-64)

| Movimiento | Si no existe la fila |
|---|---|
| Venta (`grabaVenta`) | La crea con stock negativo. |
| Salida por traslado (trigger) | La crea con stock negativo. |
| Entrada por traslado (trigger) | La crea con la cantidad recibida. |
| Ajuste (triggers) | La crea con ± cantidad. |
| **Compra** (`trg_ActualizarExistencias_Compra`) | **No la crea: la compra no suma stock** (Confirmado). |

Ningún procedimiento inserta filas al dar de alta un producto o una sucursal. Como la compra no crea la
fila, lo más probable es que el código VB.NET la creara al registrar el producto (para todas las
sucursales) o antes de la compra (Pendiente, sin código fuente).

Los reportes tratan la ausencia de fila de forma distinta:
- `sp_infoProductos`, `sp_infoProdCompras`, `verinventarioxcat`, `sp_getStoc`: `LEFT JOIN` + `ISNULL(..., 0)`, es decir, sin fila = 0.
- `verInventarioxsuc`, `verInventarioxsucfecha`: `INNER JOIN`, es decir, **el producto sin fila no aparece**.
- `verprodsstockmin`: `LEFT JOIN` sin `ISNULL`; `stockmin > NULL` es falso, así que **un producto sin fila no aparece como bajo stock** aunque tenga 0.

## 3. Consultas del catálogo en el sistema original

| Procedimiento | Uso | Columnas |
|---|---|---|
| `sp_infoProductos @suc` | Catálogo completo con existencia de la sucursal (búsqueda en venta). Sin filtro ni paginación. | id, nombre, existencia, laboratorio, presentación, precio, categoría, medida, proveedor, indicaciones, estantería, barcode |
| `sp_infoProdCompras @suc` | Catálogo para compras. | id, nombre, existencia, laboratorio, presentación, **costo**, proveedor, medida |
| `sp_getStoc @suc, @pro` | Existencia de un producto. | existencia |

Observación: `verInventarioxsuc` y `verInventarioxsucfecha` devuelven `PROVEEDOR.rzProveedor` con el alias
`laboratorio`. Proveedor y laboratorio se confunden en esos reportes (se revisará en reportes).

## 4. Decisiones

| Tema | Decisión | Origen |
|---|---|---|
| Laboratorio | **Campo de texto del producto**, sin catálogo propio. Se descarta `LABORATORIO` (ningún SP la usa). | Usuario |
| Permisos | `catalog.view` (consultar) y `catalog.manage` (administrar) cubren todo el catálogo: categorías, estanterías y productos. | Usuario |
| Categorías: campos | Nombre (máx. 50) + activo/inactivo + auditoría. Sin descripción ni jerarquía. Tabla nueva `Categories`. | Usuario |
| Categorías: unicidad | Nombre único sin distinguir mayúsculas, tildes ni espacios al inicio/fin ("Analgésicos" = "ANALGESICOS"). La "ñ" sí se distingue de la "n" (reglas del español). Columna con collation `Modern_Spanish_100_CI_AI` + índice único + validación en el servicio. Las inactivas también cuentan. Distinto del original, que admitía repetidos. | Usuario (P-79) |
| Categorías: baja | Solo desactivar; no se borran. Una categoría inactiva no se lista por defecto. | Usuario |
| Categorías en productos | Al crear o editar un producto solo se ofrecen categorías activas. Un producto cuya categoría se desactiva la conserva. | Usuario |
| Datos existentes | Se importarán después mediante script, con Id nuevos (no se conservan los originales). | Usuario (P-80) |
| Estantería | **Ubicación por sucursal en texto libre** (p. ej. "A-3", "Vitrina 2"). Su objetivo es que el usuario sepa dónde buscar el producto en su sucursal. Sin catálogo ni pantalla de estanterías; se descarta `ESTANTERIA`. Se guarda en `ProductBranchSettings.Shelf` (máx. 30). | Usuario (P-75) |
| Proveedores: campos | Todos los del original (`PROVEEDOR`): razón social (máx. 100), NIT, dirección, teléfono, correo, contacto y teléfono del contacto; opcionales salvo la razón social. Activo/inactivo y auditoría. Tabla nueva `Suppliers`. | Usuario (P-76) |
| Proveedores: unicidad | Razón social única sin distinguir mayúsculas ni tildes (inactivos cuentan). NIT único cuando se informa (varios sin NIT permitidos). Distinto del original, sin restricciones. | Usuario |
| Proveedores: baja | Solo desactivar. Un proveedor inactivo no se ofrece al crear o editar productos; el producto que ya lo tiene lo conserva. | Usuario |
| Productos: campos | Los de `PRODUCTO` (nombre, presentación, composición, acción terapéutica, indicaciones, contraindicaciones, observaciones, medida, laboratorio, código de barras, categoría, proveedor, precio, costo, stock mínimo) más tipo y estado. Categoría y proveedor son obligatorios y con FK real. La fecha de registro es `CreatedAtUtc` de la auditoría. Tablas nuevas `Products` y `ProductBranchSettings`. | Usuario |
| Productos: precio | Precio general del producto + precio opcional por sucursal. La sucursal sin precio propio usa el general. Costo único para todas las sucursales. | Usuario |
| Productos: stock mínimo | Valor general opcional + valor opcional por sucursal; la sucursal sin valor propio usa el general. | Usuario |
| Productos: validaciones | Nombre y presentación obligatorios. Costo > 0, precio > 0 y precio > costo (también cada precio de sucursal). Máximo 2 decimales y 99,999,999.99. Stock mínimo ≥ 0. Categoría y proveedor existentes y activos al crearlos o cambiarlos (el producto conserva uno que luego se desactiva). Sucursales sin repetir, existentes y activas. | Usuario |
| Productos: unicidad | Nombre + presentación únicos sin distinguir mayúsculas ni tildes (la misma medicina convive en varias presentaciones). Código de barras único cuando se informa; vacío se guarda como nulo y puede repetirse. | Usuario (P-79) |
| Productos: tipo | Dos indicadores combinables: **controlado** y **bonificable**; "normal" es ninguno de los dos. Por ahora solo se guardan, se filtran y se muestran; las reglas de venta se definen con ventas. | Usuario |
| Productos: baja | Solo desactivar; no se borran. | Usuario (P-78) |
| Productos: sucursales | `ProductBranchSettings` guarda precio, stock mínimo y estantería por sucursal; solo existe fila si algún valor está informado. Modificar una fila actualiza la auditoría del producto. | Usuario |
| Productos: existencias | Crear un producto o una sucursal **no** crea filas de existencia. La fila se crea con el primer movimiento (incluida la compra) y la ausencia cuenta como 0. Se implementa con compras, ajustes, traslados y ventas. | Usuario (P-77) |
| Formato de textos | Ver `docs/data-conventions.md`. | Usuario |

## 5. Preguntas pendientes

- **P-75 — Estantería:** resuelta (texto libre por sucursal, ver §4).
- **P-76 — Proveedor del producto:** resuelta (se implementa antes que productos como catálogo simple, ver §4).
- **P-77 — Existencias (RN-64):** resuelta (se crea en el primer movimiento y la ausencia cuenta como 0, ver §4).
- **P-78 — Estado del producto:** resuelta (activo/inactivo, ver §4).
- **P-79 — Unicidad:** categoría resuelta (nombre único, ver §4). código de barras resuelto (único cuando se informa, ver §4).
- **P-80 — Datos existentes:** resuelta (importación posterior con Id nuevos, ver §4).
- **P-74** (ya registrada): productos sin fila en `Existencias` o con stock negativo en los datos actuales.
