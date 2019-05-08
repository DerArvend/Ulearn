using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Web.Api.Authorization;
using Ulearn.Web.Api.Models.Parameters.Groups;
using Ulearn.Web.Api.Models.Responses.Groups;
using ILogger = Serilog.ILogger;

namespace Ulearn.Web.Api.Controllers.Groups
{
	[Route("/groups/")]
	public class GroupsController : BaseGroupController
	{
		private readonly IGroupsRepo groupsRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;

		public GroupsController(ILogger logger, IWebCourseManager courseManager, UlearnDb db,
			IUsersRepo usersRepo,
			IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo, IGroupMembersRepo groupMembersRepo)
			: base(logger, courseManager, db, usersRepo)
		{
			this.groupsRepo = groupsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.groupMembersRepo = groupMembersRepo;
		}

		/// <summary>
		/// Список неархивных групп в курсе
		/// </summary>
		[HttpGet]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult<GroupsListResponse>> GroupsList([FromQuery] GroupsListParameters parameters)
		{
			return await GetGroupsListResponseAsync(parameters).ConfigureAwait(false);
		}
		
		private async Task<GroupsListResponse> GetGroupsListResponseAsync(GroupsListParameters parameters)
		{
			var groups = await groupAccessesRepo.GetAvailableForUserGroupsAsync(parameters.CourseId, UserId, !parameters.Archived, parameters.Archived).ConfigureAwait(false);
			/* Order groups by (name, createTime) and get one page of data (offset...offset+count) */
			var groupIds = groups.OrderBy(g => g.Name, StringComparer.InvariantCultureIgnoreCase).ThenBy(g => g.CreateTime)
				.Skip(parameters.Offset)
				.Take(parameters.Count)
				.Select(g => g.Id)
				.ToImmutableHashSet();
			var filteredGroups = groups.Where(g => groupIds.Contains(g.Id)).ToList();

			var groupMembers = await groupMembersRepo.GetGroupsMembersAsync(groupIds).ConfigureAwait(false);
			var membersCountByGroup = groupMembers.GroupBy(m => m.GroupId).ToDictionary(g => g.Key, g => g.Count()).ToDefaultDictionary();

			var groupAccessesByGroup = await groupAccessesRepo.GetGroupAccessesAsync(groupIds).ConfigureAwait(false);

			var groupInfos = filteredGroups.Select(g => BuildGroupInfo(
				g,
				membersCountByGroup[g.Id],
				groupAccessesByGroup[g.Id],
				addGroupApiUrl: true
			)).ToList();
			
			return new GroupsListResponse
			{
				Groups = groupInfos,
				Pagination = new PaginationResponse
				{
					Offset = parameters.Offset,
					Count = filteredGroups.Count,
					TotalCount = groups.Count,
				}
			};
		}

		/// <summary>
		/// Создать новую группу в курсе
		/// </summary>
		/// <param name="parameters">Название новой группы</param>
		[HttpPost]
		[Authorize(Policy = "Instructors")]
		[ProducesResponseType((int) HttpStatusCode.Created)]
		public async Task<ActionResult<CreateGroupResponse>> CreateGroup([FromQuery] CourseAuthorizationParameters courseAuthorizationParameters, CreateGroupParameters parameters)
		{
			var ownerId = User.GetUserId();
			var group = await groupsRepo.CreateGroupAsync(courseAuthorizationParameters.CourseId, parameters.Name, ownerId).ConfigureAwait(false);

			var url = Url.Action(new UrlActionContext { Action = nameof(GroupController.Group), Controller = "Group", Values = new { groupId = group.Id }});
			return Created(url, new CreateGroupResponse
			{
				Id = group.Id,
				ApiUrl = url,
			});
		}
	}
}