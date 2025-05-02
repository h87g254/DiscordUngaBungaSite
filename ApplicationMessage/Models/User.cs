﻿using System.ComponentModel.DataAnnotations;

namespace ApplicationMessage.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;


        public string? ProfilePicturePath { get; set; }

    }
}
