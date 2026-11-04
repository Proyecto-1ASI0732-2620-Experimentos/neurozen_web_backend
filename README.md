# NeuroZen — Backend

API REST (ASP.NET Core 9 · EF Core 9 · MySQL 8) de NeuroZen, plataforma de manejo del estrés laboral.

- **Ejecutar localmente, variables de entorno, migraciones y seeder:** [`docs/ENTREGA_FINAL.md` §2](docs/ENTREGA_FINAL.md#2-ejecutar-localmente)
- **Matriz de los 35 endpoints:** [§3](docs/ENTREGA_FINAL.md#3-matriz-final-de-los-35-endpoints)
- **Cambios de contrato para la app móvil:** [§4](docs/ENTREGA_FINAL.md#4-cambios-de-contrato-que-debe-conocer-la-app-móvil)
- **Pruebas (unitarias, integración, BDD), mutación y CI:** [§6](docs/ENTREGA_FINAL.md#6-pruebas)

Inicio rápido:

```powershell
cd neurozen.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=neurozen;user=root;password=root"
dotnet user-secrets set "TokenSettings:Secret" "<32+ bytes aleatorios>"
dotnet ef migrations add InitialCreate --output-dir Shared/Infrastructure/Persistence/EFC/Migrations   # solo la primera vez
cd ..
dotnet run --project neurozen.API     # http://localhost:5059/swagger
dotnet test                           # requiere Docker o NEUROZEN_TEST_MYSQL
```
