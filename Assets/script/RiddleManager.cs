using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class RiddleManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questionPanel;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public GameObject correctPanel;
    public GameObject wrongPanel;
    public TextMeshProUGUI explanationText;

    [Header("Riddle Data")]
    public RiddleSO[] riddles;
    private int currentRiddleIndex = 0;
    private RiddleData currentRiddle;

    [Header("XR References")]
    public XRRayInteractor rightHandRayInteractor;

    // 存储原始的交互层掩码
    private InteractionLayerMask originalInteractionLayers;
    private bool hasStoredOriginalLayers = false;

    // 当前激活的灯笼引用
    private LanternHoverFeedback currentLantern;
    private bool isWaitingForAnswer = false; // 新增：标记是否在等待答题

    void Start()
    {
        // 初始隐藏所有UI
        HideAllPanels();

        // 设置按钮事件
        SetupOptionButtons();

        // 加载第一个灯谜
        if (riddles.Length > 0)
        {
            currentRiddle = riddles[currentRiddleIndex].riddleData;
            Debug.Log($"加载灯谜 {currentRiddleIndex + 1}/{riddles.Length}");
        }

        Debug.Log("RiddleManager 初始化完成");
    }

    /// <summary>
    /// 设置选项按钮点击事件
    /// </summary>
    private void SetupOptionButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int optionIndex = i;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
        }
        Debug.Log($"已设置 {optionButtons.Length} 个选项按钮的事件监听");
    }

    /// <summary>
    /// 显示灯谜问题（由灯笼调用）
    /// </summary>
    public void ShowRiddle(RiddleSO riddleSO, LanternHoverFeedback lantern)
    {
        if (riddleSO == null)
        {
            Debug.LogError("传入的灯谜数据为空！");
            return;
        }

        currentRiddle = riddleSO.riddleData;
        currentLantern = lantern;
        isWaitingForAnswer = true; // 开始等待答题

        // 更新UI显示
        questionText.text = currentRiddle.question;
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i < currentRiddle.options.Length)
            {
                optionTexts[i].text = $"{((char)('A' + i))}. {currentRiddle.options[i]}";
                optionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // 显示问题面板
        HideAllPanels();
        questionPanel.SetActive(true);

        // 切换到UI交互模式
        SetUIInteractionMode(true);

        // 播放UI显示音效
        PlayUISound("UIAppear");

        Debug.Log($"显示灯谜: {currentRiddle.question}, 当前灯笼: {currentLantern?.gameObject.name ?? "null"}");
    }

    /// <summary>
    /// 设置UI交互模式
    /// </summary>
    private void SetUIInteractionMode(bool uiMode)
    {
        if (rightHandRayInteractor == null)
        {
            Debug.LogError("RightHand Ray Interactor 未分配！");
            return;
        }

        if (!hasStoredOriginalLayers)
        {
            // 保存原始的交互层设置
            originalInteractionLayers = rightHandRayInteractor.interactionLayers;
            hasStoredOriginalLayers = true;
            Debug.Log("存储原始交互层设置完成");
        }

        if (uiMode)
        {
            // UI模式：只与UI交互
            var uiLayerMask = InteractionLayerMask.GetMask("UI");
            rightHandRayInteractor.interactionLayers = uiLayerMask;
            Debug.Log("切换到UI交互模式 - 只与UI交互");
        }
        else
        {
            // 世界模式：恢复原始交互层设置
            rightHandRayInteractor.interactionLayers = originalInteractionLayers;
            Debug.Log("恢复世界交互模式 - 可与3D物体交互");
        }
    }

    /// <summary>
    /// 选项选择处理
    /// </summary>
    private void OnOptionSelected(int selectedIndex)
    {
        Debug.Log($"玩家选择了选项: {(char)('A' + selectedIndex)}");

        if (currentRiddle == null)
        {
            Debug.LogError("当前灯谜数据为空！");
            return;
        }

        // 播放按钮点击音效
        PlayUISound("ButtonClick");

        bool isCorrect = (selectedIndex == currentRiddle.correctOptionIndex);

        if (isCorrect)
        {
            ShowCorrectFeedback();
        }
        else
        {
            ShowWrongFeedback();
        }

        // 禁用按钮防止重复点击
        SetOptionsInteractable(false);

        // 通知灯笼答题结果
        NotifyLanternAnswer(isCorrect);
    }

    /// <summary>
    /// 通知灯笼答题结果（新增方法，增强健壮性）
    /// </summary>
    private void NotifyLanternAnswer(bool isCorrect)
    {
        if (currentLantern != null)
        {
            Debug.Log($"通知灯笼答题结果: {isCorrect}, 灯笼: {currentLantern.gameObject.name}");
            currentLantern.OnAnswerGiven(isCorrect);
        }
        else
        {
            Debug.LogWarning("当前灯笼引用为空，无法通知答题结果");

            // 尝试通过其他方式找到当前激活的灯笼
            LanternHoverFeedback[] allLanterns = FindObjectsOfType<LanternHoverFeedback>();
            foreach (LanternHoverFeedback lantern in allLanterns)
            {
                if (lantern != null && lantern.HasBeenAnswered())
                {
                    Debug.Log($"找到已答题的灯笼: {lantern.gameObject.name}，通知结果: {isCorrect}");
                    lantern.OnAnswerGiven(isCorrect);
                    break;
                }
            }
        }

        isWaitingForAnswer = false; // 答题完成
    }

    /// <summary>
    /// 显示正确反馈
    /// </summary>
    private void ShowCorrectFeedback()
    {
        if (currentRiddle == null) return;

        explanationText.text = $"<color=green>✓ 正确答案！</color>\n\n{currentRiddle.explanation}";
        correctPanel.SetActive(true);

        // 播放正确音效
        PlayUISound("Correct");

        // 3秒后自动隐藏
        Invoke("HideAllPanels", 3f);

        Debug.Log("回答正确！");
    }

    /// <summary>
    /// 显示错误反馈
    /// </summary>
    private void ShowWrongFeedback()
    {
        if (currentRiddle == null) return;

        char correctChar = (char)('A' + currentRiddle.correctOptionIndex);
        explanationText.text = $"<color=red>✗ 正确答案是: {correctChar}</color>\n\n{currentRiddle.explanation}";
        wrongPanel.SetActive(true);

        // 播放错误音效
        PlayUISound("Wrong");

        // 3秒后自动隐藏
        Invoke("HideAllPanels", 3f);

        Debug.Log($"回答错误！正确答案是: {correctChar}");
    }

    /// <summary>
    /// 播放UI音效的辅助方法
    /// </summary>
    private void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
        else
        {
            Debug.LogWarning($"AudioManager 实例未找到，无法播放音效: {soundName}");
        }
    }

    /// <summary>
    /// 隐藏所有UI面板
    /// </summary>
    public void HideAllPanels()
    {
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
            Debug.Log("隐藏 QuestionPanel");
        }

        if (correctPanel != null)
        {
            correctPanel.SetActive(false);
            Debug.Log("隐藏 CorrectPanel");
        }

        if (wrongPanel != null)
        {
            wrongPanel.SetActive(false);
            Debug.Log("隐藏 WrongPanel");
        }

        // 重新启用选项按钮
        SetOptionsInteractable(true);

        // 切换回世界交互模式
        SetUIInteractionMode(false);

        // 重置当前灯笼引用（但不强制置空，保留引用直到下次显示新灯谜）
        if (!isWaitingForAnswer)
        {
            currentLantern = null;
            Debug.Log("重置当前灯笼引用");
        }

        Debug.Log("所有UI面板已隐藏，恢复世界交互");
    }

    /// <summary>
    /// 设置选项按钮的交互状态
    /// </summary>
    private void SetOptionsInteractable(bool interactable)
    {
        foreach (Button button in optionButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    /// <summary>
    /// 加载下一个灯谜
    /// </summary>
    public void LoadNextRiddle()
    {
        currentRiddleIndex = (currentRiddleIndex + 1) % riddles.Length;
        currentRiddle = riddles[currentRiddleIndex].riddleData;
        Debug.Log($"加载下一个灯谜: {currentRiddleIndex + 1}/{riddles.Length}");
    }

    /// <summary>
    /// 检查是否有UI正在显示
    /// </summary>
    public bool IsUIActive()
    {
        return (questionPanel != null && questionPanel.activeInHierarchy) ||
               (correctPanel != null && correctPanel.activeInHierarchy) ||
               (wrongPanel != null && wrongPanel.activeInHierarchy);
    }

    /// <summary>
    /// 手动触发显示当前灯谜（用于测试）
    /// </summary>
    [ContextMenu("测试显示当前灯谜")]
    public void TestShowCurrentRiddle()
    {
        if (riddles.Length > currentRiddleIndex && riddles[currentRiddleIndex] != null)
        {
            // 创建一个虚拟灯笼用于测试
            GameObject testLantern = new GameObject("TestLantern");
            LanternHoverFeedback testLanternScript = testLantern.AddComponent<LanternHoverFeedback>();

            ShowRiddle(riddles[currentRiddleIndex], testLanternScript);
        }
        else
        {
            Debug.LogError("无法测试显示灯谜：灯谜数据未设置或索引越界");
        }
    }

    /// <summary>
    /// 手动触发正确反馈（用于测试）
    /// </summary>
    [ContextMenu("测试正确反馈")]
    public void TestCorrectFeedback()
    {
        if (correctPanel != null)
        {
            explanationText.text = "测试：正确答案反馈！";
            correctPanel.SetActive(true);
            PlayUISound("Correct");
            Invoke("HideAllPanels", 2f);
        }
    }

    /// <summary>
    /// 手动触发错误反馈（用于测试）
    /// </summary>
    [ContextMenu("测试错误反馈")]
    public void TestWrongFeedback()
    {
        if (wrongPanel != null)
        {
            explanationText.text = "测试：错误答案反馈！";
            wrongPanel.SetActive(true);
            PlayUISound("Wrong");
            Invoke("HideAllPanels", 2f);
        }
    }

    /// <summary>
    /// 重置所有灯谜进度
    /// </summary>
    [ContextMenu("重置所有灯谜")]
    public void ResetAllRiddles()
    {
        currentRiddleIndex = 0;
        if (riddles.Length > 0 && riddles[0] != null)
        {
            currentRiddle = riddles[0].riddleData;
        }
        HideAllPanels();

        // 重置场景中所有灯笼
        LanternHoverFeedback[] allLanterns = FindObjectsOfType<LanternHoverFeedback>();
        foreach (LanternHoverFeedback lantern in allLanterns)
        {
            if (lantern != null)
            {
                lantern.ResetLantern();
            }
        }

        Debug.Log("所有灯谜已重置");
    }

    /// <summary>
    /// 检查UI状态（用于调试）
    /// </summary>
    [ContextMenu("检查UI状态")]
    public void CheckUIStatus()
    {
        Debug.Log("=== UI状态检查 ===");
        Debug.Log($"QuestionPanel 激活: {questionPanel != null && questionPanel.activeInHierarchy}");
        Debug.Log($"CorrectPanel 激活: {correctPanel != null && correctPanel.activeInHierarchy}");
        Debug.Log($"WrongPanel 激活: {wrongPanel != null && wrongPanel.activeInHierarchy}");
        Debug.Log($"当前灯笼引用: {currentLantern?.gameObject.name ?? "null"}");
        Debug.Log($"等待答题状态: {isWaitingForAnswer}");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas 激活: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"Canvas 名称: {canvas.gameObject.name}");
        }

        Debug.Log("=== 检查完成 ===");
    }

    void OnDestroy()
    {
        // 清理Invoke调用
        CancelInvoke();
    }
}