using UnityEngine;

public class BubbleTrigger : MonoBehaviour
{
    public Transform npc;  // 手动拖 NPC的根节点
    public Transform player; // 手动拖 Player对象

    public float visibleDistance = 3f; // 气泡显示距离

    private BubbleController bubble;

    void Start()
    {
        if (npc == null || player == null)
        {
            Debug.LogError("BubbleTrigger: npc 或 player 未手动赋值！");
            return;
        }

        // 获取 NPC 身上气泡（假设气泡是 NPC 的子对象）
        bubble = npc.GetComponentInChildren<BubbleController>();

        if (bubble == null)
        {
            Debug.LogError("在 NPC 节点下找不到 BubbleController！你确定挂对 NPC 了吗？");
        }
    }

    void Update()
    {
        if (bubble == null) return;

        float distance = Vector3.Distance(npc.position, player.position);

        if (distance <= visibleDistance)
            bubble.ShowBubble();
        else
            bubble.HideBubble();
    }
}
