using System.Collections;
using UnityEngine;
using TMPro;

// Manages overall game flow, UI states, scoring, and game lifecycle
public class GameManager : MonoBehaviour
{
    public TileBoard board;                         // Reference to the tile board
    public CanvasGroup gameOver;                    // UI overlay shown when the game ends
    public CanvasGroup mainMenu;
    public CanvasGroup pauseMenu;        // UI overlay for pause menu
    //public GameObject resumeButton;                      // UI overlay for the main menu
    public GameObject restartButton;                // Button to restart the game
    public GameObject pressSpaceKeyText;              // UI text prompting the player to start

    public TextMeshProUGUI scoreText;               // Current score display
    public TextMeshProUGUI hiscoreText;             // High score display
    public TextMeshProUGUI nextTileText;            // Shows the number of the next tile to be spawned

    private int score;                              // Internal score counter
    //private bool waitingForEnterKey = false;          // Controls transition from main menu to game
    private int nextTileNumber;                     // Holds the number for the next tile
    private int sameNextTileStreak = 0;
    private bool isPaused = false;
    void Start()
    {
        ShowMainMenu();     // Initialize to main menu
        PrepareNextTile();  // Preload the first next tile
        HidePauseMenu();
    }

    void Update()
    {
        if (isPaused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeGame();
            return; // Block all other input
        }
        // Listen for enter key press to begin the game
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //waitingForEnterKey = false;
            pressSpaceKeyText.SetActive(false);
            OnStartGamePressed(); // Start the actual game
        }
        if (Input.GetKeyDown(KeyCode.Escape) && board.enabled)
        {
            PauseGame();
        }
    }

    // Display the main menu and reset game state
    public void ShowMainMenu()
    {
        mainMenu.alpha = 1f;
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;

        gameOver.alpha = 0f;
        gameOver.interactable = false;
        gameOver.blocksRaycasts = false;

        board.enabled = false; // Disable input while on main menu
        restartButton.SetActive(false);
        board.allowInput = false;

        pressSpaceKeyText.SetActive(true);
        //waitingForEnterKey = true;
        
        isPaused = false;
        Time.timeScale = 1f;
        HidePauseMenu();  // Hide pause UI just in case
    }

    // Called when "press any key" is detected or Start button is clicked
    public void OnStartGamePressed()
    {
        mainMenu.alpha = 0f;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;
        board.allowInput = true;
        NewGame();
    }

    // Start a new game session
    public void NewGame()
    {
        SetScore(0); // Reset score
        hiscoreText.text = LoadHiscore().ToString();

        // Hide game over screen
        gameOver.alpha = 0f;
        gameOver.interactable = false;

        board.ClearBoard();
        board.CreateSpecificTile(2);
        board.CreateSpecificTile(3);
        board.CreateSpecificTile(5);

        board.enabled = true;          // Enable board input
        restartButton.SetActive(true); // Show restart button

        PrepareNextTile(); // Set up the first tile to be dropped
    }

    // Return to main menu
    public void BackToMenu()
    {
        board.ClearBoard(); // Optional cleanup
        ShowMainMenu();
    }

    // Increase score by a certain number of points
    public void IncreaseScore(int points)
    {
        SetScore(score + points);
    }

    // Update the score and save high score if needed
    private void SetScore(int score)
    {
        this.score = score;
        scoreText.text = score.ToString();
        SaveHiscore();
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
        Time.timeScale = 1f;
        restartButton.SetActive(true);
        board.allowInput = true;
        HidePauseMenu();
        
    }

    // Trigger game over sequence
    public void GameOver()
    {
        board.enabled = false;
        gameOver.interactable = true;
        gameOver.blocksRaycasts = true;
        restartButton.SetActive(false);

        StartCoroutine(Fade(gameOver, 1f, 1f)); // Smooth fade-in
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
