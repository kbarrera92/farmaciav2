# Publicación por subdominio

## 1. Objetivo

- **Fase 1:** la administración se publica en `https://admin.farmaciahorro.com`.
- **Fase 2:** el área operativa se publica en otro subdominio (nombre Pendiente, p. ej. `sucursal.farmaciahorro.com`) usando **el mismo despliegue**: un solo backend, una sola publicación y la misma base de datos.

Compatible con hosting Windows compartido (SmarterASP, Negox): no requiere Docker, servicios de Windows ni acceso administrativo.

## 2. Requisitos

1. Ser dueño del dominio `farmaciahorro.com` y tener acceso a su DNS.
2. Plan de hosting con ASP.NET Core 10 (o publicación Self-Contained) y soporte de dominios/subdominios adicionales.
3. Certificado SSL para cada subdominio (Let's Encrypt gratuito en ambos proveedores; verificar en el panel).

Pendiente de verificar en el hosting elegido: si el subdominio puede apuntar al mismo sitio IIS (alias/binding) o si exige una carpeta propia.

## 3. Pasos — Fase 1

1. **Publicar la aplicación** según `README.md` (sección Publicación).
2. **Panel del hosting:** agregar `admin.farmaciahorro.com` como dominio/alias del sitio donde está publicada la aplicación.
3. **DNS:** crear el registro `admin` (tipo `A` hacia la IP del hosting o `CNAME` hacia el host que indique el proveedor).
4. **SSL:** emitir el certificado para `admin.farmaciahorro.com` desde el panel.
5. **Configuración de producción** (variable de entorno o `appsettings.Production.json`, que no se sube al repositorio):

   ```json
   {
     "AllowedHosts": "admin.farmaciahorro.com"
   }
   ```

   Con esto la API responde `400` a peticiones con otro encabezado `Host`.
6. **Verificar:** abrir `https://admin.farmaciahorro.com`, iniciar sesión y comprobar que `http://` redirige a `https://`.

## 4. Seguridad

- La cookie de sesión no define `Domain`, por lo que es **exclusiva del host** donde se inició sesión: una sesión de `admin.` no se comparte con el subdominio operativo. No configurar `Domain` en la cookie.
- La cookie es `Secure` y `SameSite=Strict`; HSTS y redirección a HTTPS están activos fuera de Development.
- El frontend y la API están en el mismo origen (`/api`), por lo que no se necesita CORS.

## 5. Fase 2 — Subdominio operativo (diseño a detallar en esa fase)

- Agregar el segundo subdominio al mismo sitio, con su DNS y SSL.
- Ampliar `AllowedHosts`: `"admin.farmaciahorro.com;sucursal.farmaciahorro.com"`.
- La SPA decide qué área mostrar según `window.location.host`.
- El backend sigue protegiendo cada endpoint por permisos; se evaluará además restringir los endpoints administrativos al host `admin.`.
