﻿using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Data.Logic;
using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    ///     <see cref="ArticleEditLogic.UpdateOrInsert" /> result.
    /// </summary>
    public class ArticleUpdateResult
    {
        /// <summary>
        ///     Updated or Inserted model
        /// </summary>
        public ArticleViewModel Model { get; set; }

        /// <summary>
        ///     Urls that need to be flushed
        /// </summary>
        public List<string> Urls { get; set; }
    }
}