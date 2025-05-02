using System.ComponentModel.DataAnnotations;

namespace ApplicationMessage.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public int AddresseeId { get; set; }

        public bool IsAccepted { get; set; } = false;
        public User Requester { get; set; }
        public User Addressee { get; set; }

    }
}
