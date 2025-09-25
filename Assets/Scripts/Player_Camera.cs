using Unity.Cinemachine;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;

    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private float maxOffset = 3f;
    [SerializeField] private float smooth = 5f;

    private CinemachinePositionComposer positionComposer;
    private Vector3 targetOffset;

    void Start()
    {
        positionComposer = cam.GetComponent<CinemachinePositionComposer>();
    }

    void Update()
    {
        float camX = player_InputHandler.LookInput.x;
        float camY = player_InputHandler.LookInput.y;

        targetOffset = new Vector3(camX * maxOffset, camY * maxOffset, 0);

        positionComposer.TargetOffset = Vector3.Lerp(positionComposer.TargetOffset, targetOffset, Time.deltaTime * smooth);
    }

}
