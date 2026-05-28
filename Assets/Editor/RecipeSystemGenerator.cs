using UnityEngine;
using UnityEditor;
using System.IO;
using MyOvercooked.Data;

namespace MyOvercooked.Editor
{
    /// <summary>
    /// Tự động tạo TransformationSO, RecipeSO, và RecipeDatabaseSO assets.
    /// Chạy qua menu: Tools > MyOvercooked > Generate Recipe System
    /// LƯU Ý: Chạy "Generate Food Assets" trước để có đủ FoodStateSO assets.
    /// </summary>
    public static class RecipeSystemGenerator
    {
        // ==================== PATH CONFIG ====================
        private const string FOOD_DATA_FOLDER      = "Assets/Data/Foods";
        private const string TRANSFORM_DATA_FOLDER = "Assets/Data/Transformations";
        private const string RECIPE_DATA_FOLDER    = "Assets/Data/Recipes";
        private const string DATABASE_DATA_FOLDER  = "Assets/Data";
        private const string DATABASE_ASSET_NAME   = "RecipeDatabase";

        // ==================== MENU ENTRY ====================
        [MenuItem("Tools/MyOvercooked/Generate Recipe System")]
        public static void GenerateRecipeSystem()
        {
            // Đảm bảo FoodStateSO đã tồn tại
            if (!AssetDatabase.IsValidFolder(FOOD_DATA_FOLDER))
            {
                Debug.LogError("[RecipeSystemGenerator] Không tìm thấy thư mục FoodStateSO. " +
                               "Hãy chạy 'Tools > Generate Food Assets' trước!");
                return;
            }

            EnsureFolderExists(TRANSFORM_DATA_FOLDER);
            EnsureFolderExists(RECIPE_DATA_FOLDER);

            int transCount  = 0;
            int recipeCount = 0;

            // ================================================
            // BƯỚC 1: TẠO TRANSFORMATIONS
            // ================================================

            // --- CHOPPING BOARD (Duration = 3s) ---
            transCount += CreateTransformation("Chop_Fish",         new[]{"fish"},                              "chopped_fish",         StationType.ChoppingBoard, 3f);
            transCount += CreateTransformation("Chop_Meat",         new[]{"meat"},                              "chopped_meat",         StationType.ChoppingBoard, 3f);
            transCount += CreateTransformation("Chop_Onion",        new[]{"onion"},                             "chopped_onion",        StationType.ChoppingBoard, 3f);
            transCount += CreateTransformation("Chop_Tomato",       new[]{"tomato"},                            "chopped_tomato",       StationType.ChoppingBoard, 3f);
            transCount += CreateTransformation("Chop_Pickle",       new[]{"pickle"},                            "chopped_pickle",       StationType.ChoppingBoard, 3f);

            // --- STOVE — Nấu đơn (Duration = 5s) ---
            transCount += CreateTransformation("Cook_Fish",         new[]{"fish"},                              "cooked_fish",          StationType.Stove, 5f);
            transCount += CreateTransformation("Cook_Meat",         new[]{"meat"},                              "cooked_meat",          StationType.Stove, 5f);
            transCount += CreateTransformation("Cook_Onion",        new[]{"onion"},                             "cooked_onion",         StationType.Stove, 5f);
            transCount += CreateTransformation("Cook_ChoppedFish",  new[]{"chopped_fish"},                      "cooked_chopped_fish",  StationType.Stove, 5f);
            transCount += CreateTransformation("Cook_ChoppedMeat",  new[]{"chopped_meat"},                      "cooked_chopped_meat",  StationType.Stove, 5f);

            // --- STOVE — Nấu combo (Duration = 7s) ---
            transCount += CreateTransformation("Cook_FishTomato",   new[]{"fish", "chopped_tomato"},            "cooked_fish_tomato",   StationType.Stove, 7f);
            transCount += CreateTransformation("Cook_FishOnion",    new[]{"fish", "chopped_onion"},             "cooked_fish_onion",    StationType.Stove, 7f);
            transCount += CreateTransformation("Cook_MeatTomato",   new[]{"meat", "chopped_tomato"},            "cooked_meat_tomato",   StationType.Stove, 7f);
            transCount += CreateTransformation("Cook_MeatOnion",    new[]{"meat", "chopped_onion"},             "cooked_meat_onion",    StationType.Stove, 7f);
            transCount += CreateTransformation("Cook_MeatPickle",   new[]{"meat", "chopped_pickle"},            "cooked_meat_pickle",   StationType.Stove, 7f);
            transCount += CreateTransformation("Cook_TomatoSoup",   new[]{"chopped_tomato"},                    "tomato_soup",          StationType.Stove, 5f);

            // --- STOVE — Overcook: bất kỳ món nấu xong để thêm 5s → struggle_meal ---
            transCount += CreateTransformation("Overcook_Fish",       new[]{"cooked_fish"},        "struggle_meal", StationType.Stove, 5f);
            transCount += CreateTransformation("Overcook_Meat",       new[]{"cooked_meat"},        "struggle_meal", StationType.Stove, 5f);
            transCount += CreateTransformation("Overcook_FishTomato", new[]{"cooked_fish_tomato"}, "struggle_meal", StationType.Stove, 5f);
            transCount += CreateTransformation("Overcook_FishOnion",  new[]{"cooked_fish_onion"},  "struggle_meal", StationType.Stove, 5f);
            transCount += CreateTransformation("Overcook_MeatTomato", new[]{"cooked_meat_tomato"}, "struggle_meal", StationType.Stove, 5f);
            transCount += CreateTransformation("Overcook_MeatOnion",  new[]{"cooked_meat_onion"},  "struggle_meal", StationType.Stove, 5f);

            // --- PLATING TABLE (Duration = 0s = tức thì) ---
            // Sushi: cần nấu chopped_fish trước, sau đó kết hợp với rice
            transCount += CreateTransformation("Plate_Sushi",          new[]{"cooked_chopped_fish", "rice"}, "sushi",          StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_SushiTomato",    new[]{"sushi", "chopped_tomato"},     "sushi_tomato",   StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_SushiPickle",    new[]{"sushi", "chopped_pickle"},     "sushi_pickle",   StationType.PlatingTable, 0f);

            // Fish meals
            transCount += CreateTransformation("Plate_FishMeal",       new[]{"cooked_fish"},         "fish_meal",       StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_FishMealTomato", new[]{"cooked_fish_tomato"},  "fish_meal_tomato",StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_FishMealOnion",  new[]{"cooked_fish_onion"},   "fish_meal_onion", StationType.PlatingTable, 0f);

            // Meat meals
            transCount += CreateTransformation("Plate_MeatMeal",       new[]{"cooked_meat"},         "meat_meal",       StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_MeatMealTomato", new[]{"cooked_meat_tomato"},  "meat_meal_tomato",StationType.PlatingTable, 0f);
            transCount += CreateTransformation("Plate_MeatMealOnion",  new[]{"cooked_meat_onion"},   "meat_meal_onion", StationType.PlatingTable, 0f);

            // ================================================
            // BƯỚC 2: TẠO RECIPES (Đơn hàng của khách)
            // scoreValue: đơn giản=50, có topping=80, sushi=100, soup=60
            // timeLimit:  đơn giản=60s, phức tạp=90s
            // ================================================
            recipeCount += CreateRecipe("Recipe_FishMeal",       "fish_meal",        "Cá Chiên",             50,  60f);
            recipeCount += CreateRecipe("Recipe_FishMealTomato", "fish_meal_tomato", "Cá Chiên Cà Chua",     80,  90f);
            recipeCount += CreateRecipe("Recipe_FishMealOnion",  "fish_meal_onion",  "Cá Chiên Hành",        80,  90f);
            recipeCount += CreateRecipe("Recipe_MeatMeal",       "meat_meal",        "Thịt Nướng",           50,  60f);
            recipeCount += CreateRecipe("Recipe_MeatMealTomato", "meat_meal_tomato", "Thịt Nướng Cà Chua",   80,  90f);
            recipeCount += CreateRecipe("Recipe_MeatMealOnion",  "meat_meal_onion",  "Thịt Nướng Hành",      80,  90f);
            recipeCount += CreateRecipe("Recipe_Sushi",          "sushi",            "Sushi",               100,  90f);
            recipeCount += CreateRecipe("Recipe_SushiPickle",    "sushi_pickle",     "Sushi Dưa Chuột",     100,  90f);
            recipeCount += CreateRecipe("Recipe_SushiTomato",    "sushi_tomato",     "Sushi Cà Chua",       100,  90f);
            recipeCount += CreateRecipe("Recipe_TomatoSoup",     "tomato_soup",      "Súp Cà Chua",          60,  60f);

            // ================================================
            // BƯỚC 3: TẠO HOẶC CẬP NHẬT RECIPEDATABASESO
            // ================================================
            BuildRecipeDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b>[RecipeSystemGenerator]</b> Hoàn tất!\n" +
                      $"- Transformations: {transCount}\n" +
                      $"- Recipes: {recipeCount}\n" +
                      $"- Database: {DATABASE_DATA_FOLDER}/{DATABASE_ASSET_NAME}.asset");
        }

        // =====================================================
        // HELPER: Tạo TransformationSO
        // =====================================================
        private static int CreateTransformation(
            string assetName,
            string[] inputIds,
            string outputId,
            StationType station,
            float duration)
        {
            string assetPath = $"{TRANSFORM_DATA_FOLDER}/{assetName}.asset";
            TransformationSO trans = AssetDatabase.LoadAssetAtPath<TransformationSO>(assetPath);
            bool isNew = trans == null;
            if (isNew) trans = ScriptableObject.CreateInstance<TransformationSO>();

            // Resolve inputs
            var inputList = new System.Collections.Generic.List<FoodStateSO>();
            foreach (string id in inputIds)
            {
                FoodStateSO food = LoadFoodState(id);
                if (food == null)
                {
                    Debug.LogWarning($"[RecipeSystemGenerator] Không tìm thấy FoodStateSO id='{id}' cho transformation '{assetName}'. Bỏ qua.");
                    return 0;
                }
                inputList.Add(food);
            }

            FoodStateSO output = LoadFoodState(outputId);
            if (output == null)
            {
                Debug.LogWarning($"[RecipeSystemGenerator] Không tìm thấy FoodStateSO id='{outputId}' (output) cho transformation '{assetName}'. Bỏ qua.");
                return 0;
            }

            trans.inputs          = inputList.ToArray();
            trans.output          = output;
            trans.requiredStation = station;
            trans.duration        = duration;

            if (isNew) AssetDatabase.CreateAsset(trans, assetPath);
            else        EditorUtility.SetDirty(trans);

            return 1;
        }

        // =====================================================
        // HELPER: Tạo RecipeSO
        // =====================================================
        private static int CreateRecipe(
            string assetName,
            string finalMealId,
            string displayName,
            int score,
            float timeLimit)
        {
            string assetPath = $"{RECIPE_DATA_FOLDER}/{assetName}.asset";
            RecipeSO recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(assetPath);
            bool isNew = recipe == null;
            if (isNew) recipe = ScriptableObject.CreateInstance<RecipeSO>();

            FoodStateSO meal = LoadFoodState(finalMealId);
            if (meal == null)
            {
                Debug.LogWarning($"[RecipeSystemGenerator] Không tìm thấy FoodStateSO id='{finalMealId}' cho recipe '{assetName}'. Bỏ qua.");
                return 0;
            }

            recipe.recipeName  = displayName;
            recipe.finalMeal   = meal;
            recipe.scoreValue  = score;
            recipe.timeLimit   = timeLimit;

            if (isNew) AssetDatabase.CreateAsset(recipe, assetPath);
            else        EditorUtility.SetDirty(recipe);

            return 1;
        }

        // =====================================================
        // HELPER: Xây dựng RecipeDatabaseSO và điền danh sách
        // =====================================================
        private static void BuildRecipeDatabase()
        {
            string dbPath = $"{DATABASE_DATA_FOLDER}/{DATABASE_ASSET_NAME}.asset";
            RecipeDatabaseSO db = AssetDatabase.LoadAssetAtPath<RecipeDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<RecipeDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            // Xóa danh sách cũ và nạp lại từ thư mục
            db.transformations.Clear();
            db.recipes.Clear();

            string[] transGuids = AssetDatabase.FindAssets("t:TransformationSO", new[] { TRANSFORM_DATA_FOLDER });
            foreach (string guid in transGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TransformationSO t = AssetDatabase.LoadAssetAtPath<TransformationSO>(path);
                if (t != null) db.transformations.Add(t);
            }

            string[] recipeGuids = AssetDatabase.FindAssets("t:RecipeSO", new[] { RECIPE_DATA_FOLDER });
            foreach (string guid in recipeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RecipeSO r = AssetDatabase.LoadAssetAtPath<RecipeSO>(path);
                if (r != null) db.recipes.Add(r);
            }

            EditorUtility.SetDirty(db);
            Debug.Log($"[RecipeSystemGenerator] Database cập nhật: {db.transformations.Count} transformations, {db.recipes.Count} recipes.");
        }

        // =====================================================
        // HELPER: Load FoodStateSO theo id (tên file)
        // =====================================================
        private static FoodStateSO LoadFoodState(string id)
        {
            string path = $"{FOOD_DATA_FOLDER}/{id}.asset";
            return AssetDatabase.LoadAssetAtPath<FoodStateSO>(path);
        }

        // =====================================================
        // HELPER: Tạo thư mục đệ quy nếu chưa tồn tại
        // =====================================================
        private static void EnsureFolderExists(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
