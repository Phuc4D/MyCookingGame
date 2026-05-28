using UnityEngine;
using MyOvercooked.Data;

public class DebugSpawner : MonoBehaviour
{
    [Header("Settings")]
    public FoodStateSO testFoodData;     // Kéo trái cà chua vào đây
    public GameObject foodItemPrefab;    // Kéo FoodItem_Prefab vào đây

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnTest();
        }
    }

    void SpawnTest()
    {
        if (testFoodData == null || foodItemPrefab == null)
        {
            Debug.LogError("[DebugSpawner] Quên chưa kéo Cà chua hoặc Prefab vào Inspector kìa!");
            return;
        }

        Debug.Log("[DebugSpawner] Đang thử spawn trực tiếp...");
        
        GameObject spawned = Instantiate(foodItemPrefab, transform.position, Quaternion.identity);
        FoodItem item = spawned.GetComponent<FoodItem>();
        
        if (item != null)
        {
            item.Setup(testFoodData);
            Debug.Log($"[DebugSpawner] Spawn thành công: {testFoodData.id}. Kiểm tra Hierarchy xem có nó chưa!");
        }
    }
}
