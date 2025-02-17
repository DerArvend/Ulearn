﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity.EntityFramework;
using Ulearn.Common;

namespace Database.Models
{
	// You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
	public class ApplicationUser : IdentityUser
	{
		public ApplicationUser()
		{
			Registered = DateTime.Now;
		}

		public virtual ICollection<UserQuestion> Questions { get; set; }

		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTime Registered { get; set; }
		public DateTime? LastEdit { get; set; }

		// AvatarUrl is empty if user has no avatar
		public string AvatarUrl { get; set; }

		public bool HasAvatar => !string.IsNullOrEmpty(AvatarUrl);

		// TelegramChatId is null if telegram is not connected to the profile
		[Index("IDX_ApplicationUser_ByTelegramChatId")]
		public long? TelegramChatId { get; set; }

		public bool HasTelegram => TelegramChatId != null;

		[StringLength(200)]
		public string TelegramChatTitle { get; set; }

		[StringLength(200)]
		public string KonturLogin { get; set; }

		public DateTime? LastConfirmationEmailTime { get; set; }

		public Gender? Gender { get; set; }

		[Index("IDX_ApplicationUser_ByIsDeleted")]
		public bool IsDeleted { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public string Names
		{
			get { return UserName + " " + FirstName + " " + LastName; }
			private set
			{
				/* Empty for EF */
			}
		}

		public string VisibleName
		{
			get
			{
				if (FirstName + LastName != "")
					return (FirstName + " " + LastName).Trim();
				if (!string.IsNullOrEmpty(UserName))
					return UserName.Trim();
				return "Пользователь";
			}
		}

		public string VisibleNameWithLastNameFirst
		{
			get
			{
				if (FirstName + LastName != "")
					return (LastName + " " + FirstName).Trim();
				if (!string.IsNullOrEmpty(UserName))
					return UserName.Trim();
				return "Пользователь";
			}
		}
	}
}