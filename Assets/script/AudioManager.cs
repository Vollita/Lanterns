using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance { get; private set; }

    [Header("游戏音效")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip hoverSound;
    public AudioClip buttonClickSound;
    public AudioClip UIAppearSound;

    [Header("音频设置")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 设置 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置 AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D音效
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(string soundName)
    {
        AudioClip clipToPlay = null;

        switch (soundName.ToLower())
        {
            case "correct":
                clipToPlay = correctSound;
                break;
            case "wrong":
                clipToPlay = wrongSound;
                break;
            case "hover":
                clipToPlay = hoverSound;
                break;
            case "buttonclick":
                clipToPlay = buttonClickSound;
                break;
            case "uiappear":
                clipToPlay = UIAppearSound;
                break;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, masterVolume * sfxVolume);
        }
        else
        {
            Debug.LogWarning($"音效 '{soundName}' 未在AudioManager中找到！");
        }
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 测试播放所有音效
    /// </summary>
    [ContextMenu("测试所有音效")]
    public void TestAllSFX()
    {
        Debug.Log("测试播放所有音效...");

        if (correctSound != null)
        {
            audioSource.PlayOneShot(correctSound);
            Invoke("TestWrongSound", 1f);
        }
    }

    private void TestWrongSound()
    {
        if (wrongSound != null)
        {
            audioSource.PlayOneShot(wrongSound);
            Invoke("TestHoverSound", 1f);
        }
    }

    private void TestHoverSound()
    {
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
            Invoke("TestButtonSound", 1f);
        }
    }

    private void TestButtonSound()
    {
        if (buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    void OnDestroy()
    {
        // 清理Invoke调用
        CancelInvoke();
    }
}