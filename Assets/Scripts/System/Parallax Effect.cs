using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Transform cam;

    [Header("Layers")]
    [SerializeField] private Transform backgroundLayer;
    [SerializeField] private Transform midgroundLayer;

    [Header("Velocidades de Parallax")]
    [SerializeField, Range(0f, 1f)] private float backgroundSpeed = 0.3f;
    [SerializeField, Range(0f, 1f)] private float midgroundSpeed = 0.6f;

    private Vector3 lastCamPos;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main.transform;

        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCamPos;

        backgroundLayer.position += new Vector3(deltaMovement.x * backgroundSpeed, deltaMovement.y * backgroundSpeed, 0);
        midgroundLayer.position += new Vector3(deltaMovement.x * midgroundSpeed, deltaMovement.y * midgroundSpeed, 0);

        lastCamPos = cam.position;
    }
}
