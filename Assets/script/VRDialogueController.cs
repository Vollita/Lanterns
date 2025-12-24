using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// VR对话控制器 - 管理对话图片的显示和切换
/// 使用VR射线点击任意位置来切换到下一张图片
/// </summary>
public class VRDialogueController : MonoBehaviour
{
    [Header("对话图片设置")]
    [Tooltip("对话图片数组，按顺序显示")]
    public Sprite[] dialogueSprites;
    
    [Header("UI组件")]
    [Tooltip("用于显示对话图片的Image组件")]
    public Image dialogueImage;
    
    [Tooltip("对话Canvas（建议设置为World Space）")]
    public Canvas dialogueCanvas;
    
    [Header("交互设置")]
    [Tooltip("XR射线交互器（用于检测点击）")]
    public XRRayInteractor rayInteractor;
    
    [Tooltip("是否使用UI交互（如果使用Canvas的UI元素）")]
    public bool useUIInteraction = false;
    
    [Header("显示设置")]
    [Tooltip("图片透明度（0-1）")]
    [Range(0f, 1f)]
    public float imageAlpha = 0.8f;
    
    [Tooltip("对话Canvas距离摄像机的距离")]
    public float canvasDistance = 3f;
    
    [Tooltip("是否自动面向摄像机")]
    public bool faceCamera = true;
    
    [Header("自动启动")]
    [Tooltip("场景开始时自动开始对话")]
    public bool autoStartOnSceneLoad = true;
    
    // 私有变量
    private int currentImageIndex = -1;
    private bool isDialogueActive = false;
    private Camera mainCamera;
    private XRRayInteractor[] rayInteractors;
    
    void Start()
    {
        InitializeDialogue();
    }
    
    void Update()
    {
        // 让Canvas面向摄像机
        if (faceCamera && dialogueCanvas != null && mainCamera != null && isDialogueActive)
        {
            dialogueCanvas.transform.LookAt(dialogueCanvas.transform.position + mainCamera.transform.forward);
        }
        
        // 检测VR点击输入
        if (isDialogueActive)
        {
            CheckForInput();
        }
    }
    
    /// <summary>
    /// 初始化对话系统
    /// </summary>
    private void InitializeDialogue()
    {
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 如果没有指定rayInteractor，尝试自动查找
        if (rayInteractor == null)
        {
            rayInteractor = FindObjectOfType<XRRayInteractor>();
        }
        
        // 如果没有指定Canvas，尝试自动查找
        if (dialogueCanvas == null)
        {
            dialogueCanvas = GetComponentInParent<Canvas>();
            if (dialogueCanvas == null)
            {
                dialogueCanvas = FindObjectOfType<Canvas>();
            }
        }
        
        // 如果没有指定Image，尝试自动查找
        if (dialogueImage == null)
        {
            dialogueImage = GetComponentInChildren<Image>();
        }
        
        // 设置Canvas为World Space（如果还没有）
        if (dialogueCanvas != null && dialogueCanvas.renderMode != RenderMode.WorldSpace)
        {
            SetupWorldSpaceCanvas();
        }
        
        // 初始隐藏对话UI
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
        }
        
        // 设置图片透明度
        if (dialogueImage != null)
        {
            Color imageColor = dialogueImage.color;
            imageColor.a = imageAlpha;
            dialogueImage.color = imageColor;
        }
        
        // 如果启用自动启动，延迟一帧后开始对话（确保所有组件都已初始化）
        if (autoStartOnSceneLoad)
        {
            Invoke(nameof(StartDialogue), 0.1f);
        }
    }
    
    /// <summary>
    /// 设置Canvas为World Space模式
    /// </summary>
    private void SetupWorldSpaceCanvas()
    {
        dialogueCanvas.renderMode = RenderMode.WorldSpace;
        
        // 设置Canvas位置在摄像机前方
        if (mainCamera != null)
        {
            dialogueCanvas.transform.position = mainCamera.transform.position + mainCamera.transform.forward * canvasDistance;
            dialogueCanvas.transform.LookAt(mainCamera.transform);
            dialogueCanvas.transform.Rotate(0, 180, 0); // 翻转180度使其面向摄像机
        }
        
        // 设置Canvas尺寸
        RectTransform canvasRect = dialogueCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = new Vector2(2f, 2f); // 可以根据需要调整大小
        }
        
        // 添加Canvas Collider以便射线检测（如果需要）
        if (useUIInteraction)
        {
            CanvasScaler scaler = dialogueCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = dialogueCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            GraphicRaycaster raycaster = dialogueCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = dialogueCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // 添加Collider用于UI交互
            BoxCollider boxCollider = dialogueCanvas.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = dialogueCanvas.gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(2f, 2f, 0.01f);
            }
        }
    }
    
    /// <summary>
    /// 开始对话（在场景开始时调用）
    /// </summary>
    public void StartDialogue()
    {
        if (dialogueSprites == null || dialogueSprites.Length == 0)
        {
            Debug.LogWarning("VRDialogueController: 没有设置对话图片！");
            return;
        }
        
        if (dialogueImage == null)
        {
            Debug.LogError("VRDialogueController: 没有找到Image组件！");
            return;
        }
        
        // 显示Canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }
        
        // 重置索引并显示第一张图片
        currentImageIndex = 0;
        isDialogueActive = true;
        ShowCurrentImage();
        
        Debug.Log($"开始对话，共有 {dialogueSprites.Length} 张图片");
    }
    
    /// <summary>
    /// 显示当前索引的图片
    /// </summary>
    private void ShowCurrentImage()
    {
        if (currentImageIndex >= 0 && currentImageIndex < dialogueSprites.Length && dialogueImage != null)
        {
            dialogueImage.sprite = dialogueSprites[currentImageIndex];
            dialogueImage.gameObject.SetActive(true);
            Debug.Log($"显示对话图片 {currentImageIndex + 1}/{dialogueSprites.Length}");
        }
    }
    
    /// <summary>
    /// 检测用户输入（VR点击）
    /// 注意：VR模拟器会将VR控制器的输入转换为鼠标输入，所以使用鼠标点击检测即可
    /// </summary>
    private void CheckForInput()
    {
        bool inputDetected = false;
        
        // 检测鼠标左键点击（VR模拟器和编辑器测试都使用这个）
        // VR模拟器会将VR控制器的选择按钮映射为鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }
        
        // 检测空格键（用于编辑器测试的备用方法）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputDetected = true;
        }
        
        if (inputDetected)
        {
            NextImage();
        }
    }
    
    /// <summary>
    /// 切换到下一张图片
    /// </summary>
    private void NextImage()
    {
        if (!isDialogueActive) return;
        
        currentImageIndex++;
        
        if (currentImageIndex >= dialogueSprites.Length)
        {
            // 所有图片显示完毕，结束对话
            EndDialogue();
        }
        else
        {
            // 显示下一张图片
            ShowCurrentImage();
        }
    }
    
    /// <summary>
    /// 结束对话
    /// </summary>
    private void EndDialogue()
    {
        isDialogueActive = false;
        currentImageIndex = -1;
        
        // 隐藏对话UI
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log("对话结束");
    }
    
    /// <summary>
    /// 跳过对话（外部调用）
    /// </summary>
    public void SkipDialogue()
    {
        EndDialogue();
    }
    
    /// <summary>
    /// 重新开始对话（外部调用）
    /// </summary>
    public void RestartDialogue()
    {
        StartDialogue();
    }
}

