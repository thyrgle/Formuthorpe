namespace Formuthorpe;

using System.Numerics;

/// <summary>
/// A node in a reactive arithmetic expression graph.
/// Concrete nodes are either Term (a leaf holding a value) or Compound
/// (an operation over other formulas). Updates are eager: the moment a
/// Term takes a new value, the change propagates upward and every
/// dependent formula recomputes, so Value is always current.
/// </summary>
public abstract class Formula<T> where T : INumber<T>
{
    /* Fields
     * parentForms:       formulas that use this one as an operand
     * cachedValue:       current value of this formula, recomputed eagerly at
     *                    construction and whenever an operand below it changes
     * onChangeCallbacks: callbacks invoked whenever this formula's value
     *                    actually changes during an update
     */

    private readonly List<Formula<T>> parentForms = new();
    private readonly List<Action> onChangeCallbacks = new();
    private T cachedValue = default!;

    /// <summary>The current value of this formula; always up to date.</summary>
    public T Value => cachedValue;

    /// <summary>
    /// Registers a callback that runs whenever this formula's value changes.
    /// Callbacks fire only if the new value differs from the previous one;
    /// setting a formula (or an operand beneath it) to a value that leaves
    /// this formula's value unchanged does not invoke them.
    /// </summary>
    public void OnChange(Action callback) => onChangeCallbacks.Add(callback);

    /// <summary>
    /// Recomputes this formula's value and eagerly cascades the update to all
    /// dependents. Dependents are recomputed in dependency order, each exactly
    /// once, so no formula ever observes a stale operand along the way. Once
    /// the whole graph is consistent again, the on-change callbacks of every
    /// formula whose value actually changed are invoked.
    /// </summary>
    public void Update()
    {
        // Collect in post-order (dependents before their operands), then pop
        // the stack to recompute in the reverse: operands before dependents.
        Stack<Formula<T>> order = new();
        HashSet<Formula<T>> visited = new();
        CollectDependents(order, visited);
        List<Formula<T>> changed = new();
        foreach (Formula<T> form in order)
        {
            T oldVal = form.cachedValue;
            form.cachedValue = form.Evaluate();
            if (!EqualityComparer<T>.Default.Equals(form.cachedValue, oldVal))
            {
                changed.Add(form);
            }
        }
        // Fire only after every value has settled so callbacks observe a
        // fully consistent graph.
        foreach (Formula<T> form in changed)
        {
            form.FireOnChange();
        }
    }

    /// <summary>Invokes every callback registered through OnChange.</summary>
    private void FireOnChange()
    {
        foreach (Action callback in onChangeCallbacks)
        {
            callback();
        }
    }

    /// <summary>Compute this formula's value from its operands (or stored value for terms).</summary>
    protected abstract T Evaluate();

    /// <summary>Seeds the cached value from the operands; called once at construction.</summary>
    protected void Refresh()
    {
        cachedValue = Evaluate();
    }

    internal void AddParent(Formula<T> newParent)
    {
        parentForms.Add(newParent);
    }

    /// <summary>
    /// Gathers this formula and every transitive dependent in post-order,
    /// skipping formulas already reached through an earlier path, so shared
    /// dependents (diamonds) are collected exactly once.
    /// </summary>
    private void CollectDependents(Stack<Formula<T>> order, HashSet<Formula<T>> visited)
    {
        if (!visited.Add(this))
        {
            return;
        }
        foreach (Formula<T> form in parentForms)
        {
            form.CollectDependents(order, visited);
        }
        order.Push(this);
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
/// A leaf node holding an actual value. Setting a new value propagates
/// eagerly to every formula that depends on it.
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
        Refresh();
    }

    protected override T Evaluate() => val;

    public void SetValue(T newVal)
    {
        // Setting the value it already holds changes nothing: skip the
        // propagation entirely so no callbacks fire anywhere.
        if (EqualityComparer<T>.Default.Equals(val, newVal))
        {
            return;
        }
        val = newVal;
        // Push the new value upward immediately; every dependent recomputes now.
        Update();
    }
}

/// <summary>
/// An interior node applying an operation to one (unary) or two (binary)
/// operand formulas. Its value is computed eagerly: once at construction
/// and again whenever any operand below it changes.
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
        Refresh();
    }

    public Compound(Formula<T> left, Formula<T> right, Func<T, T, T> op)
    {
        lhs = left;
        rhs = right;
        binaryOp = op;
        Refresh();
    }

    protected override T Evaluate() =>
        rhs is null ? unaryOp!(lhs.Value) : binaryOp!(lhs.Value, rhs.Value);
}
