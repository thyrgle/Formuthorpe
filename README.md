# Formuthorpe

Formuthorpe is a radically simple reactive programming library for C#. Reactive programming allows for variables to *automatically update* when their children expressions change.

## Installation

```bash
dotnet add package RxdtLabs.Formuthorpe
```

```cs
using Formuthorpe;
```

Everything is generic over `INumber<T>`, so it works out of the box with `int`, `long`, `float`, `double`, `decimal`, and any other numeric type implementing .NET's `INumber<T>`.

## Assignment becomes Equality

What does reactive programming mean? Let's consider with a small example:

```cs
int x = 0;
int y = 0;
int z = x + y;

Console.WriteLine(x); // 0
Console.WriteLine(y); // 0
Console.WriteLine(z); // 0

x = 1;
y = 1;

Console.WriteLine(x); // 1
Console.WriteLine(y); // 1
Console.WriteLine(z); // 0 (z never updated to 2 even though x and y did!)
```

Using Formuthorpe this *assignment* becomes *equality*:

```cs
Term<int> x = new(0);
Term<int> y = new(0);
Formula<int> z = x + y;

Console.WriteLine(x.Value); // 0
Console.WriteLine(y.Value); // 0
Console.WriteLine(z.Value); // 0 (so far same as before!)

x.SetValue(1);
y.SetValue(1);

Console.WriteLine(x.Value); // 1
Console.WriteLine(y.Value); // 1
Console.WriteLine(z.Value); // 2 (z changed to reflect the fact that the sub-terms x and y changed!)
```

## Combining with Event Listeners

We can, in fact, do some really useful stuff with `Formula` and `Term` objects, but we're missing one secret ingredient: event listeners. Event listeners wait for something (an event!) to happen and then they execute when that event occurs. Suppose we are making a game and there is a variable `player_health`. When `player_health` decreases we want to say output the player took damage. We can do this easily in Formuthorpe:

```cs
Term<int> player_health = new(3);
player_health.OnChange(() => Console.WriteLine("Player took damage!"));

// ...

player_health.SetValue(player_health.Value - 1); // As soon as this finishes "Player took damage!" is printed, you don't worry about it!
```

Callbacks fire only when the value *actually* changes (setting a term back to its current value is a no-op), and they run only after the whole graph has settled, so they always observe consistent values.

## API Cheat Sheet

| Member | What it does |
|---|---|
| `new Term<T>(value)` | Creates a reactive value you can change with `SetValue` |
| `term.SetValue(value)` | Updates the term; every dependent formula updates immediately |
| `formula.Value` | The current value of any term or formula; always up to date |
| `a + b`, `a - b`, `a * b`, `a / b`, `a % b`, `-a` | Builds a reactive formula from terms, formulas, or plain numbers |
| `formula.OnChange(action)` | Runs `action` whenever this term/formula's value actually changes |
| `(T)formula` | Explicitly extracts the current value as a plain `T` |
| `Formula<T>.Abelian(a, b, op)` | Builds a formula over a custom Abelian group operation (see below) |

Two things worth internalizing early:

- **Terms are the only mutable part.** `Term.SetValue` is the only way the world changes; formulas are purely computed, and each of them stays *equal* to its expression forever.
- **Formulas are a graph.** Formulas can share terms, feed other formulas, and form diamond shapes — every dependent recomputes exactly once per change, in dependency order, so no formula ever observes a half-updated state.

## Advanced: Custom Operations with Abelian Groups

Arithmetic operators cover a lot, but you can register your own operation as an Abelian group (associative, commutative, with an identity and inverses) via `Formula<T>.Abelian`. Formulas built this way update *incrementally*: when an operand moves from `xOld` to `xNew`, the value is patched with `value ∘ inverse(xOld) ∘ xNew` instead of being recomputed from scratch. The `+` operator already does this for free behind the scenes (addition forms an Abelian group over every `INumber<T>`).

```cs
// XOR: every element is its own inverse, identity is 0.
var xor = new AbelianOp<int>((x, y) => x ^ y, inverse: x => x, identity: 0);

Term<int> a = new(5);
Term<int> b = new(2);
Formula<int> c = Formula<int>.Abelian(a, b, xor);

Console.WriteLine(c.Value); // 7

a.SetValue(3);

Console.WriteLine(c.Value); // 1
```

The group laws are the caller's responsibility — if they don't hold, the incremental shortcut will silently produce wrong values. For floating point types the patched result is mathematically equal to the recomputed one but may differ in the last ulps due to rounding.

## Documentation

- [Tutorial](docs/Tutorial.md) — a step-by-step walkthrough of the whole library, from your first reactive expression to custom Abelian groups, ending in a complete runnable program.

## Status

Formuthorpe is young (v0.1.0) and deliberately tiny: two node types, one callback, some operators. It is not thread-safe — use it from a single thread or bring your own locking around `SetValue`/`OnChange`.
