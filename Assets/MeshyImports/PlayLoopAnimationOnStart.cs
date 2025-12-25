using UnityEngine;

// 类名与文件名严格一致：PlayLoopAnimationOnStart
public class PlayLoopAnimationOnStart : MonoBehaviour
{
    // 公开的动画名称参数（Inspector面板可编辑）
    public string animationName = "Armature|Armature|walking_man|baselayer";

    // 私有动画组件引用
    private Animator animator;

    // 游戏启动时执行
    void Start()
    {
        // 适配嵌套模型：查找自身/子物体的Animator组件
        animator = GetComponentInChildren<Animator>();

        // 空引用+参数校验（核心防错）
        if (animator == null)
        {
            Debug.LogError($"【{gameObject.name}】未找到Animator组件！\n请检查：1.角色是否添加Animator组件 2.模型是否嵌套层级", this);
            return;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogError($"【{gameObject.name}】Animation Name未填写！\n请在Inspector面板的脚本组件中输入动画完整名称", this);
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"【{gameObject.name}】Animator未绑定Controller！\n请给Animator组件拖入对应的Animator Controller文件", this);
            return;
        }

        // 播放指定动画（0层、从头开始）
        animator.Play(animationName, 0, 0f);
        Debug.Log($"【{gameObject.name}】成功播放动画：{animationName}", this);
    }

    // 替代方案：检测动画是否在播放（修复isPlaying报错）
    void Update()
    {
        if (animator == null || string.IsNullOrEmpty(animationName)) return;

        // 获取当前动画状态（解决Animator无isPlaying的问题）
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // 检查是否是目标动画，且是否在播放中
        if (!stateInfo.IsName(animationName) || stateInfo.normalizedTime >= 1f && !animator.IsInTransition(0))
        {
            // 若动画播放完毕/切换，重新播放
            animator.Play(animationName, 0, 0f);
            Debug.LogWarning($"【{gameObject.name}】动画已结束，重新播放：{animationName}", this);
        }
    }
}