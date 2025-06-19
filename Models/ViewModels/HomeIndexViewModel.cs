using System.Collections.Generic;

namespace ClubManager.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<ClubViewModel> Clubs { get; set; }
        public List<Event> Events { get; set; }
        public List<int> JoinedClubIds { get; set; }

        public int TotalUsers { get; set; }
        public int TotalManagers { get; set; }
        public int TotalClubs { get; set; }
        public int TotalEvents { get; set; }

        public List<Student> TopStudents { get; set; } // Thành viên nổi bật
    }

}
