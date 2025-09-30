using UnityEngine;

public class Map_PlatformMoves : MonoBehaviour
{
    public float amplitude = 3f;   // distância máxima que sobe/desce
    public float speed = 2f;       // velocidade
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // guarda posição inicial
    }

    void Update()
    {
        // Movimento vertical tipo senoide (loop infinito)
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
