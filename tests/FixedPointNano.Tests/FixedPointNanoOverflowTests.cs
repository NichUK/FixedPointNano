using Seerstone;

namespace Seerstone.Tests;

[TestFixture]
public sealed class FixedPointNanoOverflowTests
{
    [Test]
    public void AdditionShouldThrowOnOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MaxValue) + FixedPointNano.FromRaw(1),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MinValue) + FixedPointNano.FromRaw(-1),
                Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void SubtractionShouldThrowOnOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MinValue) - FixedPointNano.FromRaw(1),
                Throws.TypeOf<OverflowException>());
            Assert.That(
                () => _ = FixedPointNano.FromRaw(long.MaxValue) - FixedPointNano.FromRaw(-1),
                Throws.TypeOf<OverflowException>());
        });
    }

    [Test]
    public void UnaryNegationShouldThrowOnOverflow()
    {
        Assert.That(
            () => _ = -FixedPointNano.FromRaw(long.MinValue),
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void AbsShouldThrowOnMinValue()
    {
        Assert.That(
            () => _ = FixedPointNano.Abs(FixedPointNano.FromRaw(long.MinValue)),
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void CeilingShouldThrowOnOverflowForMaxFractionalValue()
    {
        // long.MaxValue has a non-zero fractional component; Ceiling tries to
        // add 1 to the integer part and then multiply by Scale, overflowing long.
        Assert.That(
            () => _ = FixedPointNano.Ceiling(FixedPointNano.FromRaw(long.MaxValue)),
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void FloorShouldThrowOnOverflowForMinFractionalValue()
    {
        // long.MinValue has a non-zero fractional component; Floor tries to
        // subtract 1 from the integer part and then multiply by Scale, overflowing long.
        Assert.That(
            () => _ = FixedPointNano.Floor(FixedPointNano.FromRaw(long.MinValue)),
            Throws.TypeOf<OverflowException>());
    }

    [Test]
    public void DivideByIntOverloadShouldWork()
    {
        var value = FixedPointNano.FromDecimal(7.5m);
        var result = FixedPointNano.Divide(value, 3);

        Assert.That(result.ToDecimal(), Is.EqualTo(2.5m));
    }

    [Test]
    public void DivideByIntOverloadShouldThrowOnZero()
    {
        Assert.That(
            () => _ = FixedPointNano.Divide(FixedPointNano.One, (int)0),
            Throws.TypeOf<DivideByZeroException>());
    }
}
