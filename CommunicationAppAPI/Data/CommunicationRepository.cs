using CommunicationAppApi.Helpers;
using CommunicationAppApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunicationAppApi.Data
{
    public class CommunicationRepository : ICommunicationRepository
    {
        private readonly DataContext _context;

        public CommunicationRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<ICollection<User>> GetAllUsersExceptAdmin()
        {
            return await _context.Users.OrderBy(u => u.NormalizedUserName).Where(u => u.NormalizedUserName != "ADMIN").ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetConversation(int userId, int recipientId)
        {
            var messages = await _context.Messages.Include(m => m.Sender).ThenInclude(u => u.Photos)
            .Include(m => m.Recipient).ThenInclude(u => u.Photos).Where(m => m.RecipientId == userId && m.RecipientDeleted == false && m.SenderId == recipientId || m.RecipientId == recipientId && m.SenderDeleted == false && m.SenderId == userId).OrderByDescending(m => m.MessageSent).ToListAsync();
            return messages;
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(l => l.LikerId == userId && l.LikeeId == recipientId);
        }

        public async Task<ICollection<User>> GetLikersOrLikees(int userId, string type)
        {
            var users = _context.Users.Include(u => u.Photos).OrderBy(u => u.UserName).AsQueryable();
            if (type == "likers")
            {
                var userLikers = await GetUserLikes(userId, true);
                users = users.Where(u => userLikers.Contains(u.Id));
            }
            else if (type == "likees")
            {
                var userLikees = await GetUserLikes(userId, false);
                users = users.Where(u => userLikees.Contains(u.Id));
            }
            else
            {
                throw new Exception("لا توجد بيانات متاحة");
            }

            return users.ToList();
        }
        private async Task<IEnumerable<int>> GetUserLikes(int id, bool Likers)
        {
            var user = await _context.Users.Include(u => u.Likers).Include(u => u.Likees).FirstOrDefaultAsync(u => u.Id == id);
            if (Likers)
            {
                return user.Likers.Where(u => u.LikeeId == id).Select(l => l.LikerId);
            }
            else
            {
                return user.Likees.Where(u => u.LikerId == id).Select(l => l.LikeeId);
            }
        }
        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(p => p.UserId == userId).FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Payment> GetPaymentForUser(int userId)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            return photo;
        }

        public async Task<int> GetUnreadMessagesForUser(int userId)
        {
            var messages = await _context.Messages.Where(m => m.IsRead == false && m.RecipientId == userId).ToListAsync();
            var count = messages.Count();
            return count;
        }

        public async Task<User> GetUser(int id, bool isCurrentUser)
        {
            var query = _context.Users.Include(u => u.Photos).AsQueryable();
            if (isCurrentUser)
                query = query.IgnoreQueryFilters();

            var user = await query.FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(u => u.Photos).OrderByDescending(u => u.LastActive).AsQueryable();
            users = users.Where(u => u.Id != userParams.UserId);
            users = users.Where(u => u.Gender == userParams.Gender);
            if (userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }
            if (userParams.Likees)
            {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }
            if (userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);
                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }
            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }
            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            var messages = _context.Messages.Include(m => m.Sender).ThenInclude(u => u.Photos)
                       .Include(m => m.Recipient).ThenInclude(u => u.Photos).AsQueryable();
            switch (messageParams.MessageType)
            {
                case "Inbox":
                    messages = messages.Where(m => m.RecipientId == messageParams.UserId && m.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where(m => m.SenderId == messageParams.UserId && m.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where(m => m.RecipientId == messageParams.UserId && m.RecipientDeleted == false && m.IsRead == false);
                    break;
            }
            messages = messages.OrderByDescending(m => m.MessageSent);
            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }
    }
}
