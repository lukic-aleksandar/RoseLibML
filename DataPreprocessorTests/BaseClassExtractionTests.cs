using DataPreprocessor;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void BaselessTest()
        {
            ProjectAnalyser projectAnalyser = new ProjectAnalyser();

            FileInfo fileInfo1 = new FileInfo(@".\\TestFiles\\Baseless\\BaseEntity.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo1);
            
            FileInfo fileInfo2 = new FileInfo(@".\\TestFiles\\Baseless\\CommonHelper.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo2);

            var dictionary = projectAnalyser.ComponentGroups;
            var numberOfKeys = projectAnalyser.ComponentGroups.Count;
            var implicitBaseClass = ProjectAnalyser.IMPLICIT_BASE_CLASS;

            Assert.That(numberOfKeys, Is.EqualTo(1));
            Assert.IsNotNull(dictionary[implicitBaseClass]);
            Assert.That(dictionary[implicitBaseClass].Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Base classes: BaseDataProvider, INopDataProvider for SqLiteNopDataProvider
        /// Base classes: INopStartup for NopDbStartup
        /// </summary>
        [Test]
        public void WithBasesTest()
        {
            ProjectAnalyser projectAnalyser = new ProjectAnalyser();

            string expectedKey1 = "INopStartup";
            FileInfo fileInfo1 = new FileInfo(@".\\TestFiles\\Standard\\NopDbStartup.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo1);

            string expectedKey2 = "BaseDataProvider";
            string expectedKey3 = "INopDataProvider";
            FileInfo fileInfo2 = new FileInfo(@".\\TestFiles\\Standard\\SqLiteNopDataProvider.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo2);

            var dictionary = projectAnalyser.ComponentGroups;
            var numberOfKeys = projectAnalyser.ComponentGroups.Count;

            Assert.That(numberOfKeys, Is.EqualTo(3));
            Assert.IsNotNull(dictionary[expectedKey1]);
            Assert.IsNotNull(dictionary[expectedKey2]);
            Assert.IsNotNull(dictionary[expectedKey3]);
            Assert.That(dictionary[expectedKey3].First().Item1, Is.EqualTo(dictionary[expectedKey2].First().Item1));
        }

        /// <summary>
        /// Base classes: IRepository<TEntity> for EntityRepository<TEntity>
        /// Base classes: List<T>, IPagedList<T> for PagedList<T> 
        /// </summary>
        [Test]
        public void WithGenericBasesTest()
        {
            ProjectAnalyser projectAnalyser = new ProjectAnalyser();

            string expectedKey1 = "IRepository";
            FileInfo fileInfo1 = new FileInfo(@".\\TestFiles\\Generic\\EntityRepository.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo1);

            string expectedKey2 = "List";
            string expectedKey3 = "IPagedList";
            FileInfo fileInfo2 = new FileInfo(@".\\TestFiles\\Generic\\PagedList.txt");
            projectAnalyser.AnalyseFileAndGroup(fileInfo2);

            var dictionary = projectAnalyser.ComponentGroups;
            var numberOfKeys = projectAnalyser.ComponentGroups.Count;

            Assert.That(numberOfKeys, Is.EqualTo(3));
            Assert.IsNotNull(dictionary[expectedKey1]);
            Assert.IsNotNull(dictionary[expectedKey2]);
            Assert.IsNotNull(dictionary[expectedKey3]);
            Assert.That(dictionary[expectedKey3].First().Item1, Is.EqualTo(dictionary[expectedKey2].First().Item1));
        }
    }
}