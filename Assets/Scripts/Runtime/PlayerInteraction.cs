using UnityEngine;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Chịu trách nhiệm DUY NHẤT: Quản lý việc tương tác với thế giới và cầm/thả đồ ăn.
    ///
    /// LUỒNG LOGIC:
    /// 1. Player bấm E
    /// 2. OverlapCircle bắn về facingDir tìm IInteractable
    /// 3. Gọi interactable.Interact(this) → Object tự quyết định làm gì với Player
    /// 4. Ví dụ IngredientSource sẽ gọi player.PickupFood(food) để nhét đồ vào tay
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        // =====================================================
        // INSPECTOR SETTINGS
        // =====================================================
        [Header("Hold Point")]
        [Tooltip("Empty Object con của Player — vị trí 'bàn tay' hiển thị đồ đang cầm")]
        public Transform holdPoint;

        [Header("UI Prompts")]
        [Tooltip("GameObject chứa chữ 'E' (Nhặt/Thả)")]
        public GameObject interactPromptE;

        [Tooltip("GameObject chứa chữ 'F' (Thái/Lấy đồ)")]
        public GameObject interactPromptF;

        [Header("Interaction Detection")]
        [Tooltip("Khoảng cách bắn OverlapCircle về phía trước")]
        [SerializeField] private float _interactDistance = 0.7f;
        [Tooltip("Bán kính vùng kiểm tra tương tác")]
        [SerializeField] private float _interactRadius   = 0.3f;
        [Tooltip("Layer của các object có thể tương tác (tạo Layer 'Interactable' trong Unity)")]
        [SerializeField] private LayerMask _interactableMask;

        // =====================================================
        // PUBLIC STATE — Các script khác có thể đọc
        // =====================================================

        /// <summary>Đồ ăn Player đang cầm trên tay. Null nếu tay trống.</summary>
        public FoodItem CurrentHeldFood { get; private set; }

        /// <summary>True nếu đang cầm đồ ăn.</summary>
        public bool IsHoldingFood => CurrentHeldFood != null;

        // =====================================================
        // PRIVATE
        // =====================================================
        private PlayerMovement _movement;
        
        // Lưu trữ object đang được highlight để gọi khi bấm phím
        private IInteractable _hoveredInteractable;
        private Transform     _hoveredTransform;

        // =====================================================
        // UNITY LIFECYCLE
        // =====================================================
        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();

            if (_movement == null)
                Debug.LogError("[PlayerInteraction] Không tìm thấy PlayerMovement trên cùng GameObject!", this);
        }

        private void Update()
        {
            // Quét liên tục mỗi frame để biết đang đứng gần cái gì
            UpdateHoveredInteractable();

            // Nếu nhấn E/Space 1 lần -> Tương tác nhanh (Nhặt / Thả đồ)
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                if (_hoveredInteractable != null)
                {
                    _hoveredInteractable.Interact(this);
                }
            }
            
            // Nếu GIỮ phím F/Ctrl -> Tương tác liên tục (Thái đồ / Rửa bát / Chờ lấy đồ)
            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.LeftControl))
            {
                if (_hoveredInteractable != null)
                {
                    _hoveredInteractable.InteractHold(this, Time.deltaTime);
                }
            }
            // Nếu NHẢ phím F/Ctrl -> Huỷ tương tác liên tục
            else if (Input.GetKeyUp(KeyCode.F) || Input.GetKeyUp(KeyCode.LeftControl))
            {
                if (_hoveredInteractable != null)
                {
                    _hoveredInteractable.CancelInteract();
                }
            }
        }

        // =====================================================
        // PRIVATE — INTERACTION LOGIC
        // =====================================================

        /// <summary>
        /// Bắn OverlapCircle về hướng đang nhìn liên tục mỗi frame.
        /// Nếu trúng, hiện chữ E lên trên object đó.
        /// </summary>
        private void UpdateHoveredInteractable()
        {
            Vector2 checkCenter = (Vector2)transform.position + _movement.FacingDir * _interactDistance;
            Collider2D hit = Physics2D.OverlapCircle(checkCenter, _interactRadius, _interactableMask);

            if (hit != null)
            {
                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    _hoveredInteractable = interactable;
                    _hoveredTransform = hit.transform;

                    bool isHoldingF = Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.LeftControl);

                    bool showE = interactPromptE != null && interactable.CanInteract(this) && !isHoldingF;
                    bool showF = interactPromptF != null && interactable.CanInteractHold(this) && !isHoldingF;

                    if (interactPromptE != null) interactPromptE.SetActive(showE);
                    if (interactPromptF != null) interactPromptF.SetActive(showF);

                    // Vị trí gốc trên đầu object
                    Vector3 basePos = _hoveredTransform.position + Vector3.up * 1.2f;

                    if (showE && showF)
                    {
                        // Nếu hiện cả 2 nút -> Xếp dàn sang 2 bên cho khỏi đè nhau
                        interactPromptE.transform.position = basePos + Vector3.left * 0.35f;
                        interactPromptF.transform.position = basePos + Vector3.right * 0.35f;
                    }
                    else if (showE)
                    {
                        // Chỉ hiện E -> Đứng giữa
                        interactPromptE.transform.position = basePos;
                    }
                    else if (showF)
                    {
                        // Chỉ hiện F -> Đứng giữa
                        interactPromptF.transform.position = basePos;
                    }
                    return;
                }
            }

            // Nếu không quét trúng gì thì tắt hết UI đi
            _hoveredInteractable = null;
            _hoveredTransform = null;
            if (interactPromptE != null) interactPromptE.SetActive(false);
            if (interactPromptF != null) interactPromptF.SetActive(false);
        }

        // =====================================================
        // PUBLIC — API để IInteractable gọi vào
        // =====================================================

        /// <summary>
        /// Nhận đồ ăn vào tay. Được gọi bởi IInteractable (VD: IngredientSource).
        /// Gán Parent về holdPoint và reset vị trí về tâm bàn tay.
        /// </summary>
        public bool PickupFood(FoodItem food)
        {
            if (IsHoldingFood)
            {
                Debug.Log("[PlayerInteraction] Tay đang cầm rồi, không nhận thêm được!");
                return false;
            }

            if (food == null)
            {
                Debug.LogWarning("[PlayerInteraction] PickupFood nhận được null!", this);
                return false;
            }

            CurrentHeldFood = food;

            // Gắn đồ ăn vào tay Player — sẽ di chuyển cùng Player tự động
            CurrentHeldFood.transform.SetParent(holdPoint);
            CurrentHeldFood.transform.localPosition = Vector3.zero;
            CurrentHeldFood.transform.localRotation = Quaternion.identity;

            Debug.Log($"[PlayerInteraction] Đã cầm: <b>{food.CurrentData?.id ?? "unknown"}</b>");
            return true;
        }

        /// <summary>
        /// Thả đồ ăn ra. Trả về FoodItem để bàn/station có thể nhận.
        /// </summary>
        public FoodItem ReleaseFood()
        {
            if (!IsHoldingFood) return null;

            FoodItem food = CurrentHeldFood;
            CurrentHeldFood = null;

            food.transform.SetParent(null); // Tách khỏi tay Player
            Debug.Log($"[PlayerInteraction] Đã thả: <b>{food.CurrentData?.id ?? "unknown"}</b>");

            return food;
        }

        // =====================================================
        // EDITOR GIZMO — Vẽ vùng tương tác trong Scene View
        // =====================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_movement == null) _movement = GetComponent<PlayerMovement>();

            Vector2 dir    = Application.isPlaying ? _movement.FacingDir : Vector2.down;
            Vector2 center = (Vector2)transform.position + dir * _interactDistance;

            Gizmos.color = IsHoldingFood ? Color.yellow : Color.white;
            Gizmos.DrawWireSphere(center, _interactRadius);
        }
#endif
    }
}
