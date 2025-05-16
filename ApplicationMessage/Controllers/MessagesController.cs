using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Data;
using ApplicationMessage.Models;
using Microsoft.AspNetCore.Authorization;
using ApplicationMessage.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ApplicationMessage.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int userId)
        {
            try
            {
                if (userId > _context.Users.Max(u => u.Id) || userId < 1)
                {
                    return RedirectToAction("Error", "Home");
                }
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
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int receiverId, string content, IFormFile image)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                string imagePath = null;

                if (image != null && image.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine("wwwroot/uploads", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    imagePath = "/uploads/" + fileName;
                }
                else { 
                    
                }
                if (string.IsNullOrWhiteSpace(content) && imagePath == null)
                    return RedirectToAction("Chat", new { userId = receiverId });

                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = content ?? "",
                    ImagePath = imagePath
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group($"user_{receiverId}")
                    .SendAsync("ReceivePrivateMessage", User.Identity.Name, content);

                await _hubContext.Clients.Group($"user_{currentUserId}")
                    .SendAsync("ReceivePrivateMessage", User.Identity.Name, content);

                return Ok();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetMessages(int userId)
        {
            try
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
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRoomMessages(int roomId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ChatRoomId == roomId)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();


                return PartialView("_MessagesPartial", messages);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpGet]
        public IActionResult RoomChat(int roomId)
        {
            try
            {
                /*if(roomId > _context.ChatRooms.Max(r => r.Id) || roomId < 1)
                {
                    return RedirectToAction("Error", "Home");
                }*/
                var room = _context.ChatRooms.FirstOrDefault(r => r.Id == roomId);

                if (room == null)
                {
                    return NotFound();
                }
                ViewBag.OwnerId = room.OwnerId;
                ViewBag.RoomId = room.Id;
                ViewBag.RoomName = room.RoomName;



                var messages = _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ChatRoomId == roomId)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                return View("RoomChat", messages);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendRoomMessage(int roomId, string content,IFormFile image)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
                {
                    return RedirectToAction("RoomChat", new { roomId });
                }
                string imagePath = null;

                if (image != null && image.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine("wwwroot/uploads", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    imagePath = "/uploads/" + fileName;
                }
                else
                {

                }
                if (string.IsNullOrWhiteSpace(content) && imagePath == null)
                    return RedirectToAction("RoomChat", new { roomId });
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

                var message = new Message
                {
                    SenderId = userId,
                    ReceiverId = userId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    ChatRoomId = roomId,
                    ImagePath = imagePath

                };

                _context.Messages.Add(message);
                _context.SaveChanges();

                await _hubContext.Clients.Group($"room_{roomId}")
                    .SendAsync("ReceiveRoomMessage", User.Identity.Name, content);

                return Ok();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public IActionResult SendRoomInvite(int roomId, string search)
        {
            try
            {
                var results = _context.Users
                    .Where(u => u.Username.Contains(search) || u.Id.ToString() == search)
                    .ToList();

                ViewBag.SearchResults = results;

                return RoomChat(roomId);
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public IActionResult SendRoomInviteToUser(int roomId, int userId)
        {
            try
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
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }


    }
}
