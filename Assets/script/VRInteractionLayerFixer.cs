using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// VR交互层修复工具
/// 用于自动修复交互层不匹配的问题
/// </summary>
public class VRInteractionLayerFixer : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("VR Tools/修复交互层配置")]
    public static void FixInteractionLayers()
    {
        Debug.Log("========== 开始修复交互层配置 ==========");
        
        int fixedCount = 0;
        
        // 1. 修复所有XR Simple Interactable，设置为Default层
        XRSimpleInteractable[] interactables = FindObjectsOfType<XRSimpleInteractable>();
        Debug.Log($"找到 {interactables.Length} 个XR Simple Interactable");
        
        foreach (XRSimpleInteractable interactable in interactables)
        {
            var currentLayers = interactable.interactionLayers;
            uint currentValue = GetInteractionLayerValue(currentLayers);
            
            // 如果不是1（Default层），则修复
            if (currentValue != 1)
            {
                Debug.Log($"修复 {interactable.gameObject.name} 的交互层: {currentValue} -> 1 (Default)");
                SetInteractionLayerValue(interactable, 1);
                EditorUtility.SetDirty(interactable);
                fixedCount++;
            }
        }
        
        // 2. 修复所有XR Ray Interactor，设置为Default层
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>();
        Debug.Log($"找到 {rayInteractors.Length} 个XR Ray Interactor");
        
        foreach (XRRayInteractor rayInteractor in rayInteractors)
        {
            var currentLayers = rayInteractor.interactionLayers;
            uint currentValue = GetInteractionLayerValue(currentLayers);
            
            // 如果不是1（Default层），则修复
            if (currentValue != 1)
            {
                Debug.Log($"修复 {rayInteractor.gameObject.name} 的交互层: {currentValue} -> 1 (Default)");
                SetInteractionLayerValue(rayInteractor, 1);
                EditorUtility.SetDirty(rayInteractor);
                fixedCount++;
            }
        }
        
        Debug.Log($"========== 修复完成！共修复 {fixedCount} 个组件 ==========");
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("已保存更改");
        }
        else
        {
            Debug.Log("所有交互层配置都正确，无需修复");
        }
    }
    
    /// <summary>
    /// 获取InteractionLayerMask的内部值
    /// </summary>
    private static uint GetInteractionLayerValue(InteractionLayerMask layerMask)
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
    /// 设置InteractionLayerMask的内部值
    /// </summary>
    private static void SetInteractionLayerValue(Component component, uint value)
    {
        try
        {
            var layerMaskType = typeof(InteractionLayerMask);
            var valueField = layerMaskType.GetField("m_Bits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (valueField != null)
            {
                // 对于XRSimpleInteractable
                if (component is XRSimpleInteractable interactable)
                {
                    InteractionLayerMask layerMask = interactable.interactionLayers;
                    object boxedMask = layerMask;
                    valueField.SetValue(boxedMask, value);
                    interactable.interactionLayers = (InteractionLayerMask)boxedMask;
                }
                // 对于XRRayInteractor
                else if (component is XRRayInteractor rayInteractor)
                {
                    InteractionLayerMask layerMask = rayInteractor.interactionLayers;
                    object boxedMask = layerMask;
                    valueField.SetValue(boxedMask, value);
                    rayInteractor.interactionLayers = (InteractionLayerMask)boxedMask;
                }
            }
            else
            {
                Debug.LogError("无法设置InteractionLayerMask的值：找不到m_Bits字段");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置InteractionLayerMask失败: {e.Message}");
        }
    }
#endif
}

