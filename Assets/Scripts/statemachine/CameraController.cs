using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance=5f;
    [SerializeField] private float sensitivity=0.0025f;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private LayerMask collisionLayers;


    private Vector3 _cameraPosition;
    private Vector3 _camUp;
    private Vector3 _camRight;
    private float _yaw;
    private float _pitch;
    private float _pitchMin= -Mathf.PI / 2 + 0.01f;
    private float _pitchMax = Mathf.PI / 2 - 0.01f;

    void Start()
    {
        transform.position=target.position+(Vector3.back*distance);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        OrbitCamera();
        GroundCheck();
        CalculateAxis();
        UpdateCameraTransform();
    }

    private void UpdateCameraTransform()
    {
        transform.position=target.position+_cameraPosition;
        transform.LookAt(target,_camUp);
        //send data to handler
        inputHandler.inputBuffer.camForward=-_cameraPosition.normalized;
        inputHandler.inputBuffer.camUp=_camUp;
    }

    private void OrbitCamera()
    {
        //movement vector
        _yaw += inputHandler.inputBuffer.Mouse.x * sensitivity;
        _pitch -= inputHandler.inputBuffer.Mouse.y * sensitivity/1.5f;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax); // avoid flipping

        Vector3 direction;
        float cosPitch = Mathf.Cos(_pitch);
        direction.x = cosPitch * Mathf.Sin(_yaw);
        direction.y = Mathf.Sin(_pitch);
        direction.z = cosPitch * Mathf.Cos(_yaw);
        _cameraPosition = direction * distance;
    }

    void CalculateAxis()
    {
        //calculate axis
        _camRight = Vector3.Normalize(Vector3.Cross(Vector3.up, -_cameraPosition));
        _camUp = -Vector3.Normalize(Vector3.Cross(_camRight, -_cameraPosition));
    }
    
    void GroundCheck()
    {
        //RaycastHit hit;
        if (Physics.Raycast(target.position, _cameraPosition, out RaycastHit hit, distance+0.2f,collisionLayers))
        {
            if (hit.distance<distance)
            {
                _cameraPosition *=hit.distance/distance;
            }
        }
    }
}
