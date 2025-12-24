using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR方块交互控制器
/// 用右手射线与方块1交互，触发方块2的移动、消失和重置
/// </summary>
public class VRBlockInteraction : MonoBehaviour
{
    [Header("交互设置")]
    [Tooltip("方块1 - 用于触发交互的方块（需要添加XR Simple Interactable）")]
    public GameObject block1;
    
    [Tooltip("方块2 - 需要移动的方块")]
    public GameObject block2;
    
    [Header("移动设置")]
    [Tooltip("移动方向（Z轴正方向）")]
    public Vector3 moveDirection = Vector3.forward;
    
    [Tooltip("移动距离")]
    public float moveDistance = 5f;
    
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 2f;
    
    [Tooltip("移动完成后等待多久再消失（秒）")]
    public float waitBeforeDisappear = 1f;
    
    [Tooltip("消失后等待多久再重置（秒）")]
    public float waitBeforeReset = 1f;
    
    [Header("可选设置")]
    [Tooltip("是否在开始时自动设置方块1为可交互")]
    public bool autoSetupBlock1 = true;
    
    [Tooltip("启用鼠标点击测试（编辑器模式）")]
    public bool enableMouseClickTest = true;
    
    [Tooltip("使用空格键触发测试")]
    public bool enableSpaceKeyTest = true;
    
    // 私有变量
    private XRSimpleInteractable block1Interactable;
    private Vector3 block2StartPosition;
    private Quaternion block2StartRotation;
    private bool isAnimating = false;
    
    void Start()
    {
        InitializeBlocks();
    }
    
    void Update()
    {
        // 用于编辑器测试：鼠标左键点击方块1
        if (enableMouseClickTest && Input.GetMouseButtonDown(0) && block1 != null)
        {
            // 检测鼠标点击是否击中方块1
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == block1)
                {
                    Debug.Log("VRBlockInteraction: ====== 检测到鼠标点击方块1（编辑器测试模式） ======");
                    TriggerBlock2Animation();
                }
            }
        }
        
        // 空格键测试（用于快速测试，不需要点击方块）
        if (enableSpaceKeyTest && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("VRBlockInteraction: ====== 空格键触发测试 ======");
            TriggerBlock2Animation();
        }
    }
    
    /// <summary>
    /// 初始化方块
    /// </summary>
    private void InitializeBlocks()
    {
        // 检查方块1
        if (block1 == null)
        {
            Debug.LogError("VRBlockInteraction: 方块1未分配！");
            return;
        }
        
        // 检查方块2
        if (block2 == null)
        {
            Debug.LogError("VRBlockInteraction: 方块2未分配！");
            return;
        }
        
        // 保存方块2的初始位置和旋转
        block2StartPosition = block2.transform.position;
        block2StartRotation = block2.transform.rotation;
        
        // 自动设置方块1为可交互
        if (autoSetupBlock1)
        {
            SetupBlock1Interactable();
        }
        else
        {
            // 尝试获取现有的Interactable组件
            block1Interactable = block1.GetComponent<XRSimpleInteractable>();
            if (block1Interactable == null)
            {
                Debug.LogWarning("VRBlockInteraction: 方块1没有XRSimpleInteractable组件，无法交互！请在Inspector中手动添加。");
            }
            else
            {
                SubscribeToBlock1Events();
            }
        }
    }
    
    /// <summary>
    /// 设置方块1为可交互
    /// </summary>
    private void SetupBlock1Interactable()
    {
        // 检查是否已有Interactable组件
        block1Interactable = block1.GetComponent<XRSimpleInteractable>();
        
        if (block1Interactable == null)
        {
            // 添加XRSimpleInteractable组件
            block1Interactable = block1.AddComponent<XRSimpleInteractable>();
            Debug.Log("VRBlockInteraction: 已为方块1添加XRSimpleInteractable组件");
        }
        else
        {
            Debug.Log("VRBlockInteraction: 方块1已有XRSimpleInteractable组件");
        }
        
        // 确保有Collider
        Collider collider = block1.GetComponent<Collider>();
        if (collider == null)
        {
            // 尝试添加BoxCollider
            BoxCollider boxCollider = block1.AddComponent<BoxCollider>();
            Debug.Log("VRBlockInteraction: 已为方块1添加BoxCollider组件");
        }
        else
        {
            Debug.Log($"VRBlockInteraction: 方块1已有Collider: {collider.GetType().Name}");
        }
        
        // 确保XR Interaction Manager存在
        XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogWarning("VRBlockInteraction: 场景中没有找到XR Interaction Manager！交互可能无法工作。");
        }
        else
        {
            Debug.Log("VRBlockInteraction: 找到XR Interaction Manager");
        }
        
        // 订阅事件
        SubscribeToBlock1Events();
    }
    
    /// <summary>
    /// 订阅方块1的交互事件
    /// </summary>
    private void SubscribeToBlock1Events()
    {
        if (block1Interactable != null)
        {
            // 订阅hover事件用于调试
            block1Interactable.hoverEntered.AddListener(OnBlock1HoverEntered);
            block1Interactable.hoverExited.AddListener(OnBlock1HoverExited);
            // 订阅select事件
            block1Interactable.selectEntered.AddListener(OnBlock1Selected);
            Debug.Log("VRBlockInteraction: 已订阅方块1的交互事件");
        }
        else
        {
            Debug.LogError("VRBlockInteraction: block1Interactable为null，无法订阅事件！");
        }
    }
    
    /// <summary>
    /// 方块1被悬停时的回调（用于调试）
    /// </summary>
    private void OnBlock1HoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"VRBlockInteraction: 方块1被悬停！交互器类型: {args.interactorObject.GetType().Name}");
    }
    
    /// <summary>
    /// 方块1离开悬停时的回调（用于调试）
    /// </summary>
    private void OnBlock1HoverExited(HoverExitEventArgs args)
    {
        Debug.Log("VRBlockInteraction: 方块1离开悬停");
    }
    
    /// <summary>
    /// 方块1被选中时的回调
    /// </summary>
    private void OnBlock1Selected(SelectEnterEventArgs args)
    {
        Debug.Log($"VRBlockInteraction: ====== 方块1被选中！ ======");
        Debug.Log($"VRBlockInteraction: 交互器类型: {args.interactorObject.GetType().Name}");
        Debug.Log($"VRBlockInteraction: 交互器对象: {args.interactorObject.transform?.name ?? "null"}");
        Debug.Log("VRBlockInteraction: 触发方块2移动动画");
        TriggerBlock2Animation();
    }
    
    /// <summary>
    /// 触发方块2的动画（移动->消失->重置）
    /// </summary>
    public void TriggerBlock2Animation()
    {
        if (isAnimating)
        {
            Debug.Log("VRBlockInteraction: 方块2正在动画中，忽略重复触发");
            return;
        }
        
        isAnimating = true;
        StartCoroutine(Block2AnimationSequence());
    }
    
    /// <summary>
    /// 方块2动画序列协程
    /// </summary>
    private System.Collections.IEnumerator Block2AnimationSequence()
    {
        // 步骤1: 移动方块2
        Vector3 targetPosition = block2StartPosition + moveDirection.normalized * moveDistance;
        float elapsedTime = 0f;
        float moveDuration = moveDistance / moveSpeed;
        
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);
            block2.transform.position = Vector3.Lerp(block2StartPosition, targetPosition, t);
            yield return null;
        }
        
        // 确保到达目标位置
        block2.transform.position = targetPosition;
        Debug.Log("VRBlockInteraction: 方块2移动到目标位置");
        
        // 步骤2: 等待一段时间
        yield return new WaitForSeconds(waitBeforeDisappear);
        
        // 步骤3: 消失
        block2.SetActive(false);
        Debug.Log("VRBlockInteraction: 方块2消失");
        
        // 步骤4: 等待一段时间
        yield return new WaitForSeconds(waitBeforeReset);
        
        // 步骤5: 重置位置并显示
        block2.transform.position = block2StartPosition;
        block2.transform.rotation = block2StartRotation;
        block2.SetActive(true);
        Debug.Log("VRBlockInteraction: 方块2重置到初始位置");
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 手动重置方块2（外部调用）
    /// </summary>
    public void ResetBlock2()
    {
        if (isAnimating)
        {
            StopAllCoroutines();
            isAnimating = false;
        }
        
        block2.transform.position = block2StartPosition;
        block2.transform.rotation = block2StartRotation;
        block2.SetActive(true);
        Debug.Log("VRBlockInteraction: 手动重置方块2");
    }
    
    /// <summary>
    /// 手动触发动画（外部调用，用于测试）
    /// </summary>
    [ContextMenu("测试触发方块2动画")]
    public void TestTriggerAnimation()
    {
        TriggerBlock2Animation();
    }
    
    void OnDestroy()
    {
        // 清理事件订阅
        if (block1Interactable != null)
        {
            block1Interactable.hoverEntered.RemoveListener(OnBlock1HoverEntered);
            block1Interactable.hoverExited.RemoveListener(OnBlock1HoverExited);
            block1Interactable.selectEntered.RemoveListener(OnBlock1Selected);
        }
    }
}

