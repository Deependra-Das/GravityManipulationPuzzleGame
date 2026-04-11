using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _sprintSpeed = 5f;
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundDistance = 0.2f;
    [SerializeField] private float _rotationSpeed = 8f;
    [SerializeField] private float _wallCheckDistance = 10f;

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _pivot;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private InputActionAsset _inputActionsObj;

    private CharacterController _controller;
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _lookAction;
    private InputAction _gravityShiftDirection;
    private InputAction _gravityShiftAction;

    private Vector2 _lookInput;
    public Vector2 LookInput => _lookInput;

    private Vector3 _velocity;
    private Vector2 _moveInput;
    private Vector2 _gravityShiftDirectionInput;
    private Vector3 _gravityDirection = Vector3.down;

    private bool _isSprinting;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _isRotating = false;
    private bool _useGravity = true;

    private int _speedHash;
    private int _motionHash;
    private int _groundedHash;
    private int _verticalHash;
    private int _jumpStartTrigger;
    private int _jumpLandTrigger; 

    void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        _lookAction.Enable();
        _sprintAction.Enable();
        _gravityShiftDirection.Enable();
        _gravityShiftAction.Enable();

    }

    void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        _lookAction.Disable();
        _sprintAction.Disable();
        _gravityShiftDirection.Disable();
        _gravityShiftAction.Disable();
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        var actionMap = _inputActionsObj.FindActionMap("Player");
        var gravityActionMap = _inputActionsObj.FindActionMap("GravityControl");

        _moveAction = actionMap.FindAction("Move");
        _jumpAction = actionMap.FindAction("Jump");
        _lookAction = actionMap.FindAction("Look");
        _sprintAction = actionMap.FindAction("Sprint");

        _gravityShiftDirection = gravityActionMap.FindAction("GravityDirection");
        _gravityShiftAction = gravityActionMap.FindAction("GravityShift");

        _moveAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _moveAction.canceled += ctx => _moveInput = Vector2.zero;
        _jumpAction.performed += ctx => Jump();
        _lookAction.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _lookAction.canceled += ctx => _lookInput = Vector2.zero;
        _sprintAction.performed += ctx => _isSprinting = true;
        _sprintAction.canceled += ctx => _isSprinting = false;

        _gravityShiftDirection.performed += ctx => _gravityShiftDirectionInput = ctx.ReadValue<Vector2>();
        _gravityShiftDirection.canceled += ctx => _gravityShiftDirectionInput = Vector2.zero;

        AssignAnimationIDs();
    }

    private void AssignAnimationIDs()
    {
        _speedHash = Animator.StringToHash("Speed");
        _motionHash = Animator.StringToHash("MotionSpeed");
        _groundedHash = Animator.StringToHash("IsGrounded");
        _verticalHash = Animator.StringToHash("VerticalVelocity");
        _jumpStartTrigger = Animator.StringToHash("JumpStart");
        _jumpLandTrigger = Animator.StringToHash("JumpLand");
    }

    void Update()
    {
        GroundCheck();

        if (!_isRotating)
        {
            HandleMovement();
        }

        if (_gravityShiftAction.IsPressed())
        {
            TryGravityShift();
        }

        ApplyGravity();
        UpdateAnimations();

        if (!_wasGrounded && _isGrounded)
        {
            _animator.SetTrigger(_jumpLandTrigger);
        }

        _wasGrounded = _isGrounded;
    }

    void HandleMovement()
    {
        Vector3 up = -_gravityDirection;

        Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, up).normalized;
        Vector3 camRight = Vector3.Cross(up, camForward);

        Vector3 move = camForward * _moveInput.y + camRight * _moveInput.x;
        float currentSpeed = _isSprinting ? _sprintSpeed : _moveSpeed;
        _controller.Move(move * currentSpeed * Time.deltaTime);

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
            _animator.SetTrigger(_jumpStartTrigger);
        }
    }

    void ApplyGravity()
    {
        float gravitySpeed = Mathf.Abs(_gravity);

        if (_isGrounded)
        {
            float intoGravity = Vector3.Dot(_velocity, _gravityDirection);

            if (intoGravity > 0)
                _velocity -= _gravityDirection * intoGravity;

            _velocity += _gravityDirection * 2f * Time.deltaTime;
        }
        else
        {
            _velocity += _gravityDirection * gravitySpeed * Time.deltaTime;
        }

        _controller.Move(_velocity * Time.deltaTime);
    }

    void GroundCheck()
    {
        float radius = _controller.radius * 0.9f;
        float distance = _groundDistance + 0.3f;

        Vector3 up = -_gravityDirection;

        Vector3 origin = transform.position + up * 0.2f;

        _isGrounded = Physics.CheckSphere(
            origin,
            radius,
            _groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void UpdateAnimations()
    {
        Vector3 up = -_gravityDirection;

        Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, up).normalized;
        Vector3 camRight = Vector3.Cross(up, camForward);

        Vector3 move = camForward * _moveInput.y + camRight * _moveInput.x;

        float motionSpeed = _isSprinting ? _sprintSpeed : move.magnitude;

        float smoothSpeed = Mathf.Lerp(_animator.GetFloat(_speedHash), motionSpeed, 10f * Time.deltaTime);
        _animator.SetFloat(_speedHash, smoothSpeed);

        float verticalSpeed = Vector3.Dot(_velocity, _gravityDirection);
        bool groundedForAnim = _isGrounded && verticalSpeed <= 0.1f;
        _animator.SetBool(_groundedHash, groundedForAnim);

        _animator.SetFloat(_verticalHash, _velocity.y);
    }

    void TryGravityShift()
    {
        if (_isRotating) return;
        if (!_isGrounded) return;

        if (!_gravityShiftAction.IsPressed())
            return;

        Vector2 input = _gravityShiftDirection.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.1f)
            return;

        Vector3 up = -_gravityDirection;

        Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, up).normalized;
        Vector3 camRight = Vector3.Cross(up, camForward);

        Vector3 direction = camForward * input.y + camRight * input.x;

        if (direction.sqrMagnitude < 0.1f)
            return;

        direction.Normalize();

        RaycastHit hit;
        if (CheckForWall(direction, out hit))
        {
            Debug.Log("Wall Hit: " + hit.collider.name);
        }
    }

    bool CheckForWall(Vector3 direction, out RaycastHit hit)
    {
        Vector3 up = -_gravityDirection;
        Vector3 origin = transform.position + (up * (_controller.height / 2f));

        Vector3 rayDirection = (direction + _gravityDirection).normalized;

        Debug.DrawRay(origin, rayDirection * _wallCheckDistance, Color.blue);

        return Physics.Raycast(origin, rayDirection, out hit, _wallCheckDistance, _groundMask);
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