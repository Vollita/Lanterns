using UnityEngine;
using UnityEngine.EventSystems; // 必须引用，用于处理指针事件
using System.Collections;

public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    public float scaleFactor = 1.1f; // 放大倍数 (1.1 代表放大 10%)
    public float duration = 0.15f;   // 动画持续时间 (秒)

    private Vector3 originalScale;
    private Coroutine currentCoroutine;

    void Start()
    {
        // 记录按钮初始大小，防止多次缩放后变形
        originalScale = transform.localScale;
    }

    // 当射线悬停在按钮上时触发 (对应 XR 的 Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 目标大小 = 原始大小 * 放大倍数
        StartScaleAnimation(originalScale * scaleFactor);
    }

    // 当射线离开按钮时触发
    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复原始大小
        StartScaleAnimation(originalScale);
    }

    // 处理平滑缩放的逻辑
    private void StartScaleAnimation(Vector3 targetScale)
    {
        // 如果上一个动画还没做完，先停止，防止冲突
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ScaleTo(targetScale));
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 startScale = transform.localScale;
        float time = 0;

        while (time < duration)
        {
            // 使用 Lerp 插值实现平滑过渡
            transform.localScale = Vector3.Lerp(startScale, target, time / duration);
            time += Time.deltaTime;
            yield return null; // 等待下一帧
        }

        // 确保最后精确到达目标值
        transform.localScale = target;
    }

    //以此确保当对象被禁用/隐藏时（比如答题结束UI消失），重置回原始大小
    //防止下次打开UI时按钮还是大的
    void OnDisable()
    {
        transform.localScale = originalScale;
    }
}