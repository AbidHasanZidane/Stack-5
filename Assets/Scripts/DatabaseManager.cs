using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

[System.Serializable]
public class GameSaveData
{
    public int score;
    public int[,] boardTiles;
    public bool specialModeEnabled;
    public int nextTile;
}

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    private string dbPath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        dbPath = "URI=file:" + Application.persistentDataPath + "/stack5.db";

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                // Game state table
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS GameState (
                                        ID INTEGER PRIMARY KEY,
                                        Score INTEGER,
                                        TileData TEXT
                                    );";
                cmd.ExecuteNonQuery();


            }
        }
    }

    public void SaveScore(int score)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("DB path is empty. Cannot save score.");
            return;
        }

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

        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("DB path is empty. Cannot load scores.");
            return topScores;
        }

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

    public void SaveGameState(string json, int score)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("DB path is empty. Cannot save game state.");
            return;
        }

        Debug.Log("Saving Game: Score = " + score + ", Data = " + json);

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM GameState;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO GameState (Score, TileData) VALUES (@score, @data);";
                cmd.Parameters.AddWithValue("@score", score);
                cmd.Parameters.AddWithValue("@data", json);
                cmd.ExecuteNonQuery();
            }
        }

        Debug.Log("Game saved successfully.");
    }

    public (int score, string tileData) LoadGameState()
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("DB path is empty. Cannot load game state.");
            return (0, null);
        }

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Score, TileData FROM GameState LIMIT 1;";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int score = reader.GetInt32(0);
                        string tileData = reader.GetString(1);
                        Debug.Log("Loaded game: Score = " + score + ", Data = " + tileData);
                        return (score, tileData);
                    }
                }
            }
        }

        Debug.LogWarning("No saved game found.");
        return (0, null);
    }

    public void ClearSavedGame()
    {
        if (string.IsNullOrEmpty(dbPath)) return;

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM GameState;";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void ResetAllGameData()
    {
        if (string.IsNullOrEmpty(dbPath)) return;

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM GameState;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM HighScores;";
                cmd.ExecuteNonQuery();
            }
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("All game data has been reset.");
    }
}
