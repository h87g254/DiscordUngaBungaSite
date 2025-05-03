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
            if (receiverId > _context.Users.Max(u => u.Id))
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

    }
}
