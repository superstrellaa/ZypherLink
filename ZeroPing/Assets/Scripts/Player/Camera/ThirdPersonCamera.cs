using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0, 2);
    public float distance = 5f;
    public float rotationSpeed = 3f;

    public float minPitch = -20f;
    public float maxPitch = 60f;
    public float smoothSpeed = 10f;

    private float yaw; 
    private float pitch;

    private Vector3 currentPosition;
    private Quaternion currentRotation;

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        currentPosition = transform.position;
        currentRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (GUIManager.Instance != null && GUIManager.Instance.IsGUIOpen || GUIManager.Instance.freezeMovement)
            return;

        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * offset.y;

        Vector3 desiredCameraPos = targetPosition - desiredRotation * Vector3.forward * distance;

        Ray ray = new Ray(targetPosition, desiredCameraPos - targetPosition);
        float adjustedDistance = distance;

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            adjustedDistance = hit.distance - 0.1f;
        }

        Vector3 finalCameraPos = targetPosition - desiredRotation * Vector3.forward * adjustedDistance;

        currentPosition = Vector3.Lerp(currentPosition, finalCameraPos, Time.deltaTime * smoothSpeed);
        currentRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(targetPosition - currentPosition), Time.deltaTime * smoothSpeed);

        transform.position = currentPosition;
        transform.rotation = currentRotation;
    }

}
