using Microsoft.Data.Sqlite;

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

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS voters (
                    voter_id INTEGER PRIMARY KEY,
                    banned BOOLEAN DEFAULT false";
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = command.CommandText = @"CREATE TABLE IF NOT EXISTS nicknames (
                    nickname_id INTEGER PRIMARY KEY,
                    nickname TEXT NOT NULL,
                    invalid BOOLEAN DEFAULT false";
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                    voter_id INTEGER NOT NULL,
                    nickname_id TEXT NOT NULL,
                    target_id INTEGER NOT NULL,
                    PRIMARY KEY (voter_id, target_id),
                    FOREIGN KEY (nickname_id) REFERENCES nicknames (nickname_id),
                    FOREIGN KEY (vote_id) REFERENCES voters (voter_id)";
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<string, int> GetAllSuggestions(int userID)
        {
            Dictionary<string, int> result = [];
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT nickname, SUM(*)
                                        FROM votes
                                        INNER JOIN nicknames on votes.nickname_id = nicknames.nickname_id
                                        WHERE target_id = $target_id
                                        GROUP BY nickname
                                        ORDER BY SUM(*) ASC";
                command.Parameters.AddWithValue("$target_id", userID);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(reader.GetString(0), reader.GetInt32(1));
                }
            }
            return result;
        }

        public void BanUser(int userID)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"insert or replace into voters (voter_id, banned) 
                                        values ($voter_id, true)";
                command.Parameters.AddWithValue("$voter_id", userID);
                command.ExecuteNonQuery();
            }
        }

        public void UnbanUser(int userID)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"insert or replace into voters (voter_id, banned) 
                                        values ($voter_id, false)";
                command.Parameters.AddWithValue("$voter_id", userID);
                command.ExecuteNonQuery();
            }
        }

        public void SetVote(int voter, int target, string nickname)
        {
            using (var connection = new SqliteConnection(_source))
            {
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = @"insert or replace into nicknames (nickname) 
                                        values ($nickname)";
                command.Parameters.AddWithValue("$nickname", nickname);
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"insert or replace into votes (voter_id, target_id, nickname_id) 
                                        values ($voter_id, $target_id, (SELECT nickname_id FROM nicknames WHERE nickname = $nickname)";
                command.Parameters.AddWithValue("$voter_id", voter);
                command.Parameters.AddWithValue("$target_id", voter);
                command.Parameters.AddWithValue("$nickname", nickname);
                command.ExecuteNonQuery();
            }
        }
    }
}
