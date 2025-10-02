using UnityEngine;

public class Map_PlatformMoves : MonoBehaviour
{
    public float amplitude = 3f;
    public float speed = 2f;
    [SerializeField] private Vector3 startPos;
    public bool vertical = false;
    public bool horizontal = false;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (vertical == true)
        {
            float newPos = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(startPos.x, newPos, startPos.z);
        }

        if (horizontal == true)
        {
            float newPos = startPos.x + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(newPos, startPos.y, startPos.z);
        }

        if (vertical == true) horizontal = false;
        if (horizontal == true) vertical = false;
    }
}
