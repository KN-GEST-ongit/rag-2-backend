using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameTypeFromGameScoreConfig : Migration
    {
        private const string SupportedGames = "'crossyroad', 'flappybird', 'timberman', 'ballfall', " +
                                              "'twozerofoureight', 'happyjump', 'spaceinvaders', 'tetris', " +
                                              "'snake'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM public.game_score_config_table c " +
                "USING public.game_table g " +
                "WHERE c.\"GameId\" = g.\"Id\" AND lower(g.\"Name\") NOT IN (" + SupportedGames + ");"
            );

            migrationBuilder.Sql("""
                INSERT INTO public.game_table ("Name")
                VALUES
                    ('timberman'),
                    ('ballfall'),
                    ('twozerofoureight'),
                    ('happyjump'),
                    ('spaceinvaders'),
                    ('tetris'),
                    ('snake')
                ON CONFLICT ("Name") DO NOTHING;
                """);

            migrationBuilder.Sql(
                "INSERT INTO public.game_score_config_table (\"GameId\", \"ScoreType\") " +
                "SELECT g.\"Id\", 0 " +
                "FROM public.game_table g " +
                "WHERE lower(g.\"Name\") IN (" + SupportedGames + ") " +
                "AND NOT EXISTS (" +
                "    SELECT 1 FROM public.game_score_config_table c WHERE c.\"GameId\" = g.\"Id\"" +
                ");"
            );

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
