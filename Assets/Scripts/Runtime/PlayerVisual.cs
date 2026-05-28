using UnityEngine;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Chịu trách nhiệm DUY NHẤT: Hiển thị đúng Sprite theo hướng di chuyển.
    /// ĐỌC dữ liệu từ PlayerMovement, không tự tính toán input hay vị trí.
    ///
    /// SETUP TRONG INSPECTOR:
    /// Gán 4 sprite tương ứng vào các ô: spriteDown, spriteUp, spriteLeft, spriteRight.
    /// Nếu có Animator, script sẽ set parameter thay vì đổi sprite trực tiếp.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerVisual : MonoBehaviour
    {
        // =====================================================
        // INSPECTOR — Sprite 4 hướng
        // =====================================================
        [Header("Sprites — Đứng yên (Idle)")]
        [SerializeField] private Sprite _idleDown;
        [SerializeField] private Sprite _idleUp;
        [SerializeField] private Sprite _idleLeft;
        [SerializeField] private Sprite _idleRight;

        [Header("Sprites — Đang di chuyển (Walk)")]
        [SerializeField] private Sprite _walkDown;
        [SerializeField] private Sprite _walkUp;
        [SerializeField] private Sprite _walkLeft;
        [SerializeField] private Sprite _walkRight;

        // =====================================================
        // INSPECTOR — Tham chiếu
        // =====================================================
        [Header("References")]
        [Tooltip("Kéo root GameObject Player vào đây")]
        [SerializeField] private PlayerMovement _movement;

        [SerializeField] private PlayerInteraction _interaction;

        // =====================================================
        // PRIVATE
        // =====================================================
        private SpriteRenderer _spriteRenderer;
        private Animator       _animator; // Optional — nếu có Animator thì dùng
        
        // Quản lý Animation State (Cách 2: Code-Driven)
        private string _currentState = "";

        // Animator parameter names (để Blend Tree biết hướng)
        private static readonly int AnimMoveX    = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY    = Animator.StringToHash("MoveY");

        // =====================================================
        // UNITY LIFECYCLE
        // =====================================================
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator       = GetComponent<Animator>(); // null nếu không có — không sao
        }

        private void Start()
        {
            // Tự động tìm components nếu chưa gán trong Inspector
            if (_movement == null)
                _movement = GetComponentInParent<PlayerMovement>();
            
            if (_interaction == null)
                _interaction = GetComponentInParent<PlayerInteraction>();

            if (_movement == null)
                Debug.LogError("[PlayerVisual] Không tìm thấy PlayerMovement!", this);
        }

        /// <summary>
        /// LateUpdate chạy SAU Update — đảm bảo PlayerMovement đã cập nhật
        /// FacingDir và IsMoving trước khi Visual đọc.
        /// </summary>
        private void LateUpdate()
        {
            if (_movement == null) return;

            if (_animator != null)
                UpdateAnimator();
            else
                UpdateSprite();
        }

        // =====================================================
        // PRIVATE METHODS
        // =====================================================

        /// <summary>Set Animator parameters và chuyển State bằng Code-Driven.</summary>
        private void UpdateAnimator()
        {
            // 1. Cập nhật thông số hướng nhìn cho Blend Tree
            _animator.SetFloat(AnimMoveX,    _movement.FacingDir.x);
            _animator.SetFloat(AnimMoveY,    _movement.FacingDir.y);

            // 2. Quyết định trạng thái (State Logic)
            bool isHoldingFood = _interaction != null && _interaction.IsHoldingFood;
            bool isMoving = _movement.IsMoving;

            string newState;
            if (isHoldingFood)
            {
                newState = isMoving ? "RunningLifting_Tree" : "IdleLifting_Tree";
            }
            else
            {
                // Gọi tên state CHÍNH XÁC như trong bảng Animator (phải khớp viết hoa/viết thường)
                newState = isMoving ? "Running_tree" : "Idle_Tree";
            }

            // 3. Thực hiện chuyển Animation
            ChangeAnimationState(newState);
        }

        /// <summary>
        /// Chuyển đổi trạng thái Animation. 
        /// Kiểm tra state hiện tại để tránh việc gọi Play() liên tục làm reset frame.
        /// </summary>
        public void ChangeAnimationState(string newState)
        {
            if (_currentState == newState) return;

            _animator.Play(newState);
            _currentState = newState;
        }

        /// <summary>Đổi sprite trực tiếp nếu không có Animator.</summary>
        private void UpdateSprite()
        {
            _spriteRenderer.sprite = PickSprite(_movement.FacingDir, _movement.IsMoving);
        }

        /// <summary>
        /// Chọn sprite phù hợp dựa trên hướng nhìn và trạng thái.
        /// Ưu tiên trục có giá trị tuyệt đối lớn hơn (khi đi chéo).
        /// </summary>
        private Sprite PickSprite(Vector2 dir, bool isMoving)
        {
            // Xác định hướng chủ đạo (Horizontal vs Vertical)
            bool horizontalDominant = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);

            if (horizontalDominant)
            {
                if (dir.x > 0) return isMoving ? _walkRight : _idleRight;
                if (dir.x < 0) return isMoving ? _walkLeft  : _idleLeft;
            }
            else
            {
                if (dir.y > 0) return isMoving ? _walkUp   : _idleUp;
                if (dir.y < 0) return isMoving ? _walkDown : _idleDown;
            }

            // Fallback: đứng yên nhìn xuống
            return _idleDown;
        }
    }
}
