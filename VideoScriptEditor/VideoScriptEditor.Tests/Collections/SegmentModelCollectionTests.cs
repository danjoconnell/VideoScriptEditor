using System;
using VideoScriptEditor.Tests.Mocks.MockModels;
using Xunit;

namespace VideoScriptEditor.Collections.Tests
{
    public class SegmentModelCollectionTests
    {
        private SegmentModelCollection _segmentModelCollection;

        public SegmentModelCollectionTests()
        {
            _segmentModelCollection = new SegmentModelCollection();
        }

        [Fact]
        public void TestAdds()
        {
            var firstSegment = new MockSegmentModel(0, 10, 0);
            var secondSegment = new MockSegmentModel(0, 10, 1);
            var thirdSegment = new MockSegmentModel(15, 25, 0);
            _segmentModelCollection.Add(firstSegment);
            _segmentModelCollection.Add(secondSegment);
            _segmentModelCollection.Add(thirdSegment);

            Assert.Equal(3, _segmentModelCollection.Count);

            // Check sort order (should order by TrackNumber, then by StartFrame)
            Assert.Equal(firstSegment, _segmentModelCollection[0]);
            Assert.Equal(thirdSegment, _segmentModelCollection[1]);
            Assert.Equal(secondSegment, _segmentModelCollection[2]);
        }

        [Fact]
        public void TestOverlappingAddThrowsException()
        {
            _segmentModelCollection.Add(new MockSegmentModel(0, 10, 0));

            Assert.Throws<ArgumentException>(
                () => _segmentModelCollection.Add(new MockSegmentModel(8, 25, 0))
            );
        }
    }
}
