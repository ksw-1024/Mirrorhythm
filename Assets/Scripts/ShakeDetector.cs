using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class ShakeDetector : MonoBehaviour
{
    public static int ShakeCount;
    public Text shakeCountText;
    
    public event Action OnShakeDetected;
    
    [SerializeField] private float shakeThreshold = 1.5f;
    [SerializeField] private float cooldownTime = 0.3f;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private AudioClip shakeSound;
    [SerializeField] private bool allowSoundOverlap = false;
    [SerializeField] private float vibrationDuration = 0.5f;
    [SerializeField] private float vibrationIntensity = 5f;

    private Vector3 currentAcceleration;
    private Vector3 previousAcceleration;
    private float lastShakeTime;
    private float lastSoundPlayTime;
    private Accelerometer accelerometer;
    private AudioSource audioSource;
    private bool isInitialized = false;
    
    private Vector3 originalTextPosition;
    private bool isVibrating = false;
    private float vibrationStartTime;

    void Start()
    {
        SetupComponents();
        isInitialized = true;
        LogDebug("ShakeDetector initialized");
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            HandleShakeDetected();
#else
        CheckForShake();
#endif
        
        UpdateTextVibration();
    }

    private void SetupComponents()
    {
        SetupAccelerometer();
        SetupAudio();
        SetupTextVibration();
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
        try
        {
            // あらかじめAudioSourceが存在するか確認
            audioSource = GetComponent<AudioSource>();
            
            // AudioSourceが存在しない場合は新しく追加
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("ShakeDetectorにAudioSourceを新規追加しました");
            }
            
            // AudioSourceの設定
            if (audioSource != null) 
            {
                audioSource.playOnAwake = false;
                audioSource.Stop();
                
                if (shakeSound != null)
                {
                    audioSource.clip = shakeSound;
                }
                else
                {
                    LogDebug("Shake sound not assigned", true);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AudioSourceの初期化に失敗しました: {ex.Message}");
        }
    }

    private void SetupTextVibration()
    {
        if (shakeCountText != null)
        {
            originalTextPosition = shakeCountText.rectTransform.localPosition;
        }
    }

    private void CheckForShake()
    {
        if (accelerometer == null || Time.time - lastShakeTime <= cooldownTime) 
            return;
        
        previousAcceleration = currentAcceleration;
        currentAcceleration = accelerometer.acceleration.ReadValue();
        
        // 前回と今回の加速度の絶対値(magnitude)を比較
        float currentMagnitude = currentAcceleration.magnitude;
        float previousMagnitude = previousAcceleration.magnitude;
        
        // 加速度が増加した場合のみ（加速のみ）検出
        float accelerationDelta = currentMagnitude - previousMagnitude;
        
        if (accelerationDelta > shakeThreshold)
            HandleShakeDetected();
    }

    private void HandleShakeDetected()
    {
        lastShakeTime = Time.time;
        
        if (isInitialized)
        {
            bool soundPlayed = PlayShakeSound();
            // サウンドの再生結果に関わらず、常にシェイク検知を呼び出す
            ShakeCount++;
            StartTextVibration();
            LogDebug($"Shake detected! Total: {ShakeCount}");
            
            // イベントを常に呼び出す
            OnShakeDetected?.Invoke();
        }
    }

    private bool PlayShakeSound()
    {
        if (audioSource == null || shakeSound == null) 
            return false;

        if (!audioSource.isPlaying)
        {
            audioSource.clip = shakeSound;
            audioSource.Play();
            lastSoundPlayTime = Time.time;
            return true;
        }
        
        return false;
    }

    private void StartTextVibration()
    {
        if (shakeCountText != null)
        {
            isVibrating = true;
            vibrationStartTime = Time.time;
        }
    }

    private void UpdateTextVibration()
    {
        if (!isVibrating || shakeCountText == null) return;

        float elapsedTime = Time.time - vibrationStartTime;
        
        if (elapsedTime >= vibrationDuration)
        {
            isVibrating = false;
            shakeCountText.rectTransform.localPosition = originalTextPosition;
        }
        else
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-vibrationIntensity, vibrationIntensity),
                UnityEngine.Random.Range(-vibrationIntensity, vibrationIntensity),
                0
            );
            shakeCountText.rectTransform.localPosition = originalTextPosition + randomOffset;
        }
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