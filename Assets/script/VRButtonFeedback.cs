using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("按钮反馈设置")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color pressedColor = Color.green;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("缩放设置")]
    public float hoverScale = 1.0f;  // 设置为1.0禁用缩放效果

    [Header("功能开关")]
    public bool enableHoverEffects = true;  // 控制悬停效果开关

    private Button button;
    private Image buttonImage;
    private AudioSource audioSource;
    private Vector3 originalScale;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        // 设置初始颜色
        if (buttonImage != null)
            buttonImage.color = normalColor;

        // 添加点击事件
        button.onClick.AddListener(OnButtonClicked);

        // 禁用按钮的导航功能，减少交互影响
        if (button != null)
        {
            button.navigation = new Navigation() { mode = Navigation.Mode.None };
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverEffects) return;

        if (buttonImage != null && button.interactable)
        {
            buttonImage.color = hoverColor;
        }

        // 轻微放大效果
        if (hoverScale != 1.0f)
        {
            transform.localScale = originalScale * hoverScale;
        }

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverEffects) return;

        if (buttonImage != null && button.interactable)
        {
            buttonImage.color = normalColor;
        }

        // 恢复原始大小
        transform.localScale = originalScale;
    }

    private void OnButtonClicked()
    {
        if (buttonImage != null)
        {
            buttonImage.color = pressedColor;
            // 短暂显示按下颜色后恢复
            Invoke("ResetButtonColor", 0.3f);
        }

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void ResetButtonColor()
    {
        if (buttonImage != null && button.interactable)
        {
            buttonImage.color = normalColor;
        }
    }

    /// <summary>
    /// 禁用所有视觉反馈
    /// </summary>
    public void DisableAllFeedback()
    {
        // 恢复原始颜色和大小
        if (buttonImage != null)
            buttonImage.color = normalColor;

        transform.localScale = originalScale;

        // 取消所有调用
        CancelInvoke();
    }

    void OnDestroy()
    {
        // 清理Invoke调用
        CancelInvoke();
    }
}