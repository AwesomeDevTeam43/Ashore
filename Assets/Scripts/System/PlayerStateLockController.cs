using UnityEngine;

/// <summary>
/// Grants other systems a way to temporarily freeze the player (input, movement, controller logic).
/// Multiple callers can stack their locks safely.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStateLockController : MonoBehaviour
{
    public static PlayerStateLockController Instance { get; private set; }

    private Player_InputHandler playerInput;
    private Player_Controller playerController;
    private Player_Movement playerMovement;

    private int lockCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Removed [RuntimeInitializeOnLoadMethod] - add to scene manually if needed

    public bool IsLocked => lockCount > 0;

    public void RequestLock(string source)
    {
        lockCount++;
        if (lockCount == 1)
        {
            CapturePlayerReferences();
            if (playerInput != null)
                playerInput.DisablePlayerActions();
            if (playerMovement != null)
                playerMovement.enabled = false;
            if (playerController != null)
                playerController.enabled = false;
        }
    }

    public void ReleaseLock(string source)
    {
        lockCount = Mathf.Max(0, lockCount - 1);
        if (lockCount == 0)
        {
            if (playerMovement != null)
                playerMovement.enabled = true;
            if (playerController != null)
                playerController.enabled = true;
            if (playerInput != null)
                playerInput.EnablePlayerActions();
        }
    }

    private void CapturePlayerReferences()
    {
        if (playerInput != null && playerController != null && playerMovement != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (playerInput == null)
            playerInput = player.GetComponent<Player_InputHandler>();
        if (playerController == null)
            playerController = player.GetComponent<Player_Controller>();
        if (playerMovement == null)
            playerMovement = player.GetComponent<Player_Movement>();
    }
}
