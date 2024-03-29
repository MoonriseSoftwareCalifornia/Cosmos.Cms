﻿using CDT.Cosmos.Cms.Common.Models;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    ///     JSON model returned have HTML editor saves content
    /// </summary>
    public class SaveResultJsonModel : SaveCodeResultJsonModel
    {
        /// <summary>
        ///     Content model as saved
        /// </summary>
        public ArticleViewModel Model { get; set; }
    }
}