using UnityEngine;

public class ThirdPersonCameraManager : MonoBehaviour
{
    private PlayerController player;

    public Transform target;
    public float distance = 5f;
    public float height = 2f;

    public float rotationSpeed = 5f;

    private float yaw = 0f;
    private float pitch = 10f;

    void Start()
    {
        if (target != null)
            player = target.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 lookInput = player.LookInput;

        yaw += lookInput.x * rotationSpeed;
        pitch -= lookInput.y * rotationSpeed;
        pitch = Mathf.Clamp(pitch, -20f, 60f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance) + Vector3.up * height;

        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * height * 0.5f);
    }
}