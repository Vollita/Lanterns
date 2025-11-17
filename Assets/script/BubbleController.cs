using UnityEngine;

public class BubbleController : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main?.transform;
    }

    void LateUpdate()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.forward);
        }
    }

    // === 以下两个函数仅为解决报错，不添加其他功能 ===

    public void ShowBubble()
    {
        gameObject.SetActive(true);
    }

    public void HideBubble()
    {
        gameObject.SetActive(false);
    }
}
