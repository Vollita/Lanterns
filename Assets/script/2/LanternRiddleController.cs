using UnityEngine;
using UnityEngine.UI; // 引用标准UI
using TMPro;          // 引用 TextMeshPro
using System.Collections;

public class LanternRiddleController : MonoBehaviour
{
    [Header("--- 灯笼组件设置 ---")]
    public Light lanternLight;        // 灯笼的点光源
    public AudioSource lanternAudio;  // 播放的音效
    // 注意：我们不需要手动引用 Interactable，因为事件是通过 Inspector 绑定的

    [Header("--- UI 组件设置 ---")]
    public GameObject riddleCanvas;      // 整个 World Space Canvas
    public GameObject questionPanel;     // 问题面板
    public GameObject resultPanel;       // 结果面板
    public TextMeshProUGUI resultText;   // 显示结果的文本 TMP

    [Header("--- 题目逻辑设置 ---")]
    // 0=A, 1=B, 2=C, 3=D. 这里可以在Inspector设置正确答案
    public int correctAnswerIndex = 0;

    private bool isRiddleActive = false; // 标记：是否正在答题状态

    void Start()
    {
        // 初始化状态：关灯，关UI
        lanternLight.enabled = false;
        riddleCanvas.SetActive(false);
        resultPanel.SetActive(false);
    }

    // ================= XR 事件响应区 =================

    // 1. 对应 XR Simple Interactable -> Hover Entered
    // 逻辑：射线放上去，如果没在答题，就亮灯播放声音
    public void OnHoverEnter()
    {
        if (!isRiddleActive)
        {
            lanternLight.enabled = true;
            if (!lanternAudio.isPlaying) lanternAudio.Play();
        }
    }

    // XR Simple Interactable -> Hover Exited 不需要绑定
    // 因为你的逻辑要求：声音和光持续到对错UI显示，而不是离开射线就灭掉

    // 2. 对应 XR Simple Interactable -> Select Entered
    // 逻辑：扣动扳机，显示题目UI
    public void OnSelectEnter()
    {
        // 防止重复触发
        if (!isRiddleActive)
        {
            isRiddleActive = true; // 进入答题状态
            ShowQuestionUI();
        }
    }

    // ================= 内部逻辑区 =================

    void ShowQuestionUI()
    {
        riddleCanvas.SetActive(true);
        questionPanel.SetActive(true);
        resultPanel.SetActive(false);
        // 此时灯光和声音继续保持，不关闭
    }

    // 3. 绑定到 UI 按钮的 OnClick 事件
    // 参数：index (0,1,2,3) 代表用户选了哪个
    public void SubmitAnswer(int index)
    {
        // 显示结果面板
        questionPanel.SetActive(false);
        resultPanel.SetActive(true);

        // 判题
        if (index == correctAnswerIndex)
        {
            resultText.text = "回答正确！";
            resultText.color = Color.green;
            // 可选：播放额外的胜利音效
        }
        else
        {
            resultText.text = "回答错误！";
            resultText.color = Color.red;
        }

        // --- 关键点：在出结果的瞬间，关闭灯笼氛围 ---
        lanternLight.enabled = false;
        lanternAudio.Stop();
        Debug.Log("1");

        // 开启3秒关闭倒计时
        StartCoroutine(WaitAndCloseUI());
    }

    IEnumerator WaitAndCloseUI()
    {
        yield return new WaitForSeconds(3.0f);

        // 隐藏 UI
        riddleCanvas.SetActive(false);

        // 重置状态，允许下一次重新 Hover 触发
        isRiddleActive = false;
    }
}