using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

public class PlanetCreator : MonoBehaviour
{
    [Header("Vuforia References")]
    public PlaneFinderBehaviour planeFinder;
    
    [Header("Planet Prefabs")]
    public GameObject[] groundPlanetPrefabs;
    public GameObject[] midAirPlanetPrefabs;
    
    [Header("Mid-Air 'Hold' Settings")]
    public GameObject planetPreview;
    public float minHoldTime = 0.1f;
    public float planetGrowthRate = 0.1f;
    public float maxPlanetScale = 0.5f;
    public float markerBaseScale = 0.5f;
    public float markerDistance = 1.0f;

    private Camera arCamera;
    private bool isCreatingPlanet = false;
    private float holdStartTime;
    private Vector3 planetPosition;
    
    private GameObject midAirMarkerInstance;
    
    private float finalMidAirScale;
    private bool hasPlacedGroundThisFrame = false; 

    void Start()
    {
        arCamera = Camera.main;
        if (planeFinder != null)
        {
            planeFinder.OnInteractiveHitTest.AddListener(OnGroundPlaneTapped);
        }

        if (planetPreview != null)
        {
            midAirMarkerInstance = Instantiate(planetPreview);
            midAirMarkerInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        GameManager.GameState state = GameManager.Instance.currentState;
        
        hasPlacedGroundThisFrame = false;

        if (planeFinder != null)
        {
            planeFinder.enabled = (state == GameManager.GameState.PlacingStart ||
                                   state == GameManager.GameState.PlacingFinish);
        }

        if (state == GameManager.GameState.CreatingPlanets)
        {
            if (midAirMarkerInstance != null)
            {
                midAirMarkerInstance.SetActive(true);

                HandleMidAirPlanetCreation();

                if (!isCreatingPlanet)
                {
                    midAirMarkerInstance.transform.position = arCamera.transform.position + arCamera.transform.forward * markerDistance;
                    midAirMarkerInstance.transform.localScale = Vector3.one * markerBaseScale;
                }
            }
        }
        else
        {
            if (midAirMarkerInstance != null)
            {
                midAirMarkerInstance.SetActive(false);
            }
        }
    }

    void OnGroundPlaneTapped(HitTestResult result)
    {
        if (result == null || groundPlanetPrefabs == null) return;
        if (hasPlacedGroundThisFrame) return; 
        
        GameManager.GameState state = GameManager.Instance.currentState;
        
        if (state != GameManager.GameState.PlacingStart && 
            state != GameManager.GameState.PlacingFinish)
        {
            return;
        }

        hasPlacedGroundThisFrame = true;
        Vector3 position = result.Position;
        
        float planetScale = 0.3f;
        
        float planetRadius = planetScale * 0.5f;

        GameObject groundPlanetPrefab = state == GameManager.GameState.PlacingStart ? groundPlanetPrefabs[0] : groundPlanetPrefabs[1];
        GameObject planet = Instantiate(groundPlanetPrefab, position, Quaternion.identity);
        planet.transform.localScale = Vector3.one * planetScale;
        
        AddPlanetComponent(planet);
        PlayPlanetSound();

        Debug.Log("[PlanetCreator] Ground planet created. State: " + state);

        if (state == GameManager.GameState.PlacingStart)
        {
            GameManager.Instance.astronaut.transform.position = position + Vector3.up * planetRadius;
            
            GameManager.Instance.currentState = GameManager.GameState.CreatingPlanets;
            Debug.Log("[PlanetCreator] START placed. Now creating mid-air planets.");
        }
        else if (state == GameManager.GameState.PlacingFinish)
        {
            GameManager.Instance.currentState = GameManager.GameState.Playing;
            GameManager.Instance.astronaut.SetActive(true);
            GameManager.Instance.MarkPlanetVisited(0);
            Debug.Log("[PlanetCreator] FINISH placed. Game starting!");
        }
    }

    void HandleMidAirPlanetCreation()
    {
        
        if (Input.touchCount >= 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began && !isCreatingPlanet)
            {
                StartCreatingPlanet();
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isCreatingPlanet)
            {
                UpdatePlanetPreview();
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isCreatingPlanet)
            {
                FinishCreatingPlanet();
            }
        }
    }

    void StartCreatingPlanet()
    {
        isCreatingPlanet = true;
        holdStartTime = Time.time;
        
        planetPosition = midAirMarkerInstance.transform.position; 
        
        finalMidAirScale = markerBaseScale;
        midAirMarkerInstance.transform.localScale = Vector3.one * finalMidAirScale;
        
        Debug.Log("[PlanetCreator] Planet creation started at: " + planetPosition);
    }

    void UpdatePlanetPreview()
    {
        if (midAirMarkerInstance != null)
        {
            float holdTime = Time.time - holdStartTime;
            
            float scale = Mathf.Min(markerBaseScale + holdTime * planetGrowthRate, maxPlanetScale); 
            
            midAirMarkerInstance.transform.localScale = Vector3.one * scale;
            
            finalMidAirScale = scale;
        }
    }

    void FinishCreatingPlanet()
    {
        float holdTime = Time.time - holdStartTime;
        Debug.Log("[PlanetCreator] Hold time: " + holdTime + " seconds");
        
        if (holdTime >= minHoldTime && midAirPlanetPrefabs != null)
        {
            GameObject planet = Instantiate(midAirPlanetPrefabs[GameManager.Instance.planets.Count - 1], planetPosition, Quaternion.identity);
            planet.transform.localScale = Vector3.one * finalMidAirScale;

            AddPlanetComponent(planet);
            PlayPlanetSound();
            
            Debug.Log("[PlanetCreator] Mid-air planet created! Total planets: " + GameManager.Instance.planets.Count);
            
            int midAirCount = GameManager.Instance.planets.Count - 1;
            if (midAirCount >= GameManager.Instance.midAirPlanetsNeeded)
            {
                GameManager.Instance.currentState = GameManager.GameState.PlacingFinish;
                Debug.Log("All mid-air planets created. Place FINISH now.");
            }
        }
        else
        {
            Debug.Log("[PlanetCreator] Hold time too short or prefab missing");
        }
        
        isCreatingPlanet = false;
        
    }

    void AddPlanetComponent(GameObject planet)
    {
        Planet planetScript = planet.AddComponent<Planet>();
        planetScript.planetIndex = GameManager.Instance.planets.Count;
        GameManager.Instance.planets.Add(planet);
    }

    void PlayPlanetSound()
    {
        if (GameManager.Instance.audioSource && GameManager.Instance.planetSound)
        {
            GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.planetSound);
        }
    }

    void OnDestroy()
    {
        if (planeFinder != null)
        {
            planeFinder.OnInteractiveHitTest.RemoveListener(OnGroundPlaneTapped);
        }
        
        if (midAirMarkerInstance != null)
        {
            Destroy(midAirMarkerInstance);
        }
    }
}