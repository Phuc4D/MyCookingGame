using UnityEngine;
using MyOvercooked.Data; 

namespace MyOvercooked.Data
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "MyOvercooked/Data/Recipe Order")]
    public class RecipeSO : ScriptableObject
    {
        [Header("Order Details")]
        public string recipeName;
        
        [Tooltip("Món ăn cuối cùng khách yêu cầu (Category phải là Meal)")]
        public FoodStateSO finalMeal;
        
        public Sprite recipeCardSprite; // Hình ảnh tờ bill hiển thị trên UI
        
        [Header("Rewards & Rules")]
        public int scoreValue; 
        public float timeLimit; // Giây
    }
}