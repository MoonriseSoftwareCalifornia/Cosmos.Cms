using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_F02_HandleContentEditableTests
    {

        private static Utilities? _utils;
        private static IdentityUser? _testUser;

        private static Guid _testGuid = Guid.NewGuid();

        private static string _testHtml = "<div contenteditable>1</div>" +
            "<div contenteditable=\"true\">1</div>" +
            "<div crx>1</div>" +
            "<div crx=\"true\">1" +
            "<div data-ccms-ceid>1</div></div>" +
            "<div data-ccms-ceid=\"7162860c-c4f1-4677-be70-9c0dc0c0ebe0\">1</div>";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _utils = new Utilities();
            _testUser = _utils.GetIdentityUser(TestUsers.Foo).Result;
        }

        [TestMethod]
        public async Task Test_F02_HandleContentEditable()
        {
            await using var dbContext = _utils.GetApplicationDbContext();
            var logic = _utils.GetArticleEditLogic(dbContext);


            var articleViewModel = await logic.Create($"New Title { _testGuid }");

            Assert.AreEqual(0, articleViewModel.Id);
            Assert.AreEqual(0, articleViewModel.VersionNumber);

            articleViewModel.Content = _testHtml;

            //
            // CREATE THE TEST PAGE.
            //
            var testPage = await logic.UpdateOrInsert(articleViewModel, _testUser.Id);
            Assert.IsNotNull(testPage);
            TestHtml(testPage.Model.Content);

            // CHECK THE ARTICLE AS STORED IN DATABASE
            var article = await dbContext.Articles.FirstOrDefaultAsync(f => f.Id == testPage.Model.Id);
            Assert.IsNotNull(article);
            TestHtml(article.Content);

            // Finally check the logic
            var articleViewModel2 = await logic.GetByUrl(testPage.Model.UrlPath, "", false, false);
            Assert.IsNotNull(articleViewModel2);
            TestHtml(articleViewModel2.Content);

        }

        private void TestHtml(string content)
        {

            Assert.IsFalse(content.Contains("contenteditable"));
            Assert.IsFalse(content.Contains("crx"));
            Assert.IsTrue(content.Contains("data-ccms-ceid"));

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(content);

            var nodeList1 = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable]");
            var nodeList2 = htmlDoc.DocumentNode.SelectNodes("//*[@crx]");
            var nodeList3 = htmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]");

            Assert.IsNull(nodeList1);
            Assert.IsNull(nodeList2);
            Assert.IsTrue(nodeList3.Count() == 6);

            foreach (var node in nodeList3)
            {

                Assert.IsFalse(string.IsNullOrEmpty(node.Attributes["data-ccms-ceid"].Value));
            }
        }
    }
}
