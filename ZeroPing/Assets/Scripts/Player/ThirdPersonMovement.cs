using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("General Settings")]
    public Transform cameraTransform;
    public float moveSpeed = 6f;
    public float rotationSmoothTime = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private float rotationVelocity;

    private Vector3 lastSentPosition;
    private float lastSentRotationY;
    private Vector3 lastSentVelocity;

    private float sendTimer = 0f;
    public float sendInterval = 0.05f;
    private bool wasIdle = true;

    [Header("Latency Simulation")]
    public bool simulateNetwork = false;
    [Range(0f, 1f)] public float packetLossChance = 0.0f; 
    public float minLatency = 0.05f;
    public float maxLatency = 0.2f;

    private class PendingPacket
    {
        public float sendTime;
        public Vector3 position;
        public float rotationY;
        public Vector3 velocity;
    }
    private List<PendingPacket> pendingPackets = new List<PendingPacket>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastSentPosition = transform.position;
        lastSentRotationY = transform.eulerAngles.y;
        lastSentVelocity = Vector3.zero;
    }

    void Update()
    {
        if (GUIManager.Instance != null && GUIManager.Instance.IsGUIOpen)
            return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float horizontal = 0f;
        float vertical = 0f;

        if (GUIManager.Instance == null || !GUIManager.Instance.freezeMovement)
        {
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
            if (inputDir.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }
            else moveDirection = Vector3.zero;
        }
        else moveDirection = Vector3.zero;

        Vector3 horizontalVelocity = moveDirection * moveSpeed;
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * velocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

        if (PlayerManager.Instance.CurrentState == PlayerState.Playing)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= sendInterval)
            {
                Vector3 posDelta = transform.position - lastSentPosition;
                float rotDelta = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, lastSentRotationY));

                bool hasMoved = posDelta.sqrMagnitude > 0.0001f || rotDelta > 0.1f;
                bool isIdle = moveDirection.sqrMagnitude < 0.01f;

                if (hasMoved || isIdle != wasIdle)
                {
                    Vector3 sendVelocity = isIdle ? Vector3.zero : controller.velocity;

                    if (simulateNetwork)
                    {
                        if (Random.value > packetLossChance)
                        {
                            float delay = Random.Range(minLatency, maxLatency);
                            pendingPackets.Add(new PendingPacket
                            {
                                sendTime = Time.time + delay,
                                position = transform.position,
                                rotationY = transform.eulerAngles.y,
                                velocity = sendVelocity
                            });
                        }
                    }
                    else
                    {
                        MessageHandler.Move(transform.position, transform.eulerAngles.y, sendVelocity);
                    }

                    lastSentPosition = transform.position;
                    lastSentRotationY = transform.eulerAngles.y;
                    lastSentVelocity = sendVelocity;
                    wasIdle = isIdle;
                }
                sendTimer = 0f;
            }
        }

        if (simulateNetwork && pendingPackets.Count > 0)
        {
            for (int i = pendingPackets.Count - 1; i >= 0; i--)
            {
                if (Time.time >= pendingPackets[i].sendTime)
                {
                    MessageHandler.Move(pendingPackets[i].position, pendingPackets[i].rotationY, pendingPackets[i].velocity);
                    pendingPackets.RemoveAt(i);
                }
            }
        }

        if ((GUIManager.Instance == null || !GUIManager.Instance.freezeMovement) && Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
