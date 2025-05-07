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

            return View("Rooms");
        }

        // Create a new room
        [HttpPost]
        public IActionResult Create(string roomName, string roomDescription)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                TempData["Error"] = "Room name is required.";
                return RedirectToAction("Index");
            }

            var room = new ChatRoom
            {
                RoomName = roomName,
                RoomDescription = roomDescription ?? ""
            };
            _context.ChatRooms.Add(room);
            _context.SaveChanges();

            return RedirectToAction("Join", new { roomId = room.Id });
        }

        // Join a room
        public IActionResult Join(int roomId)
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
    }
}
