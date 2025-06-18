using System.Collections.Generic;

namespace ClubManager.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<ClubViewModel> Clubs { get; set; }
        public List<Event> Events { get; set; }
        public List<int> JoinedClubIds { get; set; } // Các CLB user đã tham gia
    }
}
