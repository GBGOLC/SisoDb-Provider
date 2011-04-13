using System.Linq;
using NUnit.Framework;
using SisoDb.Structures.Schemas;

namespace SisoDb.Tests.UnitTests.Structures.Schemas
{
    [TestFixture]
    public class StructurePropertyPathBuilderTests : UnitTestBase
    {
        [Test]
        public void BuildPath_ForFirstLevelScalar_NoRootButPathWithNoDelimitor()
        {
            var property = GetPropertyByPath("Int1");

            var path = PropertyPathBuilder.BuildPath(property);

            Assert.AreEqual("Int1", path);
        }

        [Test]
        public void BuildPath_ForSecondLevelScalar_NoRootButPathWithDelimitor()
        {
            var property = GetPropertyByPath("Dummy2.Bool1");

            var path = PropertyPathBuilder.BuildPath(property);

            Assert.AreEqual("Dummy2.Bool1", path);
        }

        private static IStructureProperty GetPropertyByPath(string path)
        {
            return StructureTypeFor<Dummy1>.Instance.IndexableProperties.Where(p => p.Path == path).Single();
        }

        private class Dummy1
        {
            public int Int1 { get; set; }

            public Dummy2 Dummy2 { get; set; }
        }

        private class Dummy2
        {
            public bool Bool1 { get; set; }
        }
    }
}