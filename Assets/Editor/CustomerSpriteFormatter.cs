using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace MyOvercooked.Editor
{
    public class CustomerSpriteFormatter : EditorWindow
    {
        [MenuItem("Tools/MyOvercooked/Format Customer Sprites")]
        public static void ShowWindow()
        {
            FormatCustomerFolder("Assets/Sprites/sprites/MaleCustomer");
            FormatCustomerFolder("Assets/Sprites/sprites/FemaleCustomer");
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[AI Magic]</color> Đã dọn dẹp và đổi tên xong ảnh của Customer! Giờ bạn có thể chạy Animation Generator.");
        }

        private static void FormatCustomerFolder(string customerFolderPath)
        {
            if (!AssetDatabase.IsValidFolder(customerFolderPath)) return;

            string[] actionFolders = AssetDatabase.GetSubFolders(customerFolderPath);
            
            foreach (string actionFolder in actionFolders)
            {
                string rawActionName = Path.GetFileName(actionFolder).ToLower();
                string action = "Idle";

                // Nhận dạng Action
                if (rawActionName.Contains("idle")) action = "Idle";
                else if (rawActionName.Contains("angry") || rawActionName.Contains("furious")) action = "Angry";
                else if (rawActionName.Contains("joyful") || rawActionName.Contains("happy")) action = "Joyful";
                else if (rawActionName.Contains("walk") || rawActionName.Contains("run")) action = "Run";
                else continue; // Bỏ qua các thư mục không rõ ràng

                string[] dirFolders = AssetDatabase.GetSubFolders(actionFolder);
                foreach (string dirFolder in dirFolders)
                {
                    string rawDir = Path.GetFileName(dirFolder).ToLower();
                    string direction = "Down";

                    // Nhận dạng hướng
                    if (rawDir == "south") direction = "Down";
                    else if (rawDir == "north") direction = "Up";
                    else if (rawDir == "west") direction = "Left";
                    else if (rawDir == "east") direction = "Right";
                    else continue;

                    // Đọc các frame
                    string[] frames = Directory.GetFiles(dirFolder, "*.png").OrderBy(f => f).ToArray();
                    for (int i = 0; i < frames.Length; i++)
                    {
                        string oldPath = frames[i].Replace("\\", "/");
                        string newName = $"{action}{direction}_{i}.png";
                        string newPath = customerFolderPath + "/" + newName;
                        
                        // Đổi tên và di chuyển ra ngoài thư mục gốc của Customer
                        AssetDatabase.MoveAsset(oldPath, newPath);
                    }
                }
            }
        }
    }
}
