using UnityEngine;

public class PlayLoopAnimationOnStart : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            // 触发动画过渡
            animator.SetTrigger("StartAnimation");
            Debug.Log("触发动画播放！");
        }
        else
        {
            Debug.LogError("模型上没有 Animator 组件！");
        }
    }
}