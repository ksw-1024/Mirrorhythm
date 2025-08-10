using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionEffect : MonoBehaviour
{
    // アニメーションまたはマニュアルフェードの選択
    [SerializeField] private bool useAnimator = false;
    [SerializeField] private Animator animator;
    [SerializeField] private Image fadePanel;
    
    // マニュアルフェードの設定
    [SerializeField] private float fadeDuration = 1.0f;
    private float currentAlpha = 0f;
    private Coroutine fadeCoroutine;
    
    // アニメーター用トリガー名
    private static readonly string fadeInTrigger = "FadeIn";
    private static readonly string fadeOutTrigger = "FadeOut";
    
    void Awake()
    {
        // コンポーネントの取得と初期化
        if (animator == null && useAnimator)
        {
            animator = GetComponent<Animator>();
        }
        
        if (fadePanel == null)
        {
            fadePanel = GetComponent<Image>();
        }
        
        // 最初は完全に透明にする
        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 0f;
            fadePanel.color = color;
            currentAlpha = 0f;
        }
        
        // 最初は非アクティブ
        gameObject.SetActive(false);
    }
    
    // フェードインアニメーション開始
    public void StartFadeIn()
    {
        // 既に実行中のフェードをキャンセル
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        gameObject.SetActive(true);
        
        if (useAnimator && animator != null)
        {
            // Animatorを使用
            animator.SetTrigger(fadeInTrigger);
        }
        else if (fadePanel != null)
        {
            // マニュアルフェードを使用
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }
    
    // フェードアウトアニメーション開始
    public void StartFadeOut()
    {
        // 既に実行中のフェードをキャンセル
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        if (useAnimator && animator != null)
        {
            // Animatorを使用
            animator.SetTrigger(fadeOutTrigger);
        }
        else if (fadePanel != null)
        {
            // マニュアルフェードを使用
            fadeCoroutine = StartCoroutine(FadeOut());
        }
    }
    
    // マニュアルフェードイン
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color color = fadePanel.color;
        color.a = 0f;
        fadePanel.color = color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            currentAlpha = alpha;
            yield return null;
        }
        
        // 最終的に確実に1に設定
        color.a = 1f;
        fadePanel.color = color;
        currentAlpha = 1f;
    }
    
    // マニュアルフェードアウト
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Color color = fadePanel.color;
        color.a = 1f;
        fadePanel.color = color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            currentAlpha = alpha;
            yield return null;
        }
        
        // 最終的に確実に0に設定
        color.a = 0f;
        fadePanel.color = color;
        currentAlpha = 0f;
        OnFadeOutComplete();
    }
    
    // アニメーション完了時に呼び出されるイベント
    public void OnFadeOutComplete()
    {
        gameObject.SetActive(false);
    }
    
    // 現在のフェード状態を取得
    public float GetCurrentAlpha()
    {
        return currentAlpha;
    }
}
