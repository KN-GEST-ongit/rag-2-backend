using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGameScoreConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_score_config_table",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ScoreType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_score_config_table", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_score_config_table_game_table_GameId",
                        column: x => x.GameId,
                        principalTable: "game_table",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_score_config_table_GameId",
                table: "game_score_config_table",
                column: "GameId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO public.game_table ("Name")
                VALUES ('crossyroad'), ('flappybird')
                ON CONFLICT ("Name") DO NOTHING;

                INSERT INTO public.game_score_config_table ("GameId", "ScoreType")
                SELECT g."Id", 0
                FROM public.game_table g
                WHERE lower(g."Name") IN ('pong', 'crossyroad', 'flappybird')
                AND NOT EXISTS (
                    SELECT 1 FROM public.game_score_config_table c WHERE c."GameId" = g."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_score_config_table");
        }
    }
}
