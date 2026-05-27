using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameTypeFromGameScoreConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM public.game_score_config_table c
                USING public.game_table g
                WHERE c."GameId" = g."Id" AND lower(g."Name") NOT IN ('crossyroad', 'flappybird');
                """);

            migrationBuilder.DropColumn(
                name: "GameType",
                table: "game_score_config_table");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameType",
                table: "game_score_config_table",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
