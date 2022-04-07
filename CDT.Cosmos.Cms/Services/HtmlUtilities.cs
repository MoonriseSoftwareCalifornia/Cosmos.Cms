using HtmlAgilityPack;
using System;

namespace CDT.Cosmos.Cms.Services
{
    /// <summary>
    /// Html Utilities
    /// </summary>
    public class HtmlUtilities
    {
        /// <summary>
        /// Is an absolute Uri
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool IsAbsoluteUri (string url)
        {
            if (string.IsNullOrEmpty (url) || (url == "/"))
            {
                return false;
            }

            try
            {
                var t = new Uri(url);
                return t.IsAbsoluteUri;
            }
            catch (Exception e)
            {
                var t = e; // Debugging purposes
                return false;
            }
        }
        /// <summary>
        /// Changes relative Uri's to absolute
        /// </summary>
        /// <param name="html"></param>
        /// <param name="absoluteUrl"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string RelativeToAbsoluteUrls(string html, Uri absoluteUrl)
        {
            if (string.IsNullOrEmpty(html))
                return "";
            if (absoluteUrl == null)
                return html;
            if (absoluteUrl.IsAbsoluteUri == false)
                throw new ArgumentException($"{absoluteUrl.ToString() } is not an absolute Uri.");

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            foreach (HtmlNode node in htmlDoc.DocumentNode.ChildNodes)
            {
                RelativeToAbsoluteNoUrls(node, absoluteUrl);
            }

            return htmlDoc.DocumentNode.OuterHtml;
        }

        private void RelativeToAbsoluteNoUrls(HtmlNode node, Uri absoluteUrl)
        {
            if (node.Attributes.Contains("href"))
            {
                HtmlAttribute att = node.Attributes["href"];

                if (att != null && IsAbsoluteUri(att.Value) == false)
                {
                    att.Value = new Uri(absoluteUrl, att.Value).ToString();
                }
            }
            if (node.Attributes.Contains("src"))
            {
                HtmlAttribute att = node.Attributes["src"];

                if (att != null && IsAbsoluteUri(att.Value) == false)
                {
                    att.Value = new Uri(absoluteUrl, att.Value).ToString();
                }
            }

            foreach (var child in node.ChildNodes)
            {
                RelativeToAbsoluteNoUrls(child, absoluteUrl);
            }
        }
    }
}
