# CLAUDE.md

## 1. Objetivo del proyecto

Modernizar un sistema de farmacia desarrollado originalmente en VB.NET y Windows Forms, migrándolo progresivamente a una aplicación web moderna.

La nueva aplicación debe conservar las reglas de negocio y funcionalidades del sistema original, mejorando su mantenibilidad, seguridad, experiencia de usuario y capacidad de evolución.

## 2. Stack tecnológico

### Backend
- Lenguaje: C#.
- Framework: ASP.NET Core Web API.
- Versión objetivo: .NET 10 LTS.
- Base de datos: Microsoft SQL Server.
- ORM: Entity Framework Core.
- Dapper o ADO.NET cuando exista una justificación técnica.
- API REST documentada mediante OpenAPI.

### Frontend
- Vue 3.
- TypeScript.
- Vite.
- Composition API con `<script setup lang="ts">`.
- Vue Router para navegación.
- Pinia para estado compartido cuando sea necesario.
- Axios o Fetch API para comunicación con el backend.

## 3. Principios de arquitectura

Utilizar una arquitectura de **monolito modular**, separando claramente las responsabilidades del backend y frontend.

Priorizar:
- Simplicidad y mantenibilidad.
- Separación de responsabilidades.
- Bajo acoplamiento.
- Código legible y consistente.
- Reutilización razonable.
- Principios SOLID cuando aporten valor.
- Seguridad desde el diseño.

Evitar sobreingeniería.

No incorporar por defecto:
- Microservicios.
- CQRS.
- Event Sourcing.
- MediatR.
- Repository o Unit of Work personalizados sin justificación.
- Abstracciones e interfaces innecesarias.
- Capas adicionales que no aporten valor.

Ante varias soluciones válidas, elegir la más sencilla que satisfaga los requisitos.

## 4. Organización del proyecto

Utilizar una estructura similar a:

```text
/
├── CLAUDE.md
├── README.md
├── docs/
├── database/
│   ├── scripts/
│   └── migrations/
├── src/
│   ├── backend/
│   │   ├── Pharmacy.Api/
│   │   ├── Pharmacy.Core/
│   │   └── Pharmacy.Infrastructure/
│   └── frontend/
│       └── src/
│           ├── components/
│           ├── layouts/
│           ├── modules/
│           ├── router/
│           ├── services/
│           ├── stores/
│           ├── types/
│           └── utils/
└── tests/
    ├── Pharmacy.UnitTests/
    └── Pharmacy.IntegrationTests/
```

### Responsabilidades

- **Api:** endpoints, autenticación, autorización, middleware y configuración HTTP.
- **Core:** reglas de negocio, lógica de aplicación, validaciones y contratos.
- **Infrastructure:** persistencia, SQL Server e implementaciones de servicios externos.
- **Frontend:** interfaz, navegación, componentes y comunicación con la API.

Organizar el código por funcionalidades o módulos de negocio. Evitar crear carpetas, clases o interfaces vacías por seguir una estructura teórica.

## 5. Reglas para el backend

- Mantener controladores delgados.
- No implementar reglas de negocio en los controladores.
- Utilizar DTOs para solicitudes y respuestas.
- No exponer directamente entidades de persistencia.
- Utilizar correctamente los métodos HTTP y códigos de estado.
- Validar los datos recibidos en el backend.
- Centralizar el manejo de excepciones.
- Utilizar Problem Details cuando corresponda.
- Registrar errores mediante ILogger.
- Utilizar consultas parametrizadas.
- Evitar SQL concatenado.
- Preferir operaciones asíncronas con async/await.
- No utilizar `.Result` ni `.Wait()` para bloquear operaciones asíncronas.
- Utilizar transacciones cuando una operación requiera consistencia entre varias modificaciones.

## 6. Reglas para el frontend

- Utilizar TypeScript y evitar `any`.
- Utilizar Composition API.
- Separar componentes visuales de servicios HTTP.
- Centralizar la configuración de comunicación con la API.
- Mantener los componentes pequeños y enfocados.
- Reutilizar componentes cuando exista una necesidad real.
- Manejar estados de carga, errores y respuestas vacías.
- Mantener una interfaz consistente y responsive.
- No implementar reglas de negocio exclusivamente en el frontend.

Organizar las funcionalidades por módulos, manteniendo sus vistas, componentes, servicios y tipos relacionados.

## 7. Base de datos y migración

SQL Server es el motor de base de datos existente y debe aprovecharse cuando sea conveniente.

Antes de modificar la estructura actual:
- Analizar tablas, relaciones y restricciones.
- Revisar procedimientos almacenados y funciones.
- Identificar reglas de negocio implementadas en SQL.
- Comprender las dependencias entre funcionalidades.

Reglas:
- No modificar ni eliminar estructuras sin comprender su impacto.
- Preservar la integridad de los datos.
- Utilizar migraciones controladas y scripts SQL versionados.
- No ejecutar cambios destructivos en producción.
- No inventar relaciones ni reglas de negocio.

La migración debe ser progresiva y conservar el comportamiento funcional del sistema original.

## 8. Seguridad

- Implementar autenticación y autorización en el backend.
- Utilizar roles y permisos según las necesidades del sistema.
- Almacenar contraseñas mediante algoritmos de hash seguros.
- Utilizar HTTPS.
- Proteger endpoints independientemente de la interfaz.
- Validar todas las entradas.
- Evitar SQL Injection.
- No exponer información sensible.
- Mantener credenciales y cadenas de conexión fuera del repositorio.
- Aplicar CORS restrictivo cuando corresponda.

Analizar primero el mecanismo de autenticación y permisos del sistema original antes de definir su reemplazo.

## 9. Publicación y hosting

La aplicación debe ser compatible con hosting Windows compartido que soporte ASP.NET Core e IIS, como Negox o SmarterASP.

Estrategia:
- Backend publicado en IIS.
- Frontend compilado mediante `npm run build`.
- Servir el frontend desde `wwwroot` del backend para simplificar el despliegue.
- Mantener la API bajo la ruta `/api`.
- Configurar fallback hacia `index.html` para Vue Router.
- Utilizar configuraciones independientes para desarrollo y producción.
- Considerar publicación Self-Contained cuando sea conveniente.

No depender de Docker, Kubernetes, servicios de Windows personalizados ni acceso administrativo al servidor.

Verificar las capacidades y restricciones reales del hosting antes de definir la estrategia definitiva de publicación.

## 10. Pruebas y calidad

Utilizar xUnit para pruebas del backend.

Priorizar:
- Reglas de negocio.
- Validaciones.
- Operaciones críticas.
- Endpoints importantes.
- Procesos de ventas e inventario.

Implementar pruebas frontend cuando aporten valor.

No crear pruebas artificiales únicamente para aumentar la cobertura.

Antes de finalizar una tarea:
- Compilar el backend.
- Verificar el build del frontend.
- Ejecutar las pruebas relacionadas.
- Corregir errores introducidos.
- Revisar los cambios realizados.

## 11. Convenciones de código

### C#
- PascalCase para clases, métodos y propiedades.
- camelCase para variables y parámetros.
- Utilizar nombres descriptivos.
- Habilitar nullable reference types.
- Evitar duplicación y métodos excesivamente largos.

### TypeScript
- PascalCase para componentes y tipos.
- camelCase para variables y funciones.
- Definir tipos para los datos intercambiados con la API.
- Evitar lógica compleja en los templates.

Mantener consistencia en todo el proyecto.

## 12. Instrucciones de trabajo para Claude Code

Antes de implementar una funcionalidad:

1. Analizar el código relacionado.
2. Comprender las reglas de negocio involucradas.
3. Identificar tablas y relaciones afectadas.
4. Revisar el comportamiento equivalente en el sistema original.
5. Proponer una solución coherente con la arquitectura existente.

Durante la implementación:

- No inventar reglas de negocio.
- No modificar funcionalidades ajenas al requerimiento.
- No agregar dependencias innecesarias.
- Reutilizar componentes existentes cuando corresponda.
- No realizar refactorizaciones generales no solicitadas.
- Preguntar cuando falte información importante.

Al finalizar:

- Resumir los cambios realizados.
- Indicar los archivos modificados.
- Informar las pruebas ejecutadas.
- Señalar problemas pendientes o decisiones que requieran aprobación.

## 13. Regla fundamental

El objetivo no es utilizar la mayor cantidad de tecnologías o patrones posibles.

El objetivo es construir un sistema de farmacia confiable, seguro, sencillo de comprender, fácil de mantener y preparado para evolucionar, preservando correctamente las operaciones del negocio.