using UnityEngine;
using UnityEngine.InputSystem;

namespace Simple.PlayerController
{
    [DisallowMultipleComponent]
    public class ThirdPersonControllerPhysics : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Rigidbody _body;
        [SerializeField] private LayerMask _playerLayer;

        [Header("Move")]
        [SerializeField] private InputActionProperty _moveInputAction;
        [SerializeField] private float _maxSpeed = 6f;
        [SerializeField] private float _acceleration = 150f;
        [SerializeField] private float _maxAcceleration = 200f;
        [SerializeField] private float _rotationStrength = 100f;
        [SerializeField] private float _rotationDamper = 10f;

        [Header("Jump")]
        [SerializeField] private InputActionProperty _jumpInputAction;
        [SerializeField] private float _jumpForce = 20f;
        [SerializeField] private float _fallMultiplier = 3f;
        [SerializeField] private bool _enableStomp = true;
        [SerializeField] private float _stompForce = 10f;

        [Header("Float")]
        [SerializeField] private Vector3 _rayDir = Vector3.down;
        [SerializeField] private float _floatHeight = 1.3f;
        [SerializeField] private float _rayLength = 1.5f;
        [SerializeField] private float _rayStartPointOffset = 0.5f;
        [SerializeField] private float _springStrength = 500f;
        [SerializeField] private float _springDamper = 50f;
        [SerializeField] private LayerMask _platformLayer;

        private Vector3 _moveInput = Vector3.zero;
        private bool _jumpInput;
        
        private Vector3 _moveDirection = Vector3.zero;
        private Quaternion _lookRotation;
        private Vector3 _targetVel = Vector3.zero;
        private bool _grounded = true;
        private Camera _camera;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            _moveInputAction.action.performed += ReadMoveInput;
            _moveInputAction.action.canceled += ReadMoveInput;
            _jumpInputAction.action.performed += ReadJumpInput;
            _jumpInputAction.action.canceled += ReadJumpInput;
        }

        private void OnDisable()
        {
            _moveInputAction.action.performed -= ReadMoveInput;
            _moveInputAction.action.canceled -= ReadMoveInput;
            _jumpInputAction.action.performed -= ReadJumpInput;
            _jumpInputAction.action.canceled -= ReadJumpInput;
        }

        #region Subscribers
        public void ReadMoveInput(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            _moveInput = Vector3.right * rawInput.x + Vector3.forward * rawInput.y;
        }

        private void ReadJumpInput(InputAction.CallbackContext context)
        {
            _jumpInput = context.ReadValueAsButton();
        }

        #endregion
        
        private void FixedUpdate()
        {
            Movement();
            Rotation();
            Jump();
            
            (bool isHit, RaycastHit rayHit) = CastRay();
            Floating(isHit, rayHit, _enableStomp);
        }

        private (bool, RaycastHit) CastRay()
        {
            Vector3 startPoint = _body.position;
            startPoint.y += _rayStartPointOffset;
            Ray ray = new(startPoint, Vector3.down);
            bool isHit = Physics.Raycast(ray, out RaycastHit rayHit, _rayLength + _rayStartPointOffset, ~_playerLayer);
            return (isHit, rayHit);
        }

        private void Movement()
        {
            _moveDirection = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0) * _moveInput;

            Vector3 targetVel = _moveDirection * _maxSpeed;
            _targetVel = Vector3.MoveTowards(_targetVel, targetVel, _acceleration * Time.fixedDeltaTime);

            Vector3 targetAccel = (_targetVel - _body.velocity) / Time.fixedDeltaTime;
            targetAccel = Vector3.ClampMagnitude(targetAccel, _maxAcceleration);

            Vector3 force = Vector3.Scale(targetAccel * _body.mass, new Vector3(1, 0, 1));
            //_targetBody.AddForceAtPosition(force, _targetBody.position);
            _body.AddForce(force);
        }

        private void Rotation()
        {
            if (_moveDirection.magnitude > 0)
                _lookRotation = Quaternion.LookRotation(_moveDirection);
            Quaternion targetRotation = ShortestRotation(_lookRotation, _body.rotation);

            targetRotation.ToAngleAxis(out float angle, out Vector3 axis);
            float rotationRadians = angle * Mathf.Deg2Rad;
            _body.AddTorque(axis.normalized * (rotationRadians * _rotationStrength) - _body.angularVelocity * _rotationDamper);
            
            Quaternion ShortestRotation(Quaternion a, Quaternion b)
            {
                if (Quaternion.Dot(a, b) < 0)
                    return a * Quaternion.Inverse(Multiply(b, -1f));
                return a * Quaternion.Inverse(b);
            }

            Quaternion Multiply(Quaternion q, float scalar)
            {
                return new Quaternion(q.x * scalar, q.y * scalar, q.z * scalar, q.w * scalar);
            }
        }

        private void Jump()
        {
            if (!_grounded)
            {
                if (_body.velocity.y < 0)
                    _body.velocity += Vector3.up * (Physics.gravity.y * (_fallMultiplier - 1) * Time.fixedDeltaTime);
                return;
            }

            if (_jumpInput)
            {
                _body.velocity = new Vector3(_body.velocity.x, 0, _body.velocity.z);
                _body.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            }
        }

        private void Floating(bool isHit, RaycastHit rayHit, bool applyForceOnHit)
        {
            if (!isHit)
            {
                if (_grounded)
                    _grounded = false;
                return;
            }

            _grounded = rayHit.distance <= _floatHeight + _rayStartPointOffset;
            if (_grounded)
            {
                Vector3 hitVel = rayHit.rigidbody != null ? rayHit.rigidbody.velocity : Vector3.zero;

                float rayDirVel = Vector3.Dot(_rayDir, _body.velocity);
                float hitRayDirVel = Vector3.Dot(_rayDir, hitVel);
                float relVel = rayDirVel - hitRayDirVel;
                float offset = rayHit.distance - _floatHeight - _rayStartPointOffset;
                float springForce = offset * _springStrength - relVel * _springDamper;
                _body.AddForce(_rayDir * springForce);

                if (applyForceOnHit && rayHit.rigidbody != null)
                    rayHit.rigidbody.AddForceAtPosition(_rayDir * -_stompForce, rayHit.point);
            }
        }
    }
}
