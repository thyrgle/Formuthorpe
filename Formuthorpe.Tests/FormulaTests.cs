using Formuthorpe;

using Xunit;

namespace Formuthorpe.Tests;

// Every formula in these tests follows the classic diamond shape where useful:
// changing a Term must propagate through every Compound that depends on it.
public class FormulaTests
{
    [Fact]
    public void Addition_EvaluatesInitialValue()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;

        Assert.Equal(5, c.Value);
    }

    [Fact]
    public void Addition_UpdatesAutomaticallyWhenTermChanges()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;

        Assert.Equal(5, c.Value);

        a.SetValue(3);

        Assert.Equal(6, c.Value);
    }

    [Fact]
    public void Addition_UpdatesWhenSecondOperandChanges()
    {
        Term<int> a = new(3);
        Term<int> b = new(4);
        Formula<int> c = a + b;

        Assert.Equal(7, c.Value);

        b.SetValue(10);

        Assert.Equal(13, c.Value);
    }

    [Fact]
    public void BinaryOperators_EvaluateAndTrack()
    {
        Term<int> a = new(10);
        Term<int> b = new(3);

        Formula<int> sum = a + b;
        Formula<int> difference = a - b;
        Formula<int> product = a * b;
        Formula<int> quotient = a / b;
        Formula<int> remainder = a % b;

        Assert.Equal(13, sum.Value);
        Assert.Equal(7, difference.Value);
        Assert.Equal(30, product.Value);
        Assert.Equal(3, quotient.Value);
        Assert.Equal(1, remainder.Value);

        a.SetValue(20);

        Assert.Equal(23, sum.Value);
        Assert.Equal(17, difference.Value);
        Assert.Equal(60, product.Value);
        Assert.Equal(6, quotient.Value);
        Assert.Equal(2, remainder.Value);
    }

    [Fact]
    public void UnaryNegation_TracksTermChanges()
    {
        Term<int> a = new(4);
        Formula<int> c = -a;

        Assert.Equal(-4, c.Value);

        a.SetValue(-2);

        Assert.Equal(2, c.Value);
    }

    [Fact]
    public void MixedExpression_MixesTermsAndRawValues()
    {
        Term<int> a = new(4);
        Formula<int> c = a * 2 + 1;

        Assert.Equal(9, c.Value);

        a.SetValue(10);

        Assert.Equal(21, c.Value);
    }

    [Fact]
    public void ChainedFormulas_PropagateThroughMultipleLevels()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;
        Formula<int> d = c * c;
        Formula<int> e = d + c;

        Assert.Equal(25, d.Value);
        Assert.Equal(30, e.Value);

        a.SetValue(4);

        // c = 7, d = 49, e = 56
        Assert.Equal(7, c.Value);
        Assert.Equal(49, d.Value);
        Assert.Equal(56, e.Value);
    }

    [Fact]
    public void DiamondDependency_EveryPathSeesTheUpdate()
    {
        Term<int> a = new(5);
        Term<int> b = new(3);
        Formula<int> m = a + b;
        Formula<int> n = a - b;
        Formula<int> p = m * n;

        Assert.Equal(16, p.Value);

        a.SetValue(7);

        // m = 10, n = 4, p = 40
        Assert.Equal(10, m.Value);
        Assert.Equal(4, n.Value);
        Assert.Equal(40, p.Value);
    }

    [Fact]
    public void IndependentFormulas_UpdateSeparately()
    {
        Term<int> a = new(6);
        Term<int> b = new(2);
        Formula<int> sum = a + b;
        Formula<int> product = a * b;

        Assert.Equal(8, sum.Value);
        Assert.Equal(12, product.Value);

        a.SetValue(1);

        Assert.Equal(3, sum.Value);
        Assert.Equal(2, product.Value);
    }

    [Fact]
    public void Term_UnchangedOperandsKeepTheirValues()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;

        Assert.Equal(5, c.Value);

        a.SetValue(100);

        Assert.Equal(3, b.Value);
    }

    [Fact]
    public void ImplicitConversion_WrapsRawValueAsTerm()
    {
        Formula<int> f = 5;

        Assert.Equal(5, f.Value);

        Term<int> a = new(2);
        Formula<int> g = a + 40;

        Assert.Equal(42, g.Value);
    }

    [Fact]
    public void ExplicitConversion_ExtractsCurrentValue()
    {
        Term<int> a = new(2);
        Formula<int> c = a * 10;

        Assert.Equal(20, (int)c);

        a.SetValue(5);

        Assert.Equal(50, (int)c);
    }

    [Fact]
    public void Update_EagerlyRefreshesAllDependents()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;
        Formula<int> d = c * 2;

        Assert.Equal(10, d.Value);

        a.SetValue(5);
        c.Update();

        Assert.Equal(16, d.Value);
    }

    [Fact]
    public void DoubleArithmetic_PreservesFractions()
    {
        Term<double> a = new(7.0);
        Term<double> b = new(2.0);
        Formula<double> c = a / b;

        Assert.Equal(3.5, c.Value);

        a.SetValue(7.5);

        Assert.Equal(3.75, c.Value);
    }

    [Fact]
    public void DecimalArithmetic_TracksChanges()
    {
        Term<decimal> price = new(19.99m);
        Term<decimal> quantity = new(3m);
        Formula<decimal> total = price * quantity;

        Assert.Equal(59.97m, total.Value);

        quantity.SetValue(4m);

        Assert.Equal(79.96m, total.Value);
    }
}
