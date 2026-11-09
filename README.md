# NeuroZen — Backend

## 1. Introducción a Neurozen.API

NeuroZen Backend es una API REST para una plataforma de manejo y prevención del
estrés laboral. Proporciona autenticación segura, gestión de usuarios y
profesionales de la salud mental, reserva de citas, seguimiento del bienestar y
recursos para apoyar la salud emocional.

Está construida con ASP.NET Core 9, Entity Framework Core 9 y MySQL 8. La API
usa JWT para proteger los endpoints privados y Swagger para probarla durante el
desarrollo.

## 2. Módulos y categorías

La API está organizada en los siguientes módulos:

- **Autenticación e IAM:** registro, inicio de sesión, recuperación de
  contraseña, gestión de usuarios y autorización por roles.
- **Profesionales:** consulta de psicólogos, perfiles, verificación,
  disponibilidad y reseñas.
- **Citas:** tipos de cita, creación, consulta y gestión de citas entre usuarios
  y profesionales.
- **Bienestar:** contenidos de bienestar, métricas de salud y panel de
  seguimiento del usuario.
- **Estrés:** evaluaciones, desencadenantes y reportes relacionados con el
  estrés laboral.
- **Suscripciones:** gestión de suscripciones de los usuarios.
- **Biblioteca de recursos:** bibliotecas y recursos de apoyo para el bienestar.
- **Administración y servicios compartidos:** manejo de errores, seguridad,
  límites de solicitudes, persistencia, migraciones y datos iniciales.

La API tiene como base de rutas `/api/v1`. Los endpoints privados requieren un
token JWT, excepto las rutas públicas de autenticación.

## 3. Ejecutar el proyecto localmente

### Requisitos

- .NET 9 SDK.
- MySQL 8, instalado localmente o ejecutándose en Docker.
- Docker Desktop solo es necesario si se usa MySQL mediante Docker o se ejecutan
  las pruebas de integración.

Hay dos formas recomendadas de ejecutar el backend.

### Camino A: .NET local + MySQL

Este camino ejecuta la API directamente con `dotnet run` y usa una instancia de
MySQL local. Si no tienes MySQL instalado, puedes iniciar únicamente MySQL con
Docker usando el comando indicado más abajo.

#### 1. Iniciar MySQL

Con MySQL instalado, crea una base de datos llamada `neurozen` y asegúrate de
que el servidor escuche en el puerto `3306`.

Como alternativa, inicia MySQL con Docker:

```powershell
docker run -d --name neurozen-mysql `
  -e MYSQL_ROOT_PASSWORD=root `
  -e MYSQL_DATABASE=neurozen `
  -p 3306:3306 `
  mysql:8.0
```

Si el contenedor ya existe, puedes iniciarlo con:

```powershell
docker start neurozen-mysql
```

#### 2. Configurar los secretos de desarrollo

Desde la raíz del repositorio:

```powershell
cd C:\Miguel\experimentos\neurozen_web_backend

dotnet user-secrets --project .\neurozen.API set `
  "ConnectionStrings:DefaultConnection" `
  "server=localhost;port=3306;database=neurozen;user=root;password=root"

$secret = [Convert]::ToBase64String(
  (1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]]
)
dotnet user-secrets --project .\neurozen.API set "TokenSettings:Secret" $secret
```

La clave JWT se genera localmente y debe tener al menos 32 bytes. No la guardes
en `appsettings.json` ni la subas al repositorio.

Puedes verificar la configuración con:

```powershell
dotnet user-secrets --project .\neurozen.API list
```

#### 3. Ejecutar la API

```powershell
dotnet run --project .\neurozen.API
```

Al arrancar, la API aplica automáticamente las migraciones pendientes y carga
los datos de demostración configurados para Development.

URLs disponibles:

- API: <http://localhost:5059>
- Swagger: <http://localhost:5059/swagger/index.html>
- Health check: <http://localhost:5059/health>

En Swagger puedes probar `POST /api/v1/authentication/sign-in` con:

```json
{
  "username": "admin@neurozen.com",
  "password": "Admin2026"
}
```

Copia el JWT de la respuesta, pulsa **Authorize** y escribe `Bearer` seguido
del token para probar los endpoints protegidos.

### Camino B: API y MySQL con Docker

Este camino no requiere ejecutar la API con el SDK en el host. Los dos
contenedores se conectan mediante una red Docker.

Desde la raíz del repositorio:

```powershell
docker network create neurozen-network

docker run -d --name neurozen-mysql --network neurozen-network `
  -e MYSQL_ROOT_PASSWORD=root `
  -e MYSQL_DATABASE=neurozen `
  mysql:8.0

$secret = [Convert]::ToBase64String(
  (1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]]
)

docker build -t neurozen-api .

docker run --name neurozen-api --network neurozen-network -p 5059:10000 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ConnectionStrings__DefaultConnection="server=neurozen-mysql;port=3306;database=neurozen;user=root;password=root" `
  -e TokenSettings__Secret=$secret `
  neurozen-api
```

Espera unos segundos a que MySQL termine de inicializarse. Si la API arranca
antes de que MySQL esté lista, detén y vuelve a iniciar el contenedor de la API:

```powershell
docker start neurozen-api
```

En este camino también puedes usar:

- <http://localhost:5059/swagger/index.html>
- <http://localhost:5059/health>

Para detener los contenedores:

```powershell
docker stop neurozen-api neurozen-mysql
```

Para eliminarlos posteriormente:

```powershell
docker rm neurozen-api neurozen-mysql
docker network rm neurozen-network
```

## Pruebas

Para ejecutar las pruebas:

```powershell
dotnet test
```

Las pruebas de integración y BDD requieren Docker en ejecución o la variable
`NEUROZEN_TEST_MYSQL` apuntando a una base de datos MySQL disponible.

## Seguridad y despliegue

### Destino de despliegue

Se ha consolidado el despliegue en Render. Se mantiene la configuración de Docker del backend y la pipeline de Render para producción y staging; el resto de destinos obsoletos (AWS EC2 y Railway) se retiraron para evitar configuraciones contradictorias.

### JWT y `SecurityStamp`

Cada usuario tiene un `SecurityStamp` aleatorio con formato GUID sin guiones. El token emitido incluye el claim `sst` con ese valor. Al cambiar la contraseña, resetearla, cambiar el email o anonimizar la cuenta, el stamp se regenera para invalidar automáticamente JWTs anteriores. La validación JWT comprueba además que el usuario existe y no esté eliminado.

### Proxies inversos y forwarded headers

Cuando la API se despliega detrás de un proxy inverso (Render, Nginx, AWS ALB, etc.), activa `ForwardedHeaders__Enabled=true` y configura `ForwardedHeaders__KnownProxies` / `ForwardedHeaders__KnownNetworks` según la topología real. Si `ForwardedHeaders__TrustAllProxies=true`, la app entra en modo conservador y registra un warning porque confía en todos los proxies. El rate limiting usa `X-Forwarded-For` cuando el encabezado está presente para particionar por IP real del cliente.

## Documentación adicional

- [Guía de entrega y ejecución](docs/ENTREGA_FINAL.md)
- [Matriz de endpoints](docs/ENTREGA_FINAL.md#3-matriz-final-de-los-35-endpoints)
- [Cambios de contrato para la app móvil](docs/ENTREGA_FINAL.md#4-cambios-de-contrato-que-debe-conocer-la-app-móvil)
- [Pruebas, mutación y CI](docs/ENTREGA_FINAL.md#6-pruebas)
