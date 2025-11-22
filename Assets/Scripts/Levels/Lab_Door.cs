using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using TMPro;
using System.Collections;

public class Lab_Door : MonoBehaviour
{
    private GameObject player;
    private Inventory playerInventory;
    private Player_InputHandler inputHandler;
    [SerializeField] private ItemData keyCard;

    [Header("Entrance Area")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(2f, 2f);

    [Header("Teleport")]
    public Lab_Door linkedDoor;
    [SerializeField] private float teleportCooldown = 1f;
    [HideInInspector] public float lastTeleportTime = -10f;
    [SerializeField] public Transform exitPoint;

    [Header("Visuals")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private bool snapCinemachineOnTeleport = true;

    [Header("UI Message Settings")]
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] private string noKeyMessage = "Você precisa do Keycard para abrir esta porta!";
    [SerializeField] private int fontSize = 32;
    [SerializeField] private Color messageColor = Color.white;
    [SerializeField] private Color panelColor = new Color(0, 0, 0, 0.7f);
    
    private Coroutine messageCoroutine;

    private Canvas _fadeCanvas;
    private UnityEngine.UI.Image _fadeImage;
    private Coroutine _fadeRoutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerInventory = player.GetComponent<Inventory>();
        inputHandler = player.GetComponent<Player_InputHandler>();
    }

    void Update()
    {
        if (inputHandler.InteractActionTriggered)
        {
            EnterLab();
        }
    }

    private void EnterLab()
    {
        if (!IsPlayerInArea()) return;
        
        if (!HaveKey())
        {
            ShowMessage(noKeyMessage);
            return;
        }

        if (linkedDoor != null)
        {
            if (Time.time - lastTeleportTime < teleportCooldown) return;
            if (Time.time - linkedDoor.lastTeleportTime < teleportCooldown) return;

            if (useFade)
            {
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(DoTeleportWithFade(linkedDoor));
            }
            else
            {
                TeleportPlayerTo(linkedDoor);
            }
            return;
        }
    }

    private bool HaveKey()
    {
        int itemNum = playerInventory.GetItemQuantity(keyCard);

        if (itemNum < 1)
        {
            Debug.Log("A Card is Required");
            return false;
        }
        return true;
    }

    private void ShowMessage(string message)
    {
        // Cancelar mensagem anterior se existir
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }
    
    private IEnumerator DisplayMessageCoroutine(string message)
    {
        // Criar Canvas
        GameObject canvasObj = new GameObject("MessageCanvas_LabDoor");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Criar Panel (fundo semi-transparente)
        GameObject panelObj = new GameObject("MessagePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = panelColor;
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.8f);
        panelRect.anchorMax = new Vector2(0.8f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Criar Texto
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = message;
        textComponent.fontSize = fontSize;
        textComponent.color = messageColor;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = true;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        
        // Aguardar duração
        yield return new WaitForSeconds(messageDuration);
        
        // Destruir canvas
        Destroy(canvasObj);
        
        messageCoroutine = null;
    }

    private bool IsPlayerInArea()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        Vector3 localPos = transform.InverseTransformPoint(player.transform.position);

        Vector2 half = areaSize * 0.5f;
        float minX = areaCenter.x - half.x;
        float maxX = areaCenter.x + half.x;
        float minY = areaCenter.y - half.y;
        float maxY = areaCenter.y + half.y;

        return (localPos.x >= minX && localPos.x <= maxX && localPos.y >= minY && localPos.y <= maxY);
    }

    private void TeleportPlayerTo(Lab_Door dest)
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || dest == null) return;

        Vector3 oldPos = player.transform.position;

        Vector3 finalPos;
        if (dest.exitPoint != null)
        {
            finalPos = dest.exitPoint.position;
        }
        else
        {
            finalPos = dest.transform.TransformPoint(dest.areaCenter);
        }

        finalPos += Vector3.up * 0.05f;

        finalPos.z = 0f;
        player.transform.position = finalPos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (snapCinemachineOnTeleport)
        {
            Vector3 delta = player.transform.position - oldPos;

            var mainCam = Camera.main;
            CinemachineBrain brain = null;
            if (mainCam != null) brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            var vcs = UnityEngine.Object.FindObjectsByType<CinemachineVirtualCameraBase>(UnityEngine.FindObjectsSortMode.None);
            foreach (var v in vcs)
            {
                if (v == null) continue;
                if (v.Follow == player.transform || v.LookAt == player.transform)
                {
                    v.OnTargetObjectWarped(player.transform, delta);
                }
            }

            if (brain != null) brain.enabled = true;
        }

        lastTeleportTime = Time.time;
        dest.lastTeleportTime = Time.time;
    }

    private void EnsureFader()
    {
        if (_fadeCanvas != null && _fadeImage != null) return;
        var go = new GameObject("LabDoorFader");
        DontDestroyOnLoad(go);
        _fadeCanvas = go.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = short.MaxValue;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var imgGo = new GameObject("Fade");
        imgGo.transform.SetParent(go.transform, false);
        _fadeImage = imgGo.AddComponent<UnityEngine.UI.Image>();
        _fadeImage.color = Color.black;
        _fadeImage.raycastTarget = true;
        var rt = _fadeImage.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        SetFadeAlpha(0f);
    }

    private void SetFadeAlpha(float a)
    {
        if (_fadeImage == null) return;
        var c = _fadeImage.color; c.a = Mathf.Clamp01(a); _fadeImage.color = c;
        _fadeImage.raycastTarget = c.a > 0.001f;
    }

    private System.Collections.IEnumerator FadeTo(float target, float duration)
    {
        EnsureFader();
        float start = _fadeImage.color.a; float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            SetFadeAlpha(a);
            yield return null;
        }
        SetFadeAlpha(target);
    }

    private System.Collections.IEnumerator DoTeleportWithFade(Lab_Door dest)
    {
        yield return FadeTo(1f, fadeOutDuration);

        Canvas.ForceUpdateCanvases();

        TeleportPlayerTo(dest);

        yield return null;

        yield return FadeTo(0f, fadeInDuration);

        _fadeRoutine = null;
    }

    void OnDrawGizmos()
    {
        Vector3 worldCenter = transform.TransformPoint(areaCenter);
        Vector3 worldSize = new Vector3(areaSize.x, areaSize.y, 0.01f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(worldCenter, worldSize);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            bool inside = false;
            if (Application.isPlaying)
            {
                inside = IsPlayerInArea();
            }
            else
            {
                Vector3 localPos = transform.InverseTransformPoint(p.transform.position);
                Vector2 half = areaSize * 0.5f;
                inside = (localPos.x >= areaCenter.x - half.x && localPos.x <= areaCenter.x + half.x
                          && localPos.y >= areaCenter.y - half.y && localPos.y <= areaCenter.y + half.y);
            }

            if (inside)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(p.transform.position, 0.08f);
            }
        }
    }
}