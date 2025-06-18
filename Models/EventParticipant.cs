using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManager.Models
{
    public class EventParticipant
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event Event { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }

}
