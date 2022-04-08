using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Hubs
{
    /// <summary>
    /// Chat hub
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private ApplicationDbContext _dbContext;
        private ArticleEditLogic _articleLogic;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="articleLogic"></param>
        public ChatHub(ApplicationDbContext dbContext, ArticleEditLogic articleLogic)
        {
            _dbContext = dbContext;
            _articleLogic = articleLogic;
        }

        #region CHAT METHODS

        /// <summary>
        /// Chat send method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Send(object sender, string message)
        {
            // Broadcast the message to all clients except the sender
            await Clients.Others.SendAsync("broadcastMessage", sender, message);
        }

        /// <summary>
        /// Send or broadcast message.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public async Task SendTyping(object sender)
        {
            // Broadcast the typing notification to all clients except the sender
            await Clients.Others.SendAsync("typing", sender);
        }

        /// <summary>
        /// Signals that the user has stopped typing in the chat window.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public async Task StopTyping(object sender)
        {
            // Broadcast the typing notification to all clients except the sender
            await Clients.Others.SendAsync("stoptyping", sender);
        }

        #endregion

        #region ARTICLE EDITING

        /// <summary>
        /// Signal other members of the group that a page was just saved, and to reload page.
        /// </summary>
        /// <param name="id">Article RECORD ID</param>
        /// <returns></returns>
        public async Task ArticleSaved(string id)
        {
            var idno = int.Parse(id);
            var model = await _articleLogic.Get(idno, Controllers.EnumControllerName.Edit);
            await ClearLocks(id);
            await Clients.OthersInGroup(id).SendAsync("ArticleReload", JsonConvert.SerializeObject(model));
        }

        /// <summary>
        /// Signifies an article has been loaded, and everyone needs to refresh.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task ArticleImported(string id)
        {
            var idno = int.Parse(id);
            var model = await _articleLogic.Get(idno, Controllers.EnumControllerName.Edit);
            await ClearLocks(id);
            await Clients.Group(id).SendAsync("ArticleReload", JsonConvert.SerializeObject(model));
        }

        /// <summary>
        /// Abandon edits, make everyone reload original.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task AbandonEdits(string id)
        {
            var idno = int.Parse(id);
            var model = await _articleLogic.Get(idno, Controllers.EnumControllerName.Edit);
            await Clients.Group(id).SendAsync("ArticleReload", JsonConvert.SerializeObject(model));
            await ClearLocks(id);
        }

        /// <summary>
        /// Joins users to an editing "room".
        /// </summary>
        /// <param name="id">Article record number is the room name.</param>
        /// <returns></returns>
        public async Task JoinRoom(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);
            }
        }

        /// <summary>
        /// Notifies everyone on this editing room of any article lock.
        /// </summary>
        /// <param name="id">Article record ID</param>
        /// <returns></returns>
        public async Task NotifyRoomOfLock(string id)
        {
            var idno = int.Parse(id);
            var model = await _dbContext.ArticleLocks.Where(w => w.ArticleId == idno).Select(l => new ArticleLockViewModel()
            {
                ArticleRecordId = l.ArticleId,
                Id = l.Id,
                LockSetDateTime = l.LockSetDateTime,
                UserEmail = l.IdentityUser.Email,
                ConnectionId = Context.ConnectionId
            }).FirstOrDefaultAsync();
            string message = JsonConvert.SerializeObject(model);
            await Clients.Group(id).SendAsync("ArticleLock", message);
        }
                        
        /// <summary>
        /// Removes a lock on an article
        /// </summary>
        /// <param name="id">Group ID</param>
        /// <returns></returns>
        public async Task ClearLocks(string id)
        {

            var connectionId = Context.ConnectionId;
            var idno = int.Parse(id);

            var articleLocks = await _dbContext.ArticleLocks.Where(w => w.ConnectionId == connectionId || w.ArticleId == idno).ToListAsync();

            if (articleLocks.Any())
            {
                var ids = articleLocks.Select(s => s.ArticleId);

                _dbContext.ArticleLocks.RemoveRange(articleLocks);

                await _dbContext.SaveChangesAsync();

            }

            string message = JsonConvert.SerializeObject(new ArticleLockViewModel());
            await Clients.Group(id).SendAsync("ArticleLock", message);

        }

        /// <summary>
        /// Announces that someone has started edting an article.
        /// </summary>
        /// <param name="id">Article RECORD ID.</param>
        /// <returns></returns>
        public async Task SetArticleLock(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);
                var idno = int.Parse(id);
                var articleLock = await _dbContext.ArticleLocks.FirstOrDefaultAsync(a => a.ArticleId == idno);
                if (articleLock == null)
                {
                    var identityUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == Context.User.Identity.Name);
                    articleLock = new ArticleLock()
                    {
                        Id = Guid.NewGuid(),
                        ArticleId = idno,
                        IdentityUserId = identityUser.Id,
                        LockSetDateTime = System.DateTimeOffset.UtcNow,
                        ConnectionId = Context.ConnectionId
                    };
                    _dbContext.ArticleLocks.Add(articleLock);
                    await _dbContext.SaveChangesAsync();
                }
                await NotifyRoomOfLock(id);
            }
        }

        #endregion

        /// <summary>
        /// Handles when a client disconnects
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {

            var connectionId = Context.ConnectionId;
            try
            {
                var articleLocks = await _dbContext.ArticleLocks.Where(w => w.ConnectionId == connectionId).ToListAsync();

                if (articleLocks.Any())
                {
                    foreach (var item in articleLocks)
                    {
                        var id = item.ArticleId;
                        await Groups.RemoveFromGroupAsync(connectionId, item.ArticleId.ToString());
                        _dbContext.ArticleLocks.RemoveRange(articleLocks);
                        await _dbContext.SaveChangesAsync();
                        await NotifyRoomOfLock(id.ToString());
                    }
                }
            } 
            catch (Exception ex)
            {
                var t = ex; // for debugging
            }
            
            await base.OnDisconnectedAsync(exception);
        }

    }
}
