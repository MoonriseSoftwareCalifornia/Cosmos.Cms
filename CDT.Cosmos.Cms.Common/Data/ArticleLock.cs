﻿using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    /// Represents a lock on an article because it is being edited.
    /// </summary>
    public class ArticleLock
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// User ID for this lock
        /// </summary>
        public string IdentityUserId { get; set; }

        /// <summary>
        /// Article ID for this lock
        /// </summary>
        public int ArticleId { get; set; }

        /// <summary>
        /// When the lock was set
        /// </summary>
        public DateTimeOffset LockSetDateTime{ get; set; }

        #region NAVIGATION

        /// <summary>
        ///     The article associated with this event.
        /// </summary>
        [ForeignKey("ArticleId")]
        public Article Article { get; set; }

        /// <summary>
        ///     Identity User assocated with this activity
        /// </summary>
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }

        #endregion
    }
}
