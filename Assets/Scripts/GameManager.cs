using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public enum GameState { PlacingStart, CreatingPlanets, PlacingFinish, Playing, GameOver, Victory }
    public GameState currentState = GameState.PlacingStart;
    
    [Header("Setup")]
    public GameObject astronaut;
    public int midAirPlanetsNeeded = 6;
    
    [Header("Lives")]
    public int maxLives = 7;
    public int currentLives;
    
    [Header("UI")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI stateText;
    public GameObject victoryPanel;
    public GameObject gameOverPanel;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip planetSound;
    public AudioClip missSound;
    public AudioClip victorySound;
    
    public List<GameObject> planets = new List<GameObject>();
    private HashSet<int> visitedPlanets = new HashSet<int>();

    public GameObject coverPanel;

    public float flashImageDuration = 3.0f;

    public GameObject resetButton;
    
    public GameObject resetButton2;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        StartCoroutine(FlashImage(coverPanel));
        currentLives = maxLives;
        astronaut.SetActive(false);
        victoryPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        resetButton.SetActive(false);
        resetButton2.SetActive(false);
        UpdateUI();
    }
    
    void Update()
    {
        UpdateInstructions();
    }
    
    void UpdateInstructions()
    {
        stateText.text = currentState.ToString();
        
        switch (currentState)
        {
            case GameState.PlacingStart:
                instructionText.text = "TAP detected ground plane to place START";
                break;
                
            case GameState.CreatingPlanets:
                int created = planets.Count - 1;
                instructionText.text = $"HOLD screen to create mid-air planet ({created}/{midAirPlanetsNeeded})\nMove phone to position, then hold";
                break;
                
            case GameState.PlacingFinish:
                instructionText.text = "TAP detected ground plane to place FINISH";
                break;
                
        //     case GameState.Playing:
        //         instructionText.text = "HOLD near astronaut to jump!\nVisit ALL planets to win!";
        //         break;
                
        //     case GameState.Victory:
        //         instructionText.text = "";
        //         break;
                
        //     case GameState.GameOver:
        //         instructionText.text = "";
        //         break;
        }
    }
    
    public void MarkPlanetVisited(int index)
    {
        if (!visitedPlanets.Contains(index))
        {
            visitedPlanets.Add(index);
            
            Renderer rend = planets[index].GetComponentInChildren<Renderer>();

            if (rend != null && rend.material != null)
            {
                rend.material.SetColor("_EmissionColor", Color.black);

                rend.material.DisableKeyword("_EMISSION");

            }
            
            Debug.Log($"[GameManager] Planet {index} visited! Total: {visitedPlanets.Count}/{planets.Count}");
            
            if (visitedPlanets.Count >= planets.Count)
            {
                Victory();
            }
        }
    }
    
    public void LoseLife()
    {
        currentLives--;
        UpdateUI();
        
        if (audioSource && missSound)
        {
            audioSource.PlayOneShot(missSound);
        }
        
        Debug.Log($"[GameManager] Lost a life! Remaining: {currentLives}");
        
        if (currentLives <= 0)
        {
            GameOver();
        }
    }
    
    void Victory()
    {
        currentState = GameState.Victory;
        victoryPanel.SetActive(true);
        resetButton.SetActive(true);
        
        if (audioSource && victorySound)
        {
            audioSource.PlayOneShot(victorySound);
        }
        
        Debug.Log("[GameManager] VICTORY! All planets visited!");
    }
    
    void GameOver()
    {
        currentState = GameState.GameOver;
        gameOverPanel.SetActive(true);
        resetButton2.SetActive(true);
        
        Debug.Log("[GameManager] GAME OVER! No lives remaining!");
    }
    
    void UpdateUI()
    {
        livesText.text = "Lives: " + currentLives + "/" + maxLives;
    }

    public void ResetGameButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    IEnumerator FlashImage(GameObject targetImage)
    {
        targetImage.SetActive(true);
        yield return new WaitForSeconds(flashImageDuration);
        targetImage.SetActive(false);
    }
}