using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShakeDetector : MonoBehaviour
{
    public static int ShakeCount;
    public Text shakeCountText;
    
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private float cooldownTime = 0.3f; // 連続シェイク対応のため短縮
    [SerializeField] private bool debugMode = true;
    [SerializeField] private AudioClip shakeSound;
    [SerializeField] private bool allowSoundOverlap = false; // サウンド重複許可フラグ

    private Vector3 currentAcceleration;
    private Vector3 previousAcceleration;
    private float lastShakeTime;
    private float lastSoundPlayTime; // 音声再生時間を別途管理
    
    private Accelerometer accelerometer;
    private AudioSource audioSource;

    void Start()
    {
        InitializeAccelerometer();
        InitializeAudio();
        UpdateShakeCountText();
        
        if (debugMode)
            Debug.Log("ShakeDetector initialized");
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        DetectShake();
#endif
    }

    private void InitializeAccelerometer()
    {
        accelerometer = Accelerometer.current;
        
        if (accelerometer != null)
        {
            InputSystem.EnableDevice(accelerometer);
            currentAcceleration = accelerometer.acceleration.ReadValue();
            previousAcceleration = currentAcceleration;
            
            if (debugMode)
                Debug.Log("Accelerometer available");
        }
        else if (debugMode)
        {
            Debug.LogWarning("Accelerometer not available");
        }
    }

    private void InitializeAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (shakeSound != null)
        {
            audioSource.clip = shakeSound;
            audioSource.playOnAwake = false;
        }
        else if (debugMode)
        {
            Debug.LogWarning("Shake sound not assigned");
        }
    }

#if UNITY_EDITOR
    private void HandleEditorInput()
    {
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            RegisterShake();
            if (debugMode) 
                Debug.Log("Test shake executed");
        }
    }
#endif

    private void DetectShake()
    {
        if (accelerometer == null || !CanDetectShake()) 
            return;
        
        UpdateAcceleration();
        
        float accelerationChange = (currentAcceleration - previousAcceleration).magnitude;
        
        if (accelerationChange > shakeThreshold)
        {
            RegisterShake();
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"Acceleration change: {accelerationChange}");
        }
    }

    private bool CanDetectShake()
    {
        return Time.time - lastShakeTime > cooldownTime;
    }

    private void UpdateAcceleration()
    {
        previousAcceleration = currentAcceleration;
        currentAcceleration = accelerometer.acceleration.ReadValue();
    }

    private void RegisterShake()
    {
        if (debugMode)
            Debug.Log($"RegisterShake called - Current time: {Time.time}");
            
        ShakeCount++;
        lastShakeTime = Time.time;
        UpdateShakeCountText();
        PlayShakeSound();
        
        if (debugMode)
            Debug.Log($"Shake registered! Total: {ShakeCount}");
    }

    private void PlayShakeSound()
    {
        if (audioSource == null || shakeSound == null) 
            return;

        float soundCooldown = allowSoundOverlap ? 0.1f : (shakeSound.length * 0.8f); // 音声の80%経過後に次の再生を許可
        
        if (Time.time - lastSoundPlayTime > soundCooldown)
        {
            if (allowSoundOverlap || !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(shakeSound); // PlayOneShotで重複再生対応
                lastSoundPlayTime = Time.time;
                
                if (debugMode)
                    Debug.Log($"Shake sound played at {Time.time}");
            }
        }
        else if (debugMode)
        {
            Debug.Log($"Sound cooldown active, remaining: {soundCooldown - (Time.time - lastSoundPlayTime):F2}s");
        }
    }

    private void UpdateShakeCountText()
    {
        if (shakeCountText != null)
        {
            shakeCountText.text = "シェイク回数: " + ShakeCount;
        }
    }
}
