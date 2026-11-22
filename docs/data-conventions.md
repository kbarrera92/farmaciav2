# Convenciones de datos de texto

Reglas transversales decididas por el usuario. Se aplican en el backend (`Pharmacy.Core/Common/TextNormalizer.cs`)
al crear o editar; el frontend no las replica.

## 1. Mayúsculas

| Campo | Regla | Ejemplo |
|---|---|---|
| Nombres de catálogo: categorías, formas de pago, roles, sucursales | Solo la primera letra del texto en mayúscula; el resto en minúscula. | `ANALGÉSICOS Y ANTIPIRÉTICOS` → `Analgésicos y antipiréticos` |
| Descripciones (rol) | Igual que los nombres de catálogo. | `ACCESO TOTAL` → `Acceso total` |
| Nombres propios: nombre completo del usuario, nombre de la empresa | Mayúscula inicial en cada palabra (también tras guion). Los conectores también se capitalizan. | `josé pérez-molina` → `José Pérez-Molina`; `farmacia del ahorro` → `Farmacia Del Ahorro` |
| Direcciones: sucursal, empresa | Solo se fuerza la primera letra; el resto se guarda como se escribió. | `zona 1, Ciudad de Guatemala` → `Zona 1, Ciudad de Guatemala` |
| Nombre de usuario, teléfono, correo, NIT | Sin cambios (solo se quitan espacios al inicio y al final). | |

**Siglas:** en las dos primeras reglas, una palabra de 2 a 4 letras escrita totalmente en mayúscula se conserva
(`pago POS` → `Pago POS`, `S.A.` se mantiene). Límite conocido: una palabra corta escrita en mayúscula por error
también se conserva (`CAJA CHICA` → `CAJA chica`).

Siempre se quitan los espacios al inicio y al final.

## 2. Comparación de nombres únicos

Los nombres únicos (categorías, formas de pago, roles, sucursales) no distinguen mayúsculas ni tildes:
`Analgésicos` = `ANALGESICOS`. La `ñ` sí es distinta de la `n`. Se implementa con la collation
`Modern_Spanish_100_CI_AI` en cada columna, más un índice único y la validación del servicio.
