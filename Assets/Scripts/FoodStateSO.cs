using UnityEngine;
// Nhớ import namespace chứa enum của ông
using MyOvercooked.Data; 

namespace MyOvercooked.Data
{
    // Attribute này giúp ông click chuột phải trong Unity Editor -> Create -> MyOvercooked -> Food State để tạo file
    [CreateAssetMenu(fileName = "NewFoodState", menuName = "MyOvercooked/Data/Food State")]
    public class FoodStateSO : ScriptableObject
    {
        [Header("Food Identification")]
        [Tooltip("ID duy nhất của món ăn, ví dụ: 'chopped_fish'")]
        public string id; 
        
        [Tooltip("Tên sẽ hiển thị trên UI cho người chơi xem")]
        public string displayName; 

        [Header("Visuals")]
        [Tooltip("Hình ảnh 2D của món ăn")]
        public Sprite sprite; 

        [Header("Properties")]
        [Tooltip("Phân loại trạng thái của món ăn này")]
        public FoodCategory category; 
    }
}