using UnityEngine;
using TMPro;

public class BubbleControllerr : MonoBehaviour
{
    public TextMeshProUGUI bubbleText;  // 拖入 Text (TMP)
    public string[] sentences;          // 在 Inspector 填写每一句话

    private int currentIndex = 0;

    void Start()
    {
        if (bubbleText == null)
        {
            Debug.LogError("BubbleControllerr Error: bubbleText 未赋值，请将 Text (TMP) 拖入。");
            return;
        }

        if (sentences == null || sentences.Length == 0)
        {
            Debug.LogError("BubbleControllerr Error: sentences 数组为空，请在 Inspector 填写对话内容。");
            return;
        }

        bubbleText.text = sentences[currentIndex];
        ShowBubble();    // 启动时强制显示气泡
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左键点击
        {
            NextSentence();
        }
    }

    public void ShowBubble()
    {
        gameObject.SetActive(true);  // 气泡显示
        currentIndex = 0;            // 重置句子
        bubbleText.text = sentences[currentIndex];
    }

    public void HideBubble()
    {
        gameObject.SetActive(false); // 气泡隐藏
    }

    private void NextSentence()
    {
        if (currentIndex < sentences.Length - 1)
        {
            currentIndex++;
            bubbleText.text = sentences[currentIndex];
        }
        else
        {
            Debug.Log("已到最后一句。");
        }
    }
}
