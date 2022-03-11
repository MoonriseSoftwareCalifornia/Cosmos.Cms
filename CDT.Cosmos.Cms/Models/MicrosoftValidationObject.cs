using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Microsoft Validation Object
    /// </summary>
    public class MicrosoftValidationObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MicrosoftValidationObject()
        {
            associatedApplications = new List<AssociatedApplication>();
        }

        /// <summary>
        /// List of applications
        /// </summary>
        public List<AssociatedApplication> associatedApplications { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<MicrosoftValidationObject>(myJsonResponse);
    /// <summary>
    /// Associated Application
    /// </summary>
    public class AssociatedApplication
    {
        /// <summary>
        /// Application ID
        /// </summary>
        public string applicationId { get; set; }
    }

}
