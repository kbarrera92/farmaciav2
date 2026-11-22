# Análisis de base de datos — Análisis inicial

> **Nota de alcance (actualizada):** este análisis se hizo cuando la fase 1 era operativa. Ahora la fase 1 es administrativa (`docs/phase-1-scope.md`) y la operación pasa a la fase 2 (`docs/phase-2-scope.md`). Las tablas listadas en 2.5 como "fuera de alcance" (compras, ajustes, cuentas por pagar, catálogos) **entran en la fase 1** y se analizarán en detalle al iniciar cada módulo.

Fuente: `docs/database-schema.sql` (script generado el 21/11/2026, base `db_acf2eb_farmaciahorro`, SQL Server 2022 / MSSQL16).

Leyenda de estado:
- **Confirmado**: visible en el script (estructura, procedimiento o trigger).
- **Pendiente de confirmar**: inferido o dependiente del código VB.NET / datos reales, no disponibles.

---

## 1. Visión general

| Elemento | Cantidad | Observación |
|---|---|---|
| Tablas | 37 | Todas en `dbo`. |
| Tipos tabla | 1 | `DETALLESVENTA` (parámetro de `grabaVenta`). |
| Procedimientos | 36 | Ninguna función escalar ni de tabla. |
| Triggers | 8 | 3 relacionados con traslados, 5 con compras/ajustes. |
| Vistas | 0 | — |
| Índices no clúster | 0 | Solo existen los índices clúster de las claves primarias. |
| Claves foráneas | 7 | **Solo** en `AJUSTE` (3) y `COMPRA` (4). |
| Defaults | 2 | `Existencias.FechaActualizacion = getdate()`, `USUARIO.estado = 1`. |
| CHECK / UNIQUE | 0 | — |

**Consecuencia importante (Confirmado):** ninguna tabla de la fase 1 (`USUARIO`, `VENTA`, `DETALLEVENTA`, traslados, `Existencias`) tiene claves foráneas. Todas las relaciones descritas abajo son **lógicas** (deducidas de nombres de columnas y de los JOIN en procedimientos/triggers) y **no están forzadas por la base de datos**. La integridad referencial depende hoy de la aplicación.

Otras observaciones generales (Confirmado):
- No hay índice sobre `USUARIO.nick`, `VENTA.fechaVenta`, `VENTA.usuario`, `VENTA.idSucursal`, `PRODUCTO.barcode`, `ENTXTRASLADO.nSalidaRelacionada`, etc.; todas son columnas de filtro frecuentes en los procedimientos.
- `USUARIO.nick` no es único a nivel de base de datos.
- Se usan tipos `smallint` para números de línea de detalle y cantidades de traslado (máximo 32 767).

---

## 2. Tablas por módulo

### 2.1 Seguridad y auditoría

#### `USUARIO`
| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| idUsuario | int IDENTITY(200,1) | No | PK |
| nombreUsuario | varchar(100) | No | Nombre visible. `getTraslados` filtra por este campo. |
| nick | varchar(50) | No | Login. No es único (sin índice UNIQUE). |
| contraUsuario | varchar(50) | No | **Contraseña en texto plano** (se compara por igualdad en `sp_validaUsuario`). |
| tipoUsuario | int | No | → `TIPOUSUARIO.idTipoUsuario` (lógica) |
| sucursal | int | No | → `SUCURSAL.idSucursal` (lógica). Un usuario pertenece a **una** sucursal. |
| estado | int | No | → `ESTADOUSUARIO.idEstado` (lógica). Default `1`. |

#### `TIPOUSUARIO`
- `idTipoUsuario` int IDENTITY(100,100) PK, `nombreTipo` varchar(100).
- Valores conocidos por código: `'ADMINISTRADOR'`, `'GERENTE'` (usados en `getTraslados`). El resto de roles (p.ej. vendedor/empleado) — **Pendiente de confirmar**.

#### `ESTADOUSUARIO`
- `idEstado` int PK, `estadoUsuario` varchar(50).
- Valor conocido: `'ACTIVO'` (`sp_validaUsuario`). Probable `idEstado = 1` = ACTIVO por el default — **Pendiente de confirmar**.

#### `BITACORA`
| Columna | Tipo | Notas |
|---|---|---|
| correlativo | int PK | No es IDENTITY; se obtiene de `SECUENCIAL` vía `sp_get_sequence 'BITACORA'`. |
| fecha | datetime | `GETDATE() - 6 h` (ver §5). |
| usuario | varchar(50) | Guarda el **nick** (texto), no el id. |
| estacion | varchar(100) | Nombre de equipo/estación de trabajo. |
| accion | varchar(100) | Texto libre. |

#### `SECUENCIAL`
- `SE_TABLE` varchar(50) PK, `SE_SEQ` int. Contador manual por nombre de tabla.
- Uso confirmado: `BITACORA`. Otros usos (p.ej. número de factura, `documento`) — **Pendiente de confirmar**.

### 2.2 Ventas

#### `VENTA`
| Columna | Tipo | Nulo | Notas |
|---|---|---|---|
| nVenta | int IDENTITY(1,1) | No | PK |
| fechaVenta | datetime | Sí | `GETDATE() + 2 h` en `grabaVenta`. |
| usuario | int | Sí | → `USUARIO.idUsuario` (lógica) |
| total | decimal(18,2) | Sí | Enviado por la aplicación; el SP no lo recalcula. |
| documento | bigint | Sí | Significado **Pendiente de confirmar** (¿número de factura/recibo?). El SP lo recibe como `varchar(20)`. |
| idSucursal | int | Sí | → `SUCURSAL.idSucursal` (lógica) |
| cliente | varchar(20) | Sí | Probablemente NIT → `CLIENTE.nitCliente` (lógica, **Pendiente**). El SP lo recibe como `varchar(10)`. |
| efectivo | decimal(18,2) | Sí | Monto pagado en efectivo. |
| tarjeta | decimal(18,2) | Sí | Monto pagado con tarjeta. |
| autorizacion | varchar(50) | Sí | Número de autorización de tarjeta. El SP lo recibe como `varchar(16)`. |

#### `DETALLEVENTA`
| Columna | Tipo | Notas |
|---|---|---|
| nDetalleV | smallint | PK (1/2). Número de línea generado por la aplicación. |
| nVenta | int | PK (2/2). → `VENTA.nVenta` (lógica) |
| producto | int NULL | → `PRODUCTO.idProducto` (lógica) |
| cantidad | int NULL | |
| precio | decimal(18,2) NULL | Precio aplicado en la venta (puede diferir de `PRODUCTO.precio`). |
| subtotal | decimal(18,2) NULL | Enviado por la aplicación. |

#### Tipo tabla `DETALLESVENTA`
`nDetalleVenta smallint, producto int, cantidad int, precio decimal(18,2), subtotal decimal(18,2)` — todos NOT NULL. Es el parámetro `@detalles` de `grabaVenta`.

#### `CLIENTE`
- `nitCliente` varchar(20) PK, `rzCliente` (razón social), `direccionCliente`, `telefonoCliente`, `correoCliente`.

#### `FACTURA` y `SERIEFACTURA`
- `FACTURA`: `nFactura` int PK (no IDENTITY), `fecha`, `nitCliente` varchar(25), `nombreCliente`, `direccionCliente`, `serie` → `SERIEFACTURA.idSerie` (lógica), `nVenta` → `VENTA` (lógica), `estado` int (sin catálogo).
- `SERIEFACTURA`: `idSerie` IDENTITY PK, `letra` varchar(5), `sucursal` → `SUCURSAL` (lógica).
- **Ningún procedimiento ni trigger usa estas tablas.** Si se usan, lo hacía el código VB.NET — **Pendiente de confirmar**.

#### `VENTASELIMINADAS` y `DETALLEVENTASELIMINADOS`
- Copia de `VENTA`/`DETALLEVENTA` con fecha de eliminación (`fechElim`, `felim`). `DETALLEVENTASELIMINADOS` **no tiene PK**.
- Consultadas por `verVentasEliminadas` y `getDetalleVentaElim`. No existe procedimiento que realice la eliminación ni que reponga existencias — **Pendiente de confirmar** el flujo (probablemente en código VB.NET).

### 2.3 Inventario

#### `Existencias`
| Columna | Tipo | Notas |
|---|---|---|
| IdSucursal | int | PK (1/2) → `SUCURSAL` (lógica) |
| IdProducto | int | PK (2/2) → `PRODUCTO` (lógica) |
| Stock | int NULL | Existencia actual. **Puede ser negativa** (los SP/triggers insertan filas con stock negativo). |
| FechaActualizacion | datetime NULL | Default `getdate()`; se actualiza en cada movimiento con desfases distintos (ver §5). |

Es el **único** registro de existencias: no hay kárdex/tabla de movimientos. El historial se reconstruye desde ventas, traslados, compras y ajustes.

#### `PRODUCTO`
- `idProducto` IDENTITY(10000,1) PK, `dProducto` (descripción), `presentacion`, `precio` decimal(10,2) (precio de venta vigente), `costo`, `barcode` varchar(25) NULL, `stockmin`, `categoria` → `CATEGORIA`, `proveedor` → `PROVEEDOR`, `estanteria` → `ESTANTERIA` (lógicas), `laboratorio` varchar(100) (texto libre; no referencia a `LABORATORIO`), además de composición, indicaciones, contraindicaciones, etc.
- No tiene campo de estado (activo/inactivo) — **Pendiente confirmar** cómo se descontinúan productos.
- `barcode` no es único.

#### `SUCURSAL`
- `idSucursal` IDENTITY(10000,1) PK, `nombreSuc`, `direccionSuc`, `telefono`. Sin campo de estado.

### 2.4 Traslados

Modelo actual (Confirmado por estructura y SP/trigger):

```text
Sucursal ORIGEN                                   Sucursal DESTINO
SALXTRASLADO (nSXTraslado, sucursalSalida,        ENTXTRASLADO (nEXTraslado, sucursal,
              usuario, fecha, recibido N/S)  <──── nSalidaRelacionada, usuario, fecha)
   │                                                  │
   └─ DETSALXTRASLADO (producto, cantidad)            ├─ DETENTRADAXTRASLADOTEMP (pendiente de recibir)
        trigger: resta Existencias origen             └─ DETENTRADAXTRASLADO (recibido)
                                                          trigger: suma Existencias destino
```

#### `SALXTRASLADO` (cabecera de salida)
| Columna | Tipo | Notas |
|---|---|---|
| nSXTraslado | int IDENTITY(1,1) | PK |
| fechasxtraslado | date | Solo fecha, sin hora. |
| sucursalSalida | int | Sucursal origen (lógica → `SUCURSAL`). |
| usuario | int | Usuario que genera (lógica → `USUARIO`). |
| recibido | char(1) NULL | Estado: `'N'` pendiente, `'S'` recibido. `NULL` es tratado como "ya procesado" por `sp_RecibeTraslado`. |

**No tiene columna de sucursal destino**: el destino solo se conoce por `ENTXTRASLADO.sucursal` (Confirmado).

#### `DETSALXTRASLADO`
- PK (`ndetxtraslado` int, `nsalxtraslado` int), `producto` int, `cantidad` **smallint**. Sin precio/costo.

#### `ENTXTRASLADO` (cabecera de entrada)
- `nEXTraslado` IDENTITY(1,1) PK, `fecha` date, `sucursal` (destino), `usuario`, `nSalidaRelacionada` int NULL → `SALXTRASLADO.nSXTraslado` (lógica).
- Ningún SP la inserta: la crea el código VB.NET. **Pendiente de confirmar** si se crea al generar el traslado (lo más probable, porque `sp_RecibeTraslado` la exige ya creada) y qué usuario/fecha guarda (el que envía o el que recibe).

#### `DETENTRADAXTRASLADOTEMP`
- PK (`ndetentradaxtraslado`, `nentradaxtraslado`), `producto`, `cant` int. Detalle de la entrada **pendiente de recibir**. Sin trigger: no afecta existencias.

#### `DETENTRADAXTRASLADO`
- PK (`ndetentrxtralad` smallint, `nentrxtrasld` int), `producto`, `cant` smallint. Detalle **recibido**; su inserción suma existencias en destino.

#### Tablas de trasiego (legado — Pendiente de confirmar)
`TRASIEGOE`, `TRASIEGOSSAL` (cabeceras con fecha/usuario/sucursal), `DETENTRADAXT`, `DETSALIDAXT` (con precio e importe). No las usa ningún SP ni trigger. Parecen una versión anterior del módulo de traslados, o un movimiento distinto (¿trasiego interno?).

### 2.5 Tablas fuera de alcance (referencia)

`AJUSTE`, `DETAJUSTE`, `TIPOAJUSTE`, `COMPRA`, `DETALLECOMPRA`, `CUENTAXPAGAR`, `DETALLECXP`, `ESTADOCXP`, `FORMAPAGO`, `PROVEEDOR`, `CATEGORIA`, `LABORATORIO`, `ESTANTERIA`. Relevantes solo porque compras y ajustes **también modifican `Existencias`** mediante triggers.

---

## 3. Relaciones relevantes

| Relación | Tipo | Evidencia |
|---|---|---|
| USUARIO.tipoUsuario → TIPOUSUARIO.idTipoUsuario | Lógica | `sp_validaUsuario` |
| USUARIO.estado → ESTADOUSUARIO.idEstado | Lógica | `sp_validaUsuario` |
| USUARIO.sucursal → SUCURSAL.idSucursal | Lógica | Nombre de columna; devuelto por `sp_validaUsuario` |
| VENTA.usuario → USUARIO.idUsuario | Lógica | `ReporteDeVentasPorUsuario`, `sp_ventasxproducto` |
| VENTA.idSucursal → SUCURSAL.idSucursal | Lógica | `sp_ventasSuc` y otros |
| VENTA.cliente → CLIENTE.nitCliente | Lógica, **Pendiente** | Solo por nombre/tamaño de columna; ningún JOIN |
| DETALLEVENTA.nVenta → VENTA.nVenta | Lógica | `grabaVenta`, reportes |
| DETALLEVENTA.producto → PRODUCTO.idProducto | Lógica | `getDetallesVentas` |
| FACTURA.nVenta → VENTA.nVenta | Lógica, **Pendiente** | Solo por nombre |
| Existencias (IdSucursal, IdProducto) → SUCURSAL, PRODUCTO | Lógica | `sp_infoProductos`, triggers |
| SALXTRASLADO.sucursalSalida → SUCURSAL | Lógica | `getTraslados`, `sp_RecibeTraslado` |
| SALXTRASLADO.usuario → USUARIO | Lógica | `getTraslados` |
| DETSALXTRASLADO.nsalxtraslado → SALXTRASLADO.nSXTraslado | Lógica | trigger de salida |
| ENTXTRASLADO.nSalidaRelacionada → SALXTRASLADO.nSXTraslado | Lógica | `sp_RecibeTraslado` |
| ENTXTRASLADO.sucursal → SUCURSAL | Lógica | trigger de entrada |
| DETENTRADAXTRASLADO(TEMP).nentr… → ENTXTRASLADO.nEXTraslado | Lógica | `sp_RecibeTraslado`, trigger de entrada |
| DETENTRADAXTRASLADO ↔ DETSALXTRASLADO por (nº línea, nº cabecera) | Lógica, **frágil** | `TR_actualizarDetalle` asume `nentrxtrasld = nsalxtraslado` |

---

## 4. Procedimientos almacenados relacionados con la fase 1

### 4.1 Inicio de sesión
| Procedimiento | Parámetros | Comportamiento (Confirmado) |
|---|---|---|
| `sp_validaUsuario` | `@NICK`, `@PASSWORD` varchar(40) | Devuelve `idUsuario, nombreUsuario, tipoUsuario, nombreTipo, sucursal, estadoUsuario` si nick y contraseña coinciden **en texto plano** y el estado es `'ACTIVO'`. Parámetros de 40 caracteres frente a columnas de 50. |
| `sp_grabaBitacora` | `@NICK`, `@ESTACION`, `@ACCION`, `@MSG OUT` | Obtiene correlativo con `sp_get_sequence 'BITACORA'` e inserta en `BITACORA` con fecha `GETDATE()-6h`. |
| `sp_get_sequence` | `@TABLE_NAME`, `@SEQUENCE OUT`, `@MSG OUT` | Incrementa `SECUENCIAL.SE_SEQ`. Defecto: el `ROLLBACK` está después de `RETURN` (inalcanzable). |

### 4.2 Ventas
| Procedimiento | Comportamiento (Confirmado) |
|---|---|
| `grabaVenta` | Con `XACT_ABORT ON` y TRY/CATCH: inserta `VENTA` (fecha `GETDATE()+2h`), inserta `DETALLEVENTA` desde `@detalles`, agrupa cantidades por producto y **resta** `Existencias` en la sucursal; si no existe fila, la crea con stock negativo. No valida stock, precio, total ni pago. Devuelve `0`/`-1` y `@MSG`. |
| `sp_getStoc` | Existencia de un producto en una sucursal. |
| `sp_infoProductos` | Catálogo completo de productos con existencia en la sucursal, precio, categoría, proveedor, estantería, código de barras. Sin paginación ni filtro. |
| `getDetallesVentas` | Detalle de una venta. |
| `sp_ventasSuc`, `ventasdiariassuc` | Ventas de una sucursal (todas / por fecha). |

### 4.3 Traslados
| Procedimiento | Comportamiento (Confirmado) |
|---|---|
| `getTraslados` | `@usuario` (nombre), `@rolUsuario`, `@sucursal` (no se usa), `@opt`. Construye **SQL dinámico concatenado** (riesgo de inyección SQL). `@opt` NULL/'S': ADMIN/GERENTE ven todos; resto solo los suyos (filtrando por `nombreUsuario`). Otro `@opt`: ADMIN/GERENTE ven solo pendientes (`recibido='N'`); el resto ve **todos** los traslados sin filtro ni orden. |
| `getDetalleTraslado` | Detalle de salida con `PRODUCTO.precio` vigente × cantidad. |
| `getDetalleTrasladoE` | Detalle de entrada recibida con precio vigente. |
| `sp_RecibeTraslado` | `@OPT 'B'`: valida que la salida exista y esté en `'N'`, busca la `ENTXTRASLADO` relacionada y devuelve el detalle de `DETENTRADAXTRASLADOTEMP`. `@OPT 'R'`: copia TEMP → `DETENTRADAXTRASLADO` (dispara suma de existencias), borra TEMP y marca `recibido='S'`, dentro de transacción con control por `@@ERROR` (sin `XACT_ABORT`). Devuelve por OUTPUT usuario, fecha y sucursal que envía. **No valida** que quien recibe pertenezca a la sucursal destino. |

### 4.4 Reportes de ventas por usuario
| Procedimiento | Comportamiento (Confirmado) |
|---|---|
| `ReporteDeVentasPorUsuario` | `@idUsuario`, `@fecha`. Dos resultados: (1) líneas vendidas a precio **mayor** que `PRODUCTO.precio` vigente desde `@fecha`; (2) líneas vendidas a precio **menor**, pero con fecha **fija `'2025-09-07'`** (ignora `@fecha`, probable defecto). Calcula diferencia `(precio real − precio venta) × cantidad`. |
| `sp_ventasxuser` | Ventas de un usuario en una sucursal (todas). |
| `sp_ventasxusuariofecha` | Ventas de un usuario en una sucursal en una fecha. |
| `sp_cortecaja` | Detalle de ventas de una sucursal en una fecha, opcionalmente por usuario, incluye costo. **SQL dinámico concatenado**. |
| `sp_cortexmesusuario`, `sp_cortexmesintervalousuario` | Detalle de ventas por usuario por mes o intervalo. |

---

## 5. Triggers

| Trigger | Tabla / evento | Efecto (Confirmado) |
|---|---|---|
| `trg_ActualizarExistencias_SalidaXTraslado` | `DETSALXTRASLADO` AFTER INSERT | Resta cantidad en `Existencias` de la sucursal origen; crea fila negativa si no existe. Fecha `GETDATE()-1h` (update) / `GETDATE()` (insert). |
| `trg_ActualizarExistencias_EntradaXTraslado` | `DETENTRADAXTRASLADO` AFTER INSERT | Suma cantidad en `Existencias` de la sucursal destino; crea fila si no existe. |
| `TR_actualizarDetalle` | `DETSALXTRASLADO` AFTER UPDATE | Copia la nueva cantidad a `DETENTRADAXTRASLADO` usando el **mismo número** de cabecera de salida como número de entrada. Solo funciona con una fila; no ajusta `Existencias` ni la tabla TEMP. Indica que en algún momento se permitía editar cantidades de traslados. |
| `trg_ActualizarExistencias_Compra` | `DETALLECOMPRA` AFTER INSERT | Suma stock (solo si ya existe la fila). Fuera de alcance. |
| `TR_actualizartotalBorrarC`, `TR_updateTotalActualizarC` | `DETALLECOMPRA` | Recalculan total de compra. Fuera de alcance. |
| `trg_ActualizarExistencias_Ajuste`, `..._AjusteEliminaDetalle` | `DETAJUSTE` | Ajustes 100 = entrada, 101 = salida. Fuera de alcance. |

No hay trigger sobre `DETALLEVENTA`: el descuento de stock por venta ocurre **dentro de `grabaVenta`**. Cualquier inserción de ventas fuera de ese SP no descuenta existencias.

---

## 6. Campos de estado, movimiento, existencia y auditoría

| Categoría | Campo | Valores / significado | Estado |
|---|---|---|---|
| Estado | `USUARIO.estado` | → `ESTADOUSUARIO`; `'ACTIVO'` permite login | Confirmado (valores restantes Pendientes) |
| Estado | `SALXTRASLADO.recibido` | `'N'` pendiente, `'S'` recibido, `NULL` = procesado | Confirmado |
| Estado | `FACTURA.estado` | Sin catálogo | Pendiente |
| Estado | `CUENTAXPAGAR.estado` | `200` = pendiente/insolvente (fuera de alcance) | Confirmado |
| Movimiento | `VENTA`/`DETALLEVENTA` | Salida de inventario por venta | Confirmado |
| Movimiento | `SALXTRASLADO`/`DETSALXTRASLADO` | Salida por traslado | Confirmado |
| Movimiento | `ENTXTRASLADO`/`DETENTRADAXTRASLADO` | Entrada por traslado | Confirmado |
| Movimiento | `DETENTRADAXTRASLADOTEMP` | Entrada pendiente (no afecta stock) | Confirmado |
| Movimiento | `AJUSTE.tipoAjuste` 100/101 | Entrada/salida por ajuste | Confirmado |
| Existencia | `Existencias.Stock` | Stock por sucursal, admite negativos | Confirmado |
| Existencia | `PRODUCTO.stockmin` | Umbral de reposición (`verprodsstockmin`) | Confirmado |
| Auditoría | `BITACORA` | nick, estación, acción, fecha | Confirmado |
| Auditoría | `Existencias.FechaActualizacion` | Última modificación de stock | Confirmado |
| Auditoría | `VENTASELIMINADAS.fechElim`, `DETALLEVENTASELIMINADOS.felim` | Ventas eliminadas | Confirmado |
| Auditoría | `*.usuario` en VENTA, SALXTRASLADO, ENTXTRASLADO | Usuario que registra | Confirmado |

---

## 7. Fechas y zonas horarias

| Objeto | Cálculo de fecha |
|---|---|
| `grabaVenta` | `DATEADD(HOUR, 2, GETDATE())` |
| `sp_grabaBitacora` (variable `@FECHAGT`) | `DATEADD(HOUR, -6, GETDATE())` |
| Triggers de ajuste y compra | `DATEADD(HOUR, -6, GETDATE())` |
| Triggers de traslado (update) | `DATEADD(HOUR, -1, GETDATE())` |
| Triggers (insert de nueva fila) y `sp_RecibeTraslado` | `GETDATE()` sin ajuste |

Los desfases son **inconsistentes** entre sí (Confirmado). El nombre `@FECHAGT` sugiere hora de Guatemala (UTC−6). Cuál es la hora correcta del negocio y la zona del servidor actual — **Pendiente de confirmar**. Afecta directamente a los reportes por fecha y al corte de caja.

---

## 8. Problemas técnicos detectados

| # | Problema | Objeto | Impacto |
|---|---|---|---|
| T-01 | Contraseñas en texto plano | `USUARIO.contraUsuario`, `sp_validaUsuario` | Seguridad crítica. |
| T-02 | SQL dinámico concatenado (inyección SQL) | `getTraslados` (`@usuario`), `sp_cortecaja` | Seguridad. No reutilizar. |
| T-03 | Sin claves foráneas en tablas operativas | VENTA, traslados, USUARIO, Existencias | Integridad depende de la aplicación. |
| T-04 | Parámetros más cortos que las columnas | `grabaVenta` (`@cliente` 10 vs 20, `@autoriza` 16 vs 50), `sp_validaUsuario` (40 vs 50) | Truncamiento silencioso. |
| T-05 | `@documento varchar(20)` insertado en `bigint` | `grabaVenta` | Error si no es numérico. |
| T-06 | Transacción con `@@ERROR` sin `XACT_ABORT` | `sp_RecibeTraslado` | Errores no capturados pueden dejar estados parciales. |
| T-07 | `ROLLBACK` inalcanzable | `sp_get_sequence` | Transacción abierta en caso de error. |
| T-08 | Trigger asume una sola fila y nEntrada = nSalida | `TR_actualizarDetalle` | Actualiza filas incorrectas o falla con varias filas. |
| T-09 | Fecha fija `'2025-09-07'` | `ReporteDeVentasPorUsuario` (2º resultado) | Reporte de ventas bajo precio ignora filtro. |
| T-10 | Desfases horarios inconsistentes | Ver §7 | Fechas incoherentes. |
| T-11 | Reporte compara contra precio **actual** | `ReporteDeVentasPorUsuario` | Diferencias falsas si el precio cambió después. |
| T-12 | Sin índices secundarios | Todas | Rendimiento en búsquedas y reportes. |
| T-13 | `sp_infoProductos` devuelve todo el catálogo | — | Rendimiento en la web; requerirá búsqueda paginada. |
| T-14 | `getTraslados` filtra por `nombreUsuario` (no único) | — | Usuarios homónimos ven traslados ajenos. |
