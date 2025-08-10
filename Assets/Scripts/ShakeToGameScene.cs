using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class ShakeToGameScene : MonoBehaviour
{
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private float cooldownTime = 0.5f;
    
    [Header("Scene Transition")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    [Header("Audio")]
    [SerializeField] private AudioClip shakeDetectedSound;
    [SerializeField] private float volume = 1.0f;
    
    [Header("Objects to Move")]
    [SerializeField] private GameObject firstObject;
    [SerializeField] private GameObject secondObject;
    [SerializeField] private GameObject thirdObject;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementDistance = 10f;
    [SerializeField] private float movementDuration = 0.5f;
    [SerializeField] private float delayBetweenObjects = 0.1f; // Reduced delay between object starts
    
    private Accelerometer accelerometer;
    private Vector3 previousAcceleration;
    private Vector3 currentAcceleration;
    private float lastShakeTime;
    private AudioSource audioSource;

    void Start()
    {
        // Initialize audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        // Initialize accelerometer
        accelerometer = Accelerometer.current;
        if (accelerometer != null)
        {
            InputSystem.EnableDevice(accelerometer);
            currentAcceleration = previousAcceleration = accelerometer.acceleration.ReadValue();
            Debug.Log("Accelerometer initialized for shake detection");
        }
        else
        {
            Debug.LogWarning("Accelerometer not available, using Space key for testing");
        }
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // For testing in Unity Editor
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            ChangeToGameScene();
        }
#else
        // For mobile devices
        CheckForShake();
#endif
    }

    private void CheckForShake()
    {
        if (accelerometer == null || Time.time - lastShakeTime <= cooldownTime)
            return;
        
        previousAcceleration = currentAcceleration;
        currentAcceleration = accelerometer.acceleration.ReadValue();
        
        float accelerationChange = (currentAcceleration - previousAcceleration).magnitude;
        
        if (accelerationChange > shakeThreshold)
        {
            ChangeToGameScene();
        }
    }

    private void ChangeToGameScene()
    {
        lastShakeTime = Time.time;
        Debug.Log("Shake detected! Moving objects to the left");
        
        // Play sound effect if assigned
        if (shakeDetectedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shakeDetectedSound, volume);
        }
        
        StartCoroutine(MoveObjectsSequentially());
    }
    
    private IEnumerator MoveObjectsSequentially()
    {
        // List of objects to move in sequence
        List<GameObject> objectsToMove = new List<GameObject>
        {
            firstObject,
            secondObject,
            thirdObject
        };
        
        // Start moving each object with a small delay, don't wait for completion
        for (int i = 0; i < objectsToMove.Count; i++)
        {
            GameObject obj = objectsToMove[i];
            if (obj != null)
            {
                // Start movement without yielding (don't wait for completion)
                StartCoroutine(MoveObjectLeft(obj));
                
                // Only wait for a short delay before starting the next object
                if (i < objectsToMove.Count - 1) // Don't wait after the last object
                {
                    yield return new WaitForSeconds(delayBetweenObjects);
                }
            }
        }
        
        // Wait for all animations to finish (using the duration)
        // Adding a small buffer to ensure all animations complete
        yield return new WaitForSeconds(movementDuration + 0.1f);
        
        // After all objects have moved, load the next scene
        Debug.Log($"All animations complete. Loading scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }
    
    private IEnumerator MoveObjectLeft(GameObject obj)
    {
        Vector3 startPosition = obj.transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x - movementDistance, startPosition.y, startPosition.z);
        float elapsedTime = 0f;
        
        while (elapsedTime < movementDuration)
        {
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / movementDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure the object reaches its final position
        obj.transform.position = targetPosition;
    }
}
