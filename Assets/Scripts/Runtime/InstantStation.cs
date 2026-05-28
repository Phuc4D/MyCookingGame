using UnityEngine;
using VContainer;
using MyOvercooked.Data;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Thớt chặt (Chopping Board) — biến đổi nguyên liệu bằng cách giữ phím.
    /// Kế thừa StationBase để chứa đồ, và IInteractable để Player tương tác.
    /// </summary>
    public class InstantStation : StationBase, IInteractable
    {
        [Header("Station Settings")]
        [Tooltip("Loại bàn (thường là ChoppingBoard)")]
        public StationType stationType = StationType.ChoppingBoard;

        [Header("UI")]
        [Tooltip("Kéo vòng tròn ProgressUI của bàn này vào đây (có thể tái sử dụng prefab của IngredientSource)")]
        public ProgressUI progressUI;

        [Tooltip("Kéo file RecipeDatabaseSO vào đây nếu không dùng VContainer")]
        [SerializeField] private RecipeDatabaseSO _recipeDatabase;
        private float _currentHoldTime = 0f;

        [Inject]
        public void Construct(RecipeDatabaseSO recipeDatabase)
        {
            if (recipeDatabase != null) _recipeDatabase = recipeDatabase;
        }

        // =========================================================
        // Khả năng tương tác (Dùng cho UI Prompt)
        // =========================================================
        public bool CanInteract(PlayerInteraction player)
        {
            // Bấm E khi: (tay rảnh, bàn có đồ) HOẶC (tay có đồ, bàn trống)
            return (!player.IsHoldingFood && HasFood) || (player.IsHoldingFood && !HasFood);
        }

        public bool CanInteractHold(PlayerInteraction player)
        {
            // Bấm F khi bàn có đồ và có công thức hợp lệ
            if (!HasFood || _recipeDatabase == null) return false;
            FoodStateSO[] inputs = new FoodStateSO[] { CurrentFood.CurrentData };
            return _recipeDatabase.TryGetTransformation(inputs, stationType, out _);
        }

        // =========================================================
        // Bấm 1 lần (Interact): Đặt đồ / Lấy đồ
        // =========================================================
        public void Interact(PlayerInteraction player)
        {
            // Trường hợp 1: Tay rảnh, Bàn có đồ -> Nhặt lên
            if (!player.IsHoldingFood && HasFood)
            {
                FoodItem foodToTake = TryRemoveFood();
                player.PickupFood(foodToTake);
                return;
            }

            // Trường hợp 2: Tay có đồ, Bàn trống -> Đặt xuống
            if (player.IsHoldingFood && !HasFood)
            {
                FoodItem foodToDrop = player.ReleaseFood();
                TryAddFood(foodToDrop);
                return;
            }

            // Trường hợp 3: Tay có đồ & Bàn cũng có đồ
            if (player.IsHoldingFood && HasFood)
            {
                // Nếu vật trên bàn là cái dĩa -> Thử bỏ đồ vào dĩa
                Plate plate = CurrentFood.GetComponent<Plate>();
                if (plate != null && plate.CanInteract(player))
                {
                    plate.Interact(player);
                    return;
                }
                
                // Ngược lại, nếu vật TRÊN TAY là cái dĩa -> Thử nhặt đồ trên bàn bỏ vào dĩa
                Plate heldPlate = player.CurrentHeldFood.GetComponent<Plate>();
                if (heldPlate != null)
                {
                    FoodItem foodOnStation = TryRemoveFood();
                    heldPlate.AddIngredient(foodOnStation);
                    return;
                }
            }

            Debug.Log("[InstantStation] Không thể tương tác (bàn đã có đồ hoặc tay đang có đồ)!");
        }

        // =========================================================
        // Giữ phím (InteractHold): Xử lý thái/biến đổi đồ ăn
        // =========================================================
        public bool InteractHold(PlayerInteraction player, float deltaTime)
        {
            // Chỉ làm việc nếu bàn có đồ
            if (!HasFood) return false;

            // Truy vấn database xem món trên bàn có công thức nào phù hợp với thớt này không
            FoodStateSO[] inputs = new FoodStateSO[] { CurrentFood.CurrentData };

            if (_recipeDatabase.TryGetTransformation(inputs, stationType, out TransformationSO result))
            {
                _currentHoldTime += deltaTime;
                
                // Nếu duration = 0 (tức thời), mình set tạm 0.5s để có cảm giác "thái" 1 nhịp nhỏ
                float requiredTime = result.duration > 0 ? result.duration : 0.5f; 

                if (progressUI != null)
                {
                    progressUI.ShowProgress(_currentHoldTime, requiredTime);
                }

                // Nếu giữ phím đủ thời gian -> Chặt xong
                if (_currentHoldTime >= requiredTime)
                {
                    // "Bơm kịch bản mới vào diễn viên cũ"
                    CurrentFood.Setup(result.output);
                    Debug.Log($"[InstantStation] Pặc pặc pặc! Đã biến thành: <b>{result.output.id}</b>");
                    
                    ResetProgress();
                    return false; // Trả về false để huỷ quá trình giữ phím (chờ user nhả ra bấm lại)
                }

                return true;
            }

            // Món trên bàn không thể xử lý ở trạm này
            return false;
        }

        // =========================================================
        // Nhả phím: Huỷ quá trình thái dở
        // =========================================================
        public void CancelInteract()
        {
            ResetProgress();
        }

        private void ResetProgress()
        {
            _currentHoldTime = 0f;
            if (progressUI != null)
            {
                progressUI.Hide();
            }
        }
    }
}
