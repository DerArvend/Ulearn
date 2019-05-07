using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Models.Comments;
using Database.Repos;
using Database.Repos.Comments;
using Database.Repos.CourseRoles;
using Database.Repos.Groups;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;
using Serilog;
using Ulearn.Common;
using Ulearn.Common.Api.Helpers;
using Ulearn.Common.Extensions;
using Ulearn.Web.Api.Controllers.Groups;
using Ulearn.Web.Api.Models.Common;
using Ulearn.Web.Api.Models.Responses.Comments;
using Ulearn.Web.Api.Models.Responses.Groups;

namespace Ulearn.Web.Api.Controllers.Comments
{
	public class BaseCommentController : BaseController
	{
		protected readonly ICommentsRepo commentsRepo;
		protected readonly ICommentLikesRepo commentLikesRepo;
		protected readonly ICoursesRepo coursesRepo;
		protected readonly ICourseRolesRepo courseRolesRepo;
		protected readonly INotificationsRepo notificationsRepo;
		protected readonly IGroupMembersRepo groupMembersRepo;
		protected readonly IGroupAccessesRepo groupAccessesRepo;

		public BaseCommentController(ILogger logger, IWebCourseManager courseManager, UlearnDb db, IUsersRepo usersRepo,
			ICommentsRepo commentsRepo, ICommentLikesRepo commentLikesRepo, ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo,
			INotificationsRepo notificationsRepo, IGroupMembersRepo groupMembersRepo, IGroupAccessesRepo groupAccessesRepo)
			: base(logger, courseManager, db, usersRepo)
		{
			this.commentsRepo = commentsRepo;
			this.commentLikesRepo = commentLikesRepo;
			this.coursesRepo = coursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.notificationsRepo = notificationsRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.groupAccessesRepo = groupAccessesRepo;
		}

		protected List<CommentResponse> BuildCommentsListResponse(IEnumerable<Comment> comments,
			bool canUserSeeNotApprovedComments, DefaultDictionary<int, List<Comment>> replies, DefaultDictionary<int, int> commentLikesCount, HashSet<int> likedByUserCommentsIds,
			[CanBeNull]Dictionary<string, List<Group>> authorId2Groups, [CanBeNull]HashSet<int> userAvailableGroups, bool canViewAllGroupMembers, bool addCourseIdAndSlideId, bool addParentCommentId, bool addReplies)
		{
			return comments.Select(c => BuildCommentResponse(c, canUserSeeNotApprovedComments, replies, commentLikesCount, likedByUserCommentsIds, authorId2Groups, userAvailableGroups, canViewAllGroupMembers, addCourseIdAndSlideId, addParentCommentId, addReplies)).ToList();
		}

		protected CommentResponse BuildCommentResponse(
			Comment comment,
			bool canUserSeeNotApprovedComments, DefaultDictionary<int, List<Comment>> replies, DefaultDictionary<int, int> commentLikesCount, HashSet<int> likedByUserCommentsIds,
			[CanBeNull]Dictionary<string, List<Group>> authorId2Groups, [CanBeNull]HashSet<int> userAvailableGroups, bool canViewAllGroupMembers, bool addCourseIdAndSlideId, bool addParentCommentId, bool addReplies
		)
		{
			var commentInfo = new CommentResponse
			{
				Id = comment.Id,
				Text = comment.Text,
				RenderedText = RenderCommentTextToHtml(comment.Text),
				Author = BuildShortUserInfo(comment.Author),
				PublishTime = comment.PublishTime,
				IsApproved = comment.IsApproved,
				IsLiked = likedByUserCommentsIds.Contains(comment.Id),
				LikesCount = commentLikesCount[comment.Id],
				Replies = new List<CommentResponse>()
			};

			if (authorId2Groups != null && userAvailableGroups != null && authorId2Groups.ContainsKey(comment.Author.Id))
			{
				commentInfo.AuthorGroups = authorId2Groups[comment.AuthorId]
					.Where(g => canViewAllGroupMembers || userAvailableGroups.Contains(g.Id))
					.Select(BuildShortGroupInfo)
					.ToList().NullIfEmpty();
			}

			if (addCourseIdAndSlideId)
			{
				commentInfo.CourseId = comment.CourseId;
				commentInfo.SlideId = comment.SlideId;
			}

			if (addParentCommentId && !comment.IsTopLevel)
				commentInfo.ParentCommentId = comment.ParentCommentId;

			if (! comment.IsTopLevel)
			{
				commentInfo.IsCorrectAnswer = comment.IsCorrectAnswer;
				return commentInfo;
			}
			
			commentInfo.IsPinnedToTop = comment.IsPinnedToTop;
			if (addReplies)
			{
				var commentReplies = FilterVisibleComments(replies[comment.Id], canUserSeeNotApprovedComments);
				commentInfo.Replies = BuildCommentsListResponse(commentReplies, canUserSeeNotApprovedComments, null, commentLikesCount, likedByUserCommentsIds, authorId2Groups, userAvailableGroups, canViewAllGroupMembers, addCourseIdAndSlideId, addParentCommentId, addReplies);
			}

			return commentInfo;
		}

		private ShortGroupInfo BuildShortGroupInfo(Group g)
		{
			return new ShortGroupInfo
			{
				Id = g.Id,
				Name = g.Name,
				IsArchived = g.IsArchived,
				ApiUrl = Url.Action(new UrlActionContext { Action = nameof(GroupController.Group), Controller = "Group", Values = new { groupId = g.Id }})
			};
		}

		private string RenderCommentTextToHtml(string commentText)
		{
			var encodedText = HtmlTransformations.EncodeMultiLineText(commentText);
			var renderedText = encodedText.RenderSimpleMarkdown();
			var textWithLinks = HtmlTransformations.HighlightLinks(renderedText);
			return textWithLinks;
		}

		protected async Task<bool> CanUserSeeNotApprovedCommentsAsync(string userId, string courseId)
		{
			if (string.IsNullOrEmpty(userId))
				return false;
			
			var hasCourseAccessForCommentEditing = await coursesRepo.HasCourseAccessAsync(userId, courseId, CourseAccessType.EditPinAndRemoveComments).ConfigureAwait(false);
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourseAsync(userId, courseId, CourseRoleType.CourseAdmin).ConfigureAwait(false);
			return isCourseAdmin || hasCourseAccessForCommentEditing;
		}

		protected List<Comment> FilterVisibleComments(List<Comment> comments, bool canUserSeeNotApprovedComments)
		{
			return canUserSeeNotApprovedComments ? comments : comments.Where(c => c.IsApproved || c.AuthorId == UserId).ToList();
		}

		protected async Task NotifyAboutNewCommentAsync(Comment comment)
		{
			var courseId = comment.CourseId;

			if (! comment.IsTopLevel)
			{
				var parentComment = await commentsRepo.FindCommentByIdAsync(comment.ParentCommentId).ConfigureAwait(false);
				if (parentComment != null)
				{
					var replyNotification = new RepliedToYourCommentNotification
					{
						Comment = comment,
						ParentComment = parentComment,
					};
					await notificationsRepo.AddNotificationAsync(courseId, replyNotification, comment.AuthorId).ConfigureAwait(false);
				}
			}
			
			/* Create NewCommentFromStudentFormYourGroupNotification later than RepliedToYourCommentNotification, because the last one is blocker for the first one.
			 * We don't send NewCommentNotification if there is a RepliedToYouCommentNotification */
			var commentFromYourGroupStudentNotification = new NewCommentFromYourGroupStudentNotification { Comment = comment };
			await notificationsRepo.AddNotificationAsync(courseId, commentFromYourGroupStudentNotification, comment.AuthorId);

			/* Create NewComment[ForInstructors]Notification later than RepliedToYourCommentNotification and NewCommentFromYourGroupStudentNotification, because the last one is blocker for the first one.
			 * We don't send NewCommentNotification if there is a RepliedToYouCommentNotification or NewCommentFromYourGroupStudentNotification */
			var notification = comment.IsForInstructorsOnly
				? (Notification) new NewCommentForInstructorsOnlyNotification { Comment = comment } 
				: new NewCommentNotification { Comment = comment };
			await notificationsRepo.AddNotificationAsync(courseId, notification, comment.AuthorId).ConfigureAwait(false);
		}
	}
}