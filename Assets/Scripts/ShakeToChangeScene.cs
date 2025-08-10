using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class ShakeToChangeScene : MonoBehaviour
{
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private float cooldownTime = 0.5f;
    
    [Header("Scene Transition")]
    [SerializeField] private string gameSceneName = "Game";
    
    [Header("Objects to Move")]
    [SerializeField] private GameObject firstObject;
    [SerializeField] private GameObject secondObject;
    [SerializeField] private GameObject thirdObject;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementDistance = 10f;
    [SerializeField] private float movementDuration = 0.5f;
    [SerializeField] private float delayBetweenObjects = 0.2f;
    
    private Accelerometer accelerometer;
    private Vector3 previousAcceleration;
    private Vector3 currentAcceleration;
    private float lastShakeTime;

    void Start()
    {
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
        
        // Move each object in sequence
        foreach (GameObject obj in objectsToMove)
        {
            if (obj != null)
            {
                yield return StartCoroutine(MoveObjectLeft(obj));
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }
        
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
