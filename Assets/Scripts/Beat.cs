using UnityEngine;
using System.Collections;

[System.Serializable]
public class RhythmPattern
{
    public string name;
    public int[] pattern;
}

[System.Serializable]
public class RhythmData
{
    public RhythmPattern[] patterns;
}

public enum PlayState
{
    Playing,   // パターン再生中
    UserInput  // ユーザーの入力待ち
}

public class Beat : MonoBehaviour
{
    [SerializeField] private AudioClip maracasSound;
    [SerializeField] private AudioClip tambourineSound;
    [SerializeField] private TextAsset patternFile;
    [SerializeField] private float bpm = 120f;
    
    // メトロノーム関連
    [SerializeField] private AudioClip metronomeDownBeatSound; // 小節の頭の音
    [SerializeField] private AudioClip metronomeSound; // 通常の拍の音
    [SerializeField] private TextAsset metronomePatternFile; // メトロノームのパターンファイル
    [SerializeField] private bool playMetronome = true; // メトロノームを再生するかどうか
    
    // シェイク判定関連
    [SerializeField] private float perfectAccuracy = 0.1f; // 完璧な入力と判定する許容経過（秒）
    [SerializeField] private float goodAccuracy = 0.2f; // 良い入力と判定する許容経過（秒）
    [SerializeField] private int userInputMeasures = 1; // ユーザー入力時の小節数
    [SerializeField] private float extraShakePenalty = 0.5f; // 余分なシェイクのペナルティ
    
    // BGM関連
    [SerializeField] private string bgmTag = "BGM"; // BGMオブジェクトに付けられているタグ名
    [SerializeField] private float fadeOutDuration = 2.0f; // BGMのフェードアウト時間（秒）
    private AudioSource[] bgmAudioSources; // 検索したBGM用AudioSource
    
    // シェイクパターン判定結果の音声通知用
    [SerializeField] private AudioClip patternSuccessSound; // パターン成功時の音声
    [SerializeField] private AudioClip patternFailureSound; // パターン失敗時の音声
    private AudioSource patternResultAudioSource; // 判定結果用AudioSource
    
    // シェイクパターン種類の音声通知用
    [SerializeField] private AudioClip maracasPatternSound; // マラカスパターンを振る時の通知音声
    [SerializeField] private AudioClip tambourinePatternSound; // タンバリンパターンを振る時の通知音声
    private AudioSource patternTypeAudioSource; // パターン種類通知用AudioSource
    
    private AudioSource maracasAudioSource;
    private AudioSource tambourineAudioSource;
    private AudioSource metronomeAudioSource;
    private RhythmData rhythmData;
    private RhythmData metronomeRhythmData;
    private bool isPlaying = false;
    private int currentStep = 0;
    private int maracasPatternIndex = 0;
    private int tambourinePatternIndex = 0;
    private int metronomePatternIndex = 0;
    private double startTime;
    private double stepDuration;
    private PlayState currentState = PlayState.Playing;
    
    // シェイク判定用変数
    private bool isDetectingMaracas; // trueの場合はマラカスパターンを判定、falseの場合はタンバリンパターンを判定
    // ユーザー入力の実行中にシェイクした時間
    private System.Collections.Generic.List<double> userShakeTimes = new System.Collections.Generic.List<double>();
    // 期待されるシェイクタイミング
    private System.Collections.Generic.List<double> expectedShakeTimes = new System.Collections.Generic.List<double>();
    private double userInputStartTime; // ユーザー入力開始時間
    private int perfectShakes = 0; // 完璧なタイミングで振れた回数
    private int goodShakes = 0;    // 良いタイミングで振れた回数
    private int missedShakes = 0;  // 見逃したシェイクの数
    private int extraShakes = 0;   // 余分なシェイクの数
    private int totalExpectedShakes = 0; // 期待されるシェイクの総数
    
    void Start()
    {
        // マラカス用のAudioSourceを作成
        maracasAudioSource = gameObject.AddComponent<AudioSource>();
        
        // タンバリン用のAudioSourceを作成
        tambourineAudioSource = gameObject.AddComponent<AudioSource>();
        
        // メトロノーム用のAudioSourceを作成
        metronomeAudioSource = gameObject.AddComponent<AudioSource>();
        
        // パターン判定結果通知用のAudioSourceを作成
        patternResultAudioSource = gameObject.AddComponent<AudioSource>();
        
        // パターン種類通知用のAudioSourceを作成
        patternTypeAudioSource = gameObject.AddComponent<AudioSource>();
        
        LoadRhythmPatterns();
        LoadMetronomePattern();
        
        bool canStart = true;
        
        if (rhythmData == null || rhythmData.patterns == null || rhythmData.patterns.Length == 0)
        {
            Debug.LogError("リズムパターンが読み込めませんでした");
            canStart = false;
        }
        
        if (playMetronome && (metronomeRhythmData == null || metronomeRhythmData.patterns == null || metronomeRhythmData.patterns.Length == 0))
        {
            Debug.LogError("メトロノームパターンが読み込めませんでした");
            canStart = false;
        }
        
        if (canStart)
        {
            // ShakeDetectorからのコールバックを登録
            ShakeDetector shakeDetector = FindObjectOfType<ShakeDetector>();
            if (shakeDetector != null)
            {
                shakeDetector.OnShakeDetected += OnUserShakeDetected;
                Debug.Log("シェイク検知を登録しました");
            }
            else
            {
                Debug.LogError("ShakeDetectorがScene内に見つかりません");
            }
            
            SelectRandomPatterns();
            StartSynchronizedPlay();
        }
    }
    
    void LoadRhythmPatterns()
    {
        if (patternFile != null)
        {
            rhythmData = JsonUtility.FromJson<RhythmData>(patternFile.text);
        }
        else
        {
            Debug.LogError("パターンファイルが設定されていません");
        }
    }
    
    void LoadMetronomePattern()
    {
        if (metronomePatternFile != null)
        {
            metronomeRhythmData = JsonUtility.FromJson<RhythmData>(metronomePatternFile.text);
            
            if (metronomeRhythmData != null && metronomeRhythmData.patterns != null && metronomeRhythmData.patterns.Length > 0)
            {
                metronomePatternIndex = 0; // 通常はメトロノームは最初のパターンを使用
            }
        }
        else if (playMetronome)
        {
            Debug.LogWarning("メトロノームパターンファイルが設定されていないため、メトロノームは無効です");
            playMetronome = false;
        }
    }
    
    void SelectRandomPatterns()
    {
        if (rhythmData.patterns.Length < 2)
        {
            Debug.LogWarning("パターンが2つ未満です。ランダム選択ができません。");
            maracasPatternIndex = 0;
            tambourinePatternIndex = 0;
            return;
        }
        
        // マラカス用のパターンをランダムに選択
        maracasPatternIndex = Random.Range(0, rhythmData.patterns.Length);
        
        // タンバリン用に別のパターンを選択
        do
        {
            tambourinePatternIndex = Random.Range(0, rhythmData.patterns.Length);
        } while (tambourinePatternIndex == maracasPatternIndex);
        
        // ユーザー入力で判定する楽器をランダムに決定
        isDetectingMaracas = Random.value > 0.5f;
        string patternToDetect = isDetectingMaracas ? 
            rhythmData.patterns[maracasPatternIndex].name : 
            rhythmData.patterns[tambourinePatternIndex].name;
        
        Debug.Log($"マラカスパターン: {rhythmData.patterns[maracasPatternIndex].name}");
        Debug.Log($"タンバリンパターン: {rhythmData.patterns[tambourinePatternIndex].name}");
        Debug.Log($"判定するパターン: {(isDetectingMaracas ? "マラカス" : "タンバリン")} - {patternToDetect}");
    }
    
    void StartSynchronizedPlay()
    {
        stepDuration = 60.0 / (bpm * 4); // 16分音符の長さ（秒）
        startTime = AudioSettings.dspTime + 3.0; // 開始時刻を3秒遅らせて記録
        
        Debug.Log($"準備中... 3秒後に開始します");
        
        // シェイクするパターンの種類を音声で通知
        if (isDetectingMaracas)
        {
            if (maracasPatternSound != null)
            {
                patternTypeAudioSource.PlayOneShot(maracasPatternSound);
            }
        }
        else
        {
            if (tambourinePatternSound != null)
            {
                patternTypeAudioSource.PlayOneShot(tambourinePatternSound);
            }
        }
        
        // シーン上の全てのAudioSourceを検索し、BGMをフェードアウト
        FindAllBGMAudioSources();
        if (bgmAudioSources.Length > 0)
        {
            StartCoroutine(FadeOutAllBGM());
        }
        
        isPlaying = true;
        StartCoroutine(PlayRhythmSynchronized());
    }
    
    IEnumerator PlayRhythmSynchronized()
    {
        int totalStepCount = 0; // 開始からの総ステップ数
        
        while (isPlaying && rhythmData.patterns != null && rhythmData.patterns.Length > 0)
        {
            double currentTime = AudioSettings.dspTime;
            double nextStepTime = startTime + (totalStepCount * stepDuration);
            
            if (currentTime >= nextStepTime)
            {
                // 現在の状態に応じた処理
                if (currentState == PlayState.Playing)
                {
                    // マラカスのパターンに基づいて音を再生
                    if (rhythmData.patterns[maracasPatternIndex].pattern[currentStep] == 1)
                    {
                        if (maracasSound != null)
                        {
                            maracasAudioSource.PlayOneShot(maracasSound);
                        }
                    }
                    
                    // タンバリンのパターンに基づいて音を再生
                    if (rhythmData.patterns[tambourinePatternIndex].pattern[currentStep] == 1)
                    {
                        if (tambourineSound != null)
                        {
                            tambourineAudioSource.PlayOneShot(tambourineSound);
                        }
                    }
                }
                // Pausingの場合は楽器音を鳴らさない（空白の小節）
                
                // メトロノームは状態に関わらず再生（Playing/Pausing両方で再生）
                if (playMetronome && metronomeRhythmData != null && metronomeRhythmData.patterns != null && metronomeRhythmData.patterns.Length > 0)
                {
                    if (metronomeRhythmData.patterns[metronomePatternIndex].pattern[currentStep] == 1)
                    {
                        // 小節の頭（最初のステップ）は別の音を使用
                        if (currentStep == 0)
                        {
                            if (metronomeDownBeatSound != null)
                            {
                                metronomeAudioSource.PlayOneShot(metronomeDownBeatSound);
                            }
                        }
                        else
                        {
                            if (metronomeSound != null)
                            {
                                metronomeAudioSource.PlayOneShot(metronomeSound);
                            }
                        }
                    }
                }
                
                currentStep = (currentStep + 1) % 16;
                totalStepCount++;
                
                // 1パターン（16ステップ）終了時の処理
                if (currentStep == 0)
                {
                    if (currentState == PlayState.Playing)
                    {
                        // 演奏状態からユーザー入力モードへ直接移行
                        currentState = PlayState.UserInput;
                        userInputStartTime = AudioSettings.dspTime;
                        userShakeTimes.Clear();
                        expectedShakeTimes.Clear();
                        perfectShakes = 0;
                        goodShakes = 0;
                        missedShakes = 0;
                        extraShakes = 0;
                        totalExpectedShakes = 0;
                        CalculateExpectedShakeTimes();
                        

                        
                        Debug.Log($"ユーザー入力モード開始: {(isDetectingMaracas ? "マラカス" : "タンバリン")}パターンをシェイクしてください");
                    }
                    else if (currentState == PlayState.UserInput)
                    {
                        // ユーザー入力完了の判定を行う
                        EvaluateUserInput();
                        
                        // 新しいパターンを選択して再生モードへ
                        SelectRandomPatterns();
                        currentState = PlayState.Playing;
                        
                        // シェイクするパターンの種類を音声で通知
                        if (isDetectingMaracas)
                        {
                            if (maracasPatternSound != null)
                            {
                                patternTypeAudioSource.PlayOneShot(maracasPatternSound);
                            }
                        }
                        else
                        {
                            if (tambourinePatternSound != null)
                            {
                                patternTypeAudioSource.PlayOneShot(tambourinePatternSound);
                            }
                        }
                        
                        Debug.Log($"新しいパターンを選択して演奏開始: {(isDetectingMaracas ? "マラカス" : "タンバリン")}パターン");
                    }
                }
            }
            
            yield return null;
        }
    }
    
    // ユーザーがシェイクした時のコールバック
    private void OnUserShakeDetected()
    {
        if (currentState == PlayState.UserInput)
        {
            double shakeTime = AudioSettings.dspTime;
            userShakeTimes.Add(shakeTime);
            
            // シェイク時に判定中の楽器の音を鳴らす
            if (isDetectingMaracas && maracasSound != null)
            {
                maracasAudioSource.PlayOneShot(maracasSound);
            }
            else if (!isDetectingMaracas && tambourineSound != null)
            {
                tambourineAudioSource.PlayOneShot(tambourineSound);
            }
            
            Debug.Log($"シェイク検知: {shakeTime - startTime:F2}秒");
        }
    }
    
    // 全ての再生中のAudioSourceを検索する
    private void FindAllBGMAudioSources()
    {
        // まずタグ付きBGMを探す
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(bgmTag);
        System.Collections.Generic.List<AudioSource> foundSources = new System.Collections.Generic.List<AudioSource>();
        
        // タグ付きオブジェクトからAudioSourceを取得
        foreach (GameObject obj in taggedObjects)
        {
            AudioSource[] sources = obj.GetComponents<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source.isPlaying)
                {
                    foundSources.Add(source);
                }
            }
        }
        
        // タグ付きBGMが見つからなかった場合は、DontDestroyOnLoadの可能性があるAudioSourceを全て検索
        if (foundSources.Count == 0)
        {
            AudioSource[] allSources = GameObject.FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allSources)
            {
                if (source.isPlaying && source != maracasAudioSource && source != tambourineAudioSource && source != metronomeAudioSource)
                {
                    foundSources.Add(source);
                    Debug.Log($"BGM候補を検出: {source.gameObject.name}");
                }
            }
        }
        
        bgmAudioSources = foundSources.ToArray();
        Debug.Log($"{bgmAudioSources.Length}個のBGM AudioSourceを検出しました");
    }
    
    // 全てのBGMをフェードアウトするコルーチン
    IEnumerator FadeOutAllBGM()
    {
        // 各BGMの初期音量を記憶
        float[] startVolumes = new float[bgmAudioSources.Length];
        for (int i = 0; i < bgmAudioSources.Length; i++)
        {
            startVolumes[i] = bgmAudioSources[i].volume;
        }
        
        float timer = 0;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / fadeOutDuration;
            
            // 全てのBGMの音量を同時に下げる
            for (int i = 0; i < bgmAudioSources.Length; i++)
            {
                bgmAudioSources[i].volume = Mathf.Lerp(startVolumes[i], 0, ratio);
            }
            
            yield return null;
        }
        
        // 全てのBGMを停止
        foreach (AudioSource source in bgmAudioSources)
        {
            source.Stop();
        }
        
        Debug.Log($"{bgmAudioSources.Length}個のBGMをフェードアウトして停止しました");
    }
    
    // 期待されるシェイクタイミングを計算
    private void CalculateExpectedShakeTimes()
    {
        int patternIndex = isDetectingMaracas ? maracasPatternIndex : tambourinePatternIndex;
        int[] pattern = rhythmData.patterns[patternIndex].pattern;
        
        for (int measure = 0; measure < userInputMeasures; measure++)
        {
            for (int step = 0; step < 16; step++)
            {
                if (pattern[step] == 1)
                {
                    double expectedTime = userInputStartTime + (measure * 16 + step) * stepDuration;
                    expectedShakeTimes.Add(expectedTime);
                    totalExpectedShakes++;
                }
            }
        }
        
        Debug.Log($"期待されるシェイク数: {totalExpectedShakes}");
    }
    
    // ユーザーの入力を評価
    private void EvaluateUserInput()
    {
        perfectShakes = 0;
        goodShakes = 0;
        missedShakes = 0;
        
        // 使用済みのユーザー入力を追跡するフラグ
        bool[] usedUserInputs = new bool[userShakeTimes.Count];
        
        // 各期待タイミングに対して最も近いユーザー入力を探す
        for (int i = 0; i < expectedShakeTimes.Count; i++)
        {
            double expectedTime = expectedShakeTimes[i];
            double closestDiff = double.MaxValue;
            int closestIndex = -1;
            
            // 最も近いユーザー入力を探す
            for (int j = 0; j < userShakeTimes.Count; j++)
            {
                if (usedUserInputs[j]) continue; // 既に使用済みの入力はスキップ
                
                double timeDifference = Mathf.Abs((float)(userShakeTimes[j] - expectedTime));
                if (timeDifference < closestDiff && timeDifference <= goodAccuracy)
                {
                    closestDiff = timeDifference;
                    closestIndex = j;
                }
            }
            
            // 判定結果を記録
            if (closestIndex != -1)
            {
                usedUserInputs[closestIndex] = true; // この入力は使用済み
                
                if (closestDiff <= perfectAccuracy)
                {
                    perfectShakes++;
                    Debug.Log($"Perfect! 期待時間: {expectedTime - userInputStartTime:F2}秒, 実際: {userShakeTimes[closestIndex] - userInputStartTime:F2}秒, 差: {closestDiff:F3}秒");
                }
                else
                {
                    goodShakes++;
                    Debug.Log($"Good! 期待時間: {expectedTime - userInputStartTime:F2}秒, 実際: {userShakeTimes[closestIndex] - userInputStartTime:F2}秒, 差: {closestDiff:F3}秒");
                }
            }
            else
            {
                missedShakes++;
                Debug.Log($"Miss! 期待時間: {expectedTime - userInputStartTime:F2}秒");
            }
        }
        
        // 余分なシェイクの数を数える
        extraShakes = 0;
        for (int i = 0; i < usedUserInputs.Length; i++)
        {
            if (!usedUserInputs[i])
            {
                extraShakes++;
                Debug.Log($"余分なシェイク: {userShakeTimes[i] - userInputStartTime:F2}秒");
            }
        }
        
        // 結果評価
        float score = CalculateScore();
        string instrumentName = isDetectingMaracas ? "マラカス" : "タンバリン";
        
        Debug.Log($"結果: Perfect: {perfectShakes}, Good: {goodShakes}, Miss: {missedShakes}, 余分: {extraShakes}, スコア: {score:P1}");
        
        if (score >= 0.7f)
        {
            Debug.Log($"OK! {instrumentName}のパターンを正確に再現しました。(スコア: {score:P1})");
            
            // 成功音の再生
            if (patternSuccessSound != null)
            {
                patternResultAudioSource.PlayOneShot(patternSuccessSound);
            }
        }
        else
        {
            Debug.Log($"NG! {instrumentName}のパターンを正確に再現できませんでした。(スコア: {score:P1})");
            
            // 失敗音の再生
            if (patternFailureSound != null)
            {
                patternResultAudioSource.PlayOneShot(patternFailureSound);
            }
        }
    }
    
    // スコア計算関数
    private float CalculateScore()
    {
        if (totalExpectedShakes == 0) return 0f;
        
        // 完璧なシェイクは100%ポイント、良いシェイクは70%ポイント
        float totalPoints = perfectShakes * 1.0f + goodShakes * 0.7f;
        
        // 余分なシェイクはペナルティ
        float penalty = extraShakes * extraShakePenalty;
        
        // 最終スコア計算（ペナルティを引くがマイナスにはならない）
        float score = Mathf.Max(0f, (totalPoints - penalty) / totalExpectedShakes);
        
        return score;
    }
}