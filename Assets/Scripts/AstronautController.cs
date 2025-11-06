using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class AstronautController : MonoBehaviour
{
    [Header("Jump Settings")]
    public float minJumpDistance = 0.05f;
    public float maxJumpDistance = 4.0f;
    public float jumpHeight = 1.0f;
    public float maxHoldTime = 1.5f;
    public float jumpSpeed = 2.0f; 
    public float landingRadius = 0.5f; 
    
    public float cycleSpeed = 0.2f;
    
    [Header("Trajectory Line")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 20;
    public float dotsPerUnit = 1.0f; 
    public float proximityRange = 2.0f;
    
    private Camera arCamera;
    private bool isHolding = false;
    private bool isJumping = false;
    private float holdStartTime;
    private int currentPlanetIndex = 0;
    private float loopTimer = 0f;
    
    void Start()
    {
        arCamera = Camera.main;
        
        // Setup Trajectory Line
        if (trajectoryLine == null)
        {
            GameObject lineObj = new GameObject("Trajectory");
            lineObj.transform.SetParent(transform);
            trajectoryLine = lineObj.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.02f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.positionCount = trajectoryPoints;
        }

        if (trajectoryLine.material == null || trajectoryLine.material.shader.name != "Legacy Shaders/Particles/Alpha Blended")
        {
            trajectoryLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        }
        trajectoryLine.startColor = Color.lightBlue;
        trajectoryLine.endColor = Color.cyan;
        trajectoryLine.enabled = false;
    }

    void Update()
    {
        RotateToCamera();
        if (GameManager.Instance.currentState != GameManager.GameState.Playing) return;
        if (isJumping) return;

        if (IsTouchingUI()) return;

        bool inputDown = false, inputHeld = false, inputUp = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputDown = touch.phase == TouchPhase.Began;
            inputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            inputUp = touch.phase == TouchPhase.Ended;
        }

        if (inputDown && !isHolding)
        {
            if (IsNearAstronaut())
            {
                GameManager.Instance.instructionText.text = "TOUCH and HOLD near astronaut to jump!\nVisit ALL planets to win!";
                isHolding = true;
                holdStartTime = Time.time;
                loopTimer = 0f;
                trajectoryLine.enabled = true;
                Debug.Log("[AstronautController] Started holding");
            }
            else
            {
                GameManager.Instance.instructionText.text = "MOVE closer to the astronaut";
            }
        }

        if (inputHeld && isHolding)
        {
            UpdateTrajectory();
        }

        if (inputUp && isHolding)
        {
            Jump();
        }
    }

    bool IsNearAstronaut()
    {
        float distance = Vector3.Distance(arCamera.transform.position, transform.position);        
        bool isNear = distance <= proximityRange;
        return isNear;
    }
    
    void RotateToCamera()
    {
        
        Vector3 cameraForward = arCamera.transform.forward;
        cameraForward.y = 0;
        
        if (cameraForward.sqrMagnitude > 0.01f)
        {
            cameraForward.Normalize();
            transform.rotation = Quaternion.LookRotation(cameraForward);
        }
    }
    
    bool IsTouchingUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }
    
    void UpdateTrajectory()
    {
        float holdTime = Time.time - holdStartTime;
        float distance;

        loopTimer += Time.deltaTime * cycleSpeed;
        float pingPong = Mathf.PingPong(loopTimer, 1f);
        distance = Mathf.Lerp(minJumpDistance, maxJumpDistance, pingPong);
        
        Vector3 forward = arCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 targetPos = transform.position + forward * distance;
        
        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward);
        }

        float totalLineLength = 0;
        Vector3 previousPoint = transform.position;
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i / (float)(trajectoryPoints - 1);
            Vector3 point = Vector3.Lerp(transform.position, targetPos, t);
            point.y += jumpHeight * Mathf.Sin(t * Mathf.PI); 
            
            trajectoryLine.SetPosition(i, point);

            totalLineLength += Vector3.Distance(previousPoint, point);
            previousPoint = point;
        }

        if (trajectoryLine.material != null && dotsPerUnit > 0)
        {
            trajectoryLine.material.mainTextureScale = new Vector2(totalLineLength * dotsPerUnit, 1);
        }
    }
    
    void Jump()
    {
        isHolding = false;
        trajectoryLine.enabled = false;
        
        float holdTime = Time.time - holdStartTime;
        float distance;

        float pingPong = Mathf.PingPong(loopTimer, 1f);
        distance = Mathf.Lerp(minJumpDistance, maxJumpDistance, pingPong);
        
        Vector3 forward = arCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 targetPos = transform.position + forward * distance;
        
        Debug.Log($"[AstronautController] Jumping! Distance: {distance}, Target: {targetPos}");
        
        StartCoroutine(JumpCoroutine(targetPos, distance)); 
    }
    
    IEnumerator JumpCoroutine(Vector3 target, float distance) 
    {
        isJumping = true;
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.5f, distance / jumpSpeed); 
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.y = start.y + jumpHeight * Mathf.Sin(t * Mathf.PI); 
            
            transform.position = pos;
            yield return null;
        }
        
        transform.position = target;
        isJumping = false;
        
        CheckLanding();
    }
    
    void CheckLanding()
    {
        bool landed = false;
        float minDistance = float.MaxValue;
        int closestPlanet = -1;
        
        for (int i = 0; i < GameManager.Instance.planets.Count; i++)
        {
            GameObject planet = GameManager.Instance.planets[i];
            Vector3 planetPos = planet.transform.position;
            Vector3 astronautFlat = new Vector3(transform.position.x, planetPos.y, transform.position.z);
            float dist = Vector3.Distance(astronautFlat, planetPos);
            
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPlanet = i;
            }
        }
        
        if (minDistance < landingRadius && closestPlanet >= 0)
        {
            landed = true;
            GameObject planet = GameManager.Instance.planets[closestPlanet];
            
            float planetRadius = planet.transform.localScale.y * 0.5f;
            transform.position = planet.transform.position + Vector3.up * planetRadius;
            
            GameManager.Instance.MarkPlanetVisited(closestPlanet);
            currentPlanetIndex = closestPlanet;
            Debug.Log($"[AstronautController] LANDED on planet {closestPlanet}!");
        }
        
        if (!landed)
        {
            Debug.Log($"[AstronautController] MISSED! Closest was {minDistance}m away");
            GameManager.Instance.LoseLife();
            
            if (currentPlanetIndex < GameManager.Instance.planets.Count)
            {
                GameObject lastPlanet = GameManager.Instance.planets[currentPlanetIndex];
                float planetRadius = lastPlanet.transform.localScale.y * 0.5f;
                transform.position = lastPlanet.transform.position + Vector3.up * planetRadius;
                
                Debug.Log($"[AstronautController] Respawned at planet {currentPlanetIndex}");
            }
        }
    }
}