using Unity.Cinemachine;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    [SerializeField] private CinemachineCamera cam;

    [Header ("LookAround")]
    [SerializeField] private float maxOffset = 3f;
    [SerializeField] private float smooth = 5f;

    [Header("CameraShake")]
    [SerializeField] private float hitAmplitudeGain = 2f;
    [SerializeField] private float hitFrequencyGain = 2f;

    private CinemachinePositionComposer positionComposer;
    private CinemachineBasicMultiChannelPerlin noisePerlin;
    private Vector3 targetOffset;

    void Start()
    {
        positionComposer = cam.GetComponent<CinemachinePositionComposer>();
        noisePerlin = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void Update()
    {
        LookAround();
    }

    private void LookAround()
    {
        float camX = player_InputHandler.LookInput.x;
        float camY = player_InputHandler.LookInput.y;

        targetOffset = new Vector3(camX * maxOffset, camY * maxOffset, 0);

        positionComposer.TargetOffset = Vector3.Lerp(positionComposer.TargetOffset, targetOffset, Time.deltaTime * smooth);
    }

    public void CameraShake()
    {
        noisePerlin.AmplitudeGain = hitAmplitudeGain;
        noisePerlin.FrequencyGain = hitFrequencyGain;
    }
}
