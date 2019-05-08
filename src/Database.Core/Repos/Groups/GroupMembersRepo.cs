using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Ulearn.Common.Extensions;

namespace Database.Repos.Groups
{
	public class GroupMembersRepo : IGroupMembersRepo
	{
		private readonly UlearnDb db;
		private readonly IManualCheckingsForOldSolutionsAdder manualCheckingsForOldSolutionsAdder;
		private readonly IGroupsRepo groupsRepo;
		private readonly ILogger logger;

		public GroupMembersRepo(UlearnDb db, IManualCheckingsForOldSolutionsAdder manualCheckingsForOldSolutionsAdder, IGroupsRepo groupsRepo, ILogger logger)
		{
			this.db = db;
			this.manualCheckingsForOldSolutionsAdder = manualCheckingsForOldSolutionsAdder;
			this.groupsRepo = groupsRepo;
			this.logger = logger;
		}
		
		public Task<List<ApplicationUser>> GetGroupMembersAsUsersAsync(int groupId)
		{
			return db.GroupMembers.Include(m => m.User).Where(m => m.GroupId == groupId && !m.User.IsDeleted).Select(m => m.User).ToListAsync();
		}

		public Task<List<GroupMember>> GetGroupMembersAsync(int groupId)
		{
			return db.GroupMembers.Include(m => m.User).Where(m => m.GroupId == groupId && !m.User.IsDeleted).ToListAsync();
		}
		
		public Task<List<GroupMember>> GetGroupsMembersAsync(ICollection<int> groupsIds)
		{
			return db.GroupMembers.Include(m => m.User).Where(m => groupsIds.Contains(m.GroupId) && !m.User.IsDeleted).ToListAsync();
		}
		
		public Task<bool> IsUserMemberOfGroup(int groupId, string userId)
		{
			return db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
		}
		
		public async Task<GroupMember> AddUserToGroupAsync(int groupId, string userId)
		{
			logger.Information($"Пытаюсь добавить пользователя {userId} в группу {groupId}");
			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false) ?? throw new ArgumentNullException($"Can't find group with id={groupId}");
			
			var groupMember = new GroupMember
			{
				GroupId = groupId,
				UserId = userId,
				AddingTime = DateTime.Now,
			};
			using (var transaction = db.Database.BeginTransaction())
			{
				/* Don't add member if it's already exists */
				var existsMember = db.GroupMembers.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);
				if (existsMember != null)
				{
					logger.Information($"Пользователь {userId} уже находится в группе {groupId}, повторно добавлять не буду");
					return null;
				}

				db.GroupMembers.Add(groupMember);
				await db.SaveChangesAsync().ConfigureAwait(false);

				transaction.Commit();
				
				logger.Information($"Пользователь {userId} добавлен в группу {groupId}");
			}
			
			if (group.IsManualCheckingEnabledForOldSolutions)
				await manualCheckingsForOldSolutionsAdder.AddManualCheckingsForOldSolutionsAsync(group.CourseId, userId).ConfigureAwait(false);

			return groupMember;
		}

		public async Task<GroupMember> RemoveUserFromGroupAsync(int groupId, string userId)
		{
			logger.Information($"Удаляю пользователя {userId} из группы {groupId}");
			
			var member = db.GroupMembers.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);
			if (member != null)
				db.GroupMembers.Remove(member);
			else
				logger.Information($"Пользователь {userId} не состоит в группе {groupId}");

			await db.SaveChangesAsync().ConfigureAwait(false);
			
			return member;
		}
		
		public async Task<List<GroupMember>> RemoveUsersFromGroupAsync(int groupId, List<string> userIds)
		{
			logger.Information($"Удаляю пользователей {string.Join(", ", userIds)} из группы {groupId}");
			
			var members = db.GroupMembers.Where(m => m.GroupId == groupId && userIds.Contains(m.UserId)).ToList();
			db.GroupMembers.RemoveRange(members);

			await db.SaveChangesAsync().ConfigureAwait(false);
			
			return members;
		}
		
		public async Task<List<GroupMember>> AddUsersToGroupAsync(int toGroupId, ICollection<string> userIds)
		{
			logger.Information($"Добавляю пользователей {string.Join(", ", userIds)} в группу {toGroupId}");
			
			var newMembers = new List<GroupMember>();
			foreach (var memberUserId in userIds)
				newMembers.Add(await AddUserToGroupAsync(toGroupId, memberUserId).ConfigureAwait(false));
			
			return newMembers;
		}
		
		public Task<List<string>> GetUsersIdsForAllCourseGroupsAsync(string courseId, bool includeArchived = false)
		{
			var groupsIds = groupsRepo.GetCourseGroupsQueryable(courseId, includeArchived).Select(g => g.Id);
			return db.GroupMembers.Where(m => groupsIds.Contains(m.GroupId)).Select(m => m.UserId).ToListAsync();
		}

		/* Return Dictionary<userId, List<groupId>> */
		public Task<Dictionary<string, List<int>>> GetUsersGroupsIdsAsync(string courseId, List<string> usersIds, bool includeArchived = false)
		{
			var groupsIds = groupsRepo.GetCourseGroupsQueryable(courseId, includeArchived).Select(g => g.Id);
			return db.GroupMembers
				.Where(m => groupsIds.Contains(m.GroupId) && usersIds.Contains(m.UserId))
				.GroupBy(m => m.UserId)
				.ToDictionaryAsync(g => g.Key, g => g.Select(m => m.GroupId).ToList());
		}

		public async Task<List<int>> GetUserGroupsIdsAsync(string courseId, string userId, bool includeArchived = false)
		{
			return (await GetUsersGroupsIdsAsync(courseId, new List<string> { userId }, includeArchived).ConfigureAwait(false))
				.GetOrDefault(userId, new List<int>());
		}

		public async Task<List<Group>> GetUserGroupsAsync(string courseId, string userId, bool includeArchived = false)
		{
			var userGroupsIds = await GetUserGroupsIdsAsync(courseId, userId).ConfigureAwait(false);
			return groupsRepo.GetCourseGroupsQueryable(courseId, includeArchived).Where(g => userGroupsIds.Contains(g.Id)).ToList();
		}
		
		public async Task<Dictionary<string, List<Group>>> GetUsersGroupsAsync(string courseId, List<string> usersIds, bool includeArchived = false)
		{
			var userGroupsIds = await GetUsersGroupsIdsAsync(courseId, usersIds, includeArchived).ConfigureAwait(false);
			var ids = userGroupsIds.Values.SelectMany(g => g).Distinct().ToList();
			var groups = groupsRepo.GetCourseGroupsQueryable(courseId, includeArchived).Where(g => ids.Contains(g.Id)).ToDictionary(g => g.Id, g => g);
			return userGroupsIds.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(id => groups[id]).ToList());
		}
	}
}