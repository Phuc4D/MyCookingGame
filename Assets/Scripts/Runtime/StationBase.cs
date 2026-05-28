using UnityEngine;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Base class cho tất cả các quầy/bàn trong game.
    /// Quản lý slot chứa đồ ăn — kế thừa class này để không viết lại code lặp.
    /// </summary>
    public abstract class StationBase : MonoBehaviour
    {
        [Header("Station Settings")]
        [Tooltip("Vị trí đặt đồ ăn (Tạo 1 Empty Object làm con của Quầy và kéo vào đây)")]
        public Transform foodSlot;

        /// <summary>Đồ ăn đang nằm trên bàn hiện tại</summary>
        public FoodItem CurrentFood { get; protected set; }

        /// <summary>Kiểm tra xem bàn có đang có đồ không</summary>
        public bool HasFood => CurrentFood != null;

        /// <summary>
        /// Đặt đồ ăn lên bàn.
        /// Trả về false nếu bàn đang bận hoặc newFood là null.
        /// </summary>
        public virtual bool TryAddFood(FoodItem newFood)
        {
            if (HasFood || newFood == null) return false;

            CurrentFood = newFood;

            // Di chuyển object vào đúng tâm mặt bàn
            CurrentFood.transform.position = foodSlot.position;
            CurrentFood.transform.SetParent(foodSlot); // Gom nhóm vào cho gọn Hierarchy

            return true;
        }

        /// <summary>
        /// Lấy đồ ăn khỏi bàn.
        /// Trả về null nếu bàn đang trống.
        /// </summary>
        public virtual FoodItem TryRemoveFood()
        {
            if (!HasFood) return null;

            FoodItem foodToReturn = CurrentFood;
            CurrentFood.transform.SetParent(null); // Tách khỏi bàn
            CurrentFood = null;

            return foodToReturn;
        }
    }
}
