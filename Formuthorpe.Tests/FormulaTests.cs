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

    [Fact]
    public void OnChange_FiresWhenTermValueChanges()
    {
        Term<int> a = new(2);
        int fired = 0;
        a.OnChange(() => fired++);

        a.SetValue(3);

        Assert.Equal(1, fired);
    }

    [Fact]
    public void OnChange_DoesNotFireWhenTermSetToPreviousValue()
    {
        Term<int> a = new(2);
        int fired = 0;
        a.OnChange(() => fired++);

        a.SetValue(2);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void OnChange_FiresWhenFormulaValueChanges()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;
        int fired = 0;
        c.OnChange(() => fired++);

        a.SetValue(5);

        Assert.Equal(1, fired);
    }

    [Fact]
    public void OnChange_DoesNotFireWhenFormulaValueStaysTheSame()
    {
        // c = a * zero stays 0 no matter how a changes.
        Term<int> a = new(5);
        Term<int> zero = new(0);
        Formula<int> c = a * zero;
        int termFired = 0;
        int formulaFired = 0;
        a.OnChange(() => termFired++);
        c.OnChange(() => formulaFired++);

        a.SetValue(10);

        Assert.Equal(1, termFired);
        Assert.Equal(0, formulaFired);
        Assert.Equal(0, c.Value);
    }

    [Fact]
    public void OnChange_SupportsMultipleCallbacksOnOneFormula()
    {
        Term<int> a = new(1);
        int first = 0;
        int second = 0;
        a.OnChange(() => first++);
        a.OnChange(() => second++);

        a.SetValue(2);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void OnChange_FiresOncePerFormulaInDiamond()
    {
        Term<int> a = new(5);
        Term<int> b = new(3);
        Formula<int> m = a + b;
        Formula<int> n = a - b;
        Formula<int> p = m * n;
        int fired = 0;
        p.OnChange(() => fired++);

        a.SetValue(7);

        Assert.Equal(1, fired);
    }

    [Fact]
    public void OnChange_CallbacksSeeSettledValues()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;
        Formula<int> d = c * 2;
        int observed = 0;
        c.OnChange(() => observed = d.Value);

        a.SetValue(5);

        Assert.Equal(16, observed);
    }

    [Fact]
    public void OnChange_DoesNotFireAtRegistration()
    {
        Term<int> a = new(2);
        Formula<int> c = a * 10;
        int fired = 0;

        c.OnChange(() => fired++);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void OnChange_UnrelatedFormulaDoesNotFire()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> sum = a + b;
        Formula<int> product = a * b;
        int sumFired = 0;
        int productFired = 0;
        sum.OnChange(() => sumFired++);
        product.OnChange(() => productFired++);

        b.SetValue(10);

        Assert.Equal(1, sumFired);
        Assert.Equal(1, productFired);

        a.SetValue(4);

        Assert.Equal(2, sumFired);
        Assert.Equal(2, productFired);
    }

    [Fact]
    public void OnChange_DoubleFiresOnlyOnRealChange()
    {
        Term<double> a = new(1.5);
        int fired = 0;
        a.OnChange(() => fired++);

        a.SetValue(1.5);
        Assert.Equal(0, fired);

        a.SetValue(2.5);
        Assert.Equal(1, fired);
    }
}

// Tests for user-registered Abelian group operations: values must track
// exactly as with plain operations, but the update goes through the
// inverse-based shortcut (v' = v ∘ inverse(xOld) ∘ xNew).
public class AbelianOpTests
{
    // XOR: every element is its own inverse, identity is 0.
    private static readonly AbelianOp<int> xor = new((x, y) => x ^ y, x => x, 0);

    [Fact]
    public void Abelian_CustomOperation_EvaluatesAndTracks()
    {
        Term<int> a = new(5);
        Term<int> b = new(2);
        Formula<int> c = Formula<int>.Abelian(a, b, xor);

        Assert.Equal(7, c.Value);

        a.SetValue(3);

        Assert.Equal(1, c.Value);
    }

    [Fact]
    public void Abelian_Chain_PatchesThroughEveryLevel()
    {
        Term<int> a = new(12);
        Term<int> b = new(10);
        Term<int> c = new(3);
        Formula<int> ab = Formula<int>.Abelian(a, b, xor);
        Formula<int> d = Formula<int>.Abelian(ab, c, xor);

        Assert.Equal(6, ab.Value);
        Assert.Equal(5, d.Value);

        a.SetValue(1);

        // ab = 1 ^ 10 = 11, d = 11 ^ 3 = 8
        Assert.Equal(11, ab.Value);
        Assert.Equal(8, d.Value);
    }

    [Fact]
    public void Abelian_SharedOperand_DeltaAppliedPerOccurrence()
    {
        // p combines the very same formula in both operand slots: the
        // operand's delta must be applied once per occurrence, not once
        // per distinct formula.
        Term<int> a = new(3);
        Term<int> b = new(5);
        Formula<int> m = Formula<int>.Abelian(a, b, xor);
        Formula<int> p = Formula<int>.Abelian(m, m, xor);

        Assert.Equal(6, m.Value);
        Assert.Equal(0, p.Value);

        a.SetValue(1);

        // m = 1 ^ 5 = 4, p = m ^ m = 0; a single delta would give 0 ^ 6 ^ 4 = 2
        Assert.Equal(4, m.Value);
        Assert.Equal(0, p.Value);
    }

    [Fact]
    public void Abelian_SharedOperandAggregateUnchanged_NoCallback()
    {
        Term<int> a = new(3);
        Term<int> b = new(5);
        Formula<int> m = Formula<int>.Abelian(a, b, xor);
        Formula<int> p = Formula<int>.Abelian(m, m, xor);
        int mFired = 0;
        int pFired = 0;
        m.OnChange(() => mFired++);
        p.OnChange(() => pFired++);

        a.SetValue(1);

        Assert.Equal(1, mFired);
        Assert.Equal(0, pFired);
    }

    [Fact]
    public void Abelian_MultiplicativeGroupOverDoubles_Tracks()
    {
        // Multiplication over nonzero doubles: inverse is the reciprocal.
        AbelianOp<double> mult = new((x, y) => x * y, x => 1 / x, 1.0);
        Term<double> a = new(4.0);
        Term<double> b = new(2.0);
        Term<double> c = new(8.0);
        Formula<double> ab = Formula<double>.Abelian(a, b, mult);
        Formula<double> d = Formula<double>.Abelian(ab, c, mult);

        Assert.Equal(64.0, d.Value);

        a.SetValue(2.0);

        // Powers of two keep the reciprocal patches exact.
        Assert.Equal(4.0, ab.Value);
        Assert.Equal(32.0, d.Value);
    }

    [Fact]
    public void Mixed_AbelianAndPlainNodes_ProduceCorrectValues()
    {
        // Plain nodes below and above an Abelian node: only the Abelian
        // node is patched; the plain ones recompute as before.
        Term<int> a = new(2);
        Term<int> b = new(3);
        Term<int> c = new(10);
        Formula<int> doubled = a * 2;
        Formula<int> sum = doubled + b;
        Formula<int> scaled = sum * c;

        Assert.Equal(7, sum.Value);
        Assert.Equal(70, scaled.Value);

        a.SetValue(5);

        Assert.Equal(13, sum.Value);
        Assert.Equal(130, scaled.Value);

        b.SetValue(0);

        Assert.Equal(10, sum.Value);
        Assert.Equal(100, scaled.Value);
    }

    [Fact]
    public void Abelian_Diamond_BothOperandDeltasApply()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> m = a + b;
        Formula<int> n = a - b;
        Formula<int> p = m + n;

        Assert.Equal(5, m.Value);
        Assert.Equal(-1, n.Value);
        Assert.Equal(4, p.Value);

        a.SetValue(7);

        // m and n both change at once; p must absorb both deltas.
        Assert.Equal(10, m.Value);
        Assert.Equal(4, n.Value);
        Assert.Equal(14, p.Value);
    }

    [Fact]
    public void Update_MidLevelAbelianFormula_StaysConsistent()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = a + b;
        Formula<int> d = c + 1;

        a.SetValue(5);
        c.Update();

        Assert.Equal(8, c.Value);
        Assert.Equal(9, d.Value);
    }

    [Fact]
    public void OnChange_AbelianFormula_FiresOnlyOnRealChange()
    {
        Term<int> a = new(2);
        Term<int> b = new(3);
        Formula<int> c = Formula<int>.Abelian(a, b, xor);
        int fired = 0;
        c.OnChange(() => fired++);

        a.SetValue(4);

        Assert.Equal(1, fired);
        Assert.Equal(7, c.Value);
    }
}
