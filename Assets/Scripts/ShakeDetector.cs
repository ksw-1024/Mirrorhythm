using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShakeDetector : MonoBehaviour
{
    public static int ShakeCount;
    public Text shakeCountText;
    
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private float cooldownTime = 0.3f;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private AudioClip shakeSound;
    [SerializeField] private bool allowSoundOverlap = false;

    private Vector3 currentAcceleration;
    private Vector3 previousAcceleration;
    private float lastShakeTime;
    private float lastSoundPlayTime;
    private Accelerometer accelerometer;
    private AudioSource audioSource;

    void Start()
    {
        SetupComponents();
        UpdateShakeCountText();
        LogDebug("ShakeDetector initialized");
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            OnShakeDetected();
#else
        CheckForShake();
#endif
    }

    private void SetupComponents()
    {
        SetupAccelerometer();
        SetupAudio();
    }

    private void SetupAccelerometer()
    {
        accelerometer = Accelerometer.current;
        
        if (accelerometer != null)
        {
            InputSystem.EnableDevice(accelerometer);
            currentAcceleration = previousAcceleration = accelerometer.acceleration.ReadValue();
            LogDebug("Accelerometer available");
        }
        else
        {
            LogDebug("Accelerometer not available", true);
        }
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        
        if (shakeSound != null)
        {
            audioSource.clip = shakeSound;
            audioSource.playOnAwake = false;
        }
        else
        {
            LogDebug("Shake sound not assigned", true);
        }
    }

    private void CheckForShake()
    {
        if (accelerometer == null || Time.time - lastShakeTime <= cooldownTime) 
            return;
        
        previousAcceleration = currentAcceleration;
        currentAcceleration = accelerometer.acceleration.ReadValue();
        
        float accelerationChange = (currentAcceleration - previousAcceleration).magnitude;
        
        if (accelerationChange > shakeThreshold)
            OnShakeDetected();
    }

    private void OnShakeDetected()
    {
        ShakeCount++;
        lastShakeTime = Time.time;
        UpdateShakeCountText();
        PlayShakeSound();
        LogDebug($"Shake detected! Total: {ShakeCount}");
    }

    private void PlayShakeSound()
    {
        if (audioSource == null || shakeSound == null) 
            return;

        float soundCooldown = allowSoundOverlap ? 0.1f : shakeSound.length * 0.8f;
        bool canPlaySound = Time.time - lastSoundPlayTime > soundCooldown;
        
        if (canPlaySound && (allowSoundOverlap || !audioSource.isPlaying))
        {
            audioSource.PlayOneShot(shakeSound);
            lastSoundPlayTime = Time.time;
        }
    }

    private void UpdateShakeCountText()
    {
        if (shakeCountText != null)
            shakeCountText.text = "シェイク回数: " + ShakeCount;
    }

    private void LogDebug(string message, bool isWarning = false)
    {
        if (!debugMode) return;
        
        if (isWarning)
            Debug.LogWarning(message);
        else
            Debug.Log(message);
    }
}