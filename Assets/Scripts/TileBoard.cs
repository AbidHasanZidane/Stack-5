using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBoard : MonoBehaviour
{
    public GameManager gameManager;
    public Tile tilePrefab;
    public TileState[] tileStates;
    private TileGrid grid;
    private List<Tile> tiles; // List of all active tiles on the board
    private bool waiting; // Flag to prevent inputs during animations
    public AudioSource MoveSound;
    private int lastSpawnedNumber = -1;
    private int sameTileStreak = 0;
    private bool forceNextTile = false;
    public bool allowInput = true;
    public bool isAnimating = false;
    public int GetTileCount()
    {
        return tiles.Count;
    }
    [System.Serializable]
    public class TileData
    {
        public int x;
        public int y;
        public int value;
    }

    [System.Serializable]
    public class BoardState
    {
        public List<TileData> tiles;
        public int score;
    }

    private void Update()
    {
        if (!waiting && allowInput&&!isAnimating)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                Move(Vector2Int.up, 0, 1, 1, 1);
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(Vector2Int.down, 0, 1, grid.height - 2, -1);
            }
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Move(Vector2Int.left, 1, 1, 0, 1);

            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                Move(Vector2Int.right, grid.width - 2, -1, 0, 1);
            }
        }


    }
    public string GetBoardJson()
    {
        BoardState state = new BoardState();
        state.tiles = new List<TileData>();
        state.score = gameManager.getScore();

        foreach (var tile in tiles)
        {
            if (tile != null && tile.cell != null)
            {
                state.tiles.Add(new TileData
                {
                    x = tile.cell.coordinates.x,
                    y = tile.cell.coordinates.y,
                    value = tile.number
                });
            }
        }

        return JsonUtility.ToJson(state);
    }

    public void RestoreBoardFromJson(string json)
    {
        Debug.Log("Restoring board from JSON: " + json);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("JSON is empty or null.");
            return;
        }
        BoardState state = JsonUtility.FromJson<BoardState>(json);

        if (tiles == null||state.tiles==null)
        {
            Debug.LogWarning("Deserialized board or tile list is null.");
            return;
        }
        ClearBoard(); // You must remove all current tiles from the board

        foreach (var tileInfo in state.tiles)
        {
            Tile tile = Instantiate(tilePrefab, grid.transform);
            int index = IndexOf(tileInfo.value);
            tile.SetState(tileStates[index], tileInfo.value);

            TileCell cell = grid.GetCell(tileInfo.x, tileInfo.y);
            if (cell == null)
            {
                Debug.LogWarning($"Cell not found at ({tileInfo.x}, {tileInfo.y})");
                continue;
            }
            tile.Spawn(cell);

            tiles.Add(tile);
        }
        Debug.Log("Board restored with " + tiles.Count + " tiles.");
        gameManager.SetScore(state.score);
    }

    public TileState GetStateForNumber(int number)
    {
        foreach (var state in tileStates)
        {
            if (state.number == number)
                return state;
        }
        return null;
    }
    private void Awake()
    {
        grid = GetComponentInChildren<TileGrid>();
        tiles  = new List<Tile>();
        
    }

    public void ClearBoard()
    {
        foreach(var cell in grid.cells)
        {
            cell.tile = null;
        }
        foreach(var tile in tiles)
        {
            Destroy(tile.gameObject);
        }
        tiles.Clear();
        
    }
    // public void OriginalCreateTile()
    //     {
    //          Tile tile = Instantiate(tilePrefab, grid.transform);

    //         int number = gameManager.GetNextTileNumber(); // Get the prepared tile
    //         int index = IndexOf(number);
    //         tile.SetState(tileStates[index], number);

    //         tile.Spawn(grid.GetRandomEmptyCell());
    //         tiles.Add(tile);

    //         gameManager.PrepareNextTile();
    //     }

    public void CreateTile()
    {
        if (gameManager.SpecialTileMode)
        {
            CreateTileWithST();  // with 2222 / 3333 and anti-streak logic
        }
        else
        {
            CreateTileClassic();  // just basic 2 or 3
        }
    }

    public void CreateTileWithST()
    {
        Tile tile = Instantiate(tilePrefab, grid.transform);

        int roll = Random.Range(0, 50);
        if (roll == 22)
        {
            tile.SetState(tileStates[tileStates.Length - 2], 2222, true);
            tile.Spawn(grid.GetRandomEmptyCell());
            tiles.Add(tile);
            TriggerPrimeTileEffect(2, tile);
            return;
        }
        else if (roll == 33)
        {
            tile.SetState(tileStates[tileStates.Length - 1], 3333, true);
            tile.Spawn(grid.GetRandomEmptyCell());
            tiles.Add(tile);
            TriggerPrimeTileEffect(3, tile);
            return;
        }

        int number;

        if (forceNextTile)
        {
            // Force a different number
            number = (lastSpawnedNumber == 2) ? 3 : 2;
            forceNextTile = false;
            sameTileStreak = 0;
            lastSpawnedNumber = number;
        }
        else
        {
            number = gameManager.GetNextTileNumber();

            if (number == lastSpawnedNumber)
            {
                sameTileStreak++;
                if (sameTileStreak >= 3)
                {
                    forceNextTile = true;
                    sameTileStreak = 0; // Reset to avoid double trigger
                }
            }
            else
            {
                lastSpawnedNumber = number;
                sameTileStreak = 1;
            }
        }

        try
        {
            int index = IndexOf(number);
            if (index < 0 || index >= tileStates.Length)
                throw new System.Exception($"Invalid index {index} for number {number}");

            tile.SetState(tileStates[index], number);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to set tile state: " + ex.Message);
        }

        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
        gameManager.PrepareNextTile(); // Set and show the next tile for player
    }


    public void CreateTileClassic()
    {
        Tile tile = Instantiate(tilePrefab, grid.transform);

        int number;

        if (forceNextTile)
        {
            // Force a different number
            number = (lastSpawnedNumber == 2) ? 3 : 2;
            forceNextTile = false;
            sameTileStreak = 0;
            lastSpawnedNumber = number;
        }
        else
        {
            number = gameManager.GetNextTileNumber();

            if (number == lastSpawnedNumber)
            {
                sameTileStreak++;
                if (sameTileStreak >= 3)
                {
                    forceNextTile = true;
                    sameTileStreak = 0; // Reset to avoid double trigger
                }
            }
            else
            {
                lastSpawnedNumber = number;
                sameTileStreak = 1;
            }
        }

        try
        {
            int index = IndexOf(number);
            if (index < 0 || index >= tileStates.Length)
                throw new System.Exception($"Invalid index {index} for number {number}");

            tile.SetState(tileStates[index], number);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to set tile state: " + ex.Message);
        }

        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
        gameManager.PrepareNextTile(); // Set and show the next tile for player
    }



    public void CreateSpecificTile(int number)
    {
        Tile tile = Instantiate(tilePrefab, grid.transform);

        try
        {
            int index = IndexOf(number);
            if (index < 0 || index >= tileStates.Length)
                throw new System.Exception($"Invalid index {index} for number {number}");

            tile.SetState(tileStates[index], number);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to set tile state: " + ex.Message);
        }

        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
    }


    private void Move(Vector2Int direction,int startX,int incrementX,int startY,int incrementY)
    {
        if (waiting&&!allowInput&&isAnimating) return;
        gameManager.lastBoardJson = GetBoardJson();
        gameManager.lastScore = gameManager.getScore();
        isAnimating = true;
        bool changed = false;
        for(int x = startX;x>=0 && x<grid.width;x+=incrementX)
        {
            for(int y = startY;y>=0 && y<grid.height;y+=incrementY)
            {
                TileCell cell = grid.GetCell(x,y);
                if(cell.occupied)
                {
                    changed |= MoveTile(cell.tile,direction);
                }
            }
        }
        if(changed)
        {
            StartCoroutine(WaitForChange());
            MoveSound.Play();
        }
        else
        {
            isAnimating = false;
        }
    }

    private bool MoveTile(Tile tile, Vector2Int direction)
    {
        TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);
        TileCell lastValid = null;

        while (adjacent != null)
        {
            if (adjacent.occupied)
            {
                if (CanMerge(tile, adjacent.tile))
                {
                    Merge(tile, adjacent.tile);
                    return true;
                }
                break;
            }

            lastValid = adjacent;
            adjacent = grid.GetAdjacentCell(adjacent, direction);
        }

        if (lastValid != null)
        {
            tile.MoveTo(lastValid);
            return true;
        }

        return false;
    }



    private bool CanMerge(Tile a, Tile b)
    {
        if (b.locked) return false;
        if ((a.number == 2 && b.number == 3) || (a.number == 3 && b.number == 2))
        {
            return true; // Special case
        }

        // After 5, standard rule: only same numbers can merge
        if (a.number >= 5 && a.number == b.number)
        {
            return true;
        }

        return false;
    }


    private void Merge(Tile a, Tile b)
    {
        tiles.Remove(a);
        a.Merge(b.cell);

        int number;
        if ((a.number == 2 && b.number == 3) || (a.number == 3 && b.number == 2))
        {
            number = 5;
        }
        else
        {
            number = b.number * 2;
        }
        if (number > gameManager.highestTile)
        {
            gameManager.highestTile = number;
            PlayerPrefs.SetInt("HighestTile", gameManager.highestTile);
            PlayerPrefs.Save();
        }

        int index = IndexOf(number);
        if (index >= 0 && index < tileStates.Length)
        {
            b.SetState(tileStates[index], number);
            gameManager.IncreaseScore(number * 5);
        }

    }
    private int IndexOf(int number)
    {
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (tileStates[i].number == number)
            {
                return i;
            }
        }
        return -1;
    }

    private IEnumerator WaitForChange()
    {
        waiting = true;
        
        yield return new WaitForSeconds(0.2f);
        waiting = false;
        isAnimating = false;

        foreach (var Tile in tiles)
        {
            Tile.locked = false;
        }
        if(tiles.Count!=grid.size)
        {
            CreateTile();
        }
        if(CheckForGameOver())
        {
            gameManager.GameOver();
        }
       
    }

    public void TriggerPrimeTileEffect(int targetNumber, Tile primeTile)
    {
        int index5 = IndexOf(5);

        foreach (Tile t in tiles.ToArray()) // ToArray so we don't modify while iterating
        {
            if (t != primeTile && t.number == targetNumber)
            {
                t.SetState(tileStates[index5], 5);
            }
        }

        tiles.Remove(primeTile);
        StartCoroutine(DestroyAfterSeconds(primeTile));
    }
    private IEnumerator DestroyAfterSeconds(Tile tile, float seconds = 1.0f)
    {
        yield return new WaitForSeconds(seconds);

        // Check if tile still exists before accessing it
        if (tile != null && tile.gameObject != null)
        {
            tiles.Remove(tile);
            Destroy(tile.gameObject);
        }
    }
    public bool CheckForGameOver()
    {
        if (tiles.Count != grid.size)
        {
            return false;
        }

        foreach (var tile in tiles)
        {
            TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
            TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
            TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
            TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

            if (up != null && CanMerge(tile, up.tile))
            {
                return false;
            }

            if (down != null && CanMerge(tile, down.tile))
            {
                return false;
            }

            if (left != null && CanMerge(tile, left.tile))
            {
                return false;
            }

            if (right != null && CanMerge(tile, right.tile))
            {
                return false;
            }
        }

        return true;
    }


}
