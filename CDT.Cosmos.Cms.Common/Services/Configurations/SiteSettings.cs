using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Startup.ConfigureServices method captures the site customization options found in "secrets" in this
    ///     object.
    /// </summary>
    public class SiteSettings
    {
        /// <summary>
        ///     Allowed file type extensions
        /// </summary>
        [Display(Name = "File types")]
        [Required]
        public string AllowedFileTypes { get; set; } = ".js,.css,.htm,.html,.mov,.webm,.avi,.mp4,.mpeg,.ts,.svg,.json";

        /// <summary>
        ///     For training purposes, allows a full reset of site to factory settings.
        /// </summary>
        [Display(Name = "Allow reset")]
        public bool? AllowReset { get; set; } = false;

        /// <summary>
        ///     Allows a website to go into setup mode. For use only on fresh sites.
        /// </summary>
        [Display(Name = "Allow setup")]
        public bool? AllowSetup { get; set; } = true;

        /// <summary>
        /// Allows the advanced configuration editor to be available to Administrators.
        /// </summary>
        public bool AllowConfigEdit { get; set; } = false;

        /// <summary>
        /// URI of the blob public website (can be same as publisher URL).
        /// </summary>
        /// <remarks>
        /// Publisher URL can be the same as blob public url, but this requires
        /// request rules to be setup that route requests to blob storage. See documentation
        /// for more information.
        /// </remarks>
        [Required]
        [Display(Name = "Blob Url")]
        public string BlobPublicUrl { get; set; } = "";

        /// <summary>
        ///     Determines if current instance is an editor.
        /// </summary>
        /// <remarks>Set in ArticleEditLogic constructor and no where else.</remarks>
        public bool IsEditor { get; set; } = false;

        /// <summary>
        ///     URI of the publisher website.
        /// </summary>
        [Url]
        [Required]
        [Display(Name = "Website (Publisher) Url")]
        public string PublisherUrl { get; set; } = "";

        /// <summary>
        /// Content Security Policy (CSP)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Content Security Policy (CSP) is an added layer of security
        /// that helps to detect and mitigate certain types of attacks,
        /// including Cross Site Scripting (XSS) and data injection attacks.
        /// These attacks are used for everything from data theft to site
        /// defacement to distribution of malware.
        /// </para>
        /// <para>See: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP"/></para>
        /// </remarks>
        [Display(Name = "Content-Security-Policy")]
        public string ContentSecurityPolicy { get; set; } = "";

        /// <summary>
        /// X-Frame-Options
        /// </summary>
        /// <remarks>
        /// <para>
        /// The X-Frame-Options HTTP response header can be used to indicate whether or not a
        /// browser should be allowed to render a page in a <frame>, <iframe>, <embed> or <object>.
        /// Sites can use this to avoid click-jacking attacks, by ensuring that their content
        /// is not embedded into other sites.
        /// </para>
        /// <para>See: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options"/></para>
        /// </remarks>
        [Display(Name = "X-Frame-Options")]
        public string XFrameOptions { get; set; } = "";
    }
}