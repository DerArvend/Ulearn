﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Responses.ExerciseStatistics;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/exercise-statistics")]
	public class ExerciseStatisticsController : BaseController
	{
		private readonly IUserSolutionsRepo userSolutionsRepo;
		private readonly IUserSolutionsRepo solutionsRepo;
		private readonly IUserQuizzesRepo userQuizzesRepo;
		private readonly IVisitsRepo visitsRepo;
		private readonly IGroupsRepo groupsRepo;

		public ExerciseStatisticsController(ILogger logger, WebCourseManager courseManager, IUserSolutionsRepo userSolutionsRepo, UlearnDb db, IUsersRepo usersRepo,
			IUserSolutionsRepo solutionsRepo, IUserQuizzesRepo userQuizzesRepo, IVisitsRepo visitsRepo, IGroupsRepo groupsRepo)
			: base(logger, courseManager, db, usersRepo)
		{
			this.userSolutionsRepo = userSolutionsRepo;
			this.solutionsRepo = solutionsRepo;
			this.userQuizzesRepo = userQuizzesRepo;
			this.visitsRepo = visitsRepo;
			this.groupsRepo = groupsRepo;
		}

		/// <summary>
		/// Статистика по выполнению каждого упражнения в курсе
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<CourseExercisesStatisticsResponse>> CourseStatistics([FromQuery(Name = "course_id")] [BindRequired]
			string courseId, int count = 10000, DateTime? from = null, DateTime? to = null)
		{
			var course = courseManager.FindCourse(courseId);
			if (course == null)
				return NotFound();

			if (!from.HasValue)
				from = DateTime.MinValue;
			if (!to.HasValue)
				to = DateTime.MaxValue;

			count = Math.Min(count, 10000);

			var exerciseSlides = course.Slides.OfType<ExerciseSlide>().ToList();
			/* TODO (andgein): I can't select all submissions because ApplicationUserId column doesn't exist in database (ApplicationUser_Id exists).
			   We should remove this column after EF Core 2.1 release (and remove tuples here)
			*/
			var submissions = await userSolutionsRepo.GetAllSubmissions(course.Id, includeManualCheckings: false)
				.Where(s => s.Timestamp >= from && s.Timestamp <= to)
				.OrderByDescending(s => s.Timestamp)
				.Take(count)
				.Select(s => Tuple.Create(s.SlideId, s.AutomaticCheckingIsRightAnswer, s.Timestamp))
				.ToListAsync().ConfigureAwait(false);

			var getSlideMaxScoreFunc = await BuildGetSlideMaxScoreFunc(solutionsRepo, userQuizzesRepo, visitsRepo, groupsRepo, course, User.GetUserId());

			const int daysLimit = 30;

			var result = new CourseExercisesStatisticsResponse
			{
				AnalyzedSubmissionsCount = submissions.Count,
				Exercises = exerciseSlides.Select(
					slide =>
					{
						/* Statistics for this exercise slide: */
						var exerciseSubmissions = submissions.Where(s => s.Item1 == slide.Id).ToList();
						return new OneExerciseStatistics
						{
							Exercise = BuildSlideInfo(course.Id, slide, getSlideMaxScoreFunc),
							SubmissionsCount = exerciseSubmissions.Count,
							AcceptedCount = exerciseSubmissions.Count(s => s.Item2),
							/* Select last 30 (`datesLimit`) dates */
							LastDates = exerciseSubmissions.GroupBy(s => s.Item3.Date).OrderByDescending(g => g.Key).Take(daysLimit).ToDictionary(
								/* Date: */
								g => g.Key,
								/* Statistics for this date: */
								g => new OneExerciseStatisticsForDate
								{
									SubmissionsCount = g.Count(),
									AcceptedCount = g.Count(s => s.Item2)
								}
							)
						};
					}).ToList()
			};

			return result;
		}
	}
}