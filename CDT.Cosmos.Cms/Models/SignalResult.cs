using System;
using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Signal result
    /// </summary>
    public class SignalResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SignalResult()
        {
            Exceptions = new List<Exception>();
        }

        /// <summary>
        /// Result has one or more errors
        /// </summary>
        public bool HasErrors { get; set; } = false;
        /// <summary>
        /// Exception list
        /// </summary>
        public List<Exception> Exceptions { get; }
        /// <summary>
        /// Result JSON
        /// </summary>
        public string JsonValue { get; set; }
    }
}
