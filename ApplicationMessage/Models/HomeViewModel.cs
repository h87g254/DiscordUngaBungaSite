using System.Collections.Generic;

namespace ApplicationMessage.Models
{
    public class HomeViewModel
    {
        public List<User> Friends { get; set; }
        public List<User> AllUsers { get; set; }
        public List<Friendship> PendingRequests { get; set; }
    }
}
