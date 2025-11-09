using UnityEngine;
using UnityEngine.Events;

public class Pressure_Plate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform buttonTransform = null;

    [Header("Detection")]
    [SerializeField] private string presserTag = "Player";

    [Header("Movement")]
    [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField] private float moveSpeed = 6f;

    [Header("Behavior")]
    [SerializeField] private bool pressOnce = false;
    [SerializeField] private bool requireHold = false;
    [SerializeField] private bool toggleOnPress = false;
    [SerializeField] private float autoResetDelay = 0f;

    [Header("Events")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    private Vector3 initialLocalPos;
    private Vector3 pressedLocalPos;
    private bool isPressed = false;
    private bool lockedPressed = false;
    private int presserCount = 0;
    private Coroutine moveRoutine = null;
    private Coroutine autoResetRoutine = null;

    void Reset()
    {
        if (buttonTransform == null && transform.childCount > 0)
            buttonTransform = transform.Find("Button") ?? transform.GetChild(0);
    }

    void Start()
    {
        if (buttonTransform == null)
        {
            Debug.LogWarning($"Pressure_Plate '{name}' has no Button transform assigned.", this);
            buttonTransform = transform;
        }

        initialLocalPos = buttonTransform.localPosition;
        pressedLocalPos = initialLocalPos + pressedLocalOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(presserTag)) return;

        presserCount++;
        if (lockedPressed) return;

        if (toggleOnPress)
        {
            SetPressed(!isPressed);
        }
        else if (requireHold)
        {
            SetPressed(true);
        }
        else
        {
            SetPressed(true);
            if (autoResetDelay > 0f)
            {
                if (autoResetRoutine != null) StopCoroutine(autoResetRoutine);
                autoResetRoutine = StartCoroutine(AutoReset(autoResetDelay));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(presserTag)) return;

        presserCount = Mathf.Max(0, presserCount - 1);
        if (lockedPressed) return;

        if (toggleOnPress)
        {
            return;
        }
        else if (requireHold)
        {
            if (presserCount == 0) SetPressed(false);
        }
        else
        {
            if (presserCount == 0)
            {
                SetPressed(false);
            }
        }
    }

    private void SetPressed(bool pressed)
    {
        if (pressed == isPressed) return;

        isPressed = pressed;

        if (isPressed)
        {
            if (autoResetRoutine != null) { StopCoroutine(autoResetRoutine); autoResetRoutine = null; }

            if (pressOnce)
            {
                lockedPressed = true;
            }

            onPressed?.Invoke();
            StartMoveTo(pressedLocalPos);
        }
        else
        {
            onReleased?.Invoke();
            StartMoveTo(initialLocalPos);
        }
    }

    private void StartMoveTo(Vector3 targetLocalPos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveButtonRoutine(targetLocalPos));
    }

    private System.Collections.IEnumerator MoveButtonRoutine(Vector3 targetLocalPos)
    {
        while ((buttonTransform.localPosition - targetLocalPos).sqrMagnitude > 0.0001f)
        {
            buttonTransform.localPosition = Vector3.Lerp(buttonTransform.localPosition, targetLocalPos, Mathf.Clamp01(moveSpeed * Time.deltaTime));
            yield return null;
        }
        buttonTransform.localPosition = targetLocalPos;
        moveRoutine = null;
    }

    private System.Collections.IEnumerator AutoReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!requireHold && !lockedPressed)
        {
            presserCount = 0;
            SetPressed(false);
        }
        autoResetRoutine = null;
    }
}