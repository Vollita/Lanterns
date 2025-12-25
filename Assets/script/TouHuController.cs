using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class TouHuController : MonoBehaviour
{
    [Header("投掷物设置")]
    public GameObject projectile; // 投掷物（空节点或已有物体）
    public Transform projectileSpawnPoint; // 投掷物初始/归位点
    private Quaternion projectileInitialRotation;
    private XRGrabInteractable grabInteractable;
    private Rigidbody projectileRb;
    private bool projectileScored = false; // 防止计分重复

    [Header("桶设置")]
    public GameObject bucket; // 装投掷物的桶（需要有 Trigger Collider）
    private Collider bucketCollider;

    [Header("投掷参数")]
    public float throwSpeedThreshold = 1.5f; // 投掷速度阈值（m/s）
    public float throwSpeedMultiplier = 1.5f; // 投掷速度倍数
    public float spinSpeed = 360f; // 旋转速度（度/秒）

    [Header("音效")]
    public AudioClip throwSound; // 投掷音效
    public AudioClip hitSound; // 命中音效
    public AudioClip missSound; // 失手音效
    private AudioSource audioSource;

    [Header("UI反馈")]
    public Canvas feedbackCanvas; // 反馈提示
    public TextMeshProUGUI feedbackText; // 反馈文字

    private int successCount = 0; // 成功计数
    private Vector3 lastControllerPos; // 上一帧手柄位置
    private Vector3 controllerVelocity; // 手柄速度
    private Transform grabInteractorTransform; // 抓取手柄的 Transform
    private bool isCurrentlyGrabbed = false;
    private bool hasThrown = false; // 是否已投掷

    void Start()
    {
        if (projectile == null)
        {
            Debug.LogError("ERROR: Projectile not assigned!");
            return;
        }

        grabInteractable = projectile.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = projectile.AddComponent<XRGrabInteractable>();
        }

        grabInteractable.selectEntered.AddListener(OnProjectileGrabbed);
        grabInteractable.selectExited.AddListener(OnProjectileReleased);

        projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb == null)
        {
            projectileRb = projectile.AddComponent<Rigidbody>();
        }

        projectileInitialRotation = projectile.transform.rotation;
        projectileScored = false;
        Debug.Log("Tou Hu system initialized");

        if (bucket != null)
        {
            bucketCollider = bucket.GetComponent<Collider>();
            if (bucketCollider != null && !bucketCollider.isTrigger)
            {
                bucketCollider.isTrigger = true;
            }

            // 添加 OnTriggerEnter 监听
            TriggerDetector triggerDetector = bucket.GetComponent<TriggerDetector>();
            if (triggerDetector == null)
            {
                triggerDetector = bucket.AddComponent<TriggerDetector>();
            }
            triggerDetector.onTriggerEnter = OnProjectileEnterBucket;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Canvas始终显示
        if (feedbackCanvas != null)
            feedbackCanvas.gameObject.SetActive(true);
        
        ShowText("Pick up");
    }

    void Update()
    {
        // 持续追踪手柄速度
        if (isCurrentlyGrabbed && grabInteractorTransform != null)
        {
            Vector3 currentPos = grabInteractorTransform.position;
            controllerVelocity = (currentPos - lastControllerPos) / Time.deltaTime;
            lastControllerPos = currentPos;
        }

        // 检测投掷物落地或停止（未进桶）
        if (hasThrown && !projectileScored && projectileRb != null)
        {
            if (projectileRb.velocity.magnitude < 0.1f && !projectileRb.isKinematic)
            {
                // 投掷物停止移动，判定为Miss
                ShowText("Miss!", missSound);
                Invoke(nameof(ResetProjectile), 1.5f);
                hasThrown = false;
            }
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnProjectileGrabbed);
            grabInteractable.selectExited.RemoveListener(OnProjectileReleased);
        }
    }

    // 生成投掷物
    void SpawnProjectile()
    {
        if (projectile == null)
        {
            Debug.LogError("ERROR: Projectile not assigned");
            return;
        }

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : projectile.transform.position;
        Quaternion spawnRot = projectileSpawnPoint != null ? projectileSpawnPoint.rotation : Quaternion.identity;

        projectile.transform.position = spawnPos;
        projectile.transform.rotation = spawnRot;
        projectileInitialRotation = spawnRot;
        projectileScored = false;

        if (projectileRb != null)
        {
            projectileRb.velocity = Vector3.zero;
            projectileRb.angularVelocity = Vector3.zero;
            projectileRb.isKinematic = true;
        }

        Debug.Log("Projectile reset to initial position");
    }

    // 被抓取事件
    void OnProjectileGrabbed(SelectEnterEventArgs args)
    {
        isCurrentlyGrabbed = true;
        hasThrown = false;
        grabInteractorTransform = args.interactor.transform;
        lastControllerPos = grabInteractorTransform.position;
        controllerVelocity = Vector3.zero;

        // 冻结物理，由手柄控制位置
        if (projectileRb != null)
        {
            projectileRb.isKinematic = true;
            projectileRb.velocity = Vector3.zero;
            projectileRb.angularVelocity = Vector3.zero;
        }

        ShowText("Throw!");
        Debug.Log("Projectile grabbed");
    }

    // 被释放事件
    void OnProjectileReleased(SelectExitEventArgs args)
    {
        if (!isCurrentlyGrabbed)
            return;

        isCurrentlyGrabbed = false;

        float throwSpeed = controllerVelocity.magnitude;
        Debug.Log($"Release speed: {throwSpeed:F2}m/s");

        // 判断是否为有效投掷
        if (throwSpeed >= throwSpeedThreshold)
        {
            PerformThrow(controllerVelocity);
            hasThrown = true;
        }
        else
        {
            Debug.Log($"Not enough force: {throwSpeed:F2}m/s < {throwSpeedThreshold}m/s");
            // 力度不足，归位重来
            Invoke(nameof(ResetProjectile), 0.5f);
        }

        grabInteractorTransform = null;
    }

    // 执行投掷
    void PerformThrow(Vector3 velocity)
    {
        if (projectileRb == null)
            return;

        // 解除 Kinematic，启用物理
        projectileRb.isKinematic = false;

        // 应用投掷速度（使用实际速度乘以倍数）
        Vector3 throwVelocity = velocity * throwSpeedMultiplier;
        projectileRb.velocity = throwVelocity;

        // 添加旋转
        projectileRb.angularVelocity = Random.insideUnitSphere * spinSpeed * Mathf.Deg2Rad;

        if (audioSource != null && throwSound != null)
            audioSource.PlayOneShot(throwSound, 0.6f);

        ShowText("");
        Debug.Log($"Throw! Speed: {throwVelocity.magnitude:F2}m/s");
    }

    // 进桶检测
    void OnProjectileEnterBucket(Collider other)
    {
        // 检查碰撞物体本身或其父物体是否是投掷物
        bool isProjectile = (other.gameObject == projectile) || (other.transform.parent != null && other.transform.parent.gameObject == projectile);
        
        if (isProjectile && !projectileScored)
        {
            projectileScored = true;
            hasThrown = false;
            successCount++;
            Debug.Log($"Hit! Success count: {successCount}");
            ShowText("Hit!", hitSound);

            // 延迟后归位
            Invoke(nameof(ResetProjectile), 1.5f);
        }
    }

    // 投掷物归位
    void ResetProjectile()
    {
        hasThrown = false;
        SpawnProjectile();
        ShowText("Pick up");
    }

    // 显示文字（Canvas始终显示，只改文字）
    void ShowText(string message, AudioClip clip = null)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, 0.7f);
    }
}

// 辅助类：用于 OnTriggerEnter 回调
public class TriggerDetector : MonoBehaviour
{
    public System.Action<Collider> onTriggerEnter;

    void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }
}
