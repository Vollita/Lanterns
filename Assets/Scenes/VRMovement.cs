using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class VRMovement : MonoBehaviour
{
    [Header("Input Action（摇杆）")]
    public InputActionProperty moveAction;  // 左手或右手的 Move 输入

    [Header("移动参数")]
    public float moveSpeed = 1.5f;

    private XROrigin xrOrigin;
    private CharacterController characterController;

    void Start()
    {
        xrOrigin = GetComponent<XROrigin>();

        // 给 XR Origin 自动加 CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
            characterController = gameObject.AddComponent<CharacterController>();

        characterController.height = xrOrigin.CameraInOriginSpaceHeight;
        characterController.center = xrOrigin.CameraInOriginSpacePos;
    }

    void Update()
    {
        UpdateCharacterHeight();
        Move();
    }

    void UpdateCharacterHeight()
    {
        // 保持角色胶囊跟随头显高度
        if (xrOrigin != null)
        {
            characterController.height = xrOrigin.CameraInOriginSpaceHeight;
            Vector3 center = xrOrigin.CameraInOriginSpacePos;
            center.y /= 2f;
            characterController.center = center;
        }
    }

    void Move()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Transform head = xrOrigin.Camera.transform;

        // 根据头部朝向移动（绕 Y 轴）
        Vector3 direction = head.forward * input.y + head.right * input.x;
        direction.y = 0f; // 禁止上下移动

        characterController.Move(direction * moveSpeed * Time.deltaTime);
    }
}
