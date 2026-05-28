using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Globalization;
using MyOvercooked.Data;

namespace MyOvercooked.Editor
{
    /// <summary>
    /// Automated generator for FoodStateSO assets based on sprites.
    /// This script automates the creation of ScriptableObjects to save manual data entry time.
    /// </summary>
    public static class FoodAssetGenerator
    {
        // Path configuration - adjust these if your folder structure changes
        private const string SPRITE_FOLDER = "Assets/Sprites/food_sprites/foods";
        private const string DATA_FOLDER = "Assets/Data/Foods";

        [MenuItem("Tools/Generate Food Assets")]
        public static void GenerateFoodAssets()
        {
            // 1. Validate sprite source folder
            if (!Directory.Exists(SPRITE_FOLDER))
            {
                Debug.LogError($"[FoodAssetGenerator] Sprite folder not found at: {SPRITE_FOLDER}. " +
                               "Please ensure the folder exists or update the path in the script.");
                return;
            }

            // 2. Ensure the destination data folder exists
            if (!AssetDatabase.IsValidFolder(DATA_FOLDER))
            {
                EnsureFolderExists(DATA_FOLDER);
            }

            // 3. Scan for sprite files (.png, .jpg, .jpeg)
            string[] allowedExtensions = { ".png", ".jpg", ".jpeg" };
            string[] filePaths = Directory.GetFiles(SPRITE_FOLDER, "*.*", SearchOption.AllDirectories)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();

            if (filePaths.Length == 0)
            {
                Debug.LogWarning($"[FoodAssetGenerator] No sprites found in {SPRITE_FOLDER}.");
                return;
            }

            int createdCount = 0;
            int updatedCount = 0;

            foreach (string filePath in filePaths)
            {
                // Normalize path for Unity (forward slashes)
                string unityPath = filePath.Replace("\\", "/");
                
                // Extract filename without extension for ID and naming
                string fileName = Path.GetFileNameWithoutExtension(unityPath);
                string assetPath = $"{DATA_FOLDER}/{fileName}.asset";

                // 4. Load existing asset or create a new instance
                FoodStateSO foodSO = AssetDatabase.LoadAssetAtPath<FoodStateSO>(assetPath);
                bool isNew = false;

                if (foodSO == null)
                {
                    foodSO = ScriptableObject.CreateInstance<FoodStateSO>();
                    isNew = true;
                }

                // 5. Assign fields based on filename and sprite
                
                // id: Exact filename (e.g., "chopped_fish")
                foodSO.id = fileName;

                // displayName: Formatted name (e.g., "chopped_fish" -> "Chopped Fish")
                foodSO.displayName = FormatDisplayName(fileName);

                // sprite: Load the Sprite reference from the file
                // Note: The texture must be imported as "Sprite (2D and UI)" in Unity
                foodSO.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);

                // category: Logic based on filename content
                foodSO.category = ParseCategory(fileName);

                // 6. Save changes
                if (isNew)
                {
                    AssetDatabase.CreateAsset(foodSO, assetPath);
                    createdCount++;
                }
                else
                {
                    EditorUtility.SetDirty(foodSO);
                    updatedCount++;
                }
            }

            // 7. Finalize and refresh Unity database
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b>[FoodAssetGenerator]</b> Successfully processed {filePaths.Length} sprites.\n" +
                      $"- Created: {createdCount}\n" +
                      $"- Updated: {updatedCount}\n" +
                      $"- Location: {DATA_FOLDER}");
        }

        /// <summary>
        /// Formats "chopped_fish" into "Chopped Fish"
        /// </summary>
        private static string FormatDisplayName(string name)
        {
            string spaced = name.Replace('_', ' ');
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());
        }

        /// <summary>
        /// Parses the FoodCategory enum based on filename keywords
        /// </summary>
        private static FoodCategory ParseCategory(string name)
        {
            string lower = name.ToLower();
            
            if (lower.Contains("chopped")) return FoodCategory.Chopped;
            if (lower.Contains("cooked"))  return FoodCategory.Cooked;
            if (lower.Contains("meal"))    return FoodCategory.Meal;
            
            return FoodCategory.Raw;
        }

        /// <summary>
        /// Helper to create folders recursively if they don't exist
        /// </summary>
        private static void EnsureFolderExists(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = $"{currentPath}/{folders[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
