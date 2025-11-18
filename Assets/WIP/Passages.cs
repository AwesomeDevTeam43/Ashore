using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Passages : MonoBehaviour
{
    [Header("Upward Movement")]
    [Tooltip("How many world units upward the object should travel when the event fires.")]
    [SerializeField] private float moveUpDistance = 3f;
    [Tooltip("Seconds the upward motion should take.")]
    [SerializeField] private float moveUpDuration = 1.5f;
    [Tooltip("Curve used to ease the upward motion (0-1 time to 0-1 progress).")]
    [SerializeField] private AnimationCurve moveUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Left Movement")]
    [Tooltip("How many world units to the left the object should travel when the event fires.")]
    [SerializeField] private float moveLeftDistance = 3f;
    [Tooltip("Seconds the leftward motion should take.")]
    [SerializeField] private float moveLeftDuration = 1.5f;
    [Tooltip("Curve used to ease the left motion (0-1 time to 0-1 progress).")]
    [SerializeField] private AnimationCurve moveLeftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [Tooltip("Invoke to move the object upward using the configured settings.")]
    public UnityEvent onMoveUp;
    [Tooltip("Invoke to move the object left using the configured settings.")]
    public UnityEvent onMoveLeft;

    [Header("Cleanup")]
    [Tooltip("Destroy this GameObject once a move completes.")]
    [SerializeField] private bool destroyAfterMove;

    private Coroutine moveRoutine;

    private void Awake()
    {
        onMoveUp ??= new UnityEvent();
        onMoveLeft ??= new UnityEvent();
    }

    private void OnEnable()
    {
        onMoveUp.AddListener(HandleMoveUpRequest);
        onMoveLeft.AddListener(HandleMoveLeftRequest);
    }

    private void OnDisable()
    {
        onMoveUp.RemoveListener(HandleMoveUpRequest);
        onMoveLeft.RemoveListener(HandleMoveLeftRequest);
    }

    public void TriggerMoveUp()
    {
        onMoveUp?.Invoke();
    }

    public void TriggerMoveLeft()
    {
        onMoveLeft?.Invoke();
    }

    private void HandleMoveUpRequest()
    {
        StartMove(Vector3.up * moveUpDistance, moveUpDuration, moveUpCurve);
    }

    private void HandleMoveLeftRequest()
    {
        StartMove(Vector3.left * moveLeftDistance, moveLeftDuration, moveLeftCurve);
    }

    private void StartMove(Vector3 offset, float duration, AnimationCurve curve)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(offset, duration, curve));
    }

    private IEnumerator MoveRoutine(Vector3 offset, float duration, AnimationCurve curve)
    {
        Vector3 start = transform.position;
        Vector3 end = start + offset;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration > 0f ? timer / duration : 1f;
            float easedT = (curve != null) ? curve.Evaluate(Mathf.Clamp01(t)) : t;
            transform.position = Vector3.LerpUnclamped(start, end, easedT);
            yield return null;
        }

        transform.position = end;
        moveRoutine = null;

        if (destroyAfterMove)
        {
            Destroy(gameObject);
        }
    }
}
