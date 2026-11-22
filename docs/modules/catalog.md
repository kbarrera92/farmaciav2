# Módulo de catálogo de productos — análisis

Paso 5 de la fase 1 (`docs/migration-dependencies.md` §4). Este documento recoge el análisis del esquema
original (`docs/database-schema.sql`) antes de implementar. Igual que en los módulos anteriores, la base
nueva usa **tablas propias con FKs reales**; las tablas originales sirven solo como referencia.

Estado: **análisis**. Las decisiones marcadas como Pendiente se confirman antes de implementar.

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
| Productos | Se implementan **después**, con las mejoras que definirá el usuario. Este análisis es la base. | Usuario |

## 5. Preguntas pendientes

- **P-75 — Estantería:** en el original es solo un número. ¿Se mantiene como número/texto libre en el producto, o se crea un catálogo con nombre (p. ej. "A-3")? ¿La ubicación debe ser distinta por sucursal?
- **P-76 — Proveedor del producto:** `PRODUCTO.proveedor` es obligatorio, pero proveedores es el paso 6. ¿Se implementa proveedores antes que productos, o el proveedor del producto pasa a ser opcional?
- **P-77 — Existencias (RN-64):** en la base nueva, ¿se crea la fila de existencia de cada sucursal al crear el producto (y de cada producto al crear una sucursal), o se crea al primer movimiento y la ausencia cuenta como 0? Recomendación: crearla en el primer movimiento (incluida la compra) y tratar la ausencia como 0 en todas las consultas.
- **P-78 — Estado del producto:** no existe en el original. ¿Se agrega activo/inactivo como en los demás módulos (no se borran productos con movimientos)?
- **P-79 — Unicidad:** ¿nombre de categoría único? ¿Código de barras único cuando se informa?
- **P-80 — Datos existentes:** ¿se importarán las categorías y productos del sistema original a la base nueva? Afecta a si se conservan los Id originales (10000+, 1000+).
- **P-74** (ya registrada): productos sin fila en `Existencias` o con stock negativo en los datos actuales.
