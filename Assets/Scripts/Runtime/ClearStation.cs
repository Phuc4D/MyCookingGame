using UnityEngine;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Bàn để đồ bình thường (Counter).
    /// Hỗ trợ đặt đồ xuống, nhặt đồ lên và tương tác với Dĩa.
    /// </summary>
    public class ClearStation : StationBase, IInteractable
    {
        public bool CanInteract(PlayerInteraction player)
        {
            // Bấm E khi:
            // 1. Tay có đồ (để đặt xuống hoặc bỏ vào dĩa đang nằm trên bàn)
            // 2. Tay rảnh nhưng trên bàn có đồ (để nhặt lên)
            return (player.IsHoldingFood) || (!player.IsHoldingFood && HasFood);
        }

        public bool CanInteractHold(PlayerInteraction player) => false;

        public void Interact(PlayerInteraction player)
        {
            // --- Trường hợp: Cả tay và bàn đều có đồ (Xử lý logic Dĩa) ---
            if (player.IsHoldingFood && HasFood)
            {
                // 1. Nếu trên bàn là cái dĩa -> Bỏ đồ trên tay vào dĩa
                Plate plateOnStation = CurrentFood.GetComponent<Plate>();
                if (plateOnStation != null && plateOnStation.CanInteract(player))
                {
                    plateOnStation.Interact(player);
                    return;
                }

                // 2. Nếu trên tay là cái dĩa -> Nhặt đồ trên bàn bỏ vào dĩa
                Plate heldPlate = player.CurrentHeldFood.GetComponent<Plate>();
                if (heldPlate != null)
                {
                    // Lấy đồ trên bàn ra
                    FoodItem foodOnStation = TryRemoveFood();
                    // Thêm vào dĩa trên tay
                    heldPlate.AddIngredient(foodOnStation);
                    return;
                }
            }

            // --- Trường hợp: Một trong hai bên trống (Nhặt/Thả bình thường) ---
            if (player.IsHoldingFood && !HasFood)
            {
                // Đặt đồ xuống bàn
                FoodItem foodToDrop = player.ReleaseFood();
                TryAddFood(foodToDrop);
            }
            else if (!player.IsHoldingFood && HasFood)
            {
                // Nhặt đồ từ bàn lên
                FoodItem foodToTake = TryRemoveFood();
                player.PickupFood(foodToTake);
            }
        }

        public bool InteractHold(PlayerInteraction player, float deltaTime) => false;
        public void CancelInteract() { }
    }
}
