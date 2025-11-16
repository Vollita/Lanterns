using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LanternHoverFeedback : MonoBehaviour
{
    [Header("悬停反馈设置")]
    public Color normalColor = Color.red;
    public Color hoverColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.gray;

    [Header("灯谜数据")]
    public RiddleSO assignedRiddle;

    [Header("引用")]
    public RiddleManager riddleManager;

    [Header("悬停效果")]
    public Light hoverPointLight;  // 悬停点光源
    public AudioClip hoverSound;   // 悬停音效

    // 私有变量
    private XRSimpleInteractable simpleInteractable;
    private Renderer lanternRenderer;
    private Material lanternMaterial;
    private AudioSource audioSource;
    private bool isActive = true;
    private bool hasBeenAnswered = false;

    void Start()
    {
        InitializeComponents();
        SubscribeToEvents();
    }

    private void InitializeComponents()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        lanternRenderer = GetComponent<Renderer>();

        if (lanternRenderer != null)
        {
            lanternMaterial = lanternRenderer.material;
            lanternMaterial.color = normalColor;
        }

        // 自动查找RiddleManager
        if (riddleManager == null)
        {
            riddleManager = FindObjectOfType<RiddleManager>();
        }

        // 确保有AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始隐藏点光源
        if (hoverPointLight != null)
        {
            hoverPointLight.enabled = false;
        }
    }

    private void SubscribeToEvents()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.AddListener(OnHoverStarted);
            simpleInteractable.hoverExited.AddListener(OnHoverEnded);
            simpleInteractable.selectEntered.AddListener(OnLanternSelected);
        }
    }

    private void OnHoverStarted(HoverEnterEventArgs args)
    {
        if (!isActive || hasBeenAnswered) return;

        // 视觉反馈
        if (lanternMaterial != null)
            lanternMaterial.color = hoverColor;

        // 点光源效果
        if (hoverPointLight != null)
            hoverPointLight.enabled = true;

        // 播放悬停音效
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        Debug.Log($"悬停灯笼: {gameObject.name}");
    }

    private void OnHoverEnded(HoverExitEventArgs args)
    {
        if (!isActive || hasBeenAnswered) return;

        // 恢复视觉
        if (lanternMaterial != null)
            lanternMaterial.color = normalColor;

        // 关闭点光源
        if (hoverPointLight != null)
            hoverPointLight.enabled = false;
    }

    /// <summary>
    /// 灯笼被选择（点击扳机键）时触发
    /// </summary>
    private void OnLanternSelected(SelectEnterEventArgs args)
    {
        if (!isActive || hasBeenAnswered || riddleManager == null) return;

        Debug.Log($"选择灯笼显示灯谜: {gameObject.name}");

        // 显示灯谜问题
        if (assignedRiddle != null)
        {
            riddleManager.ShowRiddle(assignedRiddle, this);
        }
        else
        {
            Debug.LogWarning($"灯笼 {gameObject.name} 没有分配灯谜数据!");
        }
    }

    /// <summary>
    /// 收到答题结果回调
    /// </summary>
    public void OnAnswerGiven(bool isCorrect)
    {
        hasBeenAnswered = true;

        if (lanternMaterial != null)
        {
            lanternMaterial.color = isCorrect ? correctColor : wrongColor;
        }

        // 关闭点光源
        if (hoverPointLight != null)
            hoverPointLight.enabled = false;

        // 禁用交互
        isActive = false;
        simpleInteractable.enabled = false;

        Debug.Log($"灯笼 {gameObject.name} 答题结果: {(isCorrect ? "正确" : "错误")}");
    }

    /// <summary>
    /// 重置灯笼状态
    /// </summary>
    public void ResetLantern()
    {
        hasBeenAnswered = false;
        isActive = true;
        simpleInteractable.enabled = true;

        if (lanternMaterial != null)
        {
            lanternMaterial.color = normalColor;
        }

        // 确保点光源关闭
        if (hoverPointLight != null)
            hoverPointLight.enabled = false;
    }

    /// <summary>
    /// 检查灯笼是否已回答（供RiddleManager调用）
    /// </summary>
    public bool HasBeenAnswered()
    {
        return hasBeenAnswered;
    }

    private void OnDestroy()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.hoverEntered.RemoveListener(OnHoverStarted);
            simpleInteractable.hoverExited.RemoveListener(OnHoverEnded);
            simpleInteractable.selectEntered.RemoveListener(OnLanternSelected);
        }
    }
}
