# Fase 2 — Alcance funcional (operación de sucursal)

## 1. Objetivo

Migrar las operaciones diarias que realizan los **empleados de cada sucursal**, sobre la base administrativa construida en la fase 1 (`docs/phase-1-scope.md`).

## 2. Acceso

El área operativa se publica en **otro subdominio** (nombre Pendiente, p. ej. `sucursal.farmaciahorro.com`) servido por **el mismo despliegue** que la administración. La aplicación determina el área según el host y el backend restringe los endpoints por permisos. Detalle en `docs/deployment.md`.

## 3. Funcionalidades previstas

### 3.1 Ventas
- Búsqueda de productos con existencia de la sucursal del usuario (`sp_infoProductos`, `sp_getStoc`).
- Registro de venta: cabecera + detalle + descuento de existencias en una transacción (`grabaVenta`).
- Formas de pago: efectivo, tarjeta (con número de autorización) o combinación (Confirmado por estructura de `VENTA`).
- Cliente asociado a la venta (`VENTA.cliente`, probablemente NIT) — Pendiente de confirmar si se crea cliente desde la venta.
- Consulta de ventas propias y su detalle (`sp_ventasxuser`, `getDetallesVentas`).
- Ticket de venta con el logo configurado en la fase 1.

### 3.2 Reportes del empleado
- Ventas por usuario con diferencias de precio (`ReporteDeVentasPorUsuario`).
- Ventas por usuario y fecha (`sp_ventasxusuariofecha`), corte de caja (`sp_cortecaja`), por mes/intervalo (`sp_cortexmesusuario`, `sp_cortexmesintervalousuario`).
- Pendiente de confirmar cuáles usa realmente el empleado y con qué filtros (P-50–P-52).

### 3.3 Pendientes de decisión
- Anulación/eliminación de ventas (`VENTASELIMINADAS`, `DETALLEVENTASELIMINADOS`): ¿empleado o administrador?
- Emisión de facturas (`FACTURA`) cuando el negocio active la facturación.
- Generación/recepción de traslados desde el área operativa (en fase 1 se implementan en administración).

Reglas detalladas de ventas y reportes: `docs/business-rules.md` secciones 2 y 5.
