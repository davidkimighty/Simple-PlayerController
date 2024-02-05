using UnityEngine;
using UnityEngine.InputSystem;

namespace Simple.PlayerController
{
    [DisallowMultipleComponent]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private CharacterController _controller;
        
        [SerializeField] private InputActionProperty _moveAction;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _accelerate = 3f;

        [SerializeField] private InputActionProperty _jumpAction;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _fallMultiplier = 3f;
        
        [SerializeField] private InputActionProperty _lookInputAction;
        [SerializeField] private Transform _lookTarget;
        [SerializeField] private float _lookSpeedX = 50f;
        [SerializeField] private float _lookSpeedY = 30f;
        [SerializeField] private LayerMask _cullingLayer;
        
        private Vector2 _moveInput;
        private bool _jumpInput;
        
        private float _targetSpeed;
        private float _verticalVel;
        private float _horizontalVel;
        private float _rotateVel;
        
        private Vector2 _lookInput;
        private Vector2 _lookVel;
        private float _pitchAngle;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Camera.main.cullingMask = 1 << _cullingLayer;
            _targetSpeed = _walkSpeed;
        }

        private void OnEnable()
        {
            _moveAction.action.performed += ReadMoveInput;
            _moveAction.action.canceled += ReadMoveInput;
            _jumpAction.action.performed += ReadJumpInput;
            _jumpAction.action.canceled += ReadJumpInput;
            
            _lookInputAction.action.performed += ReadLookInput;
            _lookInputAction.action.canceled += ReadLookInput;
        }

        private void OnDisable()
        {
            _moveAction.action.performed -= ReadMoveInput;
            _moveAction.action.canceled -= ReadMoveInput;
            _jumpAction.action.performed -= ReadJumpInput;
            _jumpAction.action.canceled -= ReadJumpInput;
            
            _lookInputAction.action.performed -= ReadLookInput;
            _lookInputAction.action.canceled -= ReadLookInput;
        }
        
        #region Subscribers
        private void ReadMoveInput(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void ReadJumpInput(InputAction.CallbackContext context)
        {
            _jumpInput = context.ReadValueAsButton();
        }
        
        private void ReadLookInput(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        #endregion
        
        private void Update()
        {
            ApplyGravity();
            Move();
            Rotate();
            Jump();
        }

        private void LateUpdate()
        {
            Look();
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded)
            {
                if (_verticalVel < 0f)
                    _verticalVel = -2f;
            }
            else
                _verticalVel += _gravity * Time.deltaTime;
        }

        private void Move()
        {
            Vector3 inputDir = Vector3.right * _moveInput.x + Vector3.forward * _moveInput.y;
            Vector3 moveDir = Quaternion.Euler(0f, _controller.transform.eulerAngles.y, 0f) * inputDir;
            
            float currentSpeed = Vector3.Scale(_controller.velocity, new Vector3(1, 0, 1)).magnitude;
            _horizontalVel = Mathf.Lerp(currentSpeed, _targetSpeed, _accelerate * Time.deltaTime);
            
            Vector3 horizontalMotion = moveDir * (_horizontalVel * Time.deltaTime);
            Vector3 verticalMotion = Vector3.up * (_verticalVel * Time.deltaTime);
            _controller.Move(horizontalMotion + verticalMotion);
        }

        private void Rotate()
        {
            _controller.transform.Rotate(_lookVel.x * Vector3.up);
        }

        private void Jump()
        {
            if (_jumpInput && _controller.isGrounded)
                _verticalVel = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

            if (_controller.velocity.y < 0)
                _verticalVel += _gravity * (_fallMultiplier - 1) * Time.deltaTime;
        }

        private void Look()
        {
            float yawVel = _lookInput.x * _lookSpeedX * Time.deltaTime;
            float pitchVel = _lookInput.y * _lookSpeedY * Time.deltaTime;
            _lookVel = new Vector2(yawVel, pitchVel);

            _pitchAngle -= pitchVel;
            _pitchAngle = Mathf.Clamp(_pitchAngle, -90f, 90f);

            _lookTarget.localRotation = Quaternion.Euler(_pitchAngle, 0, 0);
        }
    }
}
