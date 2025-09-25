
using Unity.Cinemachine;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    [SerializeField] private CinemachineCamera cam;

    [Header("LookAround")]
    [SerializeField] private float maxOffset = 3f;
    [SerializeField] private float smooth = 5f;

    [Header("CameraShake")]
    [SerializeField] public float hitAmplitudeGain = 2f;
    [SerializeField] public float hitFrequencyGain = 2f;
    [SerializeField] public float shakeTime = 0.1f;

    private CinemachinePositionComposer positionComposer;
    private CinemachineBasicMultiChannelPerlin noisePerlin;
    private Vector3 targetOffset;
    private bool isShaking = false;
    private float shakeTimeElapse = 0f;


    void Start()
    {
        positionComposer = cam.GetComponent<CinemachinePositionComposer>();
        noisePerlin = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void Update()
    {
        LookAround();

        if (isShaking)
        {
            ShakeDuration();
        }
    }

    private void LookAround()
    {
        float camX = player_InputHandler.LookInput.x;
        float camY = player_InputHandler.LookInput.y;

        targetOffset = new Vector3(camX * maxOffset, camY * maxOffset, 0);

        positionComposer.TargetOffset = Vector3.Lerp(positionComposer.TargetOffset, targetOffset, Time.deltaTime * smooth);
    }

    public void StartCameraShake()
    {
        noisePerlin.AmplitudeGain = hitAmplitudeGain;
        noisePerlin.FrequencyGain = hitFrequencyGain;
        isShaking = true;
        ShakeDuration();
    }

    public void ShakeDuration()
    {
        shakeTimeElapse += Time.deltaTime;

        if (shakeTimeElapse > shakeTime)
        {
            StopCameraShake();
        }
    }

    public void StopCameraShake()
    {
        noisePerlin.AmplitudeGain = 0;
        noisePerlin.FrequencyGain = 0;
        isShaking = false;
        shakeTimeElapse = 0f;
    }
}
