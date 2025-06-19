namespace ClubManager.Models.ViewModels
{
    public class ClubViewModel
    {
        public Club Club { get; set; }
        public bool HasJoined { get; set; }
        public bool HasRequested { get; set; } // true nếu đã gửi yêu cầu nhưng chưa duyệt và chưa bị từ chối
    }
}
