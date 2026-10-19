using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _lookAction;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 4f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Camera")]
        [SerializeField, Min(0f)] private float _mouseSensitivity = 0.12f;
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;

        private CharacterController _characterController;

        private float _verticalVelocity;
        private float _cameraPitch;

        private bool _canMove = true;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            _cameraPitch = _cameraTransform.localEulerAngles.x;

            if (_cameraPitch > 180f)
                _cameraPitch -= 360f;
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _lookAction.action.Enable();
        }

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            HandleCursor();

            if (!_canMove ||
                Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            Vector2 lookInput = _lookAction.action.ReadValue<Vector2>();

            float yaw = lookInput.x * _mouseSensitivity;
            float pitch = lookInput.y * _mouseSensitivity;

            transform.Rotate(Vector3.up, yaw);

            _cameraPitch = Mathf.Clamp(
                _cameraPitch - pitch,
                _minPitch,
                _maxPitch
            );

            _cameraTransform.localRotation =
                Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            if (!_characterController.enabled)
            {
                _verticalVelocity = 0f;
                return;
            }

            Vector2 moveInput = _moveAction.action.ReadValue<Vector2>();

            Vector3 direction =
                transform.right * moveInput.x +
                transform.forward * moveInput.y;

            direction = Vector3.ClampMagnitude(direction, 1f);

            if (_characterController.isGrounded &&
                _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 velocity = direction * _moveSpeed;
            velocity.y = _verticalVelocity;

            _characterController.Move(
                velocity * Time.deltaTime
            );
        }

        private void HandleCursor()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
                return;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            _canMove = enabled;
            _verticalVelocity = 0f;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _lookAction.action.Disable();

            UnlockCursor();
        }
    }
}
