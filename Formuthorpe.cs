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
    /// once, so no formula ever observes a stale operand along the way.
    /// Formulas built from a registered Abelian operation are not recombined
    /// from their operands; instead their cached value is patched with the
    /// inverse of each changed operand's old value followed by its new value,
    /// which is equivalent in an Abelian group. Once the whole graph is
    /// consistent again, the on-change callbacks of every formula whose value
    /// actually changed are invoked.
    /// </summary>
    public void Update()
    {
        // Collect in post-order (dependents before their operands), then pop
        // the stack to recompute in the reverse: operands before dependents.
        Stack<Formula<T>> order = new();
        HashSet<Formula<T>> visited = new();
        CollectDependents(order, visited);
        // Pre-update value of every formula processed in this pass; used to
        // derive per-operand deltas when patching Abelian formulas.
        Dictionary<Formula<T>, T> oldValues = new();
        List<Formula<T>> changed = new();
        foreach (Formula<T> form in order)
        {
            T oldVal = form.cachedValue;
            oldValues[form] = oldVal;
            T newVal = form.Recompute(oldValues);
            if (!EqualityComparer<T>.Default.Equals(newVal, oldVal))
            {
                form.cachedValue = newVal;
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

    /// <summary>
    /// Computes this formula's new value during an update pass. oldValues
    /// maps every formula already processed in this pass to its value before
    /// the pass. The default implementation just re-evaluates; Compounds
    /// built from a registered Abelian operation override this to patch the
    /// old value with per-operand deltas instead.
    /// </summary>
    internal virtual T Recompute(Dictionary<Formula<T>, T> oldValues) => Evaluate();

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

    // Addition forms an Abelian group over every INumber<T> (inverse is
    // negation, identity is zero), so + is registered as Abelian and all
    // sums in the graph update through the incremental shortcut.
    public static Formula<T> operator +(Formula<T> a, Formula<T> b) => Abelian(a, b, AbelianOps.Addition<T>());
    public static Formula<T> operator -(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x - y);
    public static Formula<T> operator *(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x * y);
    public static Formula<T> operator /(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x / y);
    public static Formula<T> operator %(Formula<T> a, Formula<T> b) => Binary(a, b, (x, y) => x % y);

    public static Formula<T> operator -(Formula<T> a) => Unary(a, x => -x);

    /// <summary>
    /// Builds a formula combining two operands with a registered Abelian
    /// group operation. Such formulas update incrementally: when an operand
    /// changes from xOld to xNew, the value is patched with
    /// value ∘ inverse(xOld) ∘ xNew rather than recomputed, and the same
    /// shortcut carries the delta to every Abelian dependent above it.
    /// </summary>
    public static Formula<T> Abelian(Formula<T> a, Formula<T> b, AbelianOp<T> op)
    {
        Compound<T> retForm = new(a, b, op);
        a.AddParent(retForm);
        b.AddParent(retForm);
        return retForm;
    }

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
     * binaryOp: function delegate for a binary operation, null when the
     *           operation was registered as Abelian
     * group:    the registered Abelian group when this compound's operation
     *           is Abelian, null for plain operations
     */

    private readonly Formula<T> lhs;
    private readonly Formula<T>? rhs;
    private readonly Func<T, T>? unaryOp;
    private readonly Func<T, T, T>? binaryOp;
    private readonly AbelianOp<T>? group;

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

    /// <summary>
    /// Creates a compound over a registered Abelian group operation, which
    /// qualifies it for incremental (inverse-based) updates.
    /// </summary>
    public Compound(Formula<T> left, Formula<T> right, AbelianOp<T> abelianOp)
    {
        lhs = left;
        rhs = right;
        group = abelianOp;
        Refresh();
    }

    protected override T Evaluate() =>
        group is not null
            ? group.Combine(lhs.Value, rhs!.Value)
            : rhs is null ? unaryOp!(lhs.Value) : binaryOp!(lhs.Value, rhs.Value);

    internal override T Recompute(Dictionary<Formula<T>, T> oldValues)
    {
        // Plain (and unary) operations recompute from scratch as before.
        if (group is null)
        {
            return Evaluate();
        }
        // Abelian shortcut: for every operand occurrence x whose value
        // changed during this pass, v' = v ∘ inverse(xOld) ∘ xNew. This
        // removes x's old contribution and adds the new one without
        // touching the operands that did not change. An operand absent
        // from oldValues took no part in this pass, so it contributes no
        // delta.
        AbelianOp<T> abelian = group;
        T result = Value;
        PatchOperand(lhs);
        PatchOperand(rhs!);
        return result;

        void PatchOperand(Formula<T> operand)
        {
            if (oldValues.TryGetValue(operand, out T? operandOld)
                && !EqualityComparer<T>.Default.Equals(operandOld, operand.Value))
            {
                result = abelian.Patch(result, operandOld, operand.Value);
            }
        }
    }
}

/// <summary>
/// A binary operation registered as an Abelian (commutative) group over
/// T: Combine must be associative and commutative, Inverse must return
/// the group inverse of an element, and Identity is the neutral element.
/// Registering one through Formula.Abelian lets every formula built from
/// it skip full recomputation: because v = x ∘ rest rearranges to
/// rest = inverse(x) ∘ v, an operand moving from xOld to xNew changes
/// the value by exactly inverse(xOld) ∘ xNew, so the new value is
/// v ∘ inverse(xOld) ∘ xNew regardless of how large the untouched rest
/// of the expression is. The caller guarantees the group laws hold; the
/// shortcut silently produces wrong values otherwise. For floating point
/// types the patched result is mathematically equal to the recomputed
/// one but may differ in the last ulps due to rounding.
/// </summary>
public sealed class AbelianOp<T> where T : INumber<T>
{
    /* Fields
     * combine:  the group operation
     * inverse:  the group inverse function
     * identity: the group's neutral element
     */

    /// <summary>The associative, commutative group operation.</summary>
    public Func<T, T, T> Combine { get; }

    /// <summary>Returns the group inverse of an element.</summary>
    public Func<T, T> Inverse { get; }

    /// <summary>The neutral element of the group.</summary>
    public T Identity { get; }

    public AbelianOp(Func<T, T, T> combine, Func<T, T> inverse, T identity)
    {
        Combine = combine;
        Inverse = inverse;
        Identity = identity;
    }

    /// <summary>
    /// Repairs an aggregate after one of its operand occurrences moved from
    /// xOld to xNew: aggregate ∘ inverse(xOld) ∘ xNew.
    /// </summary>
    public T Patch(T aggregate, T xOld, T xNew) =>
        Combine(Combine(aggregate, Inverse(xOld)), xNew);
}

/// <summary>Well-known Abelian registrations shared by all formulas.</summary>
public static class AbelianOps
{
    private static class AdditionCache<TC> where TC : INumber<TC>
    {
        public static readonly AbelianOp<TC> Op = new((x, y) => x + y, x => -x, TC.Zero);
    }

    /// <summary>
    /// The additive group every INumber type carries for free; the +
    /// operator uses it automatically.
    /// </summary>
    public static AbelianOp<T> Addition<T>() where T : INumber<T> => AdditionCache<T>.Op;
}
