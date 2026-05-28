using System.Collections.Generic;
using System.Linq; // Cần cái này cho vụ check thứ tự mảng
using UnityEngine;

namespace MyOvercooked.Data
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "MyOvercooked/Data/Recipe Database")]
    public class RecipeDatabaseSO : ScriptableObject
    {
        [Header("Databases")]
        public List<TransformationSO> transformations = new List<TransformationSO>();
        public List<RecipeSO> recipes = new List<RecipeSO>();

        /// <summary>
        /// Query tìm công thức dựa trên nguyên liệu đang có trên bàn và loại bàn
        /// </summary>
        public bool TryGetTransformation(FoodStateSO[] currentInputs, StationType station, out TransformationSO result)
        {
            result = null;

            foreach (var trans in transformations)
            {
                // 1. Kiểm tra loại trạm làm việc trước (Lọc nhanh)
                if (trans.requiredStation != station) continue;

                // 2. Kiểm tra số lượng nguyên liệu có khớp không
                if (trans.inputs.Length != currentInputs.Length) continue;

                // 3. So sánh 2 mảng nguyên liệu (Không phân biệt thứ tự bỏ vào)
                if (AreArraysEqualUnordered(trans.inputs, currentInputs))
                {
                    result = trans;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Query xem cái đĩa thức ăn đem giao có nằm trong danh sách đơn hàng không
        /// </summary>
        public bool TryMatchRecipe(FoodStateSO mealToServe, out RecipeSO result)
        {
            result = null;
            foreach (var recipe in recipes)
            {
                if (recipe.finalMeal == mealToServe)
                {
                    result = recipe;
                    return true;
                }
            }
            return false;
        }

        // --- HÀM HELPER HỖ TRỢ DƯỚI LOCAL ---

        private bool AreArraysEqualUnordered(FoodStateSO[] arr1, FoodStateSO[] arr2)
        {
            // Sắp xếp theo ID định danh nội bộ của Unity (GetInstanceID) để bảo đảm thứ tự luôn đồng nhất
            var sorted1 = arr1.OrderBy(x => x.GetInstanceID());
            var sorted2 = arr2.OrderBy(x => x.GetInstanceID());
            
            return sorted1.SequenceEqual(sorted2);
        }
    }
}