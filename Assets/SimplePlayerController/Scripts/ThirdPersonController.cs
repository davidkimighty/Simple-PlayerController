using UnityEngine;
using UnityEngine.InputSystem;

namespace Simple.PlayerController
{
    [DisallowMultipleComponent]
    public class ThirdPersonController : MonoBehaviour
    {
        [SerializeField] private CharacterController _controller;

        [SerializeField] private InputActionProperty _moveAction;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _accelerate = 3f;
        [SerializeField] private float _rotateSmoothDamp = 0.3f;

        [SerializeField] private InputActionProperty _jumpAction;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _fallMultiplier = 3f;
        
        private Vector2 _moveInput = Vector2.zero;
        private bool _jumpInput;

        private float _targetSpeed;
        private float _verticalVel;
        private float _horizontalVel;

        private Vector3 _moveDir = Vector3.zero;
        private float _rotateVel;
        private Camera _camera;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _camera = Camera.main;
            _targetSpeed = _walkSpeed;
        }

        private void OnEnable()
        {
            _moveAction.action.performed += ReadMoveInput;
            _moveAction.action.canceled += ReadMoveInput;
            _jumpAction.action.performed += ReadJumpInput;
            _jumpAction.action.canceled += ReadJumpInput;
        }

        private void OnDisable()
        {
            _moveAction.action.performed -= ReadMoveInput;
            _moveAction.action.canceled -= ReadMoveInput;
            _jumpAction.action.performed -= ReadJumpInput;
            _jumpAction.action.canceled -= ReadJumpInput;
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

        #endregion
        
        private void Update()
        {
            ApplyGravity();
            Move();
            Jump();
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

            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camera.transform.eulerAngles.y;
            float rotateAngle = Mathf.SmoothDampAngle(_controller.transform.eulerAngles.y, targetAngle, ref _rotateVel, _rotateSmoothDamp);

            if (_moveInput.magnitude > 0f)
            {
                _controller.transform.rotation = Quaternion.Euler(0f, rotateAngle, 0f);
                _moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }
            
            float currentSpeed = Vector3.Scale(_controller.velocity, new Vector3(1, 0, 1)).magnitude;
            _horizontalVel = Mathf.Lerp(currentSpeed, _targetSpeed, _accelerate * Time.deltaTime);

            Vector3 horizontalMotion = _moveDir * (_horizontalVel * Time.deltaTime);
            Vector3 verticalMotion = Vector3.up * (_verticalVel * Time.deltaTime);
            _controller.Move(horizontalMotion + verticalMotion);
        }

        private void Jump()
        {
            if (_jumpInput && _controller.isGrounded)
                _verticalVel = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

            if (_controller.velocity.y < 0)
                _verticalVel += _gravity * (_fallMultiplier - 1) * Time.deltaTime;
        }
    }
}
