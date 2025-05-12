using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApplicationMessage.Data;
using ApplicationMessage.Models;

namespace ApplicationMessage.ViewComponents
{
    public class RecentChatsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public RecentChatsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return View(new List<User>());

            var currentUserId = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var recentMessages = await _context.Messages
                .Where(m => m.ChatRoomId == null && (m.SenderId == currentUserId || m.ReceiverId == currentUserId))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            var recentUsers = recentMessages
                .Select(m => m.SenderId == currentUserId ? m.Receiver : m.Sender)
                .Where(u => u != null)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .Take(3)
                .ToList();


            return View(recentUsers);
        }

    }
}
