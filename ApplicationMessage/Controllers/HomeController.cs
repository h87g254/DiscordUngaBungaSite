using ApplicationMessage.Data;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationMessage.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var friends = await _context.Friendships
                    .Where(f => (f.RequesterId == currentUserId || f.AddresseeId == currentUserId) && f.IsAccepted)
                    .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
                    .ToListAsync();

                var friendUsers = await _context.Users
                    .Where(u => friends.Contains(u.Id))
                    .ToListAsync();

                List<User> allUsers = new List<User>();

                if (!string.IsNullOrEmpty(search))
                {
                    if (int.TryParse(search, out int idSearch))
                    {
                        // Search by ID (exact match)
                        allUsers = await _context.Users
                            .Where(u => u.Id == idSearch && u.Id != currentUserId && !friends.Contains(u.Id))
                            .ToListAsync();
                    }
                    else
                    {
                        // Search by Username (contains, case insensitive)
                        allUsers = await _context.Users
                            .Where(u => u.Id != currentUserId && !friends.Contains(u.Id))
                            .Where(u => u.Username.ToLower().Contains(search.ToLower()))
                            .ToListAsync();
                    }
                }


                var pendingRequests = await _context.Friendships
                    .Where(f => f.AddresseeId == currentUserId && !f.IsAccepted)
                    .Include(f => f.Requester)
                    .ToListAsync();

                var model = new HomeViewModel
                {
                    Friends = friendUsers,
                    AllUsers = allUsers,
                    PendingRequests = pendingRequests
                };

                return View(model);
            }

            return View(new HomeViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> AddFriend(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Проверка дали вече има заявка или са приятели
            bool alreadyRequested = await _context.Friendships.AnyAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == userId) ||
                (f.RequesterId == userId && f.AddresseeId == currentUserId));

            if (!alreadyRequested)
            {
                var friendship = new Friendship
                {
                    RequesterId = currentUserId,
                    AddresseeId = userId,
                    IsAccepted = false
                };
                _context.Friendships.Add(friendship);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptFriend(int friendshipId)
        {
            var friendship = await _context.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);

            if (friendship != null)
            {
                friendship.IsAccepted = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var friendship = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == friendId) ||
                (f.RequesterId == friendId && f.AddresseeId == currentUserId));

            if (friendship != null)
            {
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }



    }
}
