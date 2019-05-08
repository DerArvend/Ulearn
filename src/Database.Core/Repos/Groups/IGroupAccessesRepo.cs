using System.Collections.Generic;
using System.Threading.Tasks;
using Database.Models;
using Ulearn.Common;

namespace Database.Repos.Groups
{
	public interface IGroupAccessesRepo
	{
		Task<GroupAccess> GrantAccessAsync(int groupId, string userId, GroupAccessType accessType, string grantedById);
		Task<bool> CanRevokeAccessAsync(int groupId, string userId, string revokedById);
		Task<List<GroupAccess>> RevokeAccessAsync(int groupId, string userId);
		Task<List<GroupAccess>> GetGroupAccessesAsync(int groupId);
		Task<DefaultDictionary<int, List<GroupAccess>>> GetGroupAccessesAsync(IEnumerable<int> groupsIds);
		Task<bool> HasUserAccessToGroupAsync(int groupId, string userId);
		Task<bool> IsGroupVisibleForUserAsync(int groupId, string userId);
		Task<bool> IsGroupVisibleForUserAsync(Group group, string userId);
		Task<List<Group>> GetAvailableForUserGroupsAsync(string courseId, string userId, bool includeNonarchived = true, bool includeArchived = false);
		Task<List<Group>> GetAvailableForUserGroupsAsync(List<string> coursesIds, string userId, bool includeNonarchived = true, bool includeArchived = false);
		Task<List<Group>> GetAvailableForUserGroupsAsync(string userId, bool includeNonarchived = true, bool includeArchived = false);
		Task<bool> CanInstructorViewStudentAsync(string instructorId, string studentId);
		Task<List<string>> GetCoursesWhereUserCanSeeAllGroupsAsync(string userId, IEnumerable<string> coursesIds);
		Task<List<GroupMember>> GetMembersOfAllGroupsAvailableForUserAsync(string userId);
		Task<List<ApplicationUser>> GetInstructorsOfAllGroupsAvailableForUserAsync(string userId);
		Task<List<string>> GetInstructorsOfAllGroupsWhereUserIsMemberAsync(string courseId, string userId);
		Task<bool> CanUserSeeAllCourseGroupsAsync(string userId, string courseId);
	}
}