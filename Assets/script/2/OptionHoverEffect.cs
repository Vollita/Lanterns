using UnityEngine;
using UnityEngine.EventSystems;
public class OptionHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    public float hoverScale = 1.1f;
    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale; // 放大
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale; // 恢复原始大小
    }
}