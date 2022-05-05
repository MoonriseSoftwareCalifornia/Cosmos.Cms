using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Data.Logic;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    /// <summary>
    ///     Tests the <see cref="ArticleEditLogic" /> functions independent of controllers.
    /// </summary>
    [TestClass]
    public class CORE_F01_ArticleEditLogicTests
    {
        private static Utilities? utils;
        private static IdentityUser? _testUser;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            utils = new Utilities();
            using var dbContext = utils.GetApplicationDbContext();
            _testUser = utils.GetIdentityUser(TestUsers.Foo).Result;

            dbContext.ArticleLogs.RemoveRange(dbContext.ArticleLogs.ToList());
            dbContext.Articles.RemoveRange(dbContext.Articles.ToList());

            dbContext.SaveChanges();
        }


        /// <summary>
        ///     Test the creation of root page, and, version it.
        /// </summary>
        [TestMethod]
        public async Task A_Create_And_Save_Root()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            var rootModel = await logic.Create($"New Title {Guid.NewGuid().ToString()}");

            Assert.AreEqual(0, rootModel.Id);
            Assert.AreEqual(0, rootModel.VersionNumber);

            //rootModel.Title = Guid.NewGuid().ToString();
            rootModel.Content = Guid.NewGuid().ToString();
            rootModel.Published = DateTime.Now;

            //
            // CREATE THE HOME (ROOT) PAGE.
            //
            var homePage = await logic.UpdateOrInsert(rootModel, _testUser.Id);

            //var articles = await _dbContext.Articles.ToListAsync();

            Assert.IsNotNull(homePage);
            Assert.IsInstanceOfType(homePage, typeof(ArticleUpdateResult));
            Assert.IsTrue(0 < homePage.Model.Id);
            Assert.AreEqual(1, homePage.Model.VersionNumber);
            //Assert.AreEqual("root", homePage.UrlPath);
            Assert.IsTrue(homePage.Model.Published.HasValue);

            //
            // Check other properties
            //
            Assert.AreEqual(rootModel.Title, homePage.Model.Title);
            Assert.AreEqual(rootModel.Content, homePage.Model.Content);

            //
            // Check Logs
            //
            var logs = await dbContext.ArticleLogs.Where(l => l.ArticleId == homePage.Model.Id).ToListAsync();

            Assert.AreEqual(4, logs.Count);
        }

        [TestMethod]
        public async Task B_Get_Home_Page()
        {
            // ARRANGE
            // New db context
            await using var dbContext = utils.GetApplicationDbContext();
            // Ensure root exists
            Assert.AreEqual(1, await dbContext.Articles.CountAsync());

            var root = await dbContext.Articles.FirstOrDefaultAsync();

            Assert.AreEqual("root", root.UrlPath);

            // Now build the logic
            var logic = utils.GetArticleEditLogic(dbContext);

            //
            // All four should find the home page.
            //
            var test1 = await logic.GetByUrl(null);
            Assert.IsNotNull(test1);

            var test2 = await logic.GetByUrl("");
            Assert.IsNotNull(test2);

            var test3 = await logic.GetByUrl("/");
            Assert.IsNotNull(test3);

            var test4 = await logic.GetByUrl("   ");
            Assert.IsNotNull(test4);

            Assert.AreEqual(test1.Id, test2.Id);
            Assert.AreEqual(test1.Id, test3.Id);
            Assert.AreEqual(test1.Id, test4.Id);
        }

        [TestMethod]
        public async Task C_Add_Versions_Some_Published()
        {
            await using var dbContext = utils.GetApplicationDbContext();

            var logic = utils.GetArticleEditLogic(dbContext);

            //var article = await _dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "ROOT");

            //var articles = await _dbContext.Articles.ToListAsync();

            var version1 = await logic.GetByUrl("");

            // Make changes to version 1
            version1.VersionNumber = 0; // Trigger new version
            version1.Published = null; // Not published

            // Save work, now should have new version number
            var version2 = await logic.UpdateOrInsert(version1, _testUser.Id);
            //Assert.AreEqual(2, version2.VersionNumber);
            Assert.IsFalse(version2.Model.Published.HasValue);

            // Make a third version, still unpublished, now change to published
            version2.Model.VersionNumber = 0;
            var version3 = await logic.UpdateOrInsert(version2.Model, _testUser.Id);
            Assert.AreEqual(3, version3.Model.VersionNumber);
            Assert.IsFalse(version3.Model.Published.HasValue);

            // Fourth version. Third one is published, but new versions are UNPUBLISHED by default.
            version3.Model.Published = DateTime.Now;
            version3.Model.VersionNumber = 0;
            var version4 = await logic.UpdateOrInsert(version3.Model, _testUser.Id);
            Assert.AreEqual(4, version4.Model.VersionNumber);
            Assert.IsFalse(version4.Model.Published.HasValue);

            // Now set to un published for version 5
            version4.Model.Published = null;
            version4.Model.VersionNumber = 0;


            // Make a third version, still unpublished, now change to published
            var version5 = await logic.UpdateOrInsert(version4.Model, _testUser.Id);
            Assert.AreEqual(5, version5.Model.VersionNumber);
            Assert.IsFalse(version5.Model.Published.HasValue);

            var versions = await dbContext.Articles
                .Where(a => a.ArticleNumber == version5.Model.ArticleNumber).ToListAsync();
            Assert.AreEqual(5, versions.Count);
        }

        [TestMethod]
        public async Task C_Get_Last_Published_Version()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            var article = await logic.GetByUrl("");
            var article1 = article;

            var lastPublishedArticle = await dbContext.Articles
                .Where(p => p.Published != null && p.ArticleNumber == article1.ArticleNumber)
                .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            //var lastArticlePeriod = await _dbContext.Articles.Where(p => p.ArticleNumber == article.ArticleNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();


            Assert.AreEqual(lastPublishedArticle.ArticleNumber, article.ArticleNumber);
            Assert.AreEqual(lastPublishedArticle.VersionNumber,
                article.VersionNumber); // VERSION 4 is NOT PUBLISHED!! .

            var articleNumber = article.ArticleNumber;
            var versionNumber = article.VersionNumber;

            // Now get the very last version, unpublished or not.
            article = await logic.GetByUrl("", "en-US", false);
            Assert.AreEqual(articleNumber, article.ArticleNumber);
            Assert.AreNotEqual(versionNumber, article.VersionNumber);
        }

        [TestMethod]
        public async Task D_Create_New_Article_Versions_TestRedirect_Publish()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            var articleNumbers = await dbContext.Articles.Select(s => new
            {
                s.Id,
                s.ArticleNumber,
                s.VersionNumber
            }).ToListAsync();

            var nextArticleNumber = articleNumbers.Max(m => m.ArticleNumber) + 1;

            //
            // Create and save version 1
            var version1 = await logic.UpdateOrInsert(await logic.Create("This is a second page" + Guid.NewGuid()),
                _testUser.Id);
            Assert.AreEqual(1, version1.Model.VersionNumber);
            Assert.AreEqual(nextArticleNumber, version1.Model.ArticleNumber);

            //
            // Make a change to version 1, and save as an update (don't create a new version)
            version1.Model.Content = Guid.NewGuid() + "WOW";
            var ver1OriginalContent = version1.Model.Content;

            var version1UpdateA = await logic.UpdateOrInsert(version1.Model, _testUser.Id);
            Assert.AreEqual(version1.Model.Content, version1UpdateA.Model.Content);
            Assert.AreEqual(1, version1UpdateA.Model.VersionNumber);

            //
            // Make a change to version 1, and save as an update (don't create a new version)
            version1UpdateA.Model.Content = Guid.NewGuid() + "NOW";
            var version1UpdateB = await logic.UpdateOrInsert(version1UpdateA.Model, _testUser.Id);
            Assert.AreEqual(version1UpdateA.Model.Content, version1UpdateB.Model.Content);
            Assert.AreNotEqual(version1.Model.Content, version1UpdateB.Model.Content);
            Assert.AreEqual(1, version1UpdateB.Model.VersionNumber);

            //
            // Make a new version (version 2)
            version1UpdateB.Model.Published = null;
            version1UpdateB.Model.VersionNumber = 0;
            var version2 = await logic.UpdateOrInsert(version1UpdateB.Model, _testUser.Id);
            Assert.AreEqual(2, version2.Model.VersionNumber);
            Assert.AreEqual(version1UpdateB.Model.Content, version2.Model.Content);

            //
            // Create a new version, and change the title.
            // This should trigger a redirect record being created.
            //
            version2.Model.Title = "New Version " + Guid.NewGuid(); // Change the title 
            version2.Model.VersionNumber = 0; // Make a version 3
            version2.Model.Published = DateTime.Now; // New version published.

            //
            // This should change the title, create a redirect record as well.
            //
            var titleChangeVersion3 = await logic.UpdateOrInsert(version2.Model, _testUser.Id);
            Assert.AreEqual(3, titleChangeVersion3.Model.VersionNumber);
            // Even though version 2 is published, new versions should NEVER be published.
            Assert.IsFalse(titleChangeVersion3.Model.Published.HasValue);
            Assert.AreEqual(titleChangeVersion3.Model.Title, version2.Model.Title);
            Assert.AreNotEqual(version1.Model.Title, version2.Model.Title);

            titleChangeVersion3.Model.VersionNumber = 0; // Make version 4
            var version4 = await logic.UpdateOrInsert(titleChangeVersion3.Model, _testUser.Id);

            Assert.AreEqual(4, version4.Model.VersionNumber);

            //
            // Test redirect
            //
            //var list = await _dbContext.Articles.Where(w => w.StatusCode == 3).ToListAsync();
            var redirectResult = await logic.GetByUrl(version2.Model.UrlPath);
            Assert.AreEqual(StatusCodeEnum.Redirect, redirectResult.StatusCode);

            // In a redirect, the content is the new URL path.
            Assert.AreEqual(redirectResult.Content, titleChangeVersion3.Model.UrlPath);


            //
            // Get all four versions
            //
            var testVersion1 = dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 1);
            var testVersion2 = dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 2);
            var testVersion3 = dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 3);
            var testVersion4 = dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 4);

            Assert.IsNotNull(testVersion1);
            Assert.IsNotNull(testVersion2);
            Assert.IsNotNull(testVersion3);
            Assert.IsNotNull(testVersion4);

            // Test Version 1
            // Changing title, changes it for all versions of the same Article Number
            Assert.IsTrue(testVersion1.Title.Contains("New Version"));
            Assert.IsFalse(testVersion1.Published.HasValue);
            Assert.AreEqual(1, testVersion1.VersionNumber);
            Assert.AreEqual(2, testVersion1.ArticleNumber);
            Assert.AreNotEqual(ver1OriginalContent, testVersion1.Content); // Last change made.
            // Test Version 1 Logs
            Assert.IsTrue(testVersion1.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("New article",
                                                                StringComparison.CurrentCultureIgnoreCase)));
            Assert.IsTrue(testVersion1.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("New version",
                                                                StringComparison.CurrentCultureIgnoreCase)));

            Assert.AreEqual(2, testVersion1.ArticleLogs.Count(a => a.IdentityUserId == _testUser.Id &&
                                                                   a.ActivityNotes == "Edit existing"));
            Assert.AreEqual(4, testVersion1.ArticleLogs.Count);

            // Test Version 2
            //Assert.AreEqual("New Version", testVersion2.Title);
            Assert.IsFalse(testVersion2.Published.HasValue);
            Assert.AreEqual(2, testVersion2.VersionNumber);
            Assert.AreEqual(2, testVersion2.ArticleNumber);
            //// Test Version 2 Logs
            Assert.IsTrue(testVersion2.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes == "New version"));

            Assert.AreEqual(1, testVersion2.ArticleLogs.Count);

            // Test Version 3
            Assert.IsFalse(testVersion3.Published.HasValue);
            Assert.AreEqual(3, testVersion3.VersionNumber);
            Assert.AreEqual(2, testVersion3.ArticleNumber);
            // Test Version 3 Logs
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("Redirect")));
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes == "New version"));
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("Redirect")));
            Assert.AreEqual(2, testVersion3.ArticleLogs.Count);

            //var redirect = await _dbContext.ArticleRedirects.FirstOrDefaultAsync(
            //    f => f.ArticleId == testVersion3.Id
            //);
            //Assert.AreEqual(oldPath, redirect.OldUrlPath);

            // Test version 4
            // Title for all version
            Assert.AreEqual(testVersion1.Title, testVersion4.Title);
            Assert.IsFalse(testVersion4.Published.HasValue);
            Assert.AreEqual(4, testVersion4.VersionNumber);
            Assert.AreEqual(2, testVersion4.ArticleNumber);
            Assert.IsTrue(testVersion4.ArticleLogs.Any());
            Assert.IsTrue(testVersion4.ArticleLogs.Any());
            Assert.IsTrue(testVersion4.ArticleLogs.Count > 0);

            //
            // There should be 4 versions
            //
            Assert.AreEqual(4, dbContext.Articles.Count(a => a.ArticleNumber == version1.Model.ArticleNumber));
        }

        [TestMethod]
        public async Task E_SetStatus()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            //
            // Get the expected entity
            //
            var entity = await dbContext.Articles.FirstOrDefaultAsync(f => f.Title.Contains("New Version"));

            //
            // Get the last article by path, unpublished
            //
            var article = await logic.GetByUrl(entity.UrlPath, "en-US", false);
            var expectedVersionCount = dbContext.Articles.Count(a => a.ArticleNumber == article.ArticleNumber);

            //
            // This should delete the article
            //
            var results1 = await logic.SetStatus(article.ArticleNumber, StatusCodeEnum.Deleted,
                _testUser.Id);

            //
            // The result count should match the version count.
            //
            Assert.IsTrue(expectedVersionCount <= results1);

            //
            // Since this is deleted, this should not be able to be retrieved through any of these options.
            //
            var t = await logic.GetByUrl(entity.UrlPath);
            Assert.IsNull(t);
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false));
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false,
                false)); // The article is deleted, can't even retrieve it here
            // Now set to inactive, this article should now be found.
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath));
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false));

            // The article is deleted, can't even retrieve it here
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false,
                false));

            //
            // Now "un-delete" this article.
            //
            await logic.SetStatus(article.ArticleNumber, StatusCodeEnum.Inactive,
                _testUser.Id);

        }

        [TestMethod]
        public async Task F_CheckTitle()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            var entity = await dbContext.Articles.FirstOrDefaultAsync(f => f.ArticleNumber == 1);
            var article = await logic.GetByUrl(entity.UrlPath, "en-US", false, false);
            Assert.IsFalse(await logic.ValidateTitle(entity.Title, 0));
            Assert.IsTrue(await logic.ValidateTitle(article.Title, article.ArticleNumber));
        }

        [TestMethod]
        public async Task G_FindByUrlTest()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            // Create test page 0
            var article = await logic.Create("Public Safety Power Shutoff (PSPS)" + Guid.NewGuid());
            article.ArticleNumber = 0;
            article.Published = DateTime.Now.ToUniversalTime().AddDays(-1);
            article.Content = "Hello world!";
            var saved = await logic.UpdateOrInsert(article, _testUser.Id);

            var prefix = Guid.NewGuid().ToString();
            // Create test page 1
            var article1 = await logic.Create(prefix);
            article1.ArticleNumber = 0;
            article1.Published = DateTime.Now.ToUniversalTime().AddDays(-1);
            article1.Content = "Hello world!";
            var saved1 = await logic.UpdateOrInsert(article1, _testUser.Id);

            // Create test page 2
            var article2 = await logic.Create(prefix + "/Angular");
            article2.HeadJavaScript = "<meta name='ccms:framework' value='angular'>";
            article2.ArticleNumber = 0;
            article2.Published = DateTime.Now.ToUniversalTime().AddDays(-1);
            article2.Content = "Hello world!";
            var saved2 = await logic.UpdateOrInsert(article2, _testUser.Id);

            // Create test page 3
            var article3 = await logic.Create(prefix + "/Angular/Wow");
            article3.HeadJavaScript = "<meta name='ccms:framework' value='angular'>";
            article3.ArticleNumber = 0;
            article3.Published = DateTime.Now.ToUniversalTime().AddDays(-1);
            article3.Content = "Hello world!";
            var saved3 = await logic.UpdateOrInsert(article3, _testUser.Id);

            // Create test page 4
            var article4 = await logic.Create(prefix + "/Angular/Wow/We");
            article4.HeadJavaScript = "<meta name='ccms:framework' value='angular'><base href='/' />";
            article4.ArticleNumber = 0;
            article4.Published = DateTime.Now.ToUniversalTime().AddDays(-1);
            article4.Content = "Hello world!";

            var saved4 = await logic.UpdateOrInsert(article4, _testUser.Id);

            var find = await logic.GetByUrl(saved.Model.UrlPath);
            Assert.IsNotNull(find);

            var find1 = await logic.GetByUrl(saved1.Model.UrlPath);
            Assert.IsNotNull(find1);

            var find1b = await logic.GetByUrl(saved1.Model.UrlPath + "/");
            Assert.IsNotNull(find1b);

            var find2 = await logic.GetByUrl(saved4.Model.UrlPath);
            Assert.IsNotNull(find2);
            Assert.IsTrue(find2.HeadJavaScript.Contains("ccms:framework", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(find2.HeadJavaScript.Contains("base", StringComparison.CurrentCultureIgnoreCase));

            var find2b = await logic.GetByUrl(saved2.Model.UrlPath + "/");
            Assert.IsNotNull(find2);

            var find3 = await logic.GetByUrl(saved3.Model.UrlPath);
            Assert.IsNotNull(find3);

            var find3b = await logic.GetByUrl(saved3.Model.UrlPath + "/");
            Assert.IsNotNull(find3b);

            var find4 = await logic.GetByUrl(saved4.Model.UrlPath);
            Assert.IsNotNull(find4);

            var find4b = await logic.GetByUrl(saved4.Model.UrlPath + "/");
            Assert.IsNotNull(find4b);

        }

        [TestMethod]
        public async Task H_Test_ScheduledPublishing()
        {
            await using var dbContext = utils.GetApplicationDbContext();
            var logic = utils.GetArticleEditLogic(dbContext);

            //
            // Get the home page that the public now sees
            //
            var originalArticle = await logic.GetByUrl("");
            var articleNumber = originalArticle.ArticleNumber;

            //
            // Now validate by retrieving the same via Entity Framework
            //
            var mostRecentPublishedArticle = await dbContext.Articles
                .Where(p => p.Published != null && p.ArticleNumber == articleNumber)
                .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();

            //
            // The logic should have returned the current "live" article
            //
            Assert.AreEqual(mostRecentPublishedArticle.ArticleNumber, originalArticle.ArticleNumber);
            Assert.AreEqual(mostRecentPublishedArticle.VersionNumber,
                originalArticle.VersionNumber); // VERSION 4 is NOT PUBLISHED!! .

            var versionNumber = originalArticle.VersionNumber;

            //
            // Now get the very last version
            //
            var lastArticleVersion = await logic.GetByUrl("", "en-US", false);

            //
            // It should not yet be published
            //
            Assert.IsNull(lastArticleVersion.Published);

            //
            // The article number should still match
            //
            Assert.AreEqual(articleNumber, lastArticleVersion.ArticleNumber);
            //
            // But the version number should not.
            //
            Assert.AreNotEqual(versionNumber, lastArticleVersion.VersionNumber);

            // Update the last version, publish for 20 seconds from now...
            lastArticleVersion.Published = DateTime.UtcNow.AddSeconds(20);
            var newArticleViewModel = await logic.UpdateOrInsert(lastArticleVersion, _testUser.Id);

            //
            // We should still be getting the original article.
            //
            var currentArticle = await logic.GetByUrl("");
            Assert.AreEqual(originalArticle.Id, currentArticle.Id);

            //
            // Wait 25 seconds for the new article to become published...
            //
            Thread.Sleep(25000);

            //
            // Now get the article, with same URL, this should be different.
            //
            var publishedArticle = await logic.GetByUrl("");

            //
            // This should be the NEW article ID.
            Assert.AreEqual(newArticleViewModel.Model.Id, publishedArticle.Id);
        }

        //[TestMethod]
        //public async Task I_Get_Translations_Home_Page()
        //{
        //    await using var dbContext = utils.GetApplicationDbContext();
        //    var logic = utils.GetArticleEditLogic(dbContext);

        //    //var article = await _dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "ROOT");

        //    var article = await logic.Create("Rosetta Stone");

        //    article.Content =
        //        "The other night 'bout two o'clock, or maybe it was three,\r\nAn elephant with shining tusks came chasing after me.\r\nHis trunk was wavin' in the air an'  spoutin' jets of steam\r\nAn' he was out to eat me up, but still I didn't scream\r\nOr let him see that I was scared - a better thought I had,\r\nI just escaped from where I was and crawled in bed with dad.\r\n\r\nSource: https://www.familyfriendpoems.com/poem/being-brave-at-night-by-edgar-albert-guest";

        //    article.Published = DateTime.Now.ToUniversalTime().AddDays(-1);

        //    var result = await logic.UpdateOrInsert(article, _testUser.Id);

        //    //
        //    // All four should find the home page.
        //    //
        //    //var test1 = await logic.GetByUrl(result.UrlPath, "en");
        //    //var test2 = await logic.GetByUrl(result.UrlPath, "es");
        //    var test3 = await logic.GetByUrl(result.Model.UrlPath, "vi");
        //    //var test4 = await logic.GetByUrl(result.UrlPath, "fr");

        //    //Assert.IsNotNull(test1);
        //    //Assert.IsNotNull(test2);
        //    Assert.IsNotNull(test3);
        //    //Assert.IsNotNull(test4);

        //    //Assert.AreEqual(test1.Id, test2.Id);
        //    Assert.AreEqual(result.Model.Id, test3.Id);

        //    Assert.AreEqual("đá Rosetta", test3.Title);
        //    //Assert.AreEqual(test1.Id, test4.Id);
        //}

    }
}