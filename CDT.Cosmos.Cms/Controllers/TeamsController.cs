using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{
    /// <summary>
    /// Teams management controller
    /// </summary>
    [AutoValidateAntiforgeryToken]
    [Authorize(Roles = "Administrators, Editors")]
    public class TeamsController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOptions<CosmosConfig> _options;
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dbContext"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <param name="syncContext"></param>
        public TeamsController(
            IOptions<CosmosConfig> options,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            SqlDbSyncContext syncContext) :
            base(dbContext, userManager, articleLogic, options)
        {
            _options = options;
            _dbContext = dbContext;
            _userManager = userManager;

            if (syncContext.IsConfigured())
                dbContext.LoadSyncContext(syncContext);
        }

        /// <summary>
        /// Index page
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Membership page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Membership(int? id)
        {
            if (id == null) return RedirectToAction("index");

            var data = await _dbContext.Teams.FindAsync(id);
            if (data == null) return NotFound();

            var teamMembers = await _userManager.GetUsersInRoleAsync("Team Members");
            var members = teamMembers.Select(s => new TeamMemberLookupItem
            {
                UserId = s.Id,
                UserEmail = s.Email
            }).OrderBy(o => o.UserEmail);

            ViewData["Team Members"] = members;

            return View(new TeamViewModel
            {
                Id = data.Id,
                TeamName = data.TeamName,
                TeamDescription = data.TeamDescription
            });
        }

        /// <summary>
        /// Get principal
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task<ClaimsPrincipal> GetPrincipal(IdentityUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

            return principal;
        }

        #region TEAMS CRUD

        /// <summary>
        /// Read team list method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ActionResult> Teams_Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = await _dbContext.Teams.ToListAsync();
            return Json(await data.Select(s => new TeamViewModel
            {
                Id = s.Id,
                TeamName = s.TeamName,
                TeamDescription = s.TeamDescription
            }).ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Create teams method
        /// </summary>
        /// <param name="request"></param>
        /// <param name="teams"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Teams_Create([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            var results = new List<TeamViewModel>();

            if (teams != null && ModelState.IsValid)
                foreach (var team in teams)
                {
                    var entity = new Team
                    {
                        Id = 0,
                        TeamName = team.TeamName,
                        TeamDescription = team.TeamDescription
                    };
                    _dbContext.Teams.Add(entity);
                    await _dbContext.SaveChangesAsync();
                    results.Add(new TeamViewModel
                    {
                        Id = entity.Id,
                        TeamName = entity.TeamName,
                        TeamDescription = entity.TeamDescription
                    });
                }

            return Json(await results.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Update one or more teams
        /// </summary>
        /// <param name="request"></param>
        /// <param name="teams"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Teams_Update(
            [DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            if (teams != null && ModelState.IsValid)
            {
                foreach (var team in teams)
                {
                    var entity = await _dbContext.Teams.FirstOrDefaultAsync(f => f.Id == team.Id);
                    entity.TeamDescription = team.TeamDescription;
                    entity.TeamName = team.TeamName;
                }

                await _dbContext.SaveChangesAsync();
            }

            return Json(teams.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Remove one or more teams
        /// </summary>
        /// <param name="request"></param>
        /// <param name="teams"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Teams_Destroy(
            [DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            if (teams.Any())
            {
                foreach (var team in teams)
                {
                    var entity = await _dbContext.Teams.FirstOrDefaultAsync(f => f.Id == team.Id);
                    var orphanedArticles = await _dbContext.Articles.Where(a => a.TeamId == team.Id).ToListAsync();
                    foreach (var article in orphanedArticles) article.TeamId = null; // Detach the page from the team.

                    var users = await _dbContext.TeamMembers.Where(t => t.TeamId == team.Id).ToListAsync();
                    _dbContext.TeamMembers.RemoveRange(users);
                    await _dbContext.SaveChangesAsync();
                    // Now remove the team
                    _dbContext.Teams.Remove(entity);
                }

                await _dbContext.SaveChangesAsync();
            }

            return Json(teams.ToDataSourceResult(request, ModelState));
        }

        #endregion

        #region TEAM MEMBER CRUD

        /// <summary>
        /// Read team members from a team.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> TeamMembers_Read([DataSourceRequest] DataSourceRequest request, int id)
        {
            var data = await _dbContext.TeamMembers.Include(i => i.Team).Include(i => i.User)
                .Include(i => i.Team)
                .Where(w => w.TeamId == id)
                .ToListAsync();
            var model = data.Select(s => new TeamMemberViewModel
            {
                Id = s.Id,
                TeamRole = new TeamRoleLookupItem
                {
                    TeamRoleId = s.TeamId,
                    TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), s.TeamRole)
                },
                Team = new TeamViewModel
                {
                    Id = s.TeamId,
                    TeamDescription = s.Team.TeamDescription,
                    TeamName = s.Team.TeamName
                },
                Member = new TeamMemberLookupItem(s.User)
            }).ToList();
            return Json(model.ToDataSourceResult(request));
        }

        /// <summary>
        ///     Adds members to a team.
        /// </summary>
        /// <param name="request">DataSourceRequest</param>
        /// <param name="members">New member list</param>
        /// <param name="id">Team ID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> TeamMembers_Create(
            [DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members,
            int id)
        {
            var results = new List<TeamMemberViewModel>();


            if (members != null && ModelState.IsValid)
            {
                var team = await _dbContext.Teams.FindAsync(id);

                foreach (var member in members)
                    if (member.Member.UserId != Guid.Empty.ToString())
                    {
                        var user = await _userManager.FindByIdAsync(member.Member.UserId);

                        var claimsPrincipal = await GetPrincipal(user);

                        if (!claimsPrincipal.IsInRole("Team Members"))
                            await _userManager.AddToRoleAsync(user, "Team Members");

                        var membership = new TeamMember
                        {
                            Id = 0,
                            TeamRole = member.TeamRole?.TeamRoleId ?? (int)TeamRoleEnum.Reviewer,
                            TeamId = team.Id,
                            UserId = member.Member.UserId
                        };

                        _dbContext.TeamMembers.Add(membership);
                        await _dbContext.SaveChangesAsync();

                        //
                        // Now that the membership is saved, we have an ID.
                        // Retrieve with that ID plus the related User and
                        // Team information.
                        //
                        membership = await _dbContext.TeamMembers
                            .Include(i => i.User)
                            .Include(i => i.Team)
                            .Where(w => w.Id == membership.Id).FirstOrDefaultAsync();

                        results.Add(new TeamMemberViewModel
                        {
                            Id = membership.Id,
                            TeamRole = new TeamRoleLookupItem
                            {
                                TeamRoleId = membership.TeamRole,
                                TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), membership.TeamRole)
                            },
                            Team = new TeamViewModel
                            {
                                Id = membership.TeamId,
                                TeamName = membership.Team.TeamName,
                                TeamDescription = membership.Team.TeamDescription
                            },
                            Member = new TeamMemberLookupItem
                            {
                                UserId = membership.UserId,
                                UserEmail = membership.User.Email
                            }
                        });
                    }
            }

            return Json(results.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Team membership update method
        /// </summary>
        /// <param name="request"></param>
        /// <param name="members"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> TeamMembers_Update([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members, int id)
        {
            var results = new List<TeamMemberViewModel>();

            if (ModelState.IsValid == false)
                return Json(await members.ToDataSourceResultAsync(request, ModelState));

            foreach (var member in members)
            {
                var entity = await _dbContext.TeamMembers.Include(i => i.Team).Include(i => i.User)
                    .Where(t => t.Id == member.Id).FirstOrDefaultAsync();
                entity.TeamRole = member.TeamRole.TeamRoleId;
                results.Add(new TeamMemberViewModel
                {
                    Id = entity.Id,
                    Member = new TeamMemberLookupItem
                    {
                        UserEmail = entity.User.Email,
                        UserId = entity.UserId
                    },
                    Team = new TeamViewModel
                    {
                        Id = entity.TeamId,
                        TeamName = entity.Team.TeamName,
                        TeamDescription = entity.Team.TeamDescription
                    },
                    TeamRole = new TeamRoleLookupItem
                    {
                        TeamRoleId = entity.TeamRole,
                        TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), entity.TeamRole)
                    }
                });
            }

            await _dbContext.SaveChangesAsync();

            return Json(await results.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Remove members from a team
        /// </summary>
        /// <param name="request"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> TeamMembers_Destroy([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members)
        {
            if (members.Any())
            {
                var ids = members.Select(s => s.Id).ToArray();

                _dbContext.TeamMembers.RemoveRange(await _dbContext.TeamMembers.Where(w => ids.Contains(w.Id))
                    .ToListAsync());
                await _dbContext.SaveChangesAsync();
            }

            return Json(members.ToDataSourceResult(request, ModelState));
        }

        #endregion
    }
}