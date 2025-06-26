using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using System.Collections.Generic;


public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    void Awake()
    {
        dbPath = "URI=file:" + Application.persistentDataPath + "/stack5.db";

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS HighScores (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Score INTEGER
                    );";
                cmd.ExecuteNonQuery();

            }
        }
    }

    public void SaveScore(int score)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO HighScores (Score) VALUES (@score);";
                cmd.Parameters.AddWithValue("@score", score);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<int> GetTopScores()
    {
        var topScores = new List<int>();

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Score FROM HighScores ORDER BY Score DESC LIMIT 10;";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int score = reader.GetInt32(0);
                        topScores.Add(score);
                    }
                }
            }
        }

        return topScores;
    }


    public void SaveGame(int score, string tileDataJson)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM GameState;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO GameState (Score, TileData) VALUES (@score, @data);";
                cmd.Parameters.AddWithValue("@score", score);
                cmd.Parameters.AddWithValue("@data", tileDataJson);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public (int score, string tileData) LoadGame()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM GameState LIMIT 1;";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int score = reader.GetInt32(1);
                        string tileData = reader.GetString(2);
                        return (score, tileData);
                    }
                }
            }
        }
        return (0, null);
    }
}

