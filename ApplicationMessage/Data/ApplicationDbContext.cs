using ApplicationMessage.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApplicationMessage.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users {  get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

    }
}
