using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_2_backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInsertRecordedGameScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION InsertRecordedGame(
                    p_game_id INT,
                    p_values TEXT,
                    p_user_id INT,
                    p_players TEXT,
                    p_output_spec TEXT,
                    p_end_state TEXT,
                    p_started TIMESTAMP,
                    p_ended TIMESTAMP,
                    p_sizeMb DOUBLE PRECISION,
                    p_isEmptyRecord BOOLEAN,
                    p_primary_score DOUBLE PRECISION,
                    p_control_source INT,
                    p_model_name TEXT
                )
                RETURNS VOID AS
                $$
                BEGIN
                    INSERT INTO "game_record_table" (
                        "GameId", "Values", "UserId", "Players", "OutputSpec", "EndState",
                        "Started", "Ended", "SizeMb", "IsEmptyRecord",
                        "PrimaryScore", "ControlSource", "ModelName"
                    )
                    VALUES (
                        p_game_id, p_values, p_user_id, p_players, p_output_spec, p_end_state,
                        p_started, p_ended, p_sizeMb, p_isEmptyRecord,
                        p_primary_score, p_control_source, p_model_name
                    );

                    UPDATE "user_table"
                    SET "LastPlayed" = p_ended
                    WHERE "Id" = p_user_id;
                END;
                $$
                LANGUAGE plpgsql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION InsertRecordedGame(
                    p_game_id INT,
                    p_values TEXT,
                    p_user_id INT,
                    p_players TEXT,
                    p_output_spec TEXT,
                    p_end_state TEXT,
                    p_started TIMESTAMP,
                    p_ended TIMESTAMP,
                    p_sizeMb DOUBLE PRECISION,
                    p_isEmptyRecord BOOLEAN
                )
                RETURNS VOID AS
                $$
                BEGIN
                    INSERT INTO "game_record_table" (
                        "GameId", "Values", "UserId", "Players", "OutputSpec", "EndState",
                        "Started", "Ended", "SizeMb", "IsEmptyRecord"
                    )
                    VALUES (
                        p_game_id, p_values, p_user_id, p_players, p_output_spec, p_end_state,
                        p_started, p_ended, p_sizeMb, p_isEmptyRecord
                    );

                    UPDATE "user_table"
                    SET "LastPlayed" = p_ended
                    WHERE "Id" = p_user_id;
                END;
                $$
                LANGUAGE plpgsql;
                """);
        }
    }
}
