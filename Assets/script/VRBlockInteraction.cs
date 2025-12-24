using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Reflection;

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
    public bool enableMouseClickTest = false; // 默认关闭，避免干扰射线交互测试
    
    [Tooltip("使用空格键触发测试")]
    public bool enableSpaceKeyTest = false; // 默认关闭，避免干扰射线交互测试
    
    // 私有变量
    private XRSimpleInteractable block1Interactable;
    private Vector3 block2StartPosition;
    private Quaternion block2StartRotation;
    private bool isAnimating = false;
    
    // 用于检测hover状态和鼠标点击
    private int hoverCount = 0; // 使用计数来跟踪有多少个射线正在hover方块1
    private bool mouseLeftButtonPressedLastFrame = false;
    
    void Start()
    {
        InitializeBlocks();
    }
    
    void Update()
    {
        // 检测鼠标左键按下（当方块1被hover时触发交互）
        bool mouseLeftButtonDown = Input.GetMouseButtonDown(0);
        
        // 如果方块1正在被hover（左手或右手射线），并且按下鼠标左键，触发交互
        if (mouseLeftButtonDown && hoverCount > 0 && !mouseLeftButtonPressedLastFrame)
        {
            Debug.Log($"VRBlockInteraction: ====== 检测到鼠标左键按下，方块1处于hover状态（{hoverCount}个射线），触发交互 ======");
            TriggerBlock2Animation();
        }
        
        mouseLeftButtonPressedLastFrame = mouseLeftButtonDown;
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
        Debug.Log("========== VRBlockInteraction: 开始配置方块1 ==========");
        
        // 检查是否已有Interactable组件
        block1Interactable = block1.GetComponent<XRSimpleInteractable>();
        
        if (block1Interactable == null)
        {
            // 添加XRSimpleInteractable组件
            block1Interactable = block1.AddComponent<XRSimpleInteractable>();
            Debug.Log("VRBlockInteraction: ✅ 已为方块1添加XRSimpleInteractable组件");
        }
        else
        {
            Debug.Log("VRBlockInteraction: ✅ 方块1已有XRSimpleInteractable组件");
        }
        
        // 确保有Collider
        Collider collider = block1.GetComponent<Collider>();
        if (collider == null)
        {
            // 尝试添加BoxCollider
            BoxCollider boxCollider = block1.AddComponent<BoxCollider>();
            Debug.Log("VRBlockInteraction: ✅ 已为方块1添加BoxCollider组件");
        }
        else
        {
            Debug.Log($"VRBlockInteraction: ✅ 方块1已有Collider: {collider.GetType().Name}, Is Trigger: {collider.isTrigger}");
            if (collider.isTrigger)
            {
                Debug.LogWarning("VRBlockInteraction: ⚠️ Collider的Is Trigger为true，这可能导致交互失败！");
            }
        }
        
        // 检查XR Interaction Manager
        XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogError("VRBlockInteraction: ❌ 场景中没有找到XR Interaction Manager！交互无法工作！");
        }
        else
        {
            Debug.Log($"VRBlockInteraction: ✅ 找到XR Interaction Manager: {interactionManager.gameObject.name}");
            
            // 检查block1Interactable的Interaction Manager引用
            if (block1Interactable.interactionManager == null)
            {
                Debug.LogWarning("VRBlockInteraction: ⚠️ block1Interactable的Interaction Manager为null，尝试设置...");
                block1Interactable.interactionManager = interactionManager;
            }
            else
            {
                Debug.Log($"VRBlockInteraction: ✅ block1Interactable的Interaction Manager已设置: {block1Interactable.interactionManager.gameObject.name}");
            }
        }
        
        // 检查交互层
        CheckInteractionLayers();
        
        // 订阅事件
        SubscribeToBlock1Events();
        
        Debug.Log("========== VRBlockInteraction: 方块1配置完成 ==========");
    }
    
    /// <summary>
    /// 检查交互层配置
    /// </summary>
    private void CheckInteractionLayers()
    {
        Debug.Log("========== 检查交互层配置 ==========");
        
        if (block1Interactable == null)
        {
            Debug.LogError("VRBlockInteraction: block1Interactable为null，无法检查交互层");
            return;
        }
        
        // 检查方块1的交互层
        var block1Layers = block1Interactable.interactionLayers;
        uint block1LayerValue = GetInteractionLayerValue(block1Layers);
        Debug.Log($"方块1的Interaction Layers值: {block1LayerValue}");
        Debug.Log($"方块1交互层二进制: {System.Convert.ToString(block1LayerValue, 2).PadLeft(8, '0')}");
        
        // 检查所有Ray Interactor的交互层
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>();
        Debug.Log($"找到 {rayInteractors.Length} 个Ray Interactor");
        
        if (rayInteractors.Length == 0)
        {
            Debug.LogError("VRBlockInteraction: ❌ 场景中没有找到Ray Interactor！这是问题的根源！");
            Debug.LogError("VRBlockInteraction: 请确保右手控制器上有XR Ray Interactor组件！");
            Debug.Log("========== 交互层检查完成 ==========");
            return;
        }
        
        bool foundMatchingLayer = false;
        foreach (XRRayInteractor rayInteractor in rayInteractors)
        {
            var rayLayers = rayInteractor.interactionLayers;
            uint rayLayerValue = GetInteractionLayerValue(rayLayers);
            Debug.Log($"Ray Interactor ({rayInteractor.gameObject.name}) 的Interaction Layers值: {rayLayerValue}");
            Debug.Log($"Ray Interactor交互层二进制: {System.Convert.ToString(rayLayerValue, 2).PadLeft(8, '0')}");
            
            // 检查是否有匹配的层（如果两个值有交集，说明有匹配的层）
            if ((rayLayerValue & block1LayerValue) != 0)
            {
                Debug.Log($"✅ 找到匹配的交互层！Ray Interactor: {rayInteractor.gameObject.name}");
                foundMatchingLayer = true;
            }
            else
            {
                Debug.LogWarning($"❌ Ray Interactor ({rayInteractor.gameObject.name}) 的交互层与方块1不匹配！");
                Debug.LogWarning($"   方块1的值: {block1LayerValue}, Ray Interactor的值: {rayLayerValue}");
                Debug.LogWarning($"   解决方法1（手动）：");
                Debug.LogWarning($"     - 选中方块1 → XR Simple Interactable → Interaction Layer Mask → 只勾选 'Default'");
                Debug.LogWarning($"     - 选中 {rayInteractor.gameObject.name} → XR Ray Interactor → Interaction Layers → 只勾选 'Default'");
                Debug.LogWarning($"   解决方法2（自动）：");
                Debug.LogWarning($"     - 在Unity菜单栏选择：VR Tools → 修复交互层配置（会自动修复所有组件）");
            }
        }
        
        if (!foundMatchingLayer)
        {
            Debug.LogError("VRBlockInteraction: ❌ 错误：所有Ray Interactor的交互层都与方块1不匹配！");
            Debug.LogError("VRBlockInteraction: 这是交互失败的主要原因！");
            Debug.LogError("VRBlockInteraction: 解决方法：");
            Debug.LogError("  1. 选中方块1 → XR Simple Interactable → Interaction Layer Mask → 勾选 'Default'");
            Debug.LogError("  2. 选中右手控制器 → XR Ray Interactor → Interaction Layers → 勾选 'Default'");
            Debug.LogError("  3. 确保两个地方至少有一个相同的层被勾选！");
        }
        
        Debug.Log("========== 交互层检查完成 ==========");
    }
    
    /// <summary>
    /// 获取InteractionLayerMask的内部值
    /// </summary>
    private uint GetInteractionLayerValue(InteractionLayerMask layerMask)
    {
        try
        {
            var layerMaskType = typeof(InteractionLayerMask);
            var valueField = layerMaskType.GetField("m_Bits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (valueField != null)
            {
                object boxedMask = layerMask;
                uint value = (uint)valueField.GetValue(boxedMask);
                return value;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"无法获取InteractionLayerMask的值: {e.Message}");
        }
        return 0;
    }
    
    /// <summary>
    /// 订阅方块1的交互事件
    /// </summary>
    private void SubscribeToBlock1Events()
    {
        if (block1Interactable != null)
        {
            // 订阅hover事件（用于检测是否正在hover方块1）
            block1Interactable.hoverEntered.AddListener(OnBlock1HoverEntered);
            block1Interactable.hoverExited.AddListener(OnBlock1HoverExited);
            // 不再订阅select事件，改为通过hover + 鼠标左键触发
            Debug.Log("VRBlockInteraction: 已订阅方块1的hover事件（交互通过hover + 鼠标左键触发）");
        }
        else
        {
            Debug.LogError("VRBlockInteraction: block1Interactable为null，无法订阅事件！");
        }
    }
    
    /// <summary>
    /// 方块1被悬停时的回调
    /// </summary>
    private void OnBlock1HoverEntered(HoverEnterEventArgs args)
    {
        // 检查是否是射线交互器（左手或右手）
        if (args.interactorObject is XRRayInteractor)
        {
            hoverCount++;
            Debug.Log($"========== VRBlockInteraction: ✅ 方块1被射线悬停！ ==========");
            Debug.Log($"交互器类型: {args.interactorObject.GetType().Name}");
            Debug.Log($"交互器对象: {args.interactorObject.transform?.name ?? "null"}");
            Debug.Log($"交互器游戏对象: {args.interactorObject.transform?.gameObject?.name ?? "null"}");
            Debug.Log($"当前hover计数: {hoverCount}（左手或右手射线）");
            Debug.Log("提示：现在可以按下鼠标左键触发交互");
        }
    }
    
    /// <summary>
    /// 方块1离开悬停时的回调
    /// </summary>
    private void OnBlock1HoverExited(HoverExitEventArgs args)
    {
        // 检查是否是射线交互器
        if (args.interactorObject is XRRayInteractor)
        {
            hoverCount = Mathf.Max(0, hoverCount - 1); // 确保不会小于0
            Debug.Log($"VRBlockInteraction: 方块1离开悬停 (交互器: {args.interactorObject.GetType().Name})");
            Debug.Log($"当前hover计数: {hoverCount}");
        }
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
        }
    }
}

