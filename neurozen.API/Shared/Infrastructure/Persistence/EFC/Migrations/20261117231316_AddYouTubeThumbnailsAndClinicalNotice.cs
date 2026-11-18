using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <summary>
    ///     Data-only migration (no schema change), complements AddYouTubeLinksToResourceLibrary:
    ///     - thumbnail: YouTube's public preview image of the linked video (img.youtube.com/vi/ID/hqdefault.jpg), only
    ///       when the resource still points to that video and has no thumbnail (an admin's image is never replaced);
    ///     - clinical notice appended to the description of "Dolor crónico" and "Crisis de ansiedad" (once).
    ///     Idempotent and reversible: Down() removes exactly what Up() added. No rows are inserted or deleted.
    /// </summary>
    public partial class AddYouTubeThumbnailsAndClinicalNotice : Migration
    {
        public const string ClinicalNotice = "Este material no sustituye la atención de un profesional de la salud.";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/CVMBSsqTGOk/hqdefault.jpg' WHERE title IN ('Técnicas de Respiración Profunda') AND content_url = 'https://www.youtube.com/watch?v=CVMBSsqTGOk' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/H0pbvdIcHOY/hqdefault.jpg' WHERE title IN ('Ejercicios de Estiramiento para Oficina') AND content_url = 'https://www.youtube.com/watch?v=H0pbvdIcHOY' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/tS52C0Pxf_g/hqdefault.jpg' WHERE title IN ('Yoga para Reducir la Ansiedad') AND content_url = 'https://www.youtube.com/watch?v=tS52C0Pxf_g' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/PzzAlvr3O3M/hqdefault.jpg' WHERE title IN ('Ejercicios de Relajación Muscular Progresiva') AND content_url = 'https://www.youtube.com/watch?v=PzzAlvr3O3M' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/r5K8Drgzj_Y/hqdefault.jpg' WHERE title IN ('Meditación para el Manejo del Dolor Crónico') AND content_url = 'https://www.youtube.com/watch?v=r5K8Drgzj_Y' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/40LH8G7kI84/hqdefault.jpg' WHERE title IN ('Ejercicios de Atención Plena en el Trabajo') AND content_url = 'https://www.youtube.com/watch?v=40LH8G7kI84' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/pyAyEzmiKxU/hqdefault.jpg' WHERE title IN ('Meditación Guiada de 5 Minutos') AND content_url = 'https://www.youtube.com/watch?v=pyAyEzmiKxU' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/an60NyBayzE/hqdefault.jpg' WHERE title IN ('Música Relajante para Dormir') AND content_url = 'https://www.youtube.com/watch?v=an60NyBayzE' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/A8qOLnTbQdg/hqdefault.jpg' WHERE title IN ('Podcast: Manejo de Crisis de Ansiedad') AND content_url = 'https://www.youtube.com/watch?v=A8qOLnTbQdg' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/bwSdDBSJK10/hqdefault.jpg' WHERE title IN ('Sonidos de la Naturaleza - Bosque Lluvioso', 'Sonidos de la Naturaleza – Bosque Lluvioso') AND content_url = 'https://www.youtube.com/watch?v=bwSdDBSJK10' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/GiOamuperW4/hqdefault.jpg' WHERE title IN ('Técnicas de Visualización Positiva') AND content_url = 'https://www.youtube.com/watch?v=GiOamuperW4' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = 'https://img.youtube.com/vi/QLkFTi02Geo/hqdefault.jpg' WHERE title IN ('Caminata Meditativa - Guía de Audio', 'Caminata Meditativa – Guía de Audio') AND content_url = 'https://www.youtube.com/watch?v=QLkFTi02Geo' AND (thumbnail IS NULL OR thumbnail = '');");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET description = CASE WHEN description IS NULL OR description = '' THEN 'Este material no sustituye la atención de un profesional de la salud.' ELSE CONCAT(description, ' ', 'Este material no sustituye la atención de un profesional de la salud.') END WHERE title = 'Meditación para el Manejo del Dolor Crónico' AND (description IS NULL OR description NOT LIKE '%Este material no sustituye la atención de un profesional de la salud.%') AND CHAR_LENGTH(COALESCE(description, '')) <= 930;");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET description = CASE WHEN description IS NULL OR description = '' THEN 'Este material no sustituye la atención de un profesional de la salud.' ELSE CONCAT(description, ' ', 'Este material no sustituye la atención de un profesional de la salud.') END WHERE title = 'Podcast: Manejo de Crisis de Ansiedad' AND (description IS NULL OR description NOT LIKE '%Este material no sustituye la atención de un profesional de la salud.%') AND CHAR_LENGTH(COALESCE(description, '')) <= 930;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Técnicas de Respiración Profunda') AND thumbnail = 'https://img.youtube.com/vi/CVMBSsqTGOk/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Ejercicios de Estiramiento para Oficina') AND thumbnail = 'https://img.youtube.com/vi/H0pbvdIcHOY/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Yoga para Reducir la Ansiedad') AND thumbnail = 'https://img.youtube.com/vi/tS52C0Pxf_g/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Ejercicios de Relajación Muscular Progresiva') AND thumbnail = 'https://img.youtube.com/vi/PzzAlvr3O3M/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Meditación para el Manejo del Dolor Crónico') AND thumbnail = 'https://img.youtube.com/vi/r5K8Drgzj_Y/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Ejercicios de Atención Plena en el Trabajo') AND thumbnail = 'https://img.youtube.com/vi/40LH8G7kI84/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Meditación Guiada de 5 Minutos') AND thumbnail = 'https://img.youtube.com/vi/pyAyEzmiKxU/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Música Relajante para Dormir') AND thumbnail = 'https://img.youtube.com/vi/an60NyBayzE/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Podcast: Manejo de Crisis de Ansiedad') AND thumbnail = 'https://img.youtube.com/vi/A8qOLnTbQdg/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Sonidos de la Naturaleza - Bosque Lluvioso', 'Sonidos de la Naturaleza – Bosque Lluvioso') AND thumbnail = 'https://img.youtube.com/vi/bwSdDBSJK10/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Técnicas de Visualización Positiva') AND thumbnail = 'https://img.youtube.com/vi/GiOamuperW4/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET thumbnail = NULL WHERE title IN ('Caminata Meditativa - Guía de Audio', 'Caminata Meditativa – Guía de Audio') AND thumbnail = 'https://img.youtube.com/vi/QLkFTi02Geo/hqdefault.jpg';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET description = TRIM(REPLACE(description, 'Este material no sustituye la atención de un profesional de la salud.', '')) WHERE title = 'Meditación para el Manejo del Dolor Crónico' AND description LIKE '%Este material no sustituye la atención de un profesional de la salud.%';");
            migrationBuilder.Sql(@"UPDATE resource_libraries SET description = TRIM(REPLACE(description, 'Este material no sustituye la atención de un profesional de la salud.', '')) WHERE title = 'Podcast: Manejo de Crisis de Ansiedad' AND description LIKE '%Este material no sustituye la atención de un profesional de la salud.%';");
        }
    }
}
