using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Transform cam;

    [Header("Layers")]
    [SerializeField] private Transform backgroundLayer;

    [Header("Velocidades de Parallax")]
    [SerializeField, Range(0f, 1f)] private float backgroundSpeed = 0.3f;

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

        lastCamPos = cam.position;
    }
}
