#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyOvercooked.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MyOvercooked.Runtime
{
    public sealed class Customer : MonoBehaviour, IInteractable
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _arrivalThreshold = 0.05f;

        [Header("Order Bubble (child refs)")]
        [SerializeField] private GameObject _orderBubble = null!;
        [SerializeField] private Image _recipeImage = null!;
        [SerializeField] private Slider _patienceBar = null!;

        private Animator _animator = null!;
        private RecipeSO _recipe = null!;
        private WaitingSpot _waitingSpot = null!;
        private Vector2 _exitPoint;
        private Action? _onExit;

        private bool _served;
        private bool _isWaiting;

        // ── IInteractable ──────────────────────────────────────

        public bool CanInteract(PlayerInteraction player) =>
            _isWaiting && player.IsHoldingFood;

        public bool CanInteractHold(PlayerInteraction player) => false;

        public void Interact(PlayerInteraction player)
        {
            if (!CanInteract(player)) return;
            FoodItem? food = player.ReleaseFood();
            if (food == null) return;
            if (food.CurrentData == _recipe.finalMeal)
                _served = true;
            Destroy(food.gameObject);
        }

        public bool InteractHold(PlayerInteraction player, float deltaTime) => false;

        public void CancelInteract() { }

        // ── Init ───────────────────────────────────────────────

        public void Init(
            WaitingSpot spot,
            RecipeSO recipe,
            Vector2 exitPoint,
            Action onExit)
        {
            _waitingSpot = spot;
            _recipe = recipe;
            _exitPoint = exitPoint;
            _onExit = onExit;

            spot.Claim();

            if (_recipeImage != null) _recipeImage.sprite = recipe.recipeCardSprite;
            if (_patienceBar != null) _patienceBar.value = 1f;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            _waitingSpot?.Release();
        }

        // ── Lifecycle ──────────────────────────────────────────

        public void RunLifecycle()
        {
            RunLifecycleAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunLifecycleAsync(CancellationToken ct)
        {
            if (_orderBubble != null) _orderBubble.SetActive(false);

            await WalkToAsync(_waitingSpot.transform.position, ct);

            if (_orderBubble != null) _orderBubble.SetActive(true);
            _isWaiting = true;

            bool servedInTime = await WaitForServiceAsync(ct);

            _isWaiting = false;
            if (_orderBubble != null) _orderBubble.SetActive(false);

            if (servedInTime)
                _animator.SetBool("IsJoyful", true);
            else
                _animator.SetBool("IsAngry", true);

            await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: ct);

            _animator.SetBool("IsJoyful", false);
            _animator.SetBool("IsAngry", false);

            await WalkToAsync(_exitPoint, ct);

            _onExit?.Invoke();
            Destroy(gameObject);
        }

        private async UniTask WalkToAsync(Vector2 target, CancellationToken ct)
        {
            _animator.SetBool("IsMoving", true);

            while (Vector2.Distance(transform.position, target) > _arrivalThreshold)
            {
                Vector3 currentPos = transform.position;
                Vector2 dir = (target - (Vector2)currentPos).normalized;

                // Xác định hướng chủ đạo để Animator nhận diện đúng hướng (Up/Down/Left/Right)
                // Ép về giá trị -1, 0, 1 để tránh lỗi Blend Tree khi giá trị lẻ
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    _animator.SetFloat("MoveX", Mathf.Sign(dir.x));
                    _animator.SetFloat("MoveY", 0f);
                }
                else
                {
                    _animator.SetFloat("MoveX", 0f);
                    _animator.SetFloat("MoveY", Mathf.Sign(dir.y));
                }

                // Giữ nguyên trục Z để tránh lỗi layer hiển thị
                Vector3 nextPos = Vector2.MoveTowards(currentPos, target, _walkSpeed * Time.deltaTime);
                nextPos.z = currentPos.z;
                transform.position = nextPos;

                await UniTask.Yield(ct);
            }

            transform.position = new Vector3(target.x, target.y, transform.position.z);
            _animator.SetBool("IsMoving", false);
        }

        private async UniTask<bool> WaitForServiceAsync(CancellationToken ct)
        {
            float elapsed = 0f;
            float limit = _recipe.timeLimit;

            while (elapsed < limit)
            {
                if (_served) return true;
                elapsed += Time.deltaTime;
                if (_patienceBar != null)
                    _patienceBar.value = 1f - elapsed / limit;
                await UniTask.Yield(ct);
            }

            return false;
        }
    }
}
