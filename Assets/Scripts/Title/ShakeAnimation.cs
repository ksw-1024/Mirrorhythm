using UnityEngine;
using System.Collections;

public class ShakeAnimation : MonoBehaviour
{
    [Header("シェイク設定")]
    [SerializeField] private float shakeDuration = 0.5f;     // シェイクの継続時間
    [SerializeField] private float shakeAmount = 0.3f;       // シェイクの最大強さ
    [SerializeField] private float decreaseFactor = 1.0f;    // 減衰速度
    [SerializeField] private float shakeFrequency = 70.0f;   // 振動频度
    [SerializeField] private float loopInterval = 3.0f;      // シェイクの繰り返し間隔（秒）

    private Vector3 originalPosition;
    private float currentAmount;
    private float currentDuration;
    private bool isShaking = false;

    private void Start()
    {
        // 開始位置を記録
        originalPosition = transform.localPosition;
        // 最初のシェイクを開始
        StartCoroutine(ShakeLoop());
    }

    private IEnumerator ShakeLoop()
    {
        while (true)
        {
            // シェイク開始
            StartShake();
            // 次のシェイク開始まで待機
            yield return new WaitForSeconds(loopInterval);
        }
    }

    private void StartShake()
    {
        // 現在位置を記録
        originalPosition = transform.localPosition;
        currentAmount = shakeAmount;
        currentDuration = shakeDuration;
        isShaking = true;
    }

    private void Update()
    {
        if (!isShaking || currentDuration <= 0)
        {
            // シェイクが終了した場合は元の位置に戻す
            if (isShaking)
            {
                transform.localPosition = originalPosition;
                isShaking = false;
            }
            return;
        }

        // 減衰していく振幅
        float decreasedAmount = currentAmount * (currentDuration / shakeDuration);
        
        // ノイズを加えたランダムな動きを作成
        float noiseX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) * 2 - 1) * decreasedAmount;
        
        // 新しい位置を適用
        Vector3 newPosition = originalPosition;
        newPosition.x += noiseX;
        transform.localPosition = newPosition;
        
        // 時間経過による減衰
        currentDuration -= Time.deltaTime * decreaseFactor;
    }
}
