using UnityEngine;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Chịu trách nhiệm DUY NHẤT: Di chuyển nhân vật và lưu hướng nhìn.
    /// Script khác (PlayerVisual, PlayerInteraction) ĐỌC dữ liệu từ đây,
    /// không tự tính lại.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        // =====================================================
        // INSPECTOR SETTINGS
        // =====================================================
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;

        // =====================================================
        // PUBLIC READ-ONLY PROPERTIES (cho Visual & Interaction đọc)
        // =====================================================

        /// <summary>Hướng nhân vật đang nhìn (đã normalize). Mặc định: nhìn xuống.</summary>
        public Vector2 FacingDir { get; private set; } = Vector2.down;

        /// <summary>True nếu nhân vật đang di chuyển frame này.</summary>
        public bool IsMoving { get; private set; }

        // =====================================================
        // PRIVATE
        // =====================================================
        private Rigidbody2D _rb;
        private Vector2     _inputDir;   // Raw input mỗi frame

        // =====================================================
        // UNITY LIFECYCLE
        // =====================================================
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Dynamic + gravityScale 0: Physics engine tự chặn va chạm
            // Đây là cách đáng tin cậy nhất cho top-down 2D
            _rb.bodyType     = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            // Tắt linear drag để không bị "trơn" khi dừng
            _rb.linearDamping = 0f;
        }

        /// <summary>
        /// Đọc Input trong Update (theo frame) — để không bỏ sót phím bấm ngắn.
        /// </summary>
        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
            float y = Input.GetAxisRaw("Vertical");   // -1, 0, 1

            _inputDir = new Vector2(x, y).normalized; // normalize để chéo không nhanh hơn

            IsMoving = _inputDir != Vector2.zero;

            // Cập nhật hướng nhìn — chỉ đổi khi đang di chuyển
            if (IsMoving)
            {
                FacingDir = _inputDir;
            }
        }

        /// <summary>
        /// Di chuyển bằng velocity — Dynamic RB sẽ tự chặn khi gặp Collider.
        /// Đặt velocity = 0 khi không bấm phím để dừng ngay lập tức (không trượt).
        /// </summary>
        private void FixedUpdate()
        {
            _rb.linearVelocity = _inputDir * _moveSpeed;
        }

        // =====================================================
        // EDITOR GIZMO — Hiển thị hướng nhìn trong Scene View
        // =====================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, FacingDir * 0.7f);
        }
#endif
    }
}
