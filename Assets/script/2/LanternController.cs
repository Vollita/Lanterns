using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// 确保你的灯笼对象上有一个Renderer组件 (比如Mesh Renderer)
[RequireComponent(typeof(Renderer))]
public class LanternController : MonoBehaviour
{
    public AudioClip highlightSound; // 将高亮音效拖拽到这里
    public GameObject riddleUI; // 将显示谜题的UI Panel拖拽到这里
    public Vector3 uiOffset = new Vector3(0.01f, 0.01f, 0.01f); // UI相对于灯笼的位置偏移量
    private Material _lanternMaterialInstance; // 用于存储灯笼的材质实例
    private Color _originalEmissionColor;
    private bool _isHighlighted = false;
    private Renderer _renderer;

    void Start()
    {
        // 获取灯笼自身的Renderer组件
        _renderer = GetComponent<Renderer>();
        // 获取该Renderer正在使用的材质实例
        _lanternMaterialInstance = _renderer.material;

        // 保存原始的自发光颜色
        if (_lanternMaterialInstance.IsKeywordEnabled("_EMISSION"))
        {
            _originalEmissionColor = _lanternMaterialInstance.GetColor("_EmissionColor");
        }
        else
        {
            _originalEmissionColor = Color.black;
        }

        riddleUI.SetActive(false); // 初始时隐藏谜题UI
    }

    // 当射线进入时调用
    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!_isHighlighted)
        {
            _isHighlighted = true;
            _lanternMaterialInstance.EnableKeyword("_EMISSION");
            _lanternMaterialInstance.SetColor("_EmissionColor", Color.yellow * 2.0f);
            AudioSource.PlayClipAtPoint(highlightSound, transform.position);
            Debug.Log("00");
        }
    }

    // 当射线离开时调用
    public void OnHoverExited(HoverExitEventArgs args)
    {
        if (_isHighlighted && !riddleUI.activeSelf)
        {
            RestoreLantern();
        }
    }

    // 当扣动扳机键时调用
    public void OnActivate(ActivateEventArgs args)
    {
        Debug.Log("0");
        if (_isHighlighted && !riddleUI.activeSelf)
        {
            riddleUI.transform.position = transform.position + uiOffset;
            riddleUI.SetActive(true);

            AudioSource.Destroy(highlightSound);
            Debug.Log("1");

            // 获取UI面板上的AudioSource并播放它
            AudioSource uiAudio = riddleUI.GetComponent<AudioSource>();
            Debug.Log("21");
            if (uiAudio != null)
            {
                uiAudio.Play();
                Debug.Log("2");
            }
        }
    }

    // 恢复灯笼状态的公共方法
    public void RestoreLantern()
    {
        _isHighlighted = false;
        _lanternMaterialInstance.SetColor("_EmissionColor", _originalEmissionColor);
        if (_originalEmissionColor.Equals(Color.black))
        {
            _lanternMaterialInstance.DisableKeyword("_EMISSION");
        }
    }

    // 为了方便键盘测试
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.G) && _isHighlighted && !riddleUI.activeSelf)
        //{
        //    riddleUI.transform.position = transform.position + uiOffset;
        //    riddleUI.SetActive(true);
        //}
    }
}