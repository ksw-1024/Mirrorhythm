using UnityEngine;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private float fadeOutDelay = 0.5f;
    
    void Start()
    {
        // シーン遷移エフェクトを探す（DontDestroyOnLoadで保持されているはず）
        SceneTransitionEffect transitionEffect = FindObjectOfType<SceneTransitionEffect>();
        
        if (transitionEffect != null)
        {
            // 少し待ってからフェードアウト
            StartCoroutine(DelayedFadeOut(transitionEffect));
        }
    }
    
    private IEnumerator DelayedFadeOut(SceneTransitionEffect effect)
    {
        // 指定した時間だけ待機
        yield return new WaitForSeconds(fadeOutDelay);
        
        // フェードアウト開始
        effect.StartFadeOut();
        Debug.Log("新しいシーンでフェードアウト開始");
    }
}
