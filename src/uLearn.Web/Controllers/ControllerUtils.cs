﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Configuration;
using System.Web.Mvc;
using ApprovalUtilities.Utilities;
using Database;
using Database.DataContexts;
using Database.Extensions;
using Database.Models;
using log4net;
using Microsoft.AspNet.Identity;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Quizzes;

namespace uLearn.Web.Controllers
{
	public static class ControllerUtils
	{
		private static readonly string ulearnBaseUrl;

		private static readonly ILog log = LogManager.GetLogger(typeof(ControllerUtils));

		static ControllerUtils()
		{
			ulearnBaseUrl = WebConfigurationManager.AppSettings["ulearn.baseUrl"] ?? "";
		}

		public static bool HasPassword(UserManager<ApplicationUser> userManager, IPrincipal principal)
		{
			var user = userManager.FindById(principal.Identity.GetUserId());
			return user?.PasswordHash != null;
		}

		private static bool IsLocalUrl(this Controller controller, string url)
		{
			if (controller.Url.IsLocalUrl(url))
				return true;

			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(ulearnBaseUrl))
				return false;

			try
			{
				var ulearnBaseUrlBuilder = new UriBuilder(ulearnBaseUrl);
				var urlBuilder = new UriBuilder(url);
				return ulearnBaseUrlBuilder.Host == urlBuilder.Host && ulearnBaseUrlBuilder.Scheme == urlBuilder.Scheme && ulearnBaseUrlBuilder.Port == urlBuilder.Port;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static string FixRedirectUrl(this Controller controller, string url)
		{
			var isLocalUrl = controller.IsLocalUrl(url);
			log.Info($"Redirect to {url}: {(isLocalUrl ? "it's safe" : "it's not safe, redirect to home page")}. Base url is {ulearnBaseUrl}");
			return isLocalUrl ? url : controller.Url.Action("Index", "Home");
		}

		public static void AddErrors(this Controller controller, IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				controller.ModelState.AddModelError("", error);
			}
		}

		public static T GetFilterOptionsByGroup<T>(GroupsRepo groupsRepo, IPrincipal User, string courseId, List<string> groupsIds, bool allowSeeGroupForAnyMember = false) where T : AbstractFilterOptionByCourseAndUsers, new()
		{
			var result = new T { CourseId = courseId };

			/* if groupsIds contains "all" (it should be exclusive), get all users. Available only for course admins */
			if (groupsIds.Contains("all") && User.HasAccessFor(courseId, CourseRole.CourseAdmin))
				return result;
			/* if groupsIds contains "not-group" (it should be exclusive), get all users not in any groups, available only for course admins */
			if (groupsIds.Contains("not-in-group") && User.HasAccessFor(courseId, CourseRole.CourseAdmin))
			{
				var usersInGroups = groupsRepo.GetUsersIdsForAllGroups(courseId);
				result.UserIds = usersInGroups.ToList();
				result.IsUserIdsSupplement = true;
				return result;
			}

			result.UserIds = new List<string>();
			var usersIds = new HashSet<string>();

			/* if groupsIds is empty, get members of all groups user has access to. Available for instructors */
			if ((groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty)) && User.HasAccessFor(courseId, CourseRole.Instructor))
			{
				var accessableGroupsIds = groupsRepo.GetMyGroupsFilterAccessibleToUser(courseId, User).Select(g => g.Id).ToList();
				foreach (var accessableGroupId in accessableGroupsIds)
				{
					var groupUsersIds = groupsRepo.GetGroupMembersAsUsers(accessableGroupId).Select(u => u.Id);
					usersIds.AddAll(groupUsersIds);
				}

				result.UserIds = usersIds.ToList();
				return result;
			}

			foreach (var groupId in groupsIds)
			{
				if (!int.TryParse(groupId, out int groupIdInt))
					continue;

				var group = groupsRepo.FindGroupById(groupIdInt);
				if (group != null)
				{
					var groupMembersIds = groupsRepo.GetGroupMembersAsUsers(group.Id).Select(u => u.Id).ToList();
					var hasAccessToGroup = groupsRepo.IsGroupAvailableForUser(group.Id, User);
					if (allowSeeGroupForAnyMember)
						hasAccessToGroup |= groupMembersIds.Contains(User.Identity.GetUserId());

					if (hasAccessToGroup)
						usersIds.AddAll(groupMembersIds);
				}
			}

			result.UserIds = usersIds.ToList();
			return result;
		}

		public static List<string> GetEnabledAdditionalScoringGroupsForGroups(GroupsRepo groupsRepo, Course course, List<string> groupsIds, IPrincipal User)
		{
			if (groupsIds.Contains("all") || groupsIds.Contains("not-in-group"))
				return course.Settings.Scoring.Groups.Keys.ToList();

			var enabledAdditionalScoringGroupsForGroups = groupsRepo.GetEnabledAdditionalScoringGroups(course.Id)
				.GroupBy(e => e.GroupId)
				.ToDictionary(g => g.Key, g => g.Select(e => e.ScoringGroupId).ToList());

			/* if groupsIds is empty, get members of all own groups. Available for instructors */
			if (groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty))
			{
				var accessableGroupsIds = groupsRepo.GetMyGroupsFilterAccessibleToUser(course.Id, User).Select(g => g.Id).ToList();
				return enabledAdditionalScoringGroupsForGroups.Where(kv => accessableGroupsIds.Contains(kv.Key)).SelectMany(kv => kv.Value).ToList();
			}

			var result = new List<string>();
			foreach (var groupId in groupsIds)
			{
				int groupIdInt;
				if (int.TryParse(groupId, out groupIdInt))
					result.AddRange(enabledAdditionalScoringGroupsForGroups.GetOrDefault(groupIdInt, new List<string>()));
			}

			return result;
		}

		public static HashSet<Guid> GetSolvedSlides(UserSolutionsRepo solutionsRepo, UserQuizzesRepo userQuizzesRepo, Course course, string userId)
		{
			var solvedSlides = solutionsRepo.GetIdOfPassedSlides(course.Id, userId);
			solvedSlides.UnionWith(userQuizzesRepo.GetPassedSlideIds(course.Id, userId));
			return solvedSlides;
		}

		public static bool IsSlideSolved(UserSolutionsRepo solutionsRepo, UserQuizzesRepo userQuizzesRepo, Course course, string userId, Guid slideId)
		{
			return solutionsRepo.IsSlidePassed(course.Id, userId, slideId) || userQuizzesRepo.IsSlidePassed(course.Id, userId, slideId);
		}

		public static int GetMaxScoreForUsersSlide(Slide slide, bool isSolved, bool hasManualChecking, bool enabledManualCheckingForUser)
		{
			var isExerciseOrQuiz = slide is ExerciseSlide || slide is QuizSlide;

			if (!isExerciseOrQuiz)
				return slide.MaxScore;

			if (isSolved)
				return hasManualChecking ? slide.MaxScore : GetMaxScoreWithoutManualChecking(slide);
			else
				return enabledManualCheckingForUser ? slide.MaxScore : GetMaxScoreWithoutManualChecking(slide);
		}

		private static int GetMaxScoreWithoutManualChecking(Slide slide)
		{
			if (slide is ExerciseSlide)
				return (slide as ExerciseSlide).Scoring.PassedTestsScore;
			if (slide is QuizSlide)
				return (slide as QuizSlide).ManualChecking ? 0 : slide.MaxScore;
			return slide.MaxScore;
		}

		public static int GetManualCheckingsCountInQueue(SlideCheckingsRepo slideCheckingsRepo, GroupsRepo groupsRepo, IPrincipal user, string courseId, Slide slide, List<string> groupsIds)
		{
			var filterOptions = GetFilterOptionsByGroup<ManualCheckingQueueFilterOptions>(groupsRepo, user, courseId, groupsIds);
			if (filterOptions.UserIds == null)
				groupsIds = new List<string> { "all" };
			filterOptions.SlidesIds = new List<Guid> { slide.Id };

			if (slide is ExerciseSlide)
				return slideCheckingsRepo.GetManualCheckingQueue<ManualExerciseChecking>(filterOptions).Count();
			if (slide is QuizSlide)
				return slideCheckingsRepo.GetManualCheckingQueue<ManualQuizChecking>(filterOptions).Count();

			throw new ArgumentException("Slide should be quiz or exercise", nameof(slide));
		}
	}
}