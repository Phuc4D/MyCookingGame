using UnityEngine;
using MyOvercooked.Data;
using MyOvercooked.Runtime;

/// <summary>
/// Thùng nguyên liệu — Gắn vào các thùng gỗ trên Map.
/// Implement IInteractable: khi Player tương tác, spawn FoodItem và nhét vào tay Player.
/// </summary>
public class IngredientSource : MonoBehaviour, IInteractable
{
    [Header("Nguyên liệu cung cấp")]
    [Tooltip("Kéo FoodStateSO (VD: Cá sống, Thịt sống...) vào đây từ Inspector")]
    public FoodStateSO spawnItemData;

    [Header("Prefab & Spawn Point")]
    [Tooltip("Prefab FoodItem cần Instantiate (phải có component FoodItem)")]
    public GameObject foodItemPrefab;

    [Tooltip("Vị trí spawn tạm (không cần thiết vì item sẽ ngay lập tức về holdPoint của Player)")]
    public Transform spawnPoint;

    [Header("UI Progress")]
    [Tooltip("Kéo component ProgressUI (Cái vòng tròn) của thùng này vào đây")]
    public ProgressUI progressUI;
    
    [Tooltip("Thời gian cần giữ phím E để lấy đồ (giây)")]
    public float holdRequiredTime = 1f;

    private float _currentHoldTime = 0f;

    // --------------------------------------------------------
    // IInteractable — Khả năng tương tác (Dùng cho UI Prompt)
    // --------------------------------------------------------
    public bool CanInteract(PlayerInteraction player) => false; // Không dùng nút E
    public bool CanInteractHold(PlayerInteraction player) => !player.IsHoldingFood; // Chỉ dùng F nếu tay rảnh

    // --------------------------------------------------------
    // IInteractable — Được PlayerInteraction gọi khi Player bấm E 1 lần
    // --------------------------------------------------------
    public void Interact(PlayerInteraction player)
    {
        // Tay Player đang cầm rồi → không làm gì
        if (player.IsHoldingFood)
        {
            Debug.Log($"[IngredientSource] Player đang cầm đồ rồi, không lấy thêm được!");
            return;
        }

        // Đã chuyển code nhặt đồ xuống hàm InteractHold
        // Bấm 1 lần sẽ không lấy được đồ nữa
        Debug.Log("[IngredientSource] Bạn phải GIỮ phím E để lấy đồ!");
    }

    // --------------------------------------------------------
    // IInteractable — Được PlayerInteraction gọi LÊN TỤC khi Player GIỮ E
    // --------------------------------------------------------
    public bool InteractHold(PlayerInteraction player, float deltaTime)
    {
        if (player.IsHoldingFood) return false;

        _currentHoldTime += deltaTime;

        if (progressUI != null)
        {
            progressUI.ShowProgress(_currentHoldTime, holdRequiredTime);
        }

        // Đủ thời gian -> Lấy đồ
        if (_currentHoldTime >= holdRequiredTime)
        {
            SpawnAndGiveFood(player);
            
            // Reset
            _currentHoldTime = 0f;
            if (progressUI != null) progressUI.Hide();
        }

        return true;
    }

    public void CancelInteract()
    {
        // Khi nhả E ra trước khi đầy cây -> Reset
        _currentHoldTime = 0f;
        if (progressUI != null) progressUI.Hide();
    }

    private void SpawnAndGiveFood(PlayerInteraction player)
    {
        if (spawnItemData == null || foodItemPrefab == null) return;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject spawnedObj = Instantiate(foodItemPrefab, position, Quaternion.identity);

        FoodItem foodItem = spawnedObj.GetComponent<FoodItem>();
        if (foodItem != null)
        {
            foodItem.Setup(spawnItemData);
            player.PickupFood(foodItem);
        }
        else
        {
            Destroy(spawnedObj);
        }
    }

    // --------------------------------------------------------
    // Gizmo — Hiển thị trong Scene View
    // --------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.2f);
        UnityEditor.Handles.Label(pos + Vector3.up * 0.3f,
            spawnItemData != null ? $"Source: {spawnItemData.id}" : "Source: (chưa gán)");
    }
#endif
}
