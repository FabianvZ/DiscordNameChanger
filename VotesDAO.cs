﻿using Microsoft.Data.Sqlite;

namespace DiscordNameChanger
{
    public class VotesDAO
    {

        private static readonly string _source = "Data Source=votes.db";

        public static void CreateDatabase()
        {
            using SqliteConnection connection = new(_source);
            connection.Open();

            //DropDatabase(connection, "votes");
            //DropDatabase(connection, "voters");
            //DropDatabase(connection, "nicknames");

            CreateDatabase(connection, @"CREATE TABLE IF NOT EXISTS voters (
                                            voter_id UNSIGNED BIG INT PRIMARY KEY,
                                            banned BOOLEAN DEFAULT false)");
            CreateDatabase(connection, @"CREATE TABLE IF NOT EXISTS nicknames (
                                            nickname_id INTEGER PRIMARY KEY,
                                            nickname TEXT NOT NULL UNIQUE,
                                            invalid BOOLEAN DEFAULT false)");
            CreateDatabase(connection, @"CREATE TABLE IF NOT EXISTS votes (
                                            voter_id UNSIGNED BIG INT NOT NULL,
                                            nickname_id INTEGER NOT NULL,
                                            target_id UNSIGNED BIG INT NOT NULL,
                                            PRIMARY KEY (voter_id, target_id),
                                            FOREIGN KEY (nickname_id) REFERENCES nicknames (nickname_id),
                                            FOREIGN KEY (voter_id) REFERENCES voters (voter_id))");
        }

        private static void DropDatabase(SqliteConnection connection, String db)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS {db}";
            command.ExecuteNonQuery();
        }

        private static void CreateDatabase(SqliteConnection connection, String db)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = db;
            command.ExecuteNonQuery();
        }

        public static Dictionary<string, int> GetAllSuggestions(ulong userID)
        {
            Dictionary<string, int> result = [];
            using (SqliteConnection connection = new(_source))
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
        public static void SetVote(ulong voter, ulong target, string nickname)
        {
            using SqliteConnection connection = new(_source);
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

        public static void SetBannedStatus(ulong userID, Boolean banned)
        {
            using SqliteConnection connection = new(_source);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"insert or replace into voters (voter_id, banned) 
                                        values ($voter_id, $banned)";
            command.Parameters.AddWithValue("$voter_id", userID);
            command.Parameters.AddWithValue("$banned", banned);
            command.ExecuteNonQuery();
        }

        public static void SetInvalidateStatus(string nickname, Boolean invalidated)
        {
            using SqliteConnection connection = new(_source);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"insert or replace into nicknames (nickname_id, nickname, invalid) 
                                        values ((SELECT nickname_id from nicknames WHERE nickname = $nickname), $nickname, $invalidated)";
            command.Parameters.AddWithValue("$nickname", nickname);
            command.Parameters.AddWithValue("$invalidated", invalidated);
            command.ExecuteNonQuery();
        }

        public static List<ulong> GetAllTargetsWhereUserVoted(ulong voter)
        {
            List<ulong> result = [];
            using SqliteConnection connection = new(_source);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT votes.target_id
                                        FROM votes
                                        WHERE voter_id = $voter";
            command.Parameters.AddWithValue("$voter", voter);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add((ulong)reader.GetInt64(0));
            }
            return result;
        }

        internal static List<ulong> GetAllResultsWhereUserHasNicknameVoted(string username)
        {
            List<ulong> result = [];
            using SqliteConnection connection = new(_source);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT DISTINCT votes.target_id
                                        FROM votes
                                        INNER JOIN nicknames ON votes.nickname_id = nicknames.nickname_id
                                        WHERE nickname = $username";
            command.Parameters.AddWithValue("$username", username);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add((ulong)reader.GetInt64(0));
            }
            return result;
        }
    }
}