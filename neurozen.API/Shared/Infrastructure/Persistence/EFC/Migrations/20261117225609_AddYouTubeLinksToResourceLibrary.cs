using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <summary>
    ///     Data-only migration (no schema change): registers the YouTube links of the 12 videos/audios of the
    ///     resource library. Only text URLs are stored; no media file is downloaded or saved.
    ///     For each resource, matched by exact title (hyphen or en dash variant):
    ///     - if a Video/Audio row exists, only content_url is updated (type, duration, description and tags untouched);
    ///     - if no row exists, it is inserted with the library data (title, type, duration) and the link.
    ///     Every statement is idempotent: running it again neither duplicates rows nor changes anything.
    ///     Articles and exercises are never touched.
    /// </summary>
    public partial class AddYouTubeLinksToResourceLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Técnicas de Respiración Profunda
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=CVMBSsqTGOk' WHERE title IN ('Técnicas de Respiración Profunda') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=CVMBSsqTGOk');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Técnicas de Respiración Profunda', 'Respiración guiada: tres ejercicios para relajarse y meditar en cualquier lugar.', NULL, NULL, 'Video', 'https://www.youtube.com/watch?v=CVMBSsqTGOk', 15, NULL, '[""respiración"", ""relajación""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Técnicas de Respiración Profunda'));");
            // Ejercicios de Estiramiento para Oficina
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=H0pbvdIcHOY' WHERE title IN ('Ejercicios de Estiramiento para Oficina') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=H0pbvdIcHOY');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Ejercicios de Estiramiento para Oficina', 'Estiramientos en la oficina del Servicio de Prevención de Mutua MAZ.', NULL, 'Mutua MAZ', 'Video', 'https://www.youtube.com/watch?v=H0pbvdIcHOY', 8, NULL, '[""estiramiento"", ""oficina"", ""pausa activa""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Ejercicios de Estiramiento para Oficina'));");
            // Yoga para Reducir la Ansiedad
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=tS52C0Pxf_g' WHERE title IN ('Yoga para Reducir la Ansiedad') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=tS52C0Pxf_g');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Yoga para Reducir la Ansiedad', '10 posturas de yoga para aliviar el estrés y la ansiedad, apta para todos los niveles.', NULL, NULL, 'Video', 'https://www.youtube.com/watch?v=tS52C0Pxf_g', 20, NULL, '[""yoga"", ""ansiedad""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Yoga para Reducir la Ansiedad'));");
            // Ejercicios de Relajación Muscular Progresiva
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=PzzAlvr3O3M' WHERE title IN ('Ejercicios de Relajación Muscular Progresiva') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=PzzAlvr3O3M');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Ejercicios de Relajación Muscular Progresiva', 'Relajación progresiva de Jacobson guiada para la ansiedad y la tensión acumulada.', NULL, NULL, 'Video', 'https://www.youtube.com/watch?v=PzzAlvr3O3M', 18, NULL, '[""relajación"", ""jacobson""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Ejercicios de Relajación Muscular Progresiva'));");
            // Meditación para el Manejo del Dolor Crónico
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=r5K8Drgzj_Y' WHERE title IN ('Meditación para el Manejo del Dolor Crónico') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=r5K8Drgzj_Y');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Meditación para el Manejo del Dolor Crónico', 'Mindfulness con escáner corporal compasivo para aliviar el dolor crónico.', NULL, NULL, 'Video', 'https://www.youtube.com/watch?v=r5K8Drgzj_Y', 22, NULL, '[""meditación"", ""dolor crónico""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Meditación para el Manejo del Dolor Crónico'));");
            // Ejercicios de Atención Plena en el Trabajo
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=40LH8G7kI84' WHERE title IN ('Ejercicios de Atención Plena en el Trabajo') AND resource_type = 'Video' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=40LH8G7kI84');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Ejercicios de Atención Plena en el Trabajo', 'Meditación guiada de 5 minutos para hacer una pausa en el puesto de trabajo.', NULL, NULL, 'Video', 'https://www.youtube.com/watch?v=40LH8G7kI84', 10, NULL, '[""mindfulness"", ""trabajo""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Ejercicios de Atención Plena en el Trabajo'));");
            // Meditación Guiada de 5 Minutos
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=pyAyEzmiKxU' WHERE title IN ('Meditación Guiada de 5 Minutos') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=pyAyEzmiKxU');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Meditación Guiada de 5 Minutos', 'Meditación mindfulness de 5 minutos, ideal para principiantes.', NULL, NULL, 'Audio', 'https://www.youtube.com/watch?v=pyAyEzmiKxU', 5, NULL, '[""meditación"", ""mindfulness""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Meditación Guiada de 5 Minutos'));");
            // Música Relajante para Dormir
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=an60NyBayzE' WHERE title IN ('Música Relajante para Dormir') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=an60NyBayzE');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Música Relajante para Dormir', 'Música para dormir profundamente y descansar la mente.', NULL, NULL, 'Audio', 'https://www.youtube.com/watch?v=an60NyBayzE', 30, NULL, '[""música"", ""sueño""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Música Relajante para Dormir'));");
            // Podcast: Manejo de Crisis de Ansiedad
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=A8qOLnTbQdg' WHERE title IN ('Podcast: Manejo de Crisis de Ansiedad') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=A8qOLnTbQdg');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Podcast: Manejo de Crisis de Ansiedad', 'Cómo superar una crisis de ansiedad: qué hacer durante una crisis.', NULL, 'Raquel Sastre, psicóloga', 'Audio', 'https://www.youtube.com/watch?v=A8qOLnTbQdg', 25, NULL, '[""ansiedad"", ""crisis""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Podcast: Manejo de Crisis de Ansiedad'));");
            // Sonidos de la Naturaleza - Bosque Lluvioso
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=bwSdDBSJK10' WHERE title IN ('Sonidos de la Naturaleza - Bosque Lluvioso', 'Sonidos de la Naturaleza – Bosque Lluvioso') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=bwSdDBSJK10');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Sonidos de la Naturaleza - Bosque Lluvioso', 'Sonido de lluvia en el bosque desde una tienda de campaña, para relajarse y dormir.', NULL, NULL, 'Audio', 'https://www.youtube.com/watch?v=bwSdDBSJK10', 45, NULL, '[""naturaleza"", ""lluvia"", ""sueño""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Sonidos de la Naturaleza - Bosque Lluvioso', 'Sonidos de la Naturaleza – Bosque Lluvioso'));");
            // Técnicas de Visualización Positiva
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=GiOamuperW4' WHERE title IN ('Técnicas de Visualización Positiva') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=GiOamuperW4');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Técnicas de Visualización Positiva', 'Visualización guiada con mindfulness en menos de 10 minutos.', NULL, NULL, 'Audio', 'https://www.youtube.com/watch?v=GiOamuperW4', 12, NULL, '[""visualización"", ""mindfulness""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Técnicas de Visualización Positiva'));");
            // Caminata Meditativa - Guía de Audio
            migrationBuilder.Sql(@"UPDATE resource_libraries SET content_url = 'https://www.youtube.com/watch?v=QLkFTi02Geo' WHERE title IN ('Caminata Meditativa - Guía de Audio', 'Caminata Meditativa – Guía de Audio') AND resource_type = 'Audio' AND (content_url IS NULL OR content_url <> 'https://www.youtube.com/watch?v=QLkFTi02Geo');");
            migrationBuilder.Sql(@"INSERT INTO resource_libraries (title, description, content, author, resource_type, content_url, duration, thumbnail, tags, created_at) SELECT 'Caminata Meditativa - Guía de Audio', 'Meditación mindfulness al caminar, guiada por el psiquiatra Pablo Gómez.', NULL, 'Pablo Gómez, psiquiatra', 'Audio', 'https://www.youtube.com/watch?v=QLkFTi02Geo', 15, NULL, '[""meditación"", ""caminata""]', UTC_TIMESTAMP(6) FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN ('Caminata Meditativa - Guía de Audio', 'Caminata Meditativa – Guía de Audio'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: rolling back must never delete resources (they may already be referenced or edited
            // by an admin) and the previous placeholder URLs are not known in every environment. To revert a link,
            // update content_url of that row manually.
        }
    }
}
