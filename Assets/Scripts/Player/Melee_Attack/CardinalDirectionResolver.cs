using UnityEngine;

/// <summary>
/// Utility to convert a raw 2D input vector into a strict 4-way cardinal.
/// Rules:
///  - If magnitude below threshold => return facing horizontal (right or left based on facingLeft flag).
///  - Otherwise pick axis of larger absolute component.
///  - Tie goes to vertical if |y| == |x|.
///  - Output is exactly one of Vector2.right/left/up/down.
/// </summary>
public static class CardinalDirectionResolver
{
    public static Vector2 Resolve(Vector2 raw, float threshold, bool facingLeft)
    {
        float mag = raw.magnitude;
        if (mag < threshold)
        {
            return facingLeft ? Vector2.left : Vector2.right;
        }
        float ax = Mathf.Abs(raw.x);
        float ay = Mathf.Abs(raw.y);
        if (ay >= ax)
        {
            return raw.y >= 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            return raw.x >= 0 ? Vector2.right : Vector2.left;
        }
    }
}
