using UnityEngine;
using MyOvercooked.Data;

public class FoodItem : MonoBehaviour
{
   [SerializeField] private FoodStateSO _currentData;
   public FoodStateSO CurrentData => _currentData;

   private SpriteRenderer _spriteRenderer; 

   private void Awake(){
    _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    if(_spriteRenderer == null) Debug.LogError($"[FoodItem] {gameObject.name} thiếu SpriteRenderer!");
   }
   public void Setup(FoodStateSO newData){
    if(newData == null){
        Debug.LogWarning("Không thể Setup FoodItem vì dữ liệu truyền vào bị null!");
        return;
    }

    _currentData = newData;
    if(_spriteRenderer != null) _spriteRenderer.sprite = newData.sprite;
    gameObject.name = $"FoodItem_{newData.id}";
   }
}
