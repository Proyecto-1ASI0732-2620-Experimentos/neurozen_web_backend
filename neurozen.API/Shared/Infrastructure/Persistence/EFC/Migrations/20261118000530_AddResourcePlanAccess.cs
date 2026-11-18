using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <summary>
    ///     Plan-based access to the resource library. Adds resource_libraries.required_plan (1 = Básico,
    ///     2 = Avanzado, 3 = Profesional). Every existing resource starts as Avanzado (column default) and then the
    ///     first 2 resources of EACH type (by id: 2 videos, 2 audios, 2 articles, 2 exercises when they exist) become
    ///     Básico, so the Básico plan sees 1-2 of each type and Avanzado sees the whole library. No row is inserted or
    ///     deleted. The admin can change the plan of any resource later with PATCH /resource-libraries/{id}.
    /// </summary>
    public partial class AddResourcePlanAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "required_plan",
                table: "resource_libraries",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql(@"UPDATE resource_libraries r
JOIN (SELECT id, ROW_NUMBER() OVER (PARTITION BY resource_type ORDER BY id) AS position FROM resource_libraries) ranked
  ON ranked.id = r.id
SET r.required_plan = 1
WHERE ranked.position <= 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_plan",
                table: "resource_libraries");
        }
    }
}
