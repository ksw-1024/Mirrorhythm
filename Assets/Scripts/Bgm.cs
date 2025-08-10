using UnityEngine;
using System.Collections;

public class MainSoundScript : MonoBehaviour {
	public bool DontDestroyEnabled = true;
	private static MainSoundScript instance;

	// Use this for initialization
	void Start () {
		if (DontDestroyEnabled) {
			// 既にインスタンスが存在する場合は、このオブジェクトを破棄
			if (instance != null && instance != this) {
				Destroy(this.gameObject);
				return;
			}
			
			// インスタンスを設定してSceneを遷移してもオブジェクトが消えないようにする
			instance = this;
			DontDestroyOnLoad (this);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
