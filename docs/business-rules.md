# Reglas de negocio — Análisis inicial

> **Nota de alcance (actualizada):** este análisis se hizo cuando la fase 1 era operativa. Ahora la fase 1 es administrativa (`docs/phase-1-scope.md`) y la operación pasa a la fase 2 (`docs/phase-2-scope.md`). Las secciones 1, 3, 4 y 6 aplican a la fase 1; las secciones 2 y 5 (ventas y reporte del empleado) a la fase 2. Las reglas de catálogo, compras, cuentas por pagar, ajustes y configuración se agregarán al analizar cada módulo.

Reglas identificadas a partir de `docs/database-schema.sql`. El código VB.NET original **no está disponible**, por lo que las reglas que dependían de la interfaz o del código de aplicación figuran como pendientes.

- **Confirmado**: respaldado por estructura, procedimiento o trigger (se indica la evidencia).
- **Pendiente de confirmar**: requiere revisar el sistema original o consultar al usuario.

Nota: "Confirmado" significa que **el sistema actual se comporta así**, no que el comportamiento sea deseable. Las reglas marcadas con ⚠ son comportamientos confirmados que probablemente deban revisarse antes de reproducirse.

---

## 1. Inicio de sesión y seguridad

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-01 | El usuario se autentica con `nick` y contraseña. | `sp_validaUsuario` | Confirmado |
| RN-02 | Solo pueden iniciar sesión usuarios cuyo estado sea `'ACTIVO'`. | `sp_validaUsuario` (`EU.estadoUsuario = 'ACTIVO'`) | Confirmado |
| RN-03 | Cada usuario pertenece a una única sucursal; la sesión opera sobre esa sucursal. | `USUARIO.sucursal` NOT NULL, devuelto al iniciar sesión | Confirmado (estructura). Pendiente: ¿el usuario puede cambiar de sucursal en la sesión? |
| RN-04 | Cada usuario tiene un tipo/rol (`TIPOUSUARIO`). | `USUARIO.tipoUsuario` | Confirmado |
| RN-05 | Los roles `ADMINISTRADOR` y `GERENTE` tienen visibilidad global de traslados; el resto solo ve los propios. | `getTraslados` | Confirmado |
| RN-06 ⚠ | Las contraseñas se almacenan y comparan en texto plano. | `USUARIO.contraUsuario`, `sp_validaUsuario` | Confirmado. Debe reemplazarse por hash (CLAUDE.md §8). |
| RN-07 | Las acciones se registran en `BITACORA` con nick, estación de trabajo, acción y fecha. | `sp_grabaBitacora` | Confirmado (mecanismo). Pendiente: qué acciones se registran. |
| RN-08 | El correlativo de bitácora se obtiene de la tabla `SECUENCIAL`. | `sp_get_sequence 'BITACORA'` | Confirmado |

**Pendientes de login**
- P-01: Lista completa de roles (`TIPOUSUARIO`) y estados (`ESTADOUSUARIO`) existentes y qué puede hacer cada rol en las 5 funcionalidades.
- P-02: ¿Hay bloqueo por intentos fallidos, expiración de contraseña o cambio obligatorio? (no hay estructura para ello).
- P-03: ¿Un usuario puede tener sesión abierta en varias estaciones?
- P-04: ¿Qué significa "estación" en la bitácora y debe conservarse en la web (IP, navegador)?

---

## 2. Ventas

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-10 | Una venta se compone de cabecera (`VENTA`) y líneas (`DETALLEVENTA`), registradas en una sola transacción junto con la actualización de existencias. | `grabaVenta` (`XACT_ABORT`, TRANSACTION) | Confirmado |
| RN-11 | La venta descuenta existencias **de la sucursal indicada en la venta**, agrupando cantidades por producto. | `grabaVenta` pasos 3–4 | Confirmado |
| RN-12 ⚠ | La venta **no valida existencias**: se permite vender sin stock y el stock puede quedar negativo; si el producto no tiene fila en `Existencias`, se crea con stock negativo. | `grabaVenta` paso 5 | Confirmado. Pendiente: ¿la interfaz original bloqueaba o advertía? |
| RN-13 | El precio de cada línea se guarda en `DETALLEVENTA.precio` y puede diferir de `PRODUCTO.precio`. | Columna propia; `ReporteDeVentasPorUsuario` compara ambos | Confirmado |
| RN-14 | Vender a un precio distinto del vigente es una situación auditada (reporte de diferencias por usuario). | `ReporteDeVentasPorUsuario` | Confirmado. Pendiente: ¿el empleado puede cambiar precio libremente, hay límite de descuento, requiere autorización? |
| RN-15 | El pago puede ser en efectivo, con tarjeta o combinado; la tarjeta registra número de autorización. | `VENTA.efectivo`, `tarjeta`, `autorizacion` | Confirmado (estructura). Pendiente: ¿efectivo guarda el monto recibido o el aplicado? ¿se calcula cambio? ¿`efectivo + tarjeta = total` obligatorio? |
| RN-16 | El total y los subtotales los calcula la aplicación; la base de datos no los verifica. | `grabaVenta` | Confirmado. La nueva API deberá calcularlos/validarlos. |
| RN-17 | La numeración de líneas de detalle la genera la aplicación (1..n por venta). | `DETALLESVENTA.nDetalleVenta` | Confirmado |
| RN-18 | El número de venta es correlativo global (IDENTITY), no por sucursal. | `VENTA.nVenta IDENTITY(1,1)` | Confirmado |
| RN-19 | La venta referencia un cliente por un código de hasta 20 caracteres (probablemente NIT). | `VENTA.cliente varchar(20)` | Pendiente: ¿es el NIT de `CLIENTE`? ¿Se usa "CF" (consumidor final)? ¿Se crea el cliente si no existe? |
| RN-20 | La venta tiene un número de `documento` numérico. | `VENTA.documento bigint` | Pendiente: ¿qué representa (factura, recibo, ticket)? ¿quién lo genera y si es correlativo por sucursal/serie? |
| RN-21 | La fecha de la venta la asigna el servidor (`GETDATE() + 2 h`). | `grabaVenta` | Confirmado. Pendiente: zona horaria correcta del negocio. |

**Pendientes de ventas**
- P-10: ¿Se emiten facturas (`FACTURA`, `SERIEFACTURA`)? Ningún SP las usa. ¿Hay facturación electrónica (FEL) asociada?
- P-11: ¿Puede el empleado anular/eliminar ventas? ¿Se repone el stock al anular? (existen `VENTASELIMINADAS`/`DETALLEVENTASELIMINADOS`, pero no el proceso).
- P-12: ¿Se imprime comprobante/ticket? ¿con qué formato?
- P-13: ¿Puede venderse la misma línea de producto repetida en una venta? (el SP lo admite agrupando).
- P-14: ¿Hay productos que no se pueden vender (descontinuados, controlados, receta)? No hay estructura que lo indique.
- P-15: ¿Búsqueda de productos por código de barras, nombre, composición?

---

## 3. Generación de traslados

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-30 | Un traslado de salida pertenece a una sucursal de origen y a un usuario. | `SALXTRASLADO.sucursalSalida`, `usuario` | Confirmado |
| RN-31 | Al registrar el detalle de salida se descuenta inmediatamente el stock de la sucursal de origen. | `trg_ActualizarExistencias_SalidaXTraslado` | Confirmado |
| RN-32 ⚠ | La salida por traslado **no valida existencias**: puede dejar stock negativo, incluso creando filas negativas. | Mismo trigger ("con stock negativo") | Confirmado. Pendiente: ¿la interfaz validaba? |
| RN-33 | Un traslado nuevo queda en estado pendiente (`recibido = 'N'`). | `sp_RecibeTraslado`, `getTraslados` | Confirmado por uso; Pendiente: confirmar que el código VB.NET inserta `'N'` (la columna admite NULL, y NULL se trata como procesado). |
| RN-34 | La sucursal destino se registra en una entrada (`ENTXTRASLADO`) enlazada a la salida mediante `nSalidaRelacionada`. | Estructura, `sp_RecibeTraslado` | Confirmado |
| RN-35 | El detalle a recibir se guarda en `DETENTRADAXTRASLADOTEMP` y **no afecta** existencias del destino hasta la recepción. | Ausencia de trigger en TEMP; `sp_RecibeTraslado` | Confirmado |
| RN-36 | El traslado no registra precio ni costo; los listados lo valorizan con el precio vigente del producto. | `DETSALXTRASLADO` sin precio; `getDetalleTraslado` | Confirmado |
| RN-37 | Cantidades de traslado limitadas a `smallint` (máx. 32 767 por línea). | Tipos de columna | Confirmado |

**Pendientes de generación**
- P-30: Confirmar el flujo exacto al generar: ¿se insertan en una sola operación `SALXTRASLADO`, `DETSALXTRASLADO`, `ENTXTRASLADO` y `DETENTRADAXTRASLADOTEMP`? ¿Con qué usuario y fecha se crea `ENTXTRASLADO`?
- P-31: ¿Se puede trasladar a la misma sucursal de origen? (no hay restricción en BD).
- P-32: ¿Se puede editar o cancelar un traslado ya generado y no recibido? `TR_actualizarDetalle` sugiere que se editaban cantidades, pero no ajusta stock ni TEMP. ¿Esa función sigue en uso?
- P-33: ¿Hay aprobación previa del gerente antes de enviar?
- P-34: ¿Los empleados (no ADMIN/GERENTE) deben ver todos los traslados? Con `@opt` distinto de `'S'`, `getTraslados` devuelve **todos** a los no administradores; ¿es intencional para que vean lo que deben recibir?

---

## 4. Recepción de traslados

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-40 | Solo se puede recibir un traslado en estado `'N'`; si ya está en `'S'` (o es NULL / no existe) se rechaza con "ESTE TRASLADO YA FUE PROCESADO". | `sp_RecibeTraslado` | Confirmado |
| RN-41 | Antes de recibir se puede consultar el detalle pendiente (opción `B`). | `sp_RecibeTraslado` | Confirmado |
| RN-42 | La recepción es total: todas las líneas pendientes pasan al detalle definitivo; no hay recepción parcial ni registro de diferencias/faltantes. | `sp_RecibeTraslado` opción `R` | Confirmado |
| RN-43 | Al recibir se suman las cantidades al stock de la sucursal destino (`ENTXTRASLADO.sucursal`). | `trg_ActualizarExistencias_EntradaXTraslado` | Confirmado |
| RN-44 | Tras recibir, se elimina el detalle temporal y el traslado de salida queda `recibido = 'S'`, todo en una transacción. | `sp_RecibeTraslado` | Confirmado |
| RN-45 ⚠ | La recepción **no valida** que el usuario que recibe pertenezca a la sucursal destino ni registra quién recibió ni cuándo. | `sp_RecibeTraslado` | Confirmado. Pendiente: ¿la interfaz lo restringía? ¿Debe registrarse receptor y fecha de recepción? |
| RN-46 | La recepción muestra al usuario quién envió, desde qué sucursal y en qué fecha. | Parámetros OUTPUT de `sp_RecibeTraslado` | Confirmado |

**Pendientes de recepción**
- P-40: ¿Qué pasa si llega menos o más mercadería de la enviada? ¿Se requiere recepción parcial o registro de discrepancias?
- P-41: ¿Quién puede recibir: cualquier empleado de la sucursal destino, solo el gerente?
- P-42: ¿Cómo localiza el empleado el traslado a recibir: escribe el número o elige de una lista de pendientes para su sucursal?

---

## 5. Reporte de ventas por usuario

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-50 | El reporte muestra, por usuario, las líneas vendidas a un precio **superior** al precio vigente del producto desde una fecha dada. | `ReporteDeVentasPorUsuario` (1er resultado) | Confirmado |
| RN-51 | El reporte muestra las líneas vendidas a un precio **inferior** al vigente. | 2º resultado | Confirmado. ⚠ Usa fecha fija `'2025-09-07'` en vez del parámetro: Pendiente confirmar si es un defecto. |
| RN-52 | La diferencia se calcula como `(precio vigente − precio vendido) × cantidad`, ordenado por sucursal y fecha. | Mismo SP | Confirmado |
| RN-53 ⚠ | La comparación es contra el precio **actual** del producto, no el vigente al momento de la venta (no existe historial de precios). | Mismo SP | Confirmado |
| RN-54 | Existen consultas de ventas por usuario: todas, por fecha, por mes, por intervalo y corte de caja (con costo). | `sp_ventasxuser`, `sp_ventasxusuariofecha`, `sp_cortexmesusuario`, `sp_cortexmesintervalousuario`, `sp_cortecaja` | Confirmado (existencia). |

**Pendientes del reporte**
- P-50: ¿Qué reporte(s) usa el empleado exactamente? ¿El de diferencias de precio es para el empleado o para supervisión?
- P-51: ¿Un empleado solo puede ver sus propias ventas? ¿El gerente puede ver las de todos los usuarios de su sucursal?
- P-52: ¿El empleado puede ver costo y margen? (`sp_cortecaja` devuelve costo).
- P-53: ¿Se necesita exportar o imprimir (PDF/Excel)?

---

## 6. Inventario (transversal)

| ID | Regla | Evidencia | Estado |
|---|---|---|---|
| RN-60 | La existencia se mantiene por producto y sucursal en `Existencias`. | PK (IdSucursal, IdProducto) | Confirmado |
| RN-61 | Movimientos que alteran existencias: venta (−, SP), salida por traslado (−, trigger), entrada por traslado (+, trigger), compra (+, trigger), ajuste (±, trigger). | SP y triggers | Confirmado |
| RN-62 ⚠ | Se permiten existencias negativas. | `grabaVenta`, trigger de salida | Confirmado. Pendiente: ¿es deseado? |
| RN-63 | No existe historial de movimientos (kárdex) ni de precios. | Ausencia de tablas | Confirmado |
| RN-64 | La compra solo suma stock si el producto ya tiene fila en `Existencias` en esa sucursal. | `trg_ActualizarExistencias_Compra` (sin INSERT) | Confirmado (fuera de alcance, pero afecta stock visible). |

---

## 7. Pendientes transversales

- P-70: Zona horaria oficial del negocio y del servidor; los objetos usan +2 h, −6 h, −1 h y 0 h.
- P-71: ¿El sistema VB.NET seguirá funcionando en paralelo con la web sobre la misma BD?
- P-72: ¿Las tablas `TRASIEGOE`, `TRASIEGOSSAL`, `DETENTRADAXT`, `DETSALIDAXT` siguen en uso? ¿Qué representan?
- P-73: ¿Se puede obtener el código fuente VB.NET para confirmar validaciones de interfaz?
- P-74: ¿Hay datos de productos sin fila en `Existencias` o con stock negativo hoy? (afecta cómo mostrar disponibilidad).
