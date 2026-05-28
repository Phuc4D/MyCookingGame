using UnityEngine;
using UnityEngine.UI;

namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Component quản lý thanh thời gian dạng vòng tròn.
    /// Có thể dùng chung cho Station (khi tự động nấu) hoặc Thớt (khi Player băm đồ).
    /// </summary>
    public class ProgressUI : MonoBehaviour
    {
        [Tooltip("Ảnh viền tròn màu xanh/đỏ (Image Type = Filled)")]
        [SerializeField] private Image _fillImage;

        [Tooltip("Object bọc ngoài để dễ dàng bật tắt cả thanh (VD: cái Canvas hoặc GameObject chứa Image)")]
        [SerializeField] private GameObject _visualRoot;

        private void Start()
        {
            // Mặc định luôn ẩn khi mới bắt đầu game
            Hide();
        }

        /// <summary>
        /// Hiển thị thanh thời gian và cập nhật độ đầy.
        /// </summary>
        /// <param name="current">Giá trị hiện tại</param>
        /// <param name="max">Giá trị lớn nhất</param>
        public void ShowProgress(float current, float max)
        {
            if (_visualRoot != null && !_visualRoot.activeSelf)
            {
                _visualRoot.SetActive(true);
            }

            if (_fillImage != null)
            {
                _fillImage.fillAmount = current / max;
            }
        }

        /// <summary>
        /// Giấu thanh thời gian đi khi nấu xong hoặc không làm gì.
        /// </summary>
        public void Hide()
        {
            if (_visualRoot != null && _visualRoot.activeSelf)
            {
                _visualRoot.SetActive(false);
            }
        }
    }
}
