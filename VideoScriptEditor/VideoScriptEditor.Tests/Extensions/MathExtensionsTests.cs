using Xunit;

namespace VideoScriptEditor.Extensions.Tests
{
    public class MathExtensionsTests
    {
        [Theory]
        [InlineData(10d, 15d, 0d, 10d)]
        [InlineData(10d, 15d, 1d, 15d)]
        [InlineData(10d, 15d, 0.5, 12.5)]
        public void Double_LerpToTest(double fromValue, double toValue, double lerpAmount, double expectedResult)
        {
            Assert.Equal(expectedResult, fromValue.LerpTo(toValue, lerpAmount));
        }

        [Fact]
        public void Double_RoundToNearestEvenIntegralTest()
        {
            double testValue = 839.11;
            Assert.Equal(840, testValue.RoundToNearestEvenIntegral());
        }

        [Theory]
        [InlineData(54u, 24u, 6u)]
        [InlineData(42u, 56u, 14u)]
        [InlineData(48u, 180u, 12u)]
        public void GreatestCommonDivisorTest(uint firstValue, uint secondValue, uint expectedResult)
        {
            Assert.Equal(expectedResult, MathExtensions.GCD(firstValue, secondValue));
        }

        [Fact]
        public void Int_RoundToNearestEvenIntegralTest()
        {
            int testValue = 839;
            Assert.Equal(840, testValue.RoundToNearestEvenIntegral());
        }
    }
}