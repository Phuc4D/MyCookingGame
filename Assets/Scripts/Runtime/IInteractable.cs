namespace MyOvercooked.Runtime
{
    /// <summary>
    /// Bất kỳ object nào muốn Player tương tác được đều phải implement interface này.
    /// Đây là "hợp đồng": PlayerInteraction không cần biết object là gì,
    /// chỉ cần biết nó có hàm Interact() là đủ.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Trả về true nếu có thể bấm E (Nhặt/Thả) lúc này</summary>
        bool CanInteract(PlayerInteraction player);

        /// <summary>Trả về true nếu có thể đè F (Thái/Lấy đồ) lúc này</summary>
        bool CanInteractHold(PlayerInteraction player);

        /// <summary>
        /// Được gọi khi Player bấm nút tương tác và nhìn về phía object này.
        /// </summary>
        /// <param name="player">Tham chiếu đến PlayerInteraction để object có thể
        /// gọi player.PickupFood() hoặc đọc currentHeldFood.</param>
        void Interact(PlayerInteraction player);

        /// <summary>
        /// Được gọi LÊN TỤC mỗi frame khi Player đang GIỮ nút tương tác (VD: đang băm đồ).
        /// </summary>
        /// <param name="deltaTime">Thời gian của 1 frame (Time.deltaTime)</param>
        /// <returns>True nếu object có xử lý việc giữ nút, False nếu không.</returns>
        bool InteractHold(PlayerInteraction player, float deltaTime);

        /// <summary>
        /// Được gọi khi Player nhả nút E ra trước khi hoàn thành, để reset thanh quá trình.
        /// </summary>
        void CancelInteract();
    }
}
