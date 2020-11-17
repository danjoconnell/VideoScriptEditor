using VideoScriptEditor.Tests.Mocks.MockModels;
using Xunit;

namespace VideoScriptEditor.Collections.Tests
{
    public class KeyFrameModelCollectionTests
    {
        private readonly KeyFrameModelCollection _keyFrameModelCollection;

        public KeyFrameModelCollectionTests()
        {
            _keyFrameModelCollection = new KeyFrameModelCollection
            {
                new MockKeyFrameModel(0),
                new MockKeyFrameModel(7),
                new MockKeyFrameModel(10),
                new MockKeyFrameModel(15),
                new MockKeyFrameModel(20),
                new MockKeyFrameModel(32),
                new MockKeyFrameModel(45),
                new MockKeyFrameModel(60),
                new MockKeyFrameModel(77),
                new MockKeyFrameModel(95),
                new MockKeyFrameModel(110),
                new MockKeyFrameModel(125),
                new MockKeyFrameModel(134),
                new MockKeyFrameModel(148),
                new MockKeyFrameModel(151),
                new MockKeyFrameModel(155),
                new MockKeyFrameModel(161),
                new MockKeyFrameModel(170),
                new MockKeyFrameModel(180)
            };
        }

        [Fact]
        public void BinarySearchTest()
        {
            int searchResult = _keyFrameModelCollection.BinarySearch(45);
            Assert.Equal(6, searchResult);
            Assert.Equal(45, _keyFrameModelCollection[searchResult].FrameNumber);

            searchResult = _keyFrameModelCollection.BinarySearch(100);
            Assert.True(searchResult < 0);
            searchResult = ~searchResult;
            Assert.Equal(10, searchResult);
            Assert.Equal(110, _keyFrameModelCollection[searchResult].FrameNumber);

            searchResult = _keyFrameModelCollection.BinarySearch(0);
            Assert.Equal(0, searchResult);
            Assert.Equal(0, _keyFrameModelCollection[searchResult].FrameNumber);

            searchResult = _keyFrameModelCollection.BinarySearch(180);
            Assert.Equal(_keyFrameModelCollection.Count - 1, searchResult);
            Assert.Equal(180, _keyFrameModelCollection[searchResult].FrameNumber);

            searchResult = _keyFrameModelCollection.BinarySearch(200);
            Assert.True(searchResult < 0);
            searchResult = ~searchResult;
            Assert.Equal(_keyFrameModelCollection.Count, searchResult);

            _keyFrameModelCollection.RemoveAt(0);
            searchResult = _keyFrameModelCollection.BinarySearch(5);
            Assert.Equal(-1, searchResult);
            searchResult = ~searchResult;
            Assert.Equal(0, searchResult);
            Assert.Equal(7, _keyFrameModelCollection[searchResult].FrameNumber);
        }
    }
}