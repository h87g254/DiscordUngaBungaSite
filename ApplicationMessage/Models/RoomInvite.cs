using System.ComponentModel.DataAnnotations;

namespace ApplicationMessage.Models
{
    public class RoomInvite
    {
        public int Id { get; set; }

        public int RoomId { get; set; }
        public ChatRoom Room { get; set; }

        public int FromUserId { get; set; }

        public int ToUserId { get; set; }

        public bool IsAccepted { get; set; } = false;
    }
}
