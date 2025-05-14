using ApplicationMessage.Data;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return View(new HomeViewModel());

                if (!string.IsNullOrEmpty(search))
                    return RedirectToAction("Friends", new { search = search });

                // Старото поведение за Index без търсене
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var friends = await _context.Friendships
                    .Where(f => (f.RequesterId == currentUserId || f.AddresseeId == currentUserId) && f.IsAccepted)
                    .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
                    .ToListAsync();

                var friendUsers = await _context.Users
                    .Where(u => friends.Contains(u.Id))
                    .ToListAsync();

                var pendingRequests = await _context.Friendships
                    .Where(f => f.AddresseeId == currentUserId && !f.IsAccepted)
                    .Include(f => f.Requester)
                    .ToListAsync();

                var pendingRoomInvites = await _context.RoomInvites
                    .Where(r => r.ToUserId == currentUserId && !r.IsAccepted)
                    .Include(r => r.Room)
                    .ToListAsync();

                ViewBag.PendingRoomInvites = pendingRoomInvites;

                var model = new HomeViewModel
                {
                    Friends = friendUsers,
                    AllUsers = new List<User>(),
                    PendingRequests = pendingRequests
                };

                var myRooms = _context.UserChatRooms
                    .Include(uc => uc.ChatRoom)
                    .Where(uc => uc.UserId == currentUserId)
                    .Select(uc => uc.ChatRoom)
                    .ToList();

                ViewBag.MyRooms = myRooms;

                return View(model);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }



        [HttpPost]
        public async Task<IActionResult> AddFriend(int userId)
        {
            try
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

                return RedirectToAction("Friends");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AcceptFriend(int friendshipId)
        {
            try
            {
                var friendship = await _context.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);

                if (friendship != null)
                {
                    friendship.IsAccepted = true;
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Pending");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            try
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

                return RedirectToAction("Friends");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public IActionResult AcceptRoomInvite(int inviteId)
        {
            try
            {
                var invite = _context.RoomInvites.Include(i => i.Room).FirstOrDefault(i => i.Id == inviteId);

                if (invite != null && !invite.IsAccepted)
                {
                    invite.IsAccepted = true;

                    _context.UserChatRooms.Add(new UserChatRoom
                    {
                        ChatRoomId = invite.RoomId,
                        UserId = invite.ToUserId
                    });

                    _context.SaveChanges();
                }

                return RedirectToAction("Pending");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> Pending()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var pendingFriends = await _context.Friendships
                    .Where(f => f.AddresseeId == currentUserId && !f.IsAccepted)
                    .Include(f => f.Requester)
                    .ToListAsync();

                var pendingRooms = await _context.RoomInvites
                    .Where(r => r.ToUserId == currentUserId && !r.IsAccepted)
                    .Include(r => r.Room)
                    .ToListAsync();

                ViewBag.PendingFriends = pendingFriends;
                ViewBag.PendingRoomInvites = pendingRooms;

                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> Friends(string? search)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var friendsQuery = _context.Friendships
                    .Where(f => f.IsAccepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
                    .Include(f => f.Requester)
                    .Include(f => f.Addressee);

                var friends = await friendsQuery.ToListAsync();

                var friendList = friends.Select(f =>
                    f.RequesterId == userId ? f.Addressee : f.Requester
                ).ToList();

                List<User> allUsers = new();

                if (!string.IsNullOrEmpty(search))
                {
                    var friendIds = friendList.Select(u => u.Id).ToList();

                    if (int.TryParse(search, out int idSearch))
                    {
                        allUsers = await _context.Users
                            .Where(u => u.Id == idSearch && u.Id != userId && !friendIds.Contains(u.Id))
                            .ToListAsync();
                    }
                    else
                    {
                        allUsers = await _context.Users
                            .Where(u => u.Id != userId && !friendIds.Contains(u.Id))
                            .Where(u => u.Username.ToLower().Contains(search.ToLower()))
                            .ToListAsync();
                    }
                }

                var model = new HomeViewModel
                {
                    Friends = friendList,
                    AllUsers = allUsers
                };

                return View("Friends", model);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }



        public IActionResult Error()
        {
            return View(); 
        }
    }
}
