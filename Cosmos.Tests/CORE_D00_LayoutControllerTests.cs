using CDT.Cosmos.Cms.Data.Logic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_D00_LayoutControllerTests
    {
        private static Utilities? utils;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            utils = new Utilities();
        }

        [TestMethod]
        public async Task A00_ReadCommunityLayouts()
        {
            using (var controller = utils.GetLayoutsController())
            {
                var result = await controller.Read_CommunityLayouts(new Kendo.Mvc.UI.DataSourceRequest());

                Assert.IsNotNull(result);

                var jsonResult = (JsonResult) result;

                Assert.IsNotNull(jsonResult.Value);

                var dataSet = (Kendo.Mvc.UI.DataSourceResult)jsonResult.Value;

                var model = (List<LayoutCatalogItem>)dataSet.Data;

                Assert.IsTrue(model.Count > 0);




            }
        }

        [TestMethod]
        public async Task A01_LoadLayout()
        {
            using var controller = utils.GetLayoutsController();
            var result = await controller.ImportCommunityLayout("mdb-cfc");

            using var dbContext = utils.GetApplicationDbContext(null, utils.GetCosmosConfigOptionsNotInSetupMode());

            Assert.IsTrue(dbContext.Layouts.Count() > 0);
            Assert.IsTrue(dbContext.Templates.Count() > 0);
        }
    }
}
