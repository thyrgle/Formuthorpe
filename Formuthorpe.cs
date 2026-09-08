namespace Formuthorpe;

using System.Numerics;

/// <summary>
/// A node in a reactive arithmetic expression graph.
/// Concrete nodes are either Term (a leaf holding a value) or Compound
/// (an operation over other formulas). Reading Value always reflects
/// the current state of every Term the formula depends on.
/// </summary>
public abstract class Formula<T> where T : INumber<T>
{
    /* Fields
     * parentForms: formulas that use this one as an operand
     * cachedValue: last computed value of this formula
     * dirty:       true when cachedValue is stale and must be recomputed
     */

    private readonly List<Formula<T>> parentForms = new();
    private T cachedValue = default!;
    private bool dirty = true;

    /// <summary>The current value of this formula, recomputing if stale.</summary>
    public T Value
    {
        get
        {
            if (dirty)
            {
                cachedValue = Evaluate();
                dirty = false;
            }
            return cachedValue;
        }
    }

    /// <summary>Recomputes this formula's value and eagerly cascades the update to all dependents.</summary>
    public void Update()
    {
        dirty = true;
        _ = Value;
        foreach (Formula<T> form in parentForms)
        {
            form.Update();
        }
    }

    /// <summary>Compute this formula's value from its operands (or stored value for terms).</summary>
    protected abstract T Evaluate();

    internal void AddParent(Formula<T> newParent)
    {
        parentForms.Add(newParent);
    }

    /// <summary>Marks this formula and every dependent of it as stale.</summary>
    internal void Invalidate()
    {
        // If we are already dirty then, by invariant, every ancestor is dirty too.
        if (dirty) return;
        dirty = true;
        foreach (Formula<T> form in parentForms)
        {
            form.Invalidate();
        }
    }

    public override string ToString() => Value.ToString() ?? string.Empty;

    // --- Arithmetic operator factories ---

    private static Formula<T> Binary(Formula<T> a, Formula<T> b, Func<T, T, T> op)
    {
        Compound<T> retForm = new(a, b, op);
        a.AddParent(retForm);
        b.AddParent(retForm);
        return retForm;
    }

    private static Formula<T> Unary(Formula<T> a, Func<T, T> op)
    {
        Compound<T> retForm = new(a, op);
        a.AddParent(retForm);
        return retForm;
    }

    public static Formula<T> operator +(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x + y);
    public static Formula<T> operator -(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x - y);
    public static Formula<T> operator *(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x * y);
    public static Formula<T> operator /(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x / y);
    public static Formula<T> operator %(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x % y);

    public static Formula<T> operator -(Formula<T> a) => Unary(a, x => -x);

    /// <summary>Allows mixing formulas with plain numbers, e.g. a + 2.</summary>
    public static implicit operator Formula<T>(T value) => new Term<T>(value);

    public static explicit operator T(Formula<T> formula) => formula.Value;
}

/// <summary>
/// A leaf node holding an actual value. Changes propagate to every
/// formula that depends on it.
/// </summary>
public sealed class Term<T> : Formula<T> where T : INumber<T>
{
    /* Fields
     * val: value held by the term
     */

    private T val;

    public Term(T initVal)
    {
        val = initVal;
    }

    protected override T Evaluate() => val;

    public void SetValue(T newVal)
    {
        val = newVal;
        Update();
    }
}

/// <summary>
/// An interior node applying an operation to one (unary) or two (binary)
/// operand formulas. Its value is computed lazily from its operands.
/// </summary>
public sealed class Compound<T> : Formula<T> where T : INumber<T>
{
    /* Fields
     * lhs:      left hand side of the operation, only operand if unary
     * rhs:      right hand side of the operation, null if unary
     * unaryOp:  function delegate for a unary operation
     * binaryOp: function delegate for a binary operation
     */

    private readonly Formula<T> lhs;
    private readonly Formula<T>? rhs;
    private readonly Func<T, T>? unaryOp;
    private readonly Func<T, T, T>? binaryOp;

    public Compound(Formula<T> operand, Func<T, T> op)
    {
        lhs = operand;
        unaryOp = op;
    }

    public Compound(Formula<T> left, Formula<T> right, Func<T, T, T> op)
    {
        lhs = left;
        rhs = right;
        binaryOp = op;
    }

    protected override T Evaluate() =>
        rhs is null ? unaryOp!(lhs.Value) : binaryOp!(lhs.Value, rhs.Value);
}
