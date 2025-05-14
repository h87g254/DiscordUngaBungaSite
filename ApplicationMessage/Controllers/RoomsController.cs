using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Models;
using Microsoft.AspNetCore.Authorization;
using ApplicationMessage.Data;

namespace ApplicationMessage.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all rooms + show user’s rooms
        public IActionResult Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var myRooms = _context.UserChatRooms
                    .Include(uc => uc.ChatRoom)
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.ChatRoom)
                    .ToList();

                var allRooms = _context.ChatRooms
                    .Where(r => !myRooms.Select(m => m.Id).Contains(r.Id))
                    .ToList();

                ViewBag.MyRooms = myRooms;
                ViewBag.AllRooms = allRooms;

                return View("~/Views/Rooms/Rooms.cshtml");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // Create a new room
        [HttpPost]
        public IActionResult Create(string roomName, string roomDescription)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (string.IsNullOrWhiteSpace(roomName))
                {
                    TempData["Error"] = "Room name is required.";
                    return RedirectToAction("Rooms");
                }

                var room = new ChatRoom
                {
                    RoomName = roomName,
                    RoomDescription = roomDescription ?? "",
                    OwnerId = userId
                };
                _context.ChatRooms.Add(room);
                _context.SaveChanges();

                return RedirectToAction("Join", new { roomId = room.Id });
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // Join a room
        public IActionResult Join(int roomId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                bool alreadyInRoom = _context.UserChatRooms.Any(uc => uc.ChatRoomId == roomId && uc.UserId == userId);
                if (!alreadyInRoom)
                {
                    _context.UserChatRooms.Add(new UserChatRoom
                    {
                        ChatRoomId = roomId,
                        UserId = userId
                    });
                    _context.SaveChanges();
                }

                return RedirectToAction("RoomChat", "Messages", new { roomId = roomId });
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
