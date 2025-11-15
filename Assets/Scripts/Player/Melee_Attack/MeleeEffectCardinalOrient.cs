using UnityEngine;

/// <summary>
/// Ensures a melee effect prefab uses only 4-way (cardinal) orientation and consistent sprite flips.
/// Call SetDirection() immediately after instantiation.
/// As a safety net, Awake will snap any pre-set rotation to the nearest 90-degree cardinal.
/// </summary>
[DisallowMultipleComponent]
public class MeleeEffectCardinalOrient : MonoBehaviour
{
    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        // Safety: if rotation was set by prefab, snap to nearest cardinal
        float z = transform.localEulerAngles.z;
        float snapped = Mathf.Round(z / 90f) * 90f;
        transform.localRotation = Quaternion.Euler(0, 0, snapped);
        if (sprite != null && (Mathf.Approximately(snapped, 90f) || Mathf.Approximately(snapped, 270f)))
        {
            // For Up/Down orientations, ensure no horizontal flip bleed
            sprite.flipX = false;
        }
    }

    /// <summary>
    /// Apply a 4-way orientation to this effect. Direction must be cardinal.
    /// </summary>
    public void SetDirection(Vector2 dir, bool facingLeft)
    {
        // Normalize to cardinal
        if (dir == Vector2.zero)
        {
            dir = facingLeft ? Vector2.left : Vector2.right;
        }
        else
        {
            // Prefer vertical on tie so perfect diagonals become Up/Down instead of Left/Right
            if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
                dir = dir.y > 0 ? Vector2.up : Vector2.down;
            else
                dir = dir.x > 0 ? Vector2.right : Vector2.left;
        }

        if (dir == Vector2.right)
        {
            transform.localRotation = Quaternion.identity;
            if (sprite != null) sprite.flipX = false;
        }
        else if (dir == Vector2.left)
        {
            transform.localRotation = Quaternion.identity;
            if (sprite != null) sprite.flipX = true;
        }
        else if (dir == Vector2.up)
        {
            float angle = facingLeft ? -90f : 90f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            if (sprite != null) sprite.flipX = false;
        }
        else // down
        {
            float angle = facingLeft ? 90f : -90f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            if (sprite != null) sprite.flipX = false;
        }
    }
}
