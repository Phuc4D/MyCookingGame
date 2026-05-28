using UnityEngine;
using UnityEditor;
using System.Net;
using System.Threading;
using System.IO;
using System.Text;
using System;

namespace MyOvercooked.Editor
{
    [Serializable]
    public class AICommand
    {
        public string action;
        public string payload;
    }

    /// <summary>
    /// AI Bridge - Mở cổng kết nối để AI (Antigravity) có thể tương tác trực tiếp với Unity Editor.
    /// Hoạt động trên nền tảng HttpListener ở port 8080.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityAIBridge
    {
        private static HttpListener listener;
        private static Thread listenerThread;
        
        // Quản lý luồng xử lý trên Main Thread (vì Unity không cho phép sửa Scene từ Thread khác)
        private static volatile bool hasPendingCommand = false;
        private static AICommand currentCommand;
        private static string commandResult = "";
        private static volatile bool commandDone = false;

        static UnityAIBridge()
        {
            StartServer();
            EditorApplication.update += OnEditorUpdate;
        }

        private static void StartServer()
        {
            if (listener != null && listener.IsListening) return;

            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8081/ai/");
            
            try
            {
                listener.Start();
                listenerThread = new Thread(ListenForRequests);
                listenerThread.IsBackground = true;
                listenerThread.Start();
                Debug.Log("<color=green>[UnityAIBridge]</color> Server Started! Đã sẵn sàng kết nối với AI tại cổng 8081.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityAIBridge] Không thể mở cổng 8081. Lỗi: {e.Message}");
            }
        }

        private static void ListenForRequests()
        {
            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception) { /* Bỏ qua khi đóng server */ }
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string responseString = "OK";
            
            if (request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string json = reader.ReadToEnd();
                    
                    try
                    {
                        AICommand cmd = JsonUtility.FromJson<AICommand>(json);
                        
                        // Đẩy lệnh sang Main Thread của Unity
                        currentCommand = cmd;
                        hasPendingCommand = true;
                        commandDone = false;

                        // Đợi Main Thread xử lý (Timeout sau 5 giây)
                        int timeout = 5000;
                        while (!commandDone && timeout > 0)
                        {
                            Thread.Sleep(10);
                            timeout -= 10;
                        }

                        responseString = commandDone ? commandResult : "Timeout: Lệnh thực thi quá lâu!";
                    }
                    catch (Exception ex)
                    {
                        responseString = "Lỗi giải mã JSON: " + ex.Message;
                    }
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        // Chạy trên Main Thread của Unity
        private static void OnEditorUpdate()
        {
            if (hasPendingCommand)
            {
                try
                {
                    commandResult = ExecuteCommandOnMainThread(currentCommand.action, currentCommand.payload);
                }
                catch (Exception e)
                {
                    commandResult = "Error: " + e.Message;
                }
                finally
                {
                    commandDone = true;
                    hasPendingCommand = false;
                }
            }
        }

        // TỪ ĐIỂN CÁC LỆNH AI CÓ THỂ ĐIỀU KHIỂN
        private static string ExecuteCommandOnMainThread(string action, string payload)
        {
            switch (action)
            {
                case "Ping":
                    return "Pong! Chào sếp, tôi đang sống trong Unity đây!";
                
                case "ExecuteMenu":
                    bool success = EditorApplication.ExecuteMenuItem(payload);
                    return success ? $"Đã bấm menu: {payload}" : $"Thất bại khi bấm: {payload}";
                
                case "CreateCube":
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = string.IsNullOrEmpty(payload) ? "AI_Cube" : payload;
                    cube.transform.position = new Vector3(0, 5, 0); // Rơi từ trên cao xuống
                    Selection.activeGameObject = cube;
                    return $"Đã đẻ ra một khối Cube tên là {cube.name}!";

                case "GetSelection":
                    if (Selection.activeGameObject != null) 
                        return $"Bạn đang chọn: {Selection.activeGameObject.name}";
                    return "Bạn không chọn vật thể nào cả.";

                case "ScanScene":
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("=== CURRENT SCENE HIERARCHY & COMPONENTS ===");
                    foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        DumpGameObject(go, sb, 0);
                    }
                    return sb.ToString();

                default:
                    return $"AI Bridge chưa học được lệnh này: {action}";
            }
        }

        private static void DumpGameObject(GameObject go, StringBuilder sb, int indent)
        {
            string indentStr = new string('-', indent * 2);
            sb.Append(indentStr).Append("> ").Append(go.name);
            if (!go.activeSelf) sb.Append(" (Inactive)");
            sb.AppendLine();

            Component[] comps = go.GetComponents<Component>();
            foreach (Component c in comps)
            {
                if (c == null) continue; // Missing script
                if (c is Transform) continue; // Skip transform to save space
                sb.Append(indentStr).Append("    [C] ").AppendLine(c.GetType().Name);
            }

            foreach (Transform child in go.transform)
            {
                DumpGameObject(child.gameObject, sb, indent + 1);
            }
        }
    }
}
