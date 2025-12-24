using UnityEngine;

// 如果是 VR 环境，就引入 VR 相关的命名空间
#if UNITY_XR_MANAGEMENT && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE)
using UnityEngine.XR.Interaction.Toolkit;
#endif

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;

    // VR 模式下需要的变量
#if UNITY_XR_MANAGEMENT && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE)
    public XRController leftController;
    public XRController rightController;
    private Transform mainCamera;
    private Vector2 leftStickInput;
    private Vector2 rightStickInput;
#else
    // 非 VR 模式下需要的变量
    private float xRotation = 0f;
    private Camera mainCamera;
#endif

    void Start()
    {
#if UNITY_XR_MANAGEMENT && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE)
        // VR 模式初始化
        mainCamera = Camera.main.transform;
        if (leftController != null)
        {
            leftController.inputActions["Move"].performed += ctx => leftStickInput = ctx.ReadValue<Vector2>();
            leftController.inputActions["Move"].canceled += ctx => leftStickInput = Vector2.zero;
        }
        if (rightController != null)
        {
            rightController.inputActions["Look"].performed += ctx => rightStickInput = ctx.ReadValue<Vector2>();
            rightController.inputActions["Look"].canceled += ctx => rightStickInput = Vector2.zero;
        }
#else
        // 非 VR 模式初始化
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = GetComponentInChildren<Camera>();
#endif
    }

    void Update()
    {
#if UNITY_XR_MANAGEMENT && (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE)
        // VR 模式更新逻辑
        if (rightController != null)
        {
            float lookX = rightStickInput.x * Time.deltaTime * mouseSensitivity;
            float lookY = rightStickInput.y * Time.deltaTime * mouseSensitivity;

            Vector3 currentRotation = mainCamera.localEulerAngles;
            float newX = currentRotation.x - lookY;
            if (newX > 180f) newX -= 360f;
            newX = Mathf.Clamp(newX, -90f, 90f);
            mainCamera.localEulerAngles = new Vector3(newX, currentRotation.y + lookX, 0f);
        }

        if (leftController != null)
        {
            Vector3 moveDir = mainCamera.forward * leftStickInput.y + mainCamera.right * leftStickInput.x;
            moveDir.y = 0f;
            transform.Translate(moveDir * moveSpeed * Time.deltaTime);
        }
#else
        // 非 VR 模式更新逻辑
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        mainCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDir = transform.forward * vertical + transform.right * horizontal;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime);
#endif
    }
}