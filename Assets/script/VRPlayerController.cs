using UnityEngine;
using UnityEngine.InputSystem;

public class VRPlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度（单位：米/秒）")]
    public float moveSpeed = 5f;
    [Tooltip("绑定左手柄的移动动作")]
    public InputActionReference moveActionRef;

    [Header("视角设置")]
    [Tooltip("视角旋转灵敏度")]
    public float lookSensitivity = 1f;
    [Tooltip("绑定右手柄的视角动作")]
    public InputActionReference lookActionRef;
    [Tooltip("主摄像机（XR Origin → Camera Offset → Main Camera）")]
    public Transform mainCamera;

    // 存储输入值
    private Vector2 moveInput;
    private Vector2 lookInput;

    void OnEnable()
    {
        // 订阅移动输入事件
        if (moveActionRef != null)
        {
            moveActionRef.action.performed += OnMoveInput;
            moveActionRef.action.canceled += OnMoveInputCanceled;
        }

        // 订阅视角输入事件
        if (lookActionRef != null)
        {
            lookActionRef.action.performed += OnLookInput;
            lookActionRef.action.canceled += OnLookInputCanceled;
        }
    }

    void OnDisable()
    {
        // 取消订阅（避免内存泄漏）
        if (moveActionRef != null)
        {
            moveActionRef.action.performed -= OnMoveInput;
            moveActionRef.action.canceled -= OnMoveInputCanceled;
        }

        if (lookActionRef != null)
        {
            lookActionRef.action.performed -= OnLookInput;
            lookActionRef.action.canceled -= OnLookInputCanceled;
        }
    }

    void Update()
    {
        // 更新视角旋转
        UpdateLookRotation();
        // 更新移动
        UpdateMovement();
    }

    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveInputCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    /// <summary>
    /// 处理视角输入
    /// </summary>
    private void OnLookInput(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookInputCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    /// <summary>
    /// 更新视角旋转（限制垂直旋转角度）
    /// </summary>
    private void UpdateLookRotation()
    {
        if (lookInput.magnitude < 0.1f || mainCamera == null) return;

        // 计算旋转角度（基于灵敏度和帧率）
        float yaw = lookInput.x * lookSensitivity * Time.deltaTime * 100f;
        float pitch = -lookInput.y * lookSensitivity * Time.deltaTime * 100f;

        // 获取当前摄像机旋转
        Vector3 currentRotation = mainCamera.localEulerAngles;
        // 处理垂直旋转（0-360 转 -180-180，避免角度跳跃）
        float newPitch = currentRotation.x + pitch;
        if (newPitch > 180f) newPitch -= 360f;
        // 限制垂直旋转在 -90°（低头）到 90°（仰头）之间
        newPitch = Mathf.Clamp(newPitch, -90f, 90f);

        // 应用旋转
        mainCamera.localEulerAngles = new Vector3(newPitch, currentRotation.y + yaw, 0f);
    }

    /// <summary>
    /// 更新移动（基于摄像机朝向）
    /// </summary>
    private void UpdateMovement()
    {
        if (moveInput.magnitude < 0.1f || mainCamera == null) return;

        // 获取摄像机的前向和右向（忽略 Y 轴，避免上下移动）
        Vector3 forward = mainCamera.forward;
        Vector3 right = mainCamera.right;
        forward.y = 0f;
        right.y = 0f;
        // 归一化向量（确保斜向移动速度与正向一致）
        forward.Normalize();
        right.Normalize();

        // 计算移动方向（摇杆 Y 轴控制前后，X 轴控制左右）
        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;
        // 应用移动
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);
    }
}
