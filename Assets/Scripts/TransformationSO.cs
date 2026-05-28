using UnityEngine;
using MyOvercooked.Data; // Nhớ đổi theo namespace của ông

namespace MyOvercooked.Data
{
    [CreateAssetMenu(fileName = "NewTransformation", menuName = "MyOvercooked/Data/Transformation")]
    public class TransformationSO : ScriptableObject
    {
        [Header("Recipe Requirements")]
        [Tooltip("Những nguyên liệu cần có. Có thể là 1 hoặc nhiều món.")]
        public FoodStateSO[] inputs; 
        
        [Tooltip("Loại bàn bếp yêu cầu")]
        public StationType requiredStation; 

        [Tooltip("Thời gian nấu (giây). Đặt = 0 nếu muốn ra món ngay lập tức (như thái thớt).")]
        public float duration; 

        [Header("Result")]
        [Tooltip("Món ăn sinh ra sau khi hoàn thành")]
        public FoodStateSO output; 
    }
}