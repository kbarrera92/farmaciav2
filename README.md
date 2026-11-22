# Sistema de Farmacia

Modernización del sistema de farmacia (VB.NET / Windows Forms) a una aplicación web:
ASP.NET Core 10 (API) + Vue 3 / TypeScript (SPA) + SQL Server.

Ver `CLAUDE.md` para las reglas de arquitectura y `docs/` para el análisis y las decisiones.

## Estructura

```text
src/backend/Pharmacy.Api             Endpoints, autenticación, middleware, configuración HTTP
src/backend/Pharmacy.Core            Entidades, reglas de negocio, servicios, DTOs
src/backend/Pharmacy.Infrastructure  EF Core (AppDbContext, configuraciones, migraciones, seed)
src/frontend                         SPA Vue 3 + Vite + TypeScript
tests/Pharmacy.UnitTests             Pruebas de servicios (xUnit + SQLite en memoria)
tests/Pharmacy.IntegrationTests      Pruebas de la API (WebApplicationFactory + SQLite)
database/migrations                  Scripts SQL idempotentes generados desde las migraciones
docs/                                Análisis, alcance y documentación de módulos
```

## Requisitos

- .NET SDK 10
- Node.js 22 y npm
- SQL Server (desarrollo: instancia `.\ISPRODESADB2022`)
- `dotnet-ef` (`dotnet tool install -g dotnet-ef`)

## Configuración de desarrollo

Los secretos no se guardan en el repositorio. Configurar con user-secrets en `src/backend/Pharmacy.Api`:

```bash
cd src/backend/Pharmacy.Api
dotnet user-secrets set "ConnectionStrings:Pharmacy" "Server=.\ISPRODESADB2022;Database=PharmacyDev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<clave aleatoria de al menos 32 caracteres>"
dotnet user-secrets set "InitialAdmin:Password" "<contraseña temporal del primer administrador>"
```

Crear o actualizar la base de datos:

```bash
dotnet ef database update -p src/backend/Pharmacy.Infrastructure -s src/backend/Pharmacy.Api
```

Al iniciar, si la base no tiene usuarios, se crean el rol de sistema **Administrador**, la sucursal
"Sucursal principal" y el usuario `admin` con la contraseña de `InitialAdmin:Password`.
El sistema pide cambiarla en el primer ingreso.

## Ejecución

```bash
# API (https://localhost:7144)
dotnet run --project src/backend/Pharmacy.Api --launch-profile https

# Frontend en modo desarrollo (http://localhost:5173, con proxy de /api hacia la API)
cd src/frontend
npm install
npm run dev
```

## Compilación y pruebas

```bash
dotnet build Pharmacy.sln
dotnet test Pharmacy.sln
cd src/frontend && npm run build   # compila la SPA en src/backend/Pharmacy.Api/wwwroot
```

## Publicación (IIS / hosting compartido)

1. `npm run build` en `src/frontend` (genera `wwwroot` en la API).
2. `dotnet publish src/backend/Pharmacy.Api -c Release -o publish`.
3. Aplicar el script `database/migrations/*.sql` en la base de producción (idempotente).
4. Configurar `ConnectionStrings:Pharmacy`, `Jwt:Key` e `InitialAdmin:Password` como variables de
   entorno (`ConnectionStrings__Pharmacy`, `Jwt__Key`, …) o en un `appsettings.Production.json`
   que no se sube al repositorio.
5. Restringir `AllowedHosts` al dominio público (`admin.farmaciahorro.com` en la fase 1).
   Subdominios, DNS y SSL: ver `docs/deployment.md`.

Para generar el script de una migración nueva:

```bash
dotnet ef migrations script --idempotent -p src/backend/Pharmacy.Infrastructure -s src/backend/Pharmacy.Api -o database/migrations/000N_Nombre.sql
```
