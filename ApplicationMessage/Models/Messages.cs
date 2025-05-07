using System;
using System.ComponentModel.DataAnnotations;

namespace ApplicationMessage.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public User Sender { get; set; }
        public User Receiver { get; set; }

        public int? ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

    }

}
