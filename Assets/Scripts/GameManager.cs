using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool isGameover = false;
    public Text scoreText;
    public GameObject gameoverUI;

    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private MainMenuController mainMenu;

    public bool IsPlaying { get; private set; }
    public bool CanAcceptGameplayInput => IsPlaying && Time.frameCount > startFrame;

    private int score;
    private int startFrame = -1;
    private int gameOverFrame = -1;
    private PlayerController player;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        isGameover = false;
        IsPlaying = false;
    }

    private void Start()
    {
        score = 0;
        player = playerInput != null
            ? playerInput.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();

        if (scoreText != null)
        {
            scoreText.text = "Score : 0";
            scoreText.gameObject.SetActive(false);
        }

        if (gameoverUI != null)
            gameoverUI.SetActive(false);

        if (mainMenu != null)
        {
            mainMenu.gameObject.SetActive(true);
            mainMenu.Initialize(this);
        }
        else
        {
            // Standalone theme scenes without a menu still start immediately.
            StartGame();
        }
    }

    public void StartGame()
    {
        if (IsPlaying || isGameover || player == null) return;

        startFrame = Time.frameCount;
        IsPlaying = true;
        if (mainMenu != null) mainMenu.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        player.BeginRun();
    }

    private void Update()
    {
        if (isGameover &&
            Time.frameCount > gameOverFrame &&
            playerInput != null &&
            playerInput.actions["Jump"].triggered)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void AddScore(int newScore)
    {
        if (!IsPlaying) return;

        score += newScore;

        if (scoreText != null)
            scoreText.text = "Score : " + score;
    }

    public void OnPlayerDead()
    {
        if (!IsPlaying) return;

        IsPlaying = false;
        isGameover = true;
        gameOverFrame = Time.frameCount;

        if (gameoverUI != null)
            gameoverUI.SetActive(true);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
