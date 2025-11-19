using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
public class RiddleManager1 : MonoBehaviour
{
    public LanternController lanternController; // 将灯笼拖拽到这里
    public GameObject correctUI; // 将显示“正确”的UI拖拽到这里
    public GameObject incorrectUI; // 将显示“错误”的UI拖拽到这里
    void Start()
    {
        correctUI.SetActive(false);
        incorrectUI.SetActive(false);
    }

    // 这个方法将由正确的答案按钮调用
    public void OnCorrectAnswerSelected()
    {
        StartCoroutine(ShowResultAndReset(true));
    }

    // 这个方法将由错误的答案按钮调用
    public void OnIncorrectAnswerSelected()
    {
        StartCoroutine(ShowResultAndReset(false));
    }

    private System.Collections.IEnumerator ShowResultAndReset(bool isCorrect)
    {
        if (isCorrect)
        {
            correctUI.SetActive(true);
        }
        else
        {
            incorrectUI.SetActive(true);
        }

        yield return new WaitForSeconds(2.0f); // 显示结果2秒

        AudioSource uiAudio = lanternController.riddleUI.GetComponent<AudioSource>();
        uiAudio.Stop();

        // 隐藏所有UI并恢复灯笼
        correctUI.SetActive(false);
        incorrectUI.SetActive(false);
        lanternController.riddleUI.SetActive(false);
        lanternController.RestoreLantern();
        Debug.Log("3");
    }
}