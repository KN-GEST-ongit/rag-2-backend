using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseOneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ControlSource",
                table: "game_record_table",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "game_record_table",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PrimaryScore",
                table: "game_record_table",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ControlSource",
                table: "game_record_table");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "game_record_table");

            migrationBuilder.DropColumn(
                name: "PrimaryScore",
                table: "game_record_table");
        }
    }
}
