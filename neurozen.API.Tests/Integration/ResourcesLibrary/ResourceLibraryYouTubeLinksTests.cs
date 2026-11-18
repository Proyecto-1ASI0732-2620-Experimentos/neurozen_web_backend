using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Migrations;
using neurozen.API.Tests.Integration.Support;

namespace neurozen.API.Tests.Integration.ResourcesLibrary;

/// <summary>
///     Enlaces de YouTube de la biblioteca (migración AddYouTubeLinksToResourceLibrary) contra la API real y MySQL.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ResourceLibraryYouTubeLinksTests(IntegrationFixture fixture)
{
    /// <summary>Tabla §4 del requerimiento: título, tipo y enlace elegido.</summary>
    public static readonly TheoryData<string, string, string> ExpectedLinks = new()
    {
        { "Técnicas de Respiración Profunda", "Video", "https://www.youtube.com/watch?v=CVMBSsqTGOk" },
        { "Ejercicios de Estiramiento para Oficina", "Video", "https://www.youtube.com/watch?v=H0pbvdIcHOY" },
        { "Yoga para Reducir la Ansiedad", "Video", "https://www.youtube.com/watch?v=tS52C0Pxf_g" },
        { "Ejercicios de Relajación Muscular Progresiva", "Video", "https://www.youtube.com/watch?v=PzzAlvr3O3M" },
        { "Meditación para el Manejo del Dolor Crónico", "Video", "https://www.youtube.com/watch?v=r5K8Drgzj_Y" },
        { "Ejercicios de Atención Plena en el Trabajo", "Video", "https://www.youtube.com/watch?v=40LH8G7kI84" },
        { "Meditación Guiada de 5 Minutos", "Audio", "https://www.youtube.com/watch?v=pyAyEzmiKxU" },
        { "Música Relajante para Dormir", "Audio", "https://www.youtube.com/watch?v=an60NyBayzE" },
        { "Podcast: Manejo de Crisis de Ansiedad", "Audio", "https://www.youtube.com/watch?v=A8qOLnTbQdg" },
        { "Sonidos de la Naturaleza - Bosque Lluvioso", "Audio", "https://www.youtube.com/watch?v=bwSdDBSJK10" },
        { "Técnicas de Visualización Positiva", "Audio", "https://www.youtube.com/watch?v=GiOamuperW4" },
        { "Caminata Meditativa - Guía de Audio", "Audio", "https://www.youtube.com/watch?v=QLkFTi02Geo" }
    };

    private static readonly string[] Titles = ExpectedLinks.Select(row => (string)row[0]).ToArray();

    private TestEnvironment Env => fixture.Environment;

    /// <summary>Re-ejecuta las sentencias SQL de la migración (como si se aplicara otra vez).</summary>
    private Task ReapplyMigrationSqlAsync() => RunSqlAsync(new AddYouTubeLinksToResourceLibrary().UpOperations);

    private Task RunSqlAsync(IEnumerable<MigrationOperation> operations) => Env.WithDatabaseAsync(async db =>
    {
        foreach (var sql in operations.OfType<SqlOperation>().Select(operation => operation.Sql))
            await db.Database.ExecuteSqlRawAsync(sql);
    });

    private static string VideoId(string url) => url[(url.IndexOf("v=", StringComparison.Ordinal) + 2)..];

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el frontend reproduce el contentUrl de cada recurso; los 12 recursos deben
    ///   existir con el enlace de YouTube elegido y su tipo original (los audios siguen siendo Audio).
    /// - ¿Qué hace esta prueba?: un USER pide GET /resource-libraries y se comprueba cada título, tipo y enlace.
    /// </summary>
    [Theory]
    [MemberData(nameof(ExpectedLinks))]
    public async Task GetAll_ShouldReturnEachResourceWithItsYouTubeLink(string title, string type, string url)
    {
        var user = await Env.Api.AsUserWithPlan(2); // Avanzado: toda la biblioteca

        var response = await user.Client.GetAsync("/api/v1/resource-libraries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = (await response.ReadJsonAsync()).EnumerateArray()
            .Single(r => r.GetProperty("title").GetString() == title);
        Assert.Equal(type, item.GetProperty("resourceType").GetString());
        Assert.Equal(url, item.GetProperty("contentUrl").GetString());
        Assert.True(item.GetProperty("duration").GetInt32() > 0);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el detalle (y su alias /resources/{id}) también debe entregar el enlace, y el
    ///   JSON no puede cambiar de nombres de propiedades.
    /// - ¿Qué hace esta prueba?: abre un video y un audio por id y compara el enlace y el conjunto exacto de propiedades.
    /// </summary>
    [Theory]
    [InlineData("Técnicas de Respiración Profunda", "https://www.youtube.com/watch?v=CVMBSsqTGOk")]
    [InlineData("Meditación Guiada de 5 Minutos", "https://www.youtube.com/watch?v=pyAyEzmiKxU")]
    public async Task GetById_VideoAndAudio_ShouldReturnLinkWithSameJsonShape(string title, string url)
    {
        var user = await Env.Api.AsUserWithPlan(2); // Avanzado: toda la biblioteca
        var id = await Env.QueryDatabaseAsync(db =>
            db.Set<ResourceLibrary>().Where(r => r.Title == title).Select(r => r.Id).SingleAsync());

        var list = await (await user.Client.GetAsync("/api/v1/resource-libraries")).ReadJsonAsync();
        var detail = await user.Client.GetAsync($"/api/v1/resource-libraries/{id}");
        var alias = await user.Client.GetAsync($"/api/v1/resources/{id}");

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await detail.ReadJsonAsync();
        Assert.Equal(url, body.GetProperty("contentUrl").GetString());
        Assert.Equal(url, (await alias.ReadJsonAsync()).GetProperty("contentUrl").GetString());
        Assert.Equal(
            ["author", "content", "contentUrl", "createdAt", "description", "duration", "id", "resourceType", "tags",
                "thumbnail", "title"],
            body.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            ["author", "contentUrl", "createdAt", "description", "duration", "id", "resourceType", "tags", "thumbnail",
                "title"],
            list.EnumerateArray().First().EnumerateObject().Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la carga debe ser idempotente; aplicarla otra vez no puede duplicar recursos.
    /// - ¿Qué hace esta prueba?: re-ejecuta dos veces el SQL de la migración y comprueba que el total no cambia y que
    ///   cada título aparece una sola vez.
    /// </summary>
    [Fact]
    public async Task ReapplyingMigrationTwice_ShouldNotDuplicateResources()
    {
        var before = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());

        await ReapplyMigrationSqlAsync();
        await ReapplyMigrationSqlAsync();

        var after = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().CountAsync());
        var perTitle = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => Titles.Contains(r.Title)).GroupBy(r => r.Title).Select(g => g.Count()).ToListAsync());
        Assert.Equal(before, after);
        Assert.Equal(12, perTitle.Count);
        Assert.All(perTitle, count => Assert.Equal(1, count));
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: en una base que ya tenía el recurso con un enlace de relleno, la migración
    ///   solo debe corregir contentUrl, sin tocar la duración ni crear otra fila; y nunca debe tocar los artículos.
    /// - ¿Qué hace esta prueba?: pone un enlace de relleno y una duración distinta a un video, crea un artículo, vuelve a
    ///   aplicar el SQL y comprueba: enlace corregido, misma fila, misma duración y artículo intacto.
    /// </summary>
    [Fact]
    public async Task Reapplying_ShouldOnlyFixPlaceholderUrl_AndNeverTouchArticles()
    {
        const string title = "Yoga para Reducir la Ansiedad";
        var admin = await Env.Api.AsAdmin();
        var article = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = $"Artículo de control {Guid.NewGuid():N}", description = "No debe cambiar",
            content = "Texto del artículo", resourceType = "Article", duration = 4,
            contentUrl = "https://neurozen.com/articulo", tags = new[] { "control" }
        });
        await TestApi.EnsureStatusAsync(article, HttpStatusCode.Created);
        var articleId = (await article.ReadJsonAsync()).GetProperty("id").GetInt64();
        var (id, originalDuration) = await Env.QueryDatabaseAsync(async db =>
        {
            var row = await db.Set<ResourceLibrary>().Where(r => r.Title == title)
                .Select(r => new { r.Id, r.Duration }).SingleAsync();
            return (row.Id, row.Duration);
        });
        await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(r => r.ContentUrl, "https://www.youtube.com/watch?v=example1")
            .SetProperty(r => r.Duration, 99)));

        try
        {
            await ReapplyMigrationSqlAsync();

            var fixedRow = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().AsNoTracking()
                .Where(r => r.Title == title).ToListAsync());
            var storedArticle = await Env.QueryDatabaseAsync(db =>
                db.Set<ResourceLibrary>().AsNoTracking().SingleAsync(r => r.Id == articleId));
            var resource = Assert.Single(fixedRow);
            Assert.Equal(id, resource.Id);
            Assert.Equal("https://www.youtube.com/watch?v=tS52C0Pxf_g", resource.ContentUrl);
            Assert.Equal(99, resource.Duration);
            Assert.Equal(ResourceType.Video, resource.ResourceType);
            Assert.Equal(ResourceType.Article, storedArticle.ResourceType);
            Assert.Equal("https://neurozen.com/articulo", storedArticle.ContentUrl);
            Assert.Equal("No debe cambiar", storedArticle.Description);
        }
        finally
        {
            await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Duration, originalDuration)));
        }
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: solo se guardan enlaces de texto de YouTube, nunca archivos.
    /// - ¿Qué hace esta prueba?: comprueba que los 12 enlaces tienen el formato https://www.youtube.com/watch?v=ID y
    ///   caben en la columna (500).
    /// </summary>
    [Fact]
    public async Task AllLinks_ShouldBeNormalizedYouTubeUrls()
    {
        var urls = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => Titles.Contains(r.Title)).Select(r => r.ContentUrl).ToListAsync());

        Assert.Equal(12, urls.Count);
        Assert.All(urls, url =>
        {
            Assert.Matches("^https://www\\.youtube\\.com/watch\\?v=[A-Za-z0-9_-]{11}$", url!);
            Assert.True(url!.Length <= ResourceLibrary.UrlMaxLength);
        });
    }

    // ------------------------------------------------------------------ Miniaturas, aviso clínico y edición (ADMIN)

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: las tarjetas del frontend muestran la miniatura; cada recurso debe tener la
    ///   imagen pública de su propio video de YouTube.
    /// - ¿Qué hace esta prueba?: compara el thumbnail devuelto por la API con img.youtube.com/vi/{id}/hqdefault.jpg.
    /// </summary>
    [Theory]
    [MemberData(nameof(ExpectedLinks))]
    public async Task EachResource_ShouldHaveTheThumbnailOfItsVideo(string title, string type, string url)
    {
        _ = type;
        var user = await Env.Api.AsUserWithPlan(2); // Avanzado: toda la biblioteca

        var item = (await (await user.Client.GetAsync("/api/v1/resource-libraries")).ReadJsonAsync()).EnumerateArray()
            .Single(r => r.GetProperty("title").GetString() == title);

        Assert.Equal($"https://img.youtube.com/vi/{VideoId(url)}/hqdefault.jpg",
            item.GetProperty("thumbnail").GetString());
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: los recursos clínicos deben indicar que no sustituyen la atención
    ///   profesional, una sola vez aunque la migración se aplique de nuevo, y solo en esos dos recursos.
    /// - ¿Qué hace esta prueba?: re-aplica la migración y cuenta el aviso en cada descripción.
    /// </summary>
    [Fact]
    public async Task ClinicalNotice_OnlyInClinicalResources_AndOnlyOnce()
    {
        await RunSqlAsync(new AddYouTubeThumbnailsAndClinicalNotice().UpOperations);

        var descriptions = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => Titles.Contains(r.Title)).ToDictionaryAsync(r => r.Title, r => r.Description ?? ""));
        string[] clinical = ["Meditación para el Manejo del Dolor Crónico", "Podcast: Manejo de Crisis de Ansiedad"];
        foreach (var (title, description) in descriptions)
        {
            var occurrences = description.Split(AddYouTubeThumbnailsAndClinicalNotice.ClinicalNotice).Length - 1;
            Assert.Equal(clinical.Contains(title) ? 1 : 0, occurrences);
        }
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la migración nunca debe pisar una miniatura elegida por un admin, y su
    ///   Down() debe quitar exactamente lo que agregó (reversible).
    /// - ¿Qué hace esta prueba?: pone una miniatura propia a un recurso, aplica Down y Up y verifica que esa miniatura
    ///   no cambió, que Down quitó las generadas y el aviso, y que Up los restauró.
    /// </summary>
    [Fact]
    public async Task ThumbnailsAndNotice_AreReversible_AndNeverOverwriteAdminThumbnail()
    {
        const string custom = "https://neurozen.com/img/miniatura-propia.jpg";
        const string customTitle = "Música Relajante para Dormir";
        const string otherTitle = "Yoga para Reducir la Ansiedad";
        const string clinicalTitle = "Podcast: Manejo de Crisis de Ansiedad";
        var migration = new AddYouTubeThumbnailsAndClinicalNotice();
        await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Title == customTitle)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Thumbnail, custom)));

        try
        {
            await RunSqlAsync(migration.DownOperations);
            var afterDown = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().AsNoTracking()
                .Where(r => r.Title == customTitle || r.Title == otherTitle || r.Title == clinicalTitle)
                .ToDictionaryAsync(r => r.Title));
            await RunSqlAsync(migration.UpOperations);
            var afterUp = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>().AsNoTracking()
                .Where(r => r.Title == customTitle || r.Title == otherTitle || r.Title == clinicalTitle)
                .ToDictionaryAsync(r => r.Title));

            Assert.Equal(custom, afterDown[customTitle].Thumbnail);
            Assert.Null(afterDown[otherTitle].Thumbnail);
            Assert.DoesNotContain(AddYouTubeThumbnailsAndClinicalNotice.ClinicalNotice,
                afterDown[clinicalTitle].Description ?? "");
            Assert.Equal(custom, afterUp[customTitle].Thumbnail);
            Assert.Equal("https://img.youtube.com/vi/tS52C0Pxf_g/hqdefault.jpg", afterUp[otherTitle].Thumbnail);
            Assert.Contains(AddYouTubeThumbnailsAndClinicalNotice.ClinicalNotice,
                afterUp[clinicalTitle].Description ?? "");
        }
        finally
        {
            await Env.WithDatabaseAsync(db => db.Set<ResourceLibrary>().Where(r => r.Title == customTitle)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Thumbnail,
                    "https://img.youtube.com/vi/an60NyBayzE/hqdefault.jpg")));
        }
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: si un video deja de estar disponible o su duración no coincide, el ADMIN
    ///   debe poder corregir el enlace y la duración desde la API, sin acceso a la base de datos.
    /// - ¿Qué hace esta prueba?: el admin edita enlace, duración y miniatura de un recurso de control; se comprueba la
    ///   respuesta, la persistencia, que lo no enviado se conserva y que el tipo no cambia.
    /// </summary>
    [Fact]
    public async Task AdminPatch_ShouldUpdateLinkAndDuration_KeepingTheRest()
    {
        var admin = await Env.Api.AsAdmin();
        var created = await admin.Client.PostAsJsonAsync("/api/v1/resource-libraries", new
        {
            title = $"Video de control {Guid.NewGuid():N}", description = "Descripción original",
            resourceType = "Video", contentUrl = "https://www.youtube.com/watch?v=CVMBSsqTGOk", duration = 15,
            tags = new[] { "control" }
        });
        await TestApi.EnsureStatusAsync(created, HttpStatusCode.Created);
        var id = (await created.ReadJsonAsync()).GetProperty("id").GetInt64();

        var patch = await admin.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{id}", new
        {
            contentUrl = "https://www.youtube.com/watch?v=WkzMBxohi1M", duration = 5,
            thumbnail = "https://img.youtube.com/vi/WkzMBxohi1M/hqdefault.jpg"
        });

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var body = await patch.ReadJsonAsync();
        Assert.Equal("https://www.youtube.com/watch?v=WkzMBxohi1M", body.GetProperty("contentUrl").GetString());
        Assert.Equal(5, body.GetProperty("duration").GetInt32());
        var stored = await Env.QueryDatabaseAsync(db =>
            db.Set<ResourceLibrary>().AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal("https://www.youtube.com/watch?v=WkzMBxohi1M", stored.ContentUrl);
        Assert.Equal(5, stored.Duration);
        Assert.Equal("Descripción original", stored.Description);
        Assert.Equal(["control"], stored.Tags);
        Assert.Equal(ResourceType.Video, stored.ResourceType);
    }

    [Fact]
    public async Task AdminPatch_InvalidValues_ShouldReturn400_AndNotChangeAnything()
    {
        var admin = await Env.Api.AsAdmin();
        var id = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => r.Title == "Técnicas de Visualización Positiva").Select(r => r.Id).SingleAsync());

        var response = await admin.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{id}",
            new { contentUrl = "javascript:alert(1)", duration = 9999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("contentUrl", out _));
        Assert.True(errors.TryGetProperty("duration", out _));
        var stored = await Env.QueryDatabaseAsync(db =>
            db.Set<ResourceLibrary>().AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal("https://www.youtube.com/watch?v=GiOamuperW4", stored.ContentUrl);
    }

    [Fact]
    public async Task Patch_ByNonAdmin_ShouldReturn403_AndUnknownId404()
    {
        var user = await Env.Api.AsUserWithPlan(2);
        var admin = await Env.Api.AsAdmin();
        var id = await Env.QueryDatabaseAsync(db => db.Set<ResourceLibrary>()
            .Where(r => r.Title == "Técnicas de Respiración Profunda").Select(r => r.Id).SingleAsync());

        var forbidden = await user.Client.PatchAsJsonAsync($"/api/v1/resource-libraries/{id}",
            new { contentUrl = "https://www.youtube.com/watch?v=WkzMBxohi1M" });
        var missing = await admin.Client.PatchAsJsonAsync("/api/v1/resource-libraries/999999999",
            new { duration = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("https://www.youtube.com/watch?v=CVMBSsqTGOk", await Env.QueryDatabaseAsync(db =>
            db.Set<ResourceLibrary>().Where(r => r.Id == id).Select(r => r.ContentUrl).SingleAsync()));
    }
}
