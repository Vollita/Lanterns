using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class SignDrumController : MonoBehaviour
{
    [Header("签筒设置")]
    public GameObject signDrum; // 场景中的签筒GameObject
    private XRGrabInteractable grabInteractable;
    private Renderer drumRenderer;
    private Material drumMaterial;
    private Quaternion drumOriginalRotation;
    private Quaternion targetDrumRotation;

    [Header("摇晃设置")]
    public float shakeSensitivity = 5f;
    public float shakeCompleteThreshold = 0.9f; // 摇晃完成阈值
    public float rotationAngleThreshold = 30f; // 旋转角度阈值（度）
    public int shakeCountThreshold = 3; // 需要摇晃次数
    private float currentShakeIntensity = 0f;
    private Vector3 lastControllerPos;
    private bool isCurrentlyGrabbed = false;
    private bool shakeCompleted = false;
    private Quaternion lastDrumRotation;
    private int shakeCount = 0;
    private bool signSpawned = false; // 签条是否已生成

    [Header("旋转动画")]
    public float rotationSpeed = 300f;

    [Header("音效")]
    public AudioClip shakeSound;
    public AudioClip successSound;
    private AudioSource audioSource;

    [Header("签条预制体")]
    public GameObject signPrefab;
    public float signSpawnForce = 8f;

    [Header("UI提示框")]
    public Canvas tipCanvas; // 提示框Canvas（一个）
    public TextMeshProUGUI shakeText; // 摇晃提示文字
    public TextMeshProUGUI clickText; // 点击提示文字

    [Header("祝福语UI")]
    public Canvas blessingCanvas; // 祝福语Canvas

    private Transform controllerTransform;
    private Transform cameraTransform;

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
                drumMaterial = new Material(drumRenderer.material); // 复制材质避免影响其他物体
                
            drumOriginalRotation = signDrum.transform.rotation;
            targetDrumRotation = drumOriginalRotation;
            Debug.Log($"✅ 已获取签筒");
        }
        else
        {
            Debug.LogError($"❌ 签筒未赋值，请在Inspector中拖入签筒GameObject");
        }

        controllerTransform = GetComponent<Transform>();
        cameraTransform = Camera.main.transform;
        audioSource = GetComponent<AudioSource>();
        lastControllerPos = controllerTransform.position;

        // 初始化UI
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
        lastControllerPos = controllerTransform.position;
        lastDrumRotation = signDrum.transform.rotation;
        targetDrumRotation = drumOriginalRotation;
        
        // 显示"摇晃"提示
        ShowShakeTip();
        
        Debug.Log("✋ 已抓住签筒！开始摇晃");
    }

    // 被释放事件
    void OnReleased(SelectExitEventArgs args)
    {
        isCurrentlyGrabbed = false;
        currentShakeIntensity = 0f;
        targetDrumRotation = signDrum.transform.rotation;
        
        // 隐藏提示框
        HideTip();
        
        // 关闭摇晃光效
        SetShakeGlow(false);
        
        Debug.Log("🔓 已释放签筒");
    }

    // 被激活事件（按下Activate按钮）
    void OnActivate(ActivateEventArgs args)
    {
        if (!isCurrentlyGrabbed)
            return;

        // 摇晃完成后，第一次点击：生成签条
        if (shakeCompleted && !signSpawned)
        {
            Debug.Log("🎯 第一次点击！生成签条");
            HideTip();
            SpawnSign();
            signSpawned = true;
            return;
        }

        // 签条已生成，第二次点击：显示祝福语UI
        if (signSpawned)
        {
            Debug.Log("📜 第二次点击！显示祝福语");
            ShowBlessingUI();
        }
    }

    void Update()
    {
        if (signDrum == null)
            return;

        // 抓取中时：检测摇晃
        if (isCurrentlyGrabbed)
        {
            DetectShake();
            RotateSignDrum();
        }
    }

    // 检测摇晃强度
    void DetectShake()
    {
        if (signDrum == null)
            return;

        // 计算签筒的旋转角度变化
        Quaternion rotationDelta = signDrum.transform.rotation * Quaternion.Inverse(lastDrumRotation);
        
        // 转换为轴角表示法
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
        
        // 角度正规化到0-180
        if (angle > 180f)
            angle = 360f - angle;

        // 达到旋转阈值时计数
        if (angle >= rotationAngleThreshold)
        {
            shakeCount++;
            currentShakeIntensity = (float)shakeCount / shakeCountThreshold;
            currentShakeIntensity = Mathf.Clamp01(currentShakeIntensity);

            // 播放摇晃音效
            if (shakeSound != null)
            {
                audioSource.PlayOneShot(shakeSound, 0.4f);
            }

            // 摇晃光效
            SetShakeGlow(true);
            
            Debug.Log($"摇晃检测到！次数: {shakeCount}/{shakeCountThreshold}");
        }
        else
        {
            // 角度过小则逐渐减弱光效
            SetShakeGlow(false);
        }

        // 达到完成阈值时更新提示
        if (shakeCount >= shakeCountThreshold && !shakeCompleted)
        {
            shakeCompleted = true;
            currentShakeIntensity = 1f;
            ShowClickTip();
            Debug.Log("✅ 摇晃完成！");
        }

        lastDrumRotation = signDrum.transform.rotation;
        Debug.Log($"摇晃强度: {currentShakeIntensity * 100:F0}% | 旋转角度: {angle:F1}°");
    }

    // 摇晃光效
    void SetShakeGlow(bool enable)
    {
        if (drumMaterial == null)
            return;

        if (enable)
        {
            // 弱黄色发光
            drumMaterial.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f, 1f) * (0.3f + currentShakeIntensity * 0.5f));
        }
        else
        {
            drumMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    // 旋转签筒
    void RotateSignDrum()
    {
        if (signDrum == null)
            return;

        float rotateAmount = currentShakeIntensity * rotationSpeed;
        targetDrumRotation *= Quaternion.Euler(
            rotateAmount * Time.deltaTime * 0.7f,
            rotateAmount * Time.deltaTime,
            rotateAmount * Time.deltaTime * 0.3f
        );

        signDrum.transform.rotation = Quaternion.Lerp(signDrum.transform.rotation, targetDrumRotation, Time.deltaTime * 3f);
    }

    // 生成签条
    void SpawnSign()
    {
        if (signPrefab == null)
        {
            Debug.LogError("❌ 签条预制体未赋值");
            return;
        }

        // 位置：玩家眼前1.5米处
        Vector3 spawnPos = cameraTransform.position + cameraTransform.forward * 1.5f;
        
        // 旋转：竖直着放，与玩家面对面
        Quaternion spawnRotation = Quaternion.LookRotation(cameraTransform.forward);

        GameObject newSign = Instantiate(signPrefab, spawnPos, spawnRotation);

        // 添加签条发光闪烁效果
        Renderer signRenderer = newSign.GetComponent<Renderer>();
        if (signRenderer != null)
        {
            Material signMat = signRenderer.material;
            StartCoroutine(SignFlashGlow(signMat, 5f)); // 闪烁5秒，等待玩家第二次点击
        }

        Rigidbody signRb = newSign.GetComponent<Rigidbody>();
        if (signRb != null)
        {
            // 轻微向上飘动
            signRb.velocity = Vector3.up * 0.5f;
            signRb.angularVelocity = Vector3.zero;
        }

        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound, 0.8f);
        }

        Debug.Log("🎯 签条飘出！再点击一次查看祝福语");
    }

    // 签条闪烁发光
    System.Collections.IEnumerator SignFlashGlow(Material signMat, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // 闪烁效果：0.5秒闪一次
            float flashIntensity = Mathf.Sin(elapsedTime * Mathf.PI * 2f / 0.5f) * 0.5f + 0.5f;
            Color glowColor = Color.white * flashIntensity;
            signMat.SetColor("_EmissionColor", glowColor);
            
            yield return null;
        }
        
        signMat.SetColor("_EmissionColor", Color.black);
    }

    // 显示"摇晃"提示
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

    // 显示"点击"提示
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

    // 隐藏提示框
    void HideTip()
    {
        if (tipCanvas != null)
            tipCanvas.gameObject.SetActive(false);
    }

    // 显示祝福语UI
    void ShowBlessingUI()
    {
        if (blessingCanvas != null)
        {
            blessingCanvas.gameObject.SetActive(true);
            // 这里可以随机显示不同的祝福语
        }
    }
}