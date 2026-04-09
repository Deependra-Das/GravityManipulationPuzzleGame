using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundDistance = 0.2f;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private InputActionAsset _inputActionsObj;

    private CharacterController _controller;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _lookAction;

    private Vector2 _lookInput;
    public Vector2 LookInput => _lookInput;

    private Vector3 _velocity;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _wasGrounded;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        var actionMap = _inputActionsObj.FindActionMap("Player");
        _moveAction = actionMap.FindAction("Move");
        _jumpAction = actionMap.FindAction("Jump");
        _lookAction = actionMap.FindAction("Look");

        _moveAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _moveAction.canceled += ctx => _moveInput = Vector2.zero;
        _jumpAction.performed += ctx => Jump();
        _lookAction.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _lookAction.canceled += ctx => _lookInput = Vector2.zero;
    }

    void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        _lookAction.Enable();
    }

    void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        _lookAction.Disable();
    }

    void Update()
    {
        GroundCheck();
        HandleMovement();
        ApplyGravity();

        _wasGrounded = _isGrounded;
    }

    void HandleMovement()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.Cross(Vector3.up, camForward);

        Vector3 move = camForward * _moveInput.y + camRight * _moveInput.x;
        _controller.Move(move * _moveSpeed * Time.deltaTime);

        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    void Jump()
    {
        if (_isGrounded)
        {
            _velocity.y = _jumpForce;
        }
    }

    void ApplyGravity()
    {
        if (_isGrounded)
        {
            if (_velocity.y < 0f) _velocity.y = -2f;
        }
        else
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        _controller.Move(new Vector3(0, _velocity.y, 0) * Time.deltaTime);
    }

    void GroundCheck()
    {
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);
    }

    void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundDistance);
        }
    }
}