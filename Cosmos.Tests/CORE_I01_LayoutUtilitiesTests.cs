using CDT.Cosmos.Cms.Data.Logic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_I01_LayoutUtilitiesTests
    {
        [TestMethod]
        public void A01_ReadCatalogSuccess()
        {
            var layoutUtilities = new LayoutUtilities();

            Assert.IsNotNull(layoutUtilities.CommunityCatalog);
        }

        [TestMethod]
        public async Task A02_GetDefaultLayout()
        {
            var layoutUtilities = new LayoutUtilities();

            var defaultLayout = await layoutUtilities.GetDefaultCommunityLayout();

            Assert.IsNotNull(defaultLayout);
        }

        [TestMethod]
        public async Task A03_GetLayouts()
        {
            var layoutUtilities = new LayoutUtilities();

            var layouts = await layoutUtilities.GetAllCommunityLayouts();

            Assert.IsTrue(layouts.Count > 1);
        }

    }
}
