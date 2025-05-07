using System.ComponentModel.DataAnnotations;

namespace ApplicationMessage.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }

        [Required]
        public string RoomName { get; set; }

        public string RoomDescription { get; set; }

        public List<UserChatRoom> Members { get; set; } = new();
        public int OwnerId { get; set; }

    }

    public class UserChatRoom
    {
        public int Id { get; set; }

        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
