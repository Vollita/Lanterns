using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class SignDrumController : MonoBehaviour
{
    [Header("签筒设置")]
    public GameObject signDrum;
    private XRGrabInteractable grabInteractable;
    private Renderer drumRenderer;
    private Material drumMaterial;

    [Header("摇晃设置（基于旋转和平动）")]
    public float rotationAngleThreshold = 30f; // 旋转角度阈值（度）
    public float movementSpeedThreshold = 2f; // 手柄移动速度阈值
    public int shakeCountThreshold = 3; // 需要摇晃次数
    private float currentShakeIntensity = 0f;
    private bool isCurrentlyGrabbed = false;
    private bool shakeCompleted = false;
    private Vector3 lastControllerPos;
    private Quaternion lastDrumRotation;
    private int shakeCount = 0;
    private bool signSpawned = false;

    [Header("旋转动画")]
    public float rotationSpeed = 300f;

    [Header("音效")]
    public AudioClip shakeSound;
    public AudioClip successSound;
    private AudioSource audioSource;

    [Header("签条预制体")]
    public GameObject signPrefab;
    private GameObject spawnedSign;
    private XRSimpleInteractable signInteractable;

    [Header("UI提示框")]
    public Canvas tipCanvas;
    public TextMeshProUGUI shakeText;
    public TextMeshProUGUI clickText;

    [Header("祝福语UI")]
    public Canvas blessingCanvas;

    private Transform cameraTransform;
    private Transform grabInteractorTransform;
    private bool signSelected = false;

    void Start()
    {
        if (signDrum != null)
        {
            grabInteractable = signDrum.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                Debug.LogError("❌ 签筒上缺少 XRGrabInteractable 组件！");
                return;
            }

            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            grabInteractable.activated.AddListener(OnActivate);

            drumRenderer = signDrum.GetComponent<Renderer>();
            if (drumRenderer != null)
                drumMaterial = new Material(drumRenderer.material);

            Debug.Log($"✅ 已获取签筒");
        }
        else
        {
            Debug.LogError($"❌ 签筒未赋值");
        }

        cameraTransform = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (tipCanvas != null)
            tipCanvas.gameObject.SetActive(false);
        if (blessingCanvas != null)
            blessingCanvas.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
            grabInteractable.activated.RemoveListener(OnActivate);
        }
    }

    // 被抓取事件
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isCurrentlyGrabbed = true;
        shakeCompleted = false;
        signSpawned = false;
        currentShakeIntensity = 0f;
        shakeCount = 0;
        lastControllerPos = args.interactorObject.transform.position;
        lastDrumRotation = signDrum.transform.rotation;
        grabInteractorTransform = args.interactorObject.transform;

        ShowShakeTip();
        Debug.Log("✋ 已抓住签筒！开始摇晃");
    }

    // 被释放事件
    void OnReleased(SelectExitEventArgs args)
    {
        isCurrentlyGrabbed = false;
        currentShakeIntensity = 0f;
        grabInteractorTransform = null;

        HideTip();
        SetShakeGlow(false);

        Debug.Log("🔓 已释放签筒");
    }

    // 被激活事件
    void OnActivate(ActivateEventArgs args)
    {
        if (!isCurrentlyGrabbed)
            return;

        // 第一次点击：生成签条
        if (shakeCompleted && !signSpawned)
        {
            Debug.Log("🎯 第一次点击！生成签条");
            HideTip();
            SpawnSign();
            signSpawned = true;
            return;
        }

        // 第二次点击：显示祝福语
        if (signSpawned && spawnedSign != null)
        {
            if (signInteractable != null && (signInteractable.isSelected || signInteractable.isHovered))
            {
                Debug.Log("📜 点击签条！显示祝福语");
                ShowBlessingUI();
            }
            else
            {
                Debug.Log("⚠️ 请先用激光对准签条再点击");
            }
        }
    }

    void Update()
    {
        if (signDrum == null)
            return;

        if (isCurrentlyGrabbed && grabInteractorTransform != null)
        {
            DetectShake();
        }

        // 签条被选中后，检测鼠标左键点击
        if (signSelected && Input.GetMouseButtonDown(0))
        {
            ShowBlessingUI();
            signSelected = false;
        }
    }

    // 检测摇晃（基于旋转角度和平动速度，任意一个达到阈值就算一次）
    void DetectShake()
    {
        if (grabInteractorTransform == null || signDrum == null)
            return;

        bool shakeDetected = false;

        // 检测签筒旋转
        Quaternion rotationDelta = signDrum.transform.rotation * Quaternion.Inverse(lastDrumRotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
            angle = 360f - angle;

        if (angle >= rotationAngleThreshold)
        {
            shakeDetected = true;
            Debug.Log($"检测到旋转摇晃：{angle:F1}°");
        }

        // 检测手柄平动速度
        Vector3 currentPos = grabInteractorTransform.position;
        Vector3 positionDelta = currentPos - lastControllerPos;
        float movementSpeed = positionDelta.magnitude / Time.deltaTime;

        if (movementSpeed >= movementSpeedThreshold)
        {
            shakeDetected = true;
            Debug.Log($"检测到平动摇晃：速度 {movementSpeed:F2}m/s");
        }

        // 如果检测到任意摇晃方式
        if (shakeDetected)
        {
            shakeCount++;
            currentShakeIntensity = (float)shakeCount / shakeCountThreshold;
            currentShakeIntensity = Mathf.Clamp01(currentShakeIntensity);

            if (shakeSound != null && audioSource != null)
                audioSource.PlayOneShot(shakeSound, 0.4f);

            SetShakeGlow(true);
            Debug.Log($"摇晃检测到！次数: {shakeCount}/{shakeCountThreshold}");
        }
        else
        {
            // 逐渐衰减光效
            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, Time.deltaTime * 2f);
            
            if (currentShakeIntensity < 0.2f)
                SetShakeGlow(false);
        }

        // 完成摇晃
        if (shakeCount >= shakeCountThreshold && !shakeCompleted)
        {
            shakeCompleted = true;
            currentShakeIntensity = 1f;
            ShowClickTip();
            Debug.Log("✅ 摇晃完成！");
        }

        lastControllerPos = currentPos;
        lastDrumRotation = signDrum.transform.rotation;
    }

    // 摇晃光效
    void SetShakeGlow(bool enable)
    {
        if (drumMaterial == null)
            return;

        if (enable)
        {
            drumMaterial.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f, 1f) * (0.3f + currentShakeIntensity * 0.5f));
        }
        else
        {
            drumMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    // 生成签条
    void SpawnSign()
    {
        if (signPrefab == null)
        {
            Debug.LogError("❌ 签条预制体未赋值");
            return;
        }

        Vector3 spawnPos = cameraTransform.position + cameraTransform.forward * 1.5f;
        Quaternion spawnRotation = Quaternion.LookRotation(cameraTransform.forward);

        spawnedSign = Instantiate(signPrefab, spawnPos, spawnRotation);

        // 确保有Collider
        if (spawnedSign.GetComponent<Collider>() == null)
            spawnedSign.AddComponent<BoxCollider>();

        // 获取或添加 XRSimpleInteractable
        signInteractable = spawnedSign.GetComponent<XRSimpleInteractable>();
        if (signInteractable == null)
            signInteractable = spawnedSign.AddComponent<XRSimpleInteractable>();

        signInteractable.hoverEntered.AddListener(OnSignHovered);
        signInteractable.hoverExited.AddListener(OnSignHoverExited);

        // 闪烁效果
        Renderer signRenderer = spawnedSign.GetComponent<Renderer>();
        if (signRenderer != null)
            StartCoroutine(SignFlashGlow(signRenderer.material));

        // 物理效果
        Rigidbody signRb = spawnedSign.GetComponent<Rigidbody>();
        if (signRb != null)
        {
            signRb.velocity = Vector3.up * 0.5f;
            signRb.angularVelocity = Vector3.zero;
        }

        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound, 0.8f);

        Debug.Log("🎯 签条飘出！用激光对准后再点击查看祝福语");
    }

    // 签条被悬停（激光对准）
    void OnSignHovered(HoverEnterEventArgs args)
    {
        signSelected = true;
        Debug.Log("✨ 激光对准签条！用鼠标左键点击查看祝福语");
    }

    // 签条悬停结束（激光移开）
    void OnSignHoverExited(HoverExitEventArgs args)
    {
        signSelected = false;
        Debug.Log("激光移开签条");
    }

    // 闪烁效果
    System.Collections.IEnumerator SignFlashGlow(Material signMat)
    {
        float elapsedTime = 0f;

        while (spawnedSign != null && blessingCanvas != null && !blessingCanvas.gameObject.activeSelf)
        {
            elapsedTime += Time.deltaTime;
            float flashIntensity = Mathf.Sin(elapsedTime * Mathf.PI * 4f) * 0.5f + 0.5f;
            Color glowColor = Color.white * flashIntensity;
            signMat.SetColor("_EmissionColor", glowColor);
            yield return null;
        }

        if (signMat != null)
            signMat.SetColor("_EmissionColor", Color.black);
    }

    void ShowShakeTip()
    {
        if (tipCanvas != null)
        {
            tipCanvas.gameObject.SetActive(true);
            if (shakeText != null)
                shakeText.gameObject.SetActive(true);
            if (clickText != null)
                clickText.gameObject.SetActive(false);
        }
    }

    void ShowClickTip()
    {
        if (tipCanvas != null)
        {
            tipCanvas.gameObject.SetActive(true);
            if (shakeText != null)
                shakeText.gameObject.SetActive(false);
            if (clickText != null)
                clickText.gameObject.SetActive(true);
        }
    }

    void HideTip()
    {
        if (tipCanvas != null)
            tipCanvas.gameObject.SetActive(false);
    }

    void ShowBlessingUI()
    {
        if (blessingCanvas != null)
            blessingCanvas.gameObject.SetActive(true);
    }
}
