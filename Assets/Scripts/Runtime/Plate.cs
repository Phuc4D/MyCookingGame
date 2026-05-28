using System.Collections.Generic;
using UnityEngine;
using MyOvercooked.Data;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Component xử lý logic của cái Dĩa.
    /// Cho phép gộp nhiều nguyên liệu vào để tạo thành món ăn hoàn chỉnh (Meal).
    /// </summary>
    [RequireComponent(typeof(FoodItem))]
    public class Plate : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [Tooltip("Database để check công thức kết hợp món")]
        [SerializeField] private RecipeDatabaseSO _recipeDatabase;

        private FoodItem _foodItem;
        private List<FoodStateSO> _ingredients = new List<FoodStateSO>();

        private void Awake()
        {
            _foodItem = GetComponent<FoodItem>();
        }

        // =========================================================
        // IInteractable Implementation
        // =========================================================

        public bool CanInteract(PlayerInteraction player)
        {
            // Có thể tương tác nếu Player đang cầm đồ ăn (để bỏ vào dĩa)
            if (player.IsHoldingFood)
            {
                FoodItem heldFood = player.CurrentHeldFood;
                // Không cho bỏ dĩa vào dĩa
                if (heldFood.GetComponent<Plate>() != null) return false;
                
                // Chỉ nhận các loại nguyên liệu (Raw, Chopped, Cooked)
                // Meal là món đã hoàn thành trên dĩa rồi nên không nhận thêm vào dĩa khác
                return heldFood.CurrentData.category != FoodCategory.Meal;
            }
            return false;
        }

        public bool CanInteractHold(PlayerInteraction player) => false;

        public void Interact(PlayerInteraction player)
        {
            if (CanInteract(player))
            {
                AddIngredient(player.ReleaseFood());
            }
        }

        public bool InteractHold(PlayerInteraction player, float deltaTime) => false;
        public void CancelInteract() { }

        // =========================================================
        // Plate Logic
        // =========================================================

        /// <summary>
        /// Thêm nguyên liệu vào dĩa và kiểm tra công thức.
        /// </summary>
        public void AddIngredient(FoodItem food)
        {
            if (food == null) return;

            _ingredients.Add(food.CurrentData);
            Debug.Log($"[Plate] Đã nhận: <b>{food.CurrentData.id}</b>. Hiện có {_ingredients.Count} món.");

            // Xóa object nguyên liệu sau khi đã "nhét" vào dĩa
            Destroy(food.gameObject);

            // Cập nhật trạng thái dĩa (biến thành món ăn nếu đủ bộ)
            UpdatePlateVisual();
        }

        private void UpdatePlateVisual()
        {
            if (_recipeDatabase == null) return;

            // Kiểm tra xem tổ hợp nguyên liệu hiện tại có tạo thành món gì không
            // StationType.PlatingTable là loại bàn dùng cho việc trình bày dĩa
            if (_recipeDatabase.TryGetTransformation(_ingredients.ToArray(), StationType.PlatingTable, out TransformationSO result))
            {
                // Thay đổi sprite của dĩa thành sprite của món ăn hoàn chỉnh
                _foodItem.Setup(result.output);
                Debug.Log($"[Plate] Đã hoàn thành món: <color=green><b>{result.output.displayName}</b></color>");
            }
            else
            {
                // Nếu chưa thành món, có thể thêm logic hiện các nguyên liệu nhỏ (icons) ở đây nếu muốn
            }
        }

        public List<FoodStateSO> GetIngredients() => _ingredients;
    }
}
