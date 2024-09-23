using Microsoft.Data.Sqlite;
using System.Diagnostics.Metrics;

namespace DiscordNameChanger
{
    public class VotesDAO
    {

        private readonly string _source = "Data Source=votes.db";

        public VotesDAO()
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();

                //SqliteCommand command = connection.CreateCommand();
                //command.CommandText = command.CommandText = @"DROP TABLE IF EXISTS votes";
                //command.ExecuteNonQuery();
                //command = connection.CreateCommand();
                //command.CommandText = command.CommandText = @"DROP TABLE IF EXISTS voters";
                //command.ExecuteNonQuery();
                //command = connection.CreateCommand();
                //command.CommandText = command.CommandText = @"DROP TABLE IF EXISTS nicknames";
                //command.ExecuteNonQuery();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS voters (
                    voter_id UNSIGNED BIG INT PRIMARY KEY,
                    banned BOOLEAN DEFAULT false)";
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = command.CommandText = @"CREATE TABLE IF NOT EXISTS nicknames (
                    nickname_id INTEGER PRIMARY KEY,
                    nickname TEXT NOT NULL UNIQUE,
                    invalid BOOLEAN DEFAULT false)";
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                    voter_id UNSIGNED BIG INT NOT NULL,
                    nickname_id INTEGER NOT NULL,
                    target_id UNSIGNED BIG INT NOT NULL,
                    PRIMARY KEY (voter_id, target_id),
                    FOREIGN KEY (nickname_id) REFERENCES nicknames (nickname_id),
                    FOREIGN KEY (voter_id) REFERENCES voters (voter_id))";
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<string, int> GetAllSuggestions(ulong userID)
        {
            Dictionary<string, int> result = [];
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT nickname, COUNT(votes.voter_id) AS amount_of_votes
                                        FROM votes
                                        INNER JOIN nicknames on votes.nickname_id = nicknames.nickname_id
                                        INNER JOIN voters on votes.voter_id = voters.voter_id
                                        WHERE target_id = $target_id 
                                            AND NOT banned
                                            AND NOT invalid
                                        GROUP BY nickname
                                        ORDER BY amount_of_votes ASC";
                command.Parameters.AddWithValue("$target_id", userID);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(reader.GetString(0), reader.GetInt32(1));
                }
            }
            return result;
        }
        public void SetVote(ulong voter, ulong target, string nickname)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"INSERT OR IGNORE INTO nicknames (nickname) VALUES ($nickname)";
                command.Parameters.AddWithValue("$nickname", nickname);
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"INSERT OR IGNORE INTO voters (voter_id)  VALUES ($voter_id)";
                command.Parameters.AddWithValue("$voter_id", voter);
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"insert or replace into votes (voter_id, target_id, nickname_id) 
                                        values ($voter_id, $target_id, (SELECT nickname_id FROM nicknames WHERE nickname = $nickname))";
                command.Parameters.AddWithValue("$voter_id", voter);
                command.Parameters.AddWithValue("$target_id", target);
                command.Parameters.AddWithValue("$nickname", nickname);
                command.ExecuteNonQuery();
            }
        }

        public void setBannedStatus(ulong userID, Boolean banned)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"insert or replace into voters (voter_id, banned) 
                                        values ($voter_id, $banned)";
                command.Parameters.AddWithValue("$voter_id", userID);
                command.Parameters.AddWithValue("$banned", banned);
                command.ExecuteNonQuery();
            }
        }

        public void setInvalidateStatus(string nickname, Boolean invalidated)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"insert or replace into nicknames (nickname_id, nickname, invalid) 
                                        values ((SELECT nickname_id from nicknames WHERE nickname = $nickname), $nickname, $invalidated)";
                command.Parameters.AddWithValue("$nickname", nickname);
                command.Parameters.AddWithValue("$invalidated", invalidated);
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<ulong, string> getAllResultsWhereUserVoted(ulong voter)
        {
            Dictionary<ulong, string> result = [];
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"WITH valid_votes AS (
                                        SELECT target_id, nickname
                                        FROM votes
                                        INNER JOIN nicknames on votes.nickname_id = nicknames.nickname_id
                                        INNER JOIN voters on votes.voter_id = voters.voter_id
                                        WHERE NOT banned AND NOT invalid
                                        GROUP BY target_id, nickname
                                        ORDER BY COUNT(votes.voter_id) ASC
                                        )
                                        SELECT votes.target_id, (SELECT nickname FROM valid_votes WHERE valid_votes.target_id = votes.target_id LIMIT 1) AS most_voted_nickname
                                        FROM votes
                                        WHERE voter_id = $voter";
                command.Parameters.AddWithValue("$voter", voter);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add((ulong)reader.GetInt64(0), reader.IsDBNull(1)? "" : reader.GetString(1));
                }
            }
            return result;
        }

        internal Dictionary<ulong, string> getAllResultsWhereUserHasNicknameVoted(string username)
        {
            Dictionary<ulong, string> result = [];
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"WITH valid_votes AS (
                                        SELECT target_id, nickname
                                        FROM votes
                                        INNER JOIN nicknames on votes.nickname_id = nicknames.nickname_id
                                        INNER JOIN voters on votes.voter_id = voters.voter_id
                                        WHERE NOT banned AND NOT invalid
                                        GROUP BY target_id, nickname
                                        ORDER BY COUNT(votes.voter_id) ASC
                                        )
                                        SELECT votes.target_id, (SELECT nickname FROM valid_votes WHERE valid_votes.target_id = votes.target_id LIMIT 1) AS most_voted_nickname
                                        FROM votes
                                        WHERE nickname_id = (SELECT nickname_id FROM nicknames WHERE nickname = $username)";
                command.Parameters.AddWithValue("$username", username);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add((ulong)reader.GetInt64(0), reader.IsDBNull(1) ? "" : reader.GetString(1));
                }
            }
            return result;
        }
    }
}
