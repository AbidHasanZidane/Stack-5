using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;
using UnityEditor.U2D.Aseprite;

// Manages overall game flow, UI states, scoring, and game lifecycle
public class GameManager : MonoBehaviour
{
    public TileBoard board;
    public SceneManager scene;                         // Reference to the tile board
    public CanvasGroup gameOverScene;                    // UI overlay shown when the game ends
    public CanvasGroup mainMenu;// UI overlay for the main menu
    public CanvasGroup optionsMenu;
    public CanvasGroup pauseMenu;        // UI overlay for pause men
    //public GameObject resumeButton;  
    //public CanvasGroup helpMenu;
    public CanvasGroup resetConfirmPanel;
    public GameObject optionButton;
    public GameObject restartButton;                // Button to restart the game
    public TextMeshProUGUI pressAnyKeyText;              // UI text prompting the player to start
    public GameObject soundToggleButton;
    public GameObject scoreBox;
    public GameObject bestScoreBox;
    public GameObject nextTileBox;
    public GameObject helpButton;
    public TextMeshProUGUI scoreText;               // Current score display
    public TextMeshProUGUI hiscoreText;             // High score display
    public TextMeshProUGUI nextTileText;            // Shows the number of the next tile to be spawned

    public TextMeshProUGUI soundToggleText;
    
    public TextMeshProUGUI gameModeButtonText;
    public TextMeshProUGUI helpText;
    public CanvasGroup scoreboardScene;
    public TextMeshProUGUI scoreboardText;
    public TextMeshProUGUI gameModeNoticeText;
    public DatabaseManager databaseManager;
    private Coroutine noticeCoroutine;
    private Coroutine blinkCoroutine;
    public bool SpecialTileMode = false;
    public bool pendingSpecialTileMode = false;
    private int score;    // Internal score counter 
    public int highestTile;
    private bool waitingForAnyKey = false;          // Controls transition from main menu to game
    private int nextTileNumber;                     // Holds the number for the next tile
    private int sameNextTileStreak = 0;
    private bool isPaused = false;
    private bool isInOptions = false;
    private bool isSoundOn = true;
    private bool isInScoreboard = false;
    private int unlockThreshold = 160;

    void Start()
    {
        blinkCoroutine = StartCoroutine(BlinkText(pressAnyKeyText));
        (int score, string savedJson) = DatabaseManager.Instance.LoadGameState();

        if (!string.IsNullOrEmpty(savedJson))
        {
            board.RestoreBoardFromJson(savedJson);
            SetScore(score);
        }
        ShowMainMenu();     // Show main menu only if no game to resume
        PrepareNextTile();  // Preload first tile
        LoadGameMode();
        HidePauseMenu();

        highestTile = PlayerPrefs.GetInt("HighestTile", 0);
        soundToggleText.text = isSoundOn ? "Sound On" : "Sound Off";
        gameModeButtonText.text = pendingSpecialTileMode ? "Mode: Special" : "Mode: Classic";
    }


    void Update()
    {
        if (isInOptions)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame(); // Resume game
            }
            return; // Block all other input
        }
        // 2. If game is paused, allow only ESC to resume
        if (isPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame(); // Resume game
            }
            return; // Block all other input
        }
        if (isInScoreboard)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameOver();
            }
            return; // Block all other input
        }

        // 3. Start game from main menu using Enter key
        if (waitingForAnyKey && Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
        {
            waitingForAnyKey = false;
            pressAnyKeyText.gameObject.SetActive(false);
            OnStartGamePressed(); // Start the game
            return;
        }

        // 4. Pause the game while playing (board enabled)
        if (board.enabled && Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
    }


    // Display the main menu and reset game state
    public void ShowMainMenu()
    {
        isInOptions = false;
        Time.timeScale = 1f;
        HideOptionsMenu();  // Hide pause UI just in case

        mainMenu.alpha = 1f;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;

        gameOverScene.alpha = 0f;
        gameOverScene.interactable = false;
        gameOverScene.blocksRaycasts = false;
        
        board.enabled = false; // Disable input while on main menu
        restartButton.SetActive(false);
        board.allowInput = false;

        pressAnyKeyText.gameObject.SetActive(true);
        waitingForAnyKey = true;
        
        isPaused = false;
        Time.timeScale = 1f;
        HidePauseMenu();  // Hide pause UI just in case
        scene.isInHelp = false;
    }
    public void ShowOptionsMenu()
    {
        isInOptions = true;
        Time.timeScale = 0f;
        restartButton.SetActive(false);
        optionButton.SetActive(false);
        scoreBox.SetActive(false);
        bestScoreBox.SetActive(false);
        nextTileBox.SetActive(false);
        helpButton.SetActive(false);
        scene.helpMenu.gameObject.SetActive(false);

        board.allowInput = false;
        optionsMenu.alpha = 1f;
        optionsMenu.interactable = true;
        optionsMenu.blocksRaycasts = true;

        isPaused = false;
        HidePauseMenu();  // Hide pause UI just in case


    }
    public void HideOptionsMenu()
    {

        // Hide Options Menu
        scene.helpMenu.gameObject.SetActive(true);
        isInOptions = false;
        optionsMenu.alpha = 0f;
        optionsMenu.interactable = false;
        optionsMenu.blocksRaycasts = false;


    }

    // Called when "press any key" is detected or Start button is clicked
    public void OnStartGamePressed()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        pressAnyKeyText.gameObject.SetActive(false);
        mainMenu.alpha = 0f;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;
        board.allowInput = true;
        
        if(board.GetTileCount()==0)
        {
            NewGame();
        }
        else
        {
            Debug.Log("Current number of tiles: " + board.GetTileCount());
            NewGameWithSave();
        }
    }

    // Start a new game session
    public void NewGame()
    {
        SpecialTileMode = pendingSpecialTileMode;
        SetScore(0); // Reset score
        hiscoreText.text = LoadHiscore().ToString();

        // Hide game over screen
        gameOverScene.alpha = 0f;
        gameOverScene.interactable = false;

        board.ClearBoard();
        board.CreateSpecificTile(2);
        board.CreateSpecificTile(3);
        board.CreateSpecificTile(5);

        board.enabled = true;          // Enable board input
        restartButton.SetActive(true); // Show restart button
        optionButton.SetActive(true);

        PrepareNextTile(); // Set up the first tile to be dropped
    }
    public void NewGameWithSave()
    {
        SpecialTileMode = pendingSpecialTileMode;
        SetScore(0); // Reset score
        hiscoreText.text = LoadHiscore().ToString();

        // Hide game over UI
        gameOverScene.alpha = 0f;
        gameOverScene.interactable = false;

        board.ClearBoard(); // Always clear the board before a new game

        // Try to load saved state
        var (savedScore, savedBoardJson) = DatabaseManager.Instance.LoadGameState();

        if (!string.IsNullOrEmpty(savedBoardJson) && savedBoardJson.Contains("["))
        {
            // Restore if saved state exists
            SetScore(savedScore);
            board.RestoreBoardFromJson(savedBoardJson);
        }
        else
        {
            Debug.LogWarning("Saved JSON is invalid or empty. Starting new game...");
            NewGame(); // Fall back to clean game logic
            return;    // Exit early so we don’t run PrepareNextTile() twice
        }

        board.enabled = true;
        restartButton.SetActive(true);
        optionButton.SetActive(true);

        PrepareNextTile(); // Ready next tile for gameplay
    }


    // Return to main menu
    public void BackToMenu()
    {
        blinkCoroutine = StartCoroutine(BlinkText(pressAnyKeyText));
        pressAnyKeyText.gameObject.SetActive(true);
        gameOverScene.gameObject.SetActive(false);
        board.ClearBoard(); // Optional cleanup
        ShowMainMenu();
    }

    // Increase score by a certain number of points
    public void IncreaseScore(int points)
    {
        SetScore(score + points);
    }

    // Update the score and save high score if needed
    public void SetScore(int score)
    {
        this.score = score;
        scoreText.text = score.ToString();
        SaveHiscore();
    }
    public int getScore()
    {
        return this.score;
    }

    // Save score to PlayerPrefs if it's higher than existing high score
    private void SaveHiscore()
    {
        int hiscore = LoadHiscore();
        if (score > hiscore)
        {
            PlayerPrefs.SetInt("hiscore", score);
        }
    }

    // Load stored high score from PlayerPrefs
    private int LoadHiscore()
    {
        return PlayerPrefs.GetInt("hiscore", 0);
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        restartButton.SetActive(false);
        optionButton.SetActive(false);
        scoreBox.SetActive(false);
        bestScoreBox.SetActive(false);
        nextTileBox.SetActive(false);
        helpButton.SetActive(false);
        board.allowInput = false;

        pauseMenu.alpha = 1f;
        pauseMenu.interactable = true;
        pauseMenu.blocksRaycasts = true;
        
    }
    private void HidePauseMenu()
    {
        
        pauseMenu.alpha = 0f;
        pauseMenu.interactable = false;
        pauseMenu.blocksRaycasts = false;
    }

    public void ResumeGame()
    {
        isPaused = false;
        scene.isInHelp = false;
        Time.timeScale = 1f;

        restartButton.SetActive(true);
        optionButton.SetActive(true);
        scoreBox.SetActive(true);
        bestScoreBox.SetActive(true);
        nextTileBox.SetActive(true);
        helpButton.SetActive(true);
        board.allowInput = true;
        gameModeNoticeText.gameObject.SetActive(false);

        HidePauseMenu();
        HideOptionsMenu();
        scene.HideHelpMenu();

    }

    public void ShowScoreboard()
    {
        scoreboardScene.alpha = 1f;
        scoreboardScene.interactable = true;
        scoreboardScene.blocksRaycasts = true;
        helpButton.SetActive(false);

        var scores = databaseManager.GetTopScores();
        StringBuilder sb = new StringBuilder("Top Scores:\n");

        int rank = 1;
        foreach (var score in scores)
        {
            sb.AppendLine($"{rank}. {score}");
            rank++;
        }

        scoreboardText.text = sb.ToString(); // Make sure you set this to a UI Text/TMP field
    }
    public void HideScoreboard()
    {
        scoreboardScene.alpha = 0f;
        scoreboardScene.interactable = false;
        scoreboardScene.blocksRaycasts = false;
    }
    public void ToggleScoreboard()
    {
        isInScoreboard = !isInScoreboard;

        if (isInScoreboard)
        {
            ShowScoreboard();
        }
        else
        {
            HideScoreboard();
        }
    }


    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;

        // Example: mute/unmute all audio
        AudioListener.volume = isSoundOn ? 1f : 0f;

        // Update button text
        soundToggleText.text = isSoundOn ? "Sound On" : "Sound Off";
    }

    public void ToggleGameMode()
    {
        int highestTile = PlayerPrefs.GetInt("HighestTile", 0);

        // Show notice ONLY if not yet unlocked
        if (highestTile < unlockThreshold)
        {
            gameModeNoticeText.gameObject.SetActive(true);

            if (noticeCoroutine != null)
                StopCoroutine(noticeCoroutine);

            noticeCoroutine = StartCoroutine(
                ShowTemporaryNotice($"Unlock Special Mode by reaching tile {unlockThreshold}", 2f)
            );
            return;
        }

        gameModeNoticeText.gameObject.SetActive(false);

        pendingSpecialTileMode = !pendingSpecialTileMode;

        if (pendingSpecialTileMode)
        {
            gameModeButtonText.text = "Mode: Special";
            PlayerPrefs.SetInt("GameMode", 1);
        }
        else
        {
            gameModeButtonText.text = "Mode: Classic";
            PlayerPrefs.SetInt("GameMode", 0);
        }

        PlayerPrefs.Save();
    }



    // Trigger game over sequence
    public void GameOver()
    {
        gameOverScene.gameObject.SetActive(true);
        board.enabled = false;
        gameOverScene.interactable = true;
        gameOverScene.blocksRaycasts = true;
        restartButton.SetActive(false);
        optionButton.SetActive(false);
        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.SaveScore(score);
        }
        DatabaseManager.Instance.ClearSavedGame();  
        StartCoroutine(Fade(gameOverScene, 1f, 1f)); // Smooth fade-in
    }
    private void LoadGameMode()
    {
        int mode = PlayerPrefs.GetInt("GameMode", 0); // Default is Classic (0)
        pendingSpecialTileMode = (mode == 1);

        if (pendingSpecialTileMode)
            gameModeButtonText.text = "Mode: Special";
        else
            gameModeButtonText.text = "Mode: Classic";
    }

    // Prepare the number of the next tile to be spawned
    public void PrepareNextTile()
    {
        int newNumber;

        if (sameNextTileStreak >= 2)
        {
            newNumber = (nextTileNumber == 2) ? 3 : 2;
            sameNextTileStreak = 0;
        }
        else
        {
            newNumber = Random.value < 0.5f ? 2 : 3;

            if (newNumber == nextTileNumber)
                sameNextTileStreak++;
            else
                sameNextTileStreak = 1;
        }

        nextTileNumber = newNumber;
        nextTileNumber = newNumber;
        nextTileText.text = nextTileNumber.ToString();
    }


    // Return the prepared next tile value
    public int GetNextTileNumber()
    {
        return nextTileNumber;
    }

    // Smoothly fade a CanvasGroup to a target alpha
    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator BlinkText(TextMeshProUGUI textElement)
    {
        float duration = 1.5f;
        float alpha = 0f;
        Color originalColor = textElement.color;

        while (true)
        {
            // Fade In
            while (alpha < 1f)
            {
                alpha += Time.deltaTime / duration;
                textElement.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            // Fade Out
            while (alpha > 0f)
            {
                alpha -= Time.deltaTime / duration;
                textElement.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
    }

    private IEnumerator ShowTemporaryNotice(string message, float duration)
    {
        gameModeNoticeText.text = message;
        yield return new WaitForSeconds(duration);
        gameModeNoticeText.gameObject.SetActive(false);
    }
 

    public void ShowResetConfirmPanel()
    {
        HideOptionsMenu();
        resetConfirmPanel.alpha = 1f;
        resetConfirmPanel.interactable = true;
        resetConfirmPanel.blocksRaycasts = true;
    }

    public void HideResetConfirmPanel()
    {
        ShowOptionsMenu();
        resetConfirmPanel.alpha = 0f;
        resetConfirmPanel.interactable = false;
        resetConfirmPanel.blocksRaycasts = false;
    }

    public void ConfirmReset()
    {
        DatabaseManager.Instance.ResetAllGameData();
        HideResetConfirmPanel();

        // Reload the main menu or current scene
        QuitGame();
    }

    public void CancelReset()
    {
        HideResetConfirmPanel();
    }


    public void OnApplicationQuit()
    {
        if(!board.CheckForGameOver())//to solve start from full board issue
        {
            string boardJson = board.GetBoardJson();
            int currentScore = getScore(); // Replace with your actual score variable
            DatabaseManager.Instance.SaveGameState(boardJson, currentScore);
        }
        
    }

    // Quit the application (supports both editor and built version)
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Exit play mode in editor
#else
        Application.Quit(); // Quit built application
#endif
    }
}
