using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    /// <summary>
    ///     This is a series of tests that exercise the <see cref="Cms.Controllers.EditorController" />.
    /// </summary>
    [TestClass]
    public class CORE_E02_EditControllerTests
    {
        private static Utilities utils;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            utils = new Utilities();
            using var dbContext = utils.GetApplicationDbContext();
            dbContext.ArticleLogs.RemoveRange(dbContext.ArticleLogs.ToList());
            dbContext.Articles.RemoveRange(dbContext.Articles.ToList());

            dbContext.SaveChanges();
        }

        /// <summary>
        ///     Test the ability to create the home page
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task A01A_CreateHomePage()
        {
            //
            // Test creating and saving a page
            //
            ArticleViewModel model;
            //ArticleViewModel savedModel;

            using (var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                // Step 1. Create a blank, unsaved CreatePageViewModel
                var initialViewResult = (ViewResult)await controller.Create();
                var createModel = (CreatePageViewModel)initialViewResult.Model;

                // Step 2. Save the new page with a unique title. This redirects to the edit function.
                createModel.Title = Guid.NewGuid().ToString();
                var redirectResult = (RedirectToActionResult)await controller.Create(createModel);

                // Edit function returns a model, ready to edit.  It is saved in the database.
                var id = (int)redirectResult.RouteValues["Id"];
                var viewResult = (ViewResult)await controller.Edit(id.ToString());
                model = (ArticleViewModel)viewResult.Model;
            }

            //
            // Using EF make sure the article was created
            //
            await using var dbContext = utils.GetApplicationDbContext();
            var articleTest1 = await dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);

            //
            // The model of the new page, should be found by EF.
            //
            Assert.IsNotNull(articleTest1); // Should exist
            // titles should match
            Assert.AreEqual(model.Title, articleTest1.Title);
            // Being the first page, the URL should be "root"
            Assert.AreEqual("root", articleTest1.UrlPath);
        }

        //
        // Test the ability for the editor controller to make and save content changes
        //
        [TestMethod]
        public async Task A01B_ModifyHomePageTitle()
        {
            //
            // Test updating home page title, should not change URL
            //
            ArticleViewModel model;
            ArticleViewModel savedModel;

            //
            // Using EF get the home page
            //
            Article articleTest1;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                articleTest1 = await dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "root");
            }

            //
            // Get the home page so we can edit it
            //
            using (var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                // Edit function returns a model, ready to edit.  It is saved in the database.
                var viewResult = (ViewResult)await controller.Edit(articleTest1.Id.ToString());
                model = (ArticleViewModel)viewResult.Model;
            }

            //
            // Change the title now
            //
            var oldTitle = model.Title;
            model.Title = "New Page";
            model.Content = LoremIpsum.WhyLoremIpsum;

            //
            // Add some javascript to the header and footer
            //
            using var reader = File.OpenText(@"JavaScript1.js");

            var js = await reader.ReadToEndAsync();
            model.HeaderJavaScript = js;
            model.FooterJavaScript = js;

            //
            // Now save the title and javascript block changes.
            //
            using (var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                var jsonResult = (JsonResult)await controller.SaveHtml(model);
                var jsonData = (SaveResultJsonModel)jsonResult.Value;
                savedModel = jsonData.Model;
            }

            //
            // Use EF to make sure the changes were saved
            //
            Article articleTest2;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                articleTest2 = await dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);
            }


            //
            // Use EF to make sure we are looking at the right article
            //
            Assert.AreEqual(articleTest2.Id, articleTest1.Id);
            Assert.AreEqual("New Page", articleTest2.Title);

            //
            // Title should now be different from the original
            //
            Assert.AreNotEqual(oldTitle, articleTest2.Title);

            //
            // Title should now be the same
            //
            Assert.AreEqual("New Page", savedModel.Title);

            //
            // Check that the content block saved fine.
            // 
            Assert.IsTrue(!string.IsNullOrEmpty(savedModel.Content));

            //
            // Check to make sure the header javascript is saved.
            //
            Assert.IsNotNull(savedModel.HeaderJavaScript);

            //
            // Check to make sure the footer javascript is saved
            //
            Assert.AreEqual(js, savedModel.FooterJavaScript);

            //
            // But the UrlPath should stay as "root" as this is the home page
            //
            Assert.AreEqual("root", savedModel.UrlPath);

            //
            // Check the version number, we didn't create one, so should still be version 1
            //
            Assert.AreEqual(1,
                savedModel
                    .VersionNumber); // Original model.VersionNumber should be 1, and the saved model should have an incremented id.

            //
            // Article number should stay the same
            //
            Assert.AreEqual(savedModel.ArticleNumber, model.ArticleNumber);
        }

        //
        // Now lets try getting the home page using URL ""
        //
        [TestMethod]
        public async Task A02_GetHomePage_Success()
        {
            List<Article> articles1;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                articles1 = await dbContext.Articles.ToListAsync();
            }

            Assert.IsTrue(articles1.Count > 0);
            using var homeController =
                utils.GetHomeController(await utils.GetPrincipal(TestUsers.Foo), false);

            using var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo));


            List<Article> articles2;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                articles2 = await dbContext.Articles.ToListAsync();
            }

            Assert.AreEqual(1, articles2.Count);

            var home = (ViewResult)await homeController.Index();

            var homeModel = (ArticleViewModel)home.Model;

            var homePage = await controller.Edit(homeModel.Id.ToString());

            Assert.IsNotNull(homePage);
            Assert.IsInstanceOfType(homePage, typeof(ViewResult));
        }

        //
        // Test the ability to save a page, without changes, and test that NO changes were actually made.
        //
        [TestMethod]
        public async Task A03_EditPageSaveChanges_Success()
        {
            //
            // Using EF, get the article we are going to work with.
            //
            Article article;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            //
            // This represents the original, unaltered page.
            //
            ArticleViewModel originalArticleViewModel;

            //
            // The model we are going to edit
            //
            ArticleViewModel editModel;

            //
            // This represents the article after being saved
            //
            ArticleViewModel savedArticleViewModel;

            using (var homeController =
                utils.GetHomeController(await utils.GetPrincipal(TestUsers.Foo), false))
            {
                homeController.Request.Path = article.UrlPath;
                var page = (ViewResult)await homeController.Index();

                originalArticleViewModel = (ArticleViewModel)page.Model;
            }

            Article testArticle;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                testArticle = await dbContext.Articles.FirstOrDefaultAsync(a => a.Id == originalArticleViewModel.Id);
            }

            //
            // Use EF to make sure we are looking at the right article
            //
            Assert.AreEqual(testArticle.Id, originalArticleViewModel.Id);
            Assert.AreEqual(testArticle.Title, originalArticleViewModel.Title);

            //
            // Now save
            //
            using (var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                //
                // Get the page we are going to edit
                //
                var editPage = (ViewResult)await controller.Edit(originalArticleViewModel.Id.ToString());

                Assert.IsNotNull(editPage);
                Assert.IsInstanceOfType(editPage, typeof(ViewResult));

                //
                // Pull the model out, we are going to change this.
                //
                editModel = (ArticleViewModel)editPage.Model;

                //
                // Save again, NO changes. Saving should not alter content.
                //
                var jsonResult = (JsonResult)await controller.SaveHtml(editModel);
                Assert.IsInstanceOfType(jsonResult, typeof(JsonResult));

                var testPull = (ViewResult)await controller.Edit(originalArticleViewModel.Id.ToString());
                savedArticleViewModel = (ArticleViewModel)testPull.Model;
            }

            Assert.IsTrue(savedArticleViewModel.UrlPath == testArticle.UrlPath);
            Assert.IsTrue(savedArticleViewModel.Title == testArticle.Title);
            //Assert.IsTrue(savedArticleViewModel.Content == testArticle.Content);
            Assert.IsTrue(savedArticleViewModel.HeaderJavaScript == testArticle.HeaderJavaScript);
            Assert.IsTrue(savedArticleViewModel.FooterJavaScript == testArticle.FooterJavaScript);
        }

        //
        // Test the ability to edit  CODE, and save with success
        //
        [TestMethod]
        public async Task A04_EditCode_Success()
        {
            using var homeController =
                utils.GetHomeController(await utils.GetPrincipal(TestUsers.Foo), false);

            using var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo));

            Article article;
            await using (var dbContext = utils.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            homeController.Request.Path = article.UrlPath;
            var page = (ViewResult)await homeController.Index();

            var pageModel = (ArticleViewModel)page.Model;

            var editPage = (ViewResult)await controller.EditCode(pageModel.Id);

            var codeModel = (EditCodePostModel)editPage.Model;

            var result1 = (JsonResult)await controller.EditCode(codeModel);
            var editResult1 = (SaveCodeResultJsonModel)result1.Value;

            Assert.IsTrue(editResult1.IsValid);
        }

        //
        // Test what happens when HTML syntax error is injected, and tried to be saved with Edit Code method.
        //
        //[TestMethod]
        //public async Task A05_EditCode_FailValidation()
        //{
        //    using var homeController =
        //        utils.GetHomeController(await utils.GetPrincipal(TestUsers.Foo), false);

        //    using var controller =
        //        utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo));

        //    Article article;

        //    await using (var dbContext = utils.GetApplicationDbContext())
        //    {
        //        article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
        //            .FirstOrDefaultAsync();
        //    }

        //    var page = (ViewResult)await homeController.Index(article.UrlPath, "");

        //    var pageModel = (ArticleViewModel)page.Model;

        //    var editPage = (ViewResult)await controller.EditCode(pageModel.Id);

        //    var codeModel = (EditCodePostModel)editPage.Model;
        //    codeModel.Content = "<div><div><span><h1>Wow this is messed up!";
        //    var result1 = (JsonResult)await controller.EditCode(codeModel);
        //    var editResult1 = (SaveCodeResultJsonModel)result1.Value;

        //    Assert.IsFalse(editResult1.IsValid);
        //    Assert.AreEqual(1, editResult1.Errors.Count);
        //}

        [TestMethod]
        public async Task A06_SaveDate_Success()
        {
            using var homeController =
                utils.GetHomeController(await utils.GetPrincipal(TestUsers.Foo), false);

            using var editorController =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo));

            var testDate = DateTime.SpecifyKind(new DateTime(2021, 3, 1, 0, 0, 0), DateTimeKind.Utc);

            Article article;

            await using (var dbContext = utils.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            var savedViewResult = (ViewResult)await editorController.Edit(article.Id.ToString());
            var articleViewModel = (ArticleViewModel)savedViewResult.Model;

            articleViewModel.Published = testDate;

            //
            // Save the published date.
            //
            var jsonResult = (JsonResult)await editorController.SaveHtml(articleViewModel);
            var savedArticle = (SaveResultJsonModel)jsonResult.Value;
            var savedModel = savedArticle.Model;

            //
            // The date/time should stay exactly the same after the save.
            //
            Assert.IsTrue(utils.DateTimesAreEqual(testDate, savedModel.Published.Value));

            var articleJsonResult = (JsonResult)await editorController.Get_Articles(new DataSourceRequest());
            var dataSourceResult = (DataSourceResult)articleJsonResult.Value;
            var articleListModel = (List<ArticleListItem>)dataSourceResult.Data;

            var target = articleListModel.FirstOrDefault(f => f.ArticleNumber == article.ArticleNumber);

            Assert.IsTrue(utils.DateTimesAreEqual(testDate, target.LastPublished.Value));
        }

        /// <summary>
        /// This method tests article version scheduling work flow
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task A07_ScheduleVersionWorkFlow_Success()
        {
            var options = utils.GetCosmosConfigOptions();

            ArticleViewModel homePageModel;

            //
            // STEP 1: GET ARTICLE TO EDIT
            //
            // Get the home page so we can create a new version,
            // and schedule that new version for publishing in the future.
            //
            using (var dbContext = utils.GetApplicationDbContext())
            {
                // Get an instance of article logic. -- add to cache
                var articleLogic = utils.GetArticleLogic(dbContext);

                // Get the home page model
                homePageModel = await articleLogic.GetByUrl("");

                // CHECK: Should NOT have an expires date/time
                Assert.IsNull(homePageModel.Expires);
                //
                // DOUBLE CHECK DATABASE TO MAKE SURE THE MAX ARTICLE VERSION CAN BE FOUND
                //
                var allArticles = await dbContext.Articles.ToListAsync();
                var articles = await dbContext.Articles.Where(w => w.ArticleNumber == homePageModel.ArticleNumber).ToListAsync();
                var maxVersionNumber = dbContext.Articles.Where(w => w.ArticleNumber == homePageModel.ArticleNumber).Max(m => m.VersionNumber);

            }




            //
            // STEP 2: CREATE A NEW VERSION
            //
            // Create the new version here so we can edit it.
            //
            int id;
            // Create a new editor controller.
            using (var editorController =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                // Create a new version of the home page
                var createResult =
                    (RedirectToActionResult)await editorController.CreateVersion(homePageModel.ArticleNumber);

                // Get the ID of the article
                id = (int)createResult.RouteValues["id"];
            }

            //
            // STEP 3: RETRIEVE NEW VERSION TO EDIT, SET PUBLISHING DATE
            //
            ArticleViewModel newVersionViewModel;
            using (var editorController =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                // Now get the article
                var editViewResult = (ViewResult)await editorController.Edit(id.ToString());

                // Pull out the model
                newVersionViewModel = (ArticleViewModel)editViewResult.Model;

                // CHECK
                // Publishing date should NOT be set yet.
                Assert.IsNull(newVersionViewModel.Published);
            }

            //
            // STEP 4: SET THE PUBLISHING DATE OF THIS ARTICLE FOR THE FUTURE
            //
            // publish this version in the future and save
            var publishedDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(4), DateTimeKind.Utc);
            newVersionViewModel.Published = publishedDate;

            SaveResultJsonModel savedArticleViewModel;

            //
            // STEP 5: SAVE VERSION EDIT WITH NEW PUBLISHING DATE/TIME
            //
            using (var editorController =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo)))
            {
                // Save the article
                var saveHtmlResult = (JsonResult)await editorController.SaveHtml(newVersionViewModel);

                // Pull out the model
                savedArticleViewModel = (SaveResultJsonModel)saveHtmlResult.Value;

                Assert.IsTrue(savedArticleViewModel.Model.Published == publishedDate);

            }

            //
            // WORK FLOW DONE.
            //

            //
            // VALIDATE DATABASE FOR PUBLISHING AND EXPIRES DATE/TIME
            //
            Article original;
            Article newVersion;
            using (var dbContext = utils.GetApplicationDbContext())
            {
                original = dbContext.Articles.FirstOrDefault(f => f.Id == homePageModel.Id);
                newVersion = dbContext.Articles.FirstOrDefault(f => f.Id == newVersionViewModel.Id);

                //
                // CHECK: The publishing date/time from the
                //        new version should now match
                //        the expires date/time of the original.
                Assert.AreEqual(newVersion.Published.Value.ToUniversalTime(), original.Expires.Value.ToUniversalTime());
            }

        }
    }
}