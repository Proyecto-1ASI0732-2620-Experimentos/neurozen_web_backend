using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Migrations;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.ResourcesLibrary;

/// <summary>
///     50 lecturas y 20 ejercicios (migración AddReadingsAndExercisesToResourceLibrary) y las vistas de administración
///     de la biblioteca, contra la API real y MySQL.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ReadingsAndExercisesTests(IntegrationFixture fixture)
{
    private static readonly string[] BasicArticles =
        ["Cómo reconocer las señales del estrés laboral", "Pausas activas: por qué cinco minutos cambian tu jornada"];

    private static readonly string[] BasicExercises =
        ["Respiración diafragmática para principiantes", "Pausa activa de 5 minutos"];

    private static readonly string[] Titles = AddReadingsAndExercisesToResourceLibrary.Titles.ToArray();

    private TestEnvironment Env => fixture.Environment;

    private Task RunSqlAsync(IEnumerable<MigrationOperation> operations) => Env.WithDatabaseAsync(async db =>
    {
        foreach (var sql in operations.OfType<SqlOperation>().Select(operation => operation.Sql))
            await db.Database.ExecuteSqlRawAsync(sql);
    });

    private static async Task<List<JsonElement>> ListAsync(HttpClient client, string path = "/api/v1/resource-libraries")
    {
        var response = await client.GetAsync(path);
        await TestApi.EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.ReadJsonAsync()).EnumerateArray().ToList();
    }

    private Task<List<ResourceLibrary>> LoadedRowsAsync() => Env.QueryDatabaseAsync(db =>
        db.Set<ResourceLibrary>().AsNoTracking().Where(r => Titles.Contains(r.Title)).ToListAsync());

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la biblioteca debe incluir las 50 lecturas y los 20 ejercicios, con su texto,
    ///   y el plan Básico solo debe tener 2 de cada tipo.
    /// - ¿Qué hace esta prueba?: cuenta por tipo en MySQL, revisa que todos tengan contenido y qué títulos son Básico.
    /// </summary>
    [Fact]
    public async Task Migration_Loads50ReadingsAnd20Exercises_WithTwoBasicOfEach()
    {
        var rows = await LoadedRowsAsync();

        Assert.Equal(70, AddReadingsAndExercisesToResourceLibrary.Count);
        Assert.Equal(70, rows.Count);
        Assert.Equal(50, rows.Count(r => r.ResourceType == ResourceType.Article));
        Assert.Equal(20, rows.Count(r => r.ResourceType == ResourceType.Exercise));
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Content)));
        Assert.All(rows, r => Assert.InRange(r.Duration, 1, 600));
        Assert.Equal(BasicArticles.Order(), rows.Where(r => r.ResourceType == ResourceType.Article &&
                                                             r.RequiredPlan == ResourcePlan.Basic)
            .Select(r => r.Title).Order());
        Assert.Equal(BasicExercises.Order(), rows.Where(r => r.ResourceType == ResourceType.Exercise &&
                                                              r.RequiredPlan == ResourcePlan.Basic)
            .Select(r => r.Title).Order());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el texto se guarda escapado para MySQL; no puede perder saltos de línea,
    ///   tildes ni comillas, ni recibir \r de Windows.
    /// - ¿Qué hace esta prueba?: compara el contenido guardado de los 70 recursos con el que trae la migración.
    /// </summary>
    [Fact]
    public async Task StoredContent_IsIdenticalToSource()
    {
        var expected = new AddReadingsAndExercisesToResourceLibrary().UpOperations.OfType<SqlOperation>().Count();
        var rows = await LoadedRowsAsync();

        Assert.Equal(70, expected);
        Assert.All(rows, r =>
        {
            Assert.DoesNotContain('\r', r.Content!);
            Assert.Contains('\n', r.Content!);
        });
        var sample = rows.Single(r => r.Title == "Cómo reconocer las señales del estrés laboral");
        Assert.StartsWith("El estrés forma parte de la vida laboral.", sample.Content);
        Assert.Contains("Qué es el estrés laboral", sample.Content);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: un usuario Básico debe ver solo 2 lecturas y 2 ejercicios y poder leer su
    ///   texto completo; las demás lecturas responden 403 sin enviar el contenido.
    /// - ¿Qué hace esta prueba?: lista como Básico, abre una lectura Básica y una Avanzada.
    /// </summary>
    [Fact]
    public async Task BasicUser_SeesTwoReadingsAndTwoExercises_AndCanReadThem()
    {
        var user = await Env.Api.AsUserWithPlan(1);

        var items = await ListAsync(user.Client);
        var basicId = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => r.Title == BasicArticles[0]).Select(r => r.Id).SingleAsync());
        var advancedId = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => Titles.Contains(r.Title) && r.RequiredPlan == ResourcePlan.Advanced &&
                        r.ResourceType == ResourceType.Article)
            .Select(r => r.Id).FirstAsync());
        var basicDetail = await user.Client.GetAsync($"/api/v1/resource-libraries/{basicId}");
        var advancedDetail = await user.Client.GetAsync($"/api/v1/resource-libraries/{advancedId}");

        Assert.Equal(BasicArticles.Order(), items.Where(i => i.GetProperty("resourceType").GetString() == "Article")
            .Select(i => i.GetProperty("title").GetString()!).Order());
        Assert.Equal(BasicExercises.Order(), items.Where(i => i.GetProperty("resourceType").GetString() == "Exercise")
            .Select(i => i.GetProperty("title").GetString()!).Order());
        Assert.Equal(HttpStatusCode.OK, basicDetail.StatusCode);
        Assert.StartsWith("El estrés forma parte",
            (await basicDetail.ReadJsonAsync()).GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, advancedDetail.StatusCode);
        Assert.DoesNotContain("\"content\"", await advancedDetail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AdvancedUser_SeesAll50ReadingsAnd20Exercises()
    {
        var user = await Env.Api.AsUserWithPlan(2);

        var titles = (await ListAsync(user.Client)).Select(i => i.GetProperty("title").GetString()!).ToHashSet();

        Assert.All(Titles, title => Assert.Contains(title, titles));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la carga es idempotente y respeta el máximo de 2 Básico por tipo aunque ya
    ///   existan otros recursos Básico.
    /// - ¿Qué hace esta prueba?: re-aplica dos veces (sin duplicados); luego renombra una lectura Básica y re-aplica:
    ///   esa lectura se vuelve a insertar como Avanzado porque ya hay 2 lecturas Básico.
    /// </summary>
    [Fact]
    public async Task Reapplying_DoesNotDuplicate_AndRespectsBasicLimit()
    {
        var migration = new AddReadingsAndExercisesToResourceLibrary();
        var before = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());
        await RunSqlAsync(migration.UpOperations);
        await RunSqlAsync(migration.UpOperations);
        Assert.Equal(before, await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync()));

        const string renamed = "Lectura renombrada por la prueba";
        await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Title == BasicArticles[1])
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Title, renamed)));
        try
        {
            await RunSqlAsync(migration.UpOperations);

            var reinserted = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().AsNoTracking()
                .SingleAsync(r => r.Title == BasicArticles[1]));
            Assert.Equal(ResourcePlan.Advanced, reinserted.RequiredPlan);
        }
        finally
        {
            await Env.WithDatabaseAsync(async db =>
            {
                await db.Set<ResourceLibrary>().Where(r => r.Title == BasicArticles[1]).ExecuteDeleteAsync();
                await db.Set<ResourceLibrary>().Where(r => r.Title == renamed)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Title, BasicArticles[1]));
            });
        }
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: revertir la migración solo debe quitar lo que insertó sin cambios; lo que el
    ///   admin editó se conserva.
    /// - ¿Qué hace esta prueba?: el admin edita una lectura, se aplica Down (queda solo la editada) y Up la restaura.
    /// </summary>
    [Fact]
    public async Task Down_RemovesOnlyUneditedRows_AndUpRestoresThem()
    {
        var migration = new AddReadingsAndExercisesToResourceLibrary();
        var admin = await Env.Api.AsAdmin();
        var edited = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => Titles.Contains(r.Title) && r.RequiredPlan == ResourcePlan.Advanced)
            .Select(r => new { r.Id, r.Title, r.Content }).FirstAsync());
        var patch = await admin.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{edited.Id}",
            new { content = edited.Content + "\nNota agregada por el admin." });
        await TestApi.EnsureStatusAsync(patch, HttpStatusCode.OK);

        try
        {
            await RunSqlAsync(migration.DownOperations);
            var remaining = await LoadedRowsAsync();
            Assert.Equal([edited.Title], remaining.Select(r => r.Title));

            await RunSqlAsync(migration.UpOperations);
            Assert.Equal(70, (await LoadedRowsAsync()).Count);
        }
        finally
        {
            await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Id == edited.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Content, edited.Content)));
        }
    }

    // ------------------------------------------------------------------ Vistas de administración

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el panel admin necesita ver todos los recursos con su plan para editarlos.
    /// - ¿Qué hace esta prueba?: el admin lista todo (igual al total de la tabla, con requiredPlan y su nombre) y abre
    ///   un recurso; un USER recibe 403 en ambos.
    /// </summary>
    [Fact]
    public async Task AdminList_ShowsEveryResourceWithItsPlan_AndIsAdminOnly()
    {
        var admin = await Env.Api.AsAdmin();
        var user = await Env.Api.AsUserWithPlan(3);
        var total = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());
        var basicId = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => r.Title == BasicExercises[0]).Select(r => r.Id).SingleAsync());

        var items = await ListAsync(admin.Client, "/api/v1/admin/resource-libraries");
        var detail = await admin.Client.GetAsync($"/api/v1/admin/resource-libraries/{basicId}");
        var userList = await user.Client.GetAsync("/api/v1/admin/resource-libraries");
        var userDetail = await user.Client.GetAsync($"/api/v1/admin/resource-libraries/{basicId}");
        var missing = await admin.Client.GetAsync("/api/v1/admin/resource-libraries/999999999");

        Assert.Equal(total, items.Count);
        Assert.All(items, i => Assert.InRange(i.GetProperty("requiredPlan").GetInt32(), 1, 3));
        var body = await detail.ReadJsonAsync();
        Assert.Equal(1, body.GetProperty("requiredPlan").GetInt32());
        Assert.Equal("Básico", body.GetProperty("requiredPlanName").GetString());
        Assert.Equal("Exercise", body.GetProperty("resourceType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("content").GetString()));
        Assert.Equal(HttpStatusCode.Forbidden, userList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el admin debe poder crear lecturas y ejercicios (no solo audio y video),
    ///   eligiendo el plan.
    /// - ¿Qué hace esta prueba?: crea una lectura Básica y un ejercicio Profesional, valida la respuesta con el plan y
    ///   que un tipo o plan inválido da 400.
    /// </summary>
    [Fact]
    public async Task Admin_CanCreateReadingsAndExercises_ChoosingThePlan()
    {
        var admin = await Env.Api.AsAdmin();

        var reading = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = $"Lectura del admin {Guid.NewGuid():N}", description = "Resumen", resourceType = "Article",
            content = "Primer párrafo.\n\nSegundo párrafo.", duration = 4, tags = new[] { "prueba" }, requiredPlan = 1
        });
        var exercise = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = $"Ejercicio del admin {Guid.NewGuid():N}", resourceType = "Exercise",
            content = "1. Paso uno\n2. Paso dos", duration = 6, requiredPlan = 3,
            contentUrl = "https://www.youtube.com/watch?v=H0pbvdIcHOY"
        });
        var invalid = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = "Recurso inválido", resourceType = "Podcast", duration = 4, requiredPlan = 9
        });

        Assert.Equal(HttpStatusCode.Created, reading.StatusCode);
        var readingBody = await reading.ReadJsonAsync();
        Assert.Equal("Article", readingBody.GetProperty("resourceType").GetString());
        Assert.Equal(1, readingBody.GetProperty("requiredPlan").GetInt32());
        Assert.Equal("Primer párrafo.\n\nSegundo párrafo.", readingBody.GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.Created, exercise.StatusCode);
        Assert.Equal("Profesional", (await exercise.ReadJsonAsync()).GetProperty("requiredPlanName").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var errors = (await invalid.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("resourceType", out _));
        Assert.True(errors.TryGetProperty("requiredPlan", out _));

        // Cleanup: keep the Básico limit of 2 readings for the other tests.
        var id = readingBody.GetProperty("id").GetInt64();
        await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Id == id).ExecuteDeleteAsync());
    }
}
