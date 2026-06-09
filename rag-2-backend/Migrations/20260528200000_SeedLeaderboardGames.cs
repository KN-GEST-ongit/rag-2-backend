using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeaderboardGames : Migration
    {
        private const string AllGames =
            "'crossyroad', 'flappybird', 'timberman', 'ballfall', " +
            "'twozerofoureight', 'happyjump', 'spaceinvaders', 'tetris', 'snake', 'pacman'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO public.game_table (\"Name\") VALUES " +
                "('timberman'), ('ballfall'), ('twozerofoureight'), ('happyjump'), " +
                "('spaceinvaders'), ('tetris'), ('snake'), ('pacman') " +
                "ON CONFLICT (\"Name\") DO NOTHING;"
            );

            migrationBuilder.Sql(
                "INSERT INTO public.game_score_config_table (\"GameId\", \"ScoreType\") " +
                "SELECT g.\"Id\", 0 " +
                "FROM public.game_table g " +
                "WHERE lower(g.\"Name\") IN (" + AllGames + ") " +
                "AND NOT EXISTS (" +
                "    SELECT 1 FROM public.game_score_config_table c WHERE c.\"GameId\" = g.\"Id\"" +
                ");"
            );

            migrationBuilder.Sql(
                "UPDATE public.game_score_config_table " +
                "SET \"ScoreType\" = 1 " +
                "WHERE \"GameId\" IN (" +
                "    SELECT g.\"Id\" FROM public.game_table g WHERE lower(g.\"Name\") = 'ballfall'" +
                ");"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM public.game_score_config_table c " +
                "USING public.game_table g " +
                "WHERE c.\"GameId\" = g.\"Id\" AND lower(g.\"Name\") IN (" + AllGames + ");"
            );
        }
    }
}
