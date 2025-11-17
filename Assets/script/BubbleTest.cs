using UnityEngine;
using TMPro;

public class BubbleTest : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI text;

    void Start()
    {
        // 在初始化时直接显示
        panel.SetActive(true);

        // 给文本赋值
        text.text = "测试成功！这是一条气泡内容。";
    }
}
