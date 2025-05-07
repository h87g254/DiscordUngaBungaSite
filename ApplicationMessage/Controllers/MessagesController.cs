using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Data;
using ApplicationMessage.Models;
using Microsoft.AspNetCore.Authorization;

namespace ApplicationMessage.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Взимаме всички съобщения между двамата
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();


            ViewBag.ChatUserId = userId; // За да знаем към кого пишем

            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            if (receiverId > _context.Users.Max(u => u.Id) || receiverId < 1)
            {
                return RedirectToAction("Index", "Home");
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Chat", new { userId = receiverId });
            }

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Chat", new { userId = receiverId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();


            return PartialView("_MessagesPartial", messages);
        }

        [Authorize]
        public IActionResult RoomChat(int roomId)
        {
            var room = _context.ChatRooms.FirstOrDefault(r => r.Id == roomId);

            if (room == null)
            {
                return NotFound();
            }
            ViewBag.OwnerId = room.OwnerId;
            ViewBag.RoomId = room.Id;
            ViewBag.RoomName = room.RoomName;



            var messages = _context.Messages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.Timestamp)
                .ToList();

            return View("RoomChat", messages);
        }

        [HttpPost]
        [Authorize]
        public IActionResult SendRoomMessage(int roomId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("RoomChat", new { roomId });
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var message = new Message
            {
                SenderId = userId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                ChatRoomId = roomId
            };

            _context.Messages.Add(message);
            _context.SaveChanges();

            return RedirectToAction("RoomChat", new { roomId });
        }

        [HttpPost]
        public IActionResult SendRoomInvite(int roomId, string search)
        {
            var results = _context.Users
                .Where(u => u.Username.Contains(search) || u.Id.ToString() == search)
                .ToList();

            ViewBag.SearchResults = results;

            return RoomChat(roomId);
        }

        [HttpPost]
        public IActionResult SendRoomInviteToUser(int roomId, int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Check if already invited
            bool alreadyInvited = _context.RoomInvites
                .Any(i => i.RoomId == roomId && i.ToUserId == userId && !i.IsAccepted);

            if (!alreadyInvited)
            {
                _context.RoomInvites.Add(new RoomInvite
                {
                    RoomId = roomId,
                    FromUserId = currentUserId,
                    ToUserId = userId
                });
                _context.SaveChanges();
            }

            return RedirectToAction("RoomChat", new { roomId });
        }


    }
}
