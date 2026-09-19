# Formuthorpe Tutorial

Formuthorpe is a radically simple reactive programming library for C#. The
big idea: **assignment becomes equality**. When you write `z = x + y`, `z`
isn't a snapshot of `x + y` at that moment — it *is* `x + y`, forever, always
up to date, even as `x` and `y` change.

This tutorial walks through everything the library can do, one small step at
a time. Every snippet is a complete thought, and the full program is listed
at the end.

- [What you need](#what-you-need)
- [1. Assignment becomes equality](#1-assignment-becomes-equality)
- [2. Every operator works](#2-every-operator-works)
- [3. Mixing formulas and plain numbers](#3-mixing-formulas-and-plain-numbers)
- [4. Formulas are a graph](#4-formulas-are-a-graph)
- [5. Listening for changes](#5-listening-for-changes)
- [6. A worked example: character damage](#6-a-worked-example-character-damage)
- [7. Custom operations with Abelian groups](#7-custom-operations-with-abelian-groups)
- [8. Things to know](#8-things-to-know)
- [9. Complete program](#9-complete-program)

## What you need

Formuthorpe targets modern .NET and ships as a NuGet package:

```bash
dotnet add package RxdtLabs.Formuthorpe
```

Then bring the namespace into scope:

```cs
using Formuthorpe;
```

Everything in Formuthorpe is generic over `INumber<T>`, so it works out of
the box with `int`, `long`, `float`, `double`, `decimal`, and any other
numeric type that implements .NET's `INumber<T>` interface.

## 1. Assignment becomes equality

Here is the problem Formuthorpe solves. In plain C#, `z` is copied once and
then forgotten:

```cs
int x = 0;
int y = 0;
int z = x + y;

x = 1;
y = 1;

Console.WriteLine(z); // 0 — z never learned that x and y changed!
```

In Formuthorpe you declare *terms* (values you set) and *formulas* (values
that are computed). A formula stays equal to its expression at all times:

```cs
Term<int> x = new(0);
Term<int> y = new(0);
Formula<int> z = x + y;

Console.WriteLine(z.Value); // 0

x.SetValue(1);
y.SetValue(1);

Console.WriteLine(z.Value); // 2 — always up to date
```

Two vocabulary words and the rest of the library follows:

- A **`Term<T>`** is a leaf that *holds* a value. You change it with
  `SetValue`. This is the only way anything ever changes.
- A **`Formula<T>`** is anything *computed* from other formulas, like the
  `x + y` above. You never set it; you read it through `.Value`.

Updates are eager: the instant `SetValue` returns, every formula that
depends (directly or indirectly) on that term already reflects the new
value. There is no "refresh" step and no polling.

## 2. Every operator works

All of the arithmetic operators you'd expect build reactive formulas:

| Operator  | Meaning          |
|-----------|------------------|
| `a + b`   | addition         |
| `a - b`   | subtraction      |
| `a * b`   | multiplication   |
| `a / b`   | division         |
| `a % b`   | remainder        |
| `-a`      | negation (unary) |

```cs
Term<int> a = new(10);
Term<int> b = new(3);

Formula<int> sum = a + b;        // 13
Formula<int> difference = a - b; // 7
Formula<int> product = a * b;    // 30
Formula<int> quotient = a / b;   // 3
Formula<int> remainder = a % b;  // 1
Formula<int> negated = -a;       // -10

a.SetValue(20);

// Every one of them is still "equal to" its expression:
Console.WriteLine($"{sum.Value} {difference.Value} {product.Value} "
    + $"{quotient.Value} {remainder.Value} {negated.Value}");
// 23 17 60 6 2 -20
```

## 3. Mixing formulas and plain numbers

Plain numbers are welcome anywhere a formula is expected — they are
implicitly wrapped in a constant term for you:

```cs
Term<double> celsius = new(100.0);
Formula<double> fahrenheit = celsius * 9 / 5 + 32;

Console.WriteLine(fahrenheit.Value); // 212

celsius.SetValue(37.0);

Console.WriteLine(fahrenheit.Value); // 98.6
```

Note that `celsius * 9 / 5 + 32` builds a chain of formula nodes:
`celsius` feeds a `* 9`, which feeds a `/ 5`, which feeds a `+ 32`. A change
to `celsius` ripples through the whole chain immediately.

You can also go the other direction and pull the current value back out as
a plain number with an explicit cast:

```cs
double rightNow = (double)fahrenheit;
```

This is handy at the boundary of the reactive world — feeding `.Value`s
into APIs that expect ordinary numbers, writing to a log, and so on.

## 4. Formulas are a graph

Nothing stops several formulas from sharing terms — or sharing each other.
Formuthorpe keeps the whole graph consistent. Here each formula depends on
the same two terms:

```cs
Term<double> width = new(3.0);
Term<double> height = new(4.0);

Formula<double> area = width * height;
Formula<double> perimeter = 2 * (width + height);

Console.WriteLine($"{area.Value} {perimeter.Value}"); // 12 14

width.SetValue(6.0);

Console.WriteLine($"{area.Value} {perimeter.Value}"); // 24 20
```

Formulas can also feed other formulas, arbitrarily deep:

```cs
Term<double> radius = new(1.0);
Formula<double> circumference = 2 * 3.14159 * radius;
Formula<double> arc = circumference / 4;

Console.WriteLine(arc.Value); // 1.570795

radius.SetValue(2.0);

Console.WriteLine(arc.Value); // 3.14159
```

Even when the graph forms a *diamond* — one term reaching the same formula
through multiple paths — each formula recomputes exactly once per change,
in dependency order, and no formula ever observes a half-updated state.

## 5. Listening for changes

Reading `.Value` whenever you feel like it is fine, but often you want to
*react*: log something, update a UI, trigger a sound. `OnChange` registers
a callback that runs whenever a term's or formula's value changes:

```cs
Term<double> hours = new(0);
Formula<double> pay = hours * 15;
pay.OnChange(() => Console.WriteLine($"Pay is now {pay.Value}"));

hours.SetValue(8); // Pay is now 120
```

Three properties make `OnChange` pleasant to reason about:

**It fires only on real changes.** If an update leaves the value alone, no
callback runs. In `total = quantity * outOfStockMultiplier` with the
multiplier at `0`, changing `quantity` updates the term but never fires
`total`'s callbacks — the value didn't change:

```cs
Term<int> quantity = new(5);
Term<int> zero = new(0);
Formula<int> total = quantity * zero;

int fired = 0;
total.OnChange(() => fired++);

quantity.SetValue(10);

Console.WriteLine(fired); // 0 — total stayed 0
```

The same applies to terms: `SetValue` with the value it already holds is a
complete no-op, and nothing fires anywhere.

**Callbacks see a settled graph.** All values are updated first; callbacks
run only after the whole update has finished. If you inspect other formulas
from inside a callback, they're already current:

```cs
Term<int> a = new(2);
Formula<int> c = a * 2;
Formula<int> d = c * 2;

int observed = 0;
c.OnChange(() => observed = d.Value);

a.SetValue(5);

Console.WriteLine(observed); // 20 — d was already updated
```

**You can register as many callbacks as you like**, on terms and formulas
alike, and callbacks never fire at registration time — only on actual
changes afterwards.

## 6. A worked example: character damage

Let's put it together into something you might actually write — a tiny
RPG damage model:

```cs
Term<int> baseDamage = new(4);
Term<int> strength = new(8);
Term<int> weaponBonus = new(6);

Formula<int> attackPower = baseDamage + strength / 2 + weaponBonus;
Formula<int> criticalHit = attackPower * 2;

attackPower.OnChange(() =>
    Console.WriteLine($"Attack power is now {attackPower.Value}"));
criticalHit.OnChange(() =>
    Console.WriteLine($"A critical hit deals {criticalHit.Value}"));

Console.WriteLine($"{attackPower.Value} / {criticalHit.Value}"); // 14 / 28

strength.SetValue(12);
// Attack power is now 16
// A critical hit deals 32
```

One `SetValue` on `strength` updated `attackPower`, which updated
`criticalHit`, and each formula that changed announced it — exactly once,
in order, with no bookkeeping on your part. This is the promise of the
library: you describe *what things are equal to*, and the "when does it
change" question disappears.

## 7. Custom operations with Abelian groups

The built-in operators cover arithmetic, but what if your formula combines
values with some other operation? If your operation forms an **Abelian
group** — it's associative, commutative, has an identity element, and every
element has an inverse — you can register it with `Formula.Abelian`.

Why bother? Because such formulas update *incrementally*. When an operand
moves from `xOld` to `xNew`, the value is patched with
`value ∘ inverse(xOld) ∘ xNew` instead of being recomputed from scratch.
For a two-term formula that's a wash, but deep chains of the same operation
get cheaper updates.

The canonical non-arithmetic example is XOR over integers: every element is
its own inverse (identity is `0`).

```cs
var xor = new AbelianOp<int>(
    combine: (x, y) => x ^ y,
    inverse: x => x,     // x ^ x = 0, so XOR is its own inverse
    identity: 0);

Term<int> a = new(5);
Term<int> b = new(2);
Formula<int> c = Formula<int>.Abelian(a, b, xor);

Console.WriteLine(c.Value); // 7

a.SetValue(3);

Console.WriteLine(c.Value); // 1 (3 ^ 2)
```

You already use one of these for free: `a + b` is registered as the
*additive group* (`combine: +`, `inverse: -x`, `identity: 0`) behind the
scenes, which is what makes sums so cheap to update.

One honest caveat: the group laws are *your* responsibility. If `combine`,
`inverse`, or `identity` don't actually form an Abelian group, the
incremental shortcut will silently produce wrong values. And with floating
point types the patched result is mathematically equal to the recomputed
one but can differ in the last ulps due to rounding — for exact values over
`double`, prefer a multiplicative group of exact-friendly values or plain
operators.

## 8. Things to know

A few facts that will save you a head-scratch:

- **Terms are the only mutable part.** Change the world through
  `Term.SetValue`. There is deliberately no way to "set" a formula.
- **Watch out for re-binding.** If your variable is typed `Formula<int>`,
  an innocent-looking `f = f - 1` compiles — but it doesn't update
  anything. It builds a brand-new formula node `f - 1` and points `f` at
  it, leaving your original graph untouched. Old `OnChange` registrations
  won't follow. If you meant "change the underlying value", that's
  `term.SetValue(term.Value - 1)` on a `Term`.
- **Constants via implicit conversion.** `Formula<int> f = 5;` wraps `5`
  in an anonymous term. Nothing can ever change it — great for constants,
  useless for state.
- **Callbacks are plain `Action`s**, fire only on real changes, only after
  the graph settles, and never at registration.
- **Not thread-safe.** Like `List<T>`, Formuthorpe has no internal
  synchronization. Use it from one thread, or put a lock around
  `SetValue`/`OnChange` yourself.
- **`Update()` exists but you rarely need it.** Formulas recompute eagerly
  and automatically; `someFormula.Update()` just forces the reachable part
  of the graph to recompute, which is occasionally handy in tests.

## 9. Complete program

Everything from this tutorial in one runnable file:

```cs
using Formuthorpe;

// 1. Assignment becomes equality
Term<int> x = new(0);
Term<int> y = new(0);
Formula<int> z = x + y;
x.SetValue(1);
y.SetValue(1);
Console.WriteLine($"z = {z.Value}"); // z = 2

// 2. Every operator
Term<int> a = new(10);
Term<int> b = new(3);
Formula<int> negated = -a;
Console.WriteLine($"{(a + b).Value} {(a * b).Value} {negated.Value}"); // 13 30 -10
a.SetValue(20);
Console.WriteLine($"{(a + b).Value} {(a * b).Value} {negated.Value}"); // 23 60 -20

// 3. Mixing raw numbers
Term<double> celsius = new(100.0);
Formula<double> fahrenheit = celsius * 9 / 5 + 32;
celsius.SetValue(37.0);
Console.WriteLine($"{fahrenheit.Value}°F"); // 98.6°F

// 4. A shared graph
Term<double> radius = new(2.0);
Formula<double> circumference = 2 * 3.14159 * radius;
Formula<double> arc = circumference / 4;
Console.WriteLine($"arc = {arc.Value}"); // arc = 3.14159

// 5. Reacting to change
Term<double> hours = new(0);
Formula<double> pay = hours * 15;
pay.OnChange(() => Console.WriteLine($"Pay is now {pay.Value}"));
hours.SetValue(8); // Pay is now 120

// 6. Character damage
Term<int> baseDamage = new(4);
Term<int> strength = new(8);
Term<int> weaponBonus = new(6);
Formula<int> attackPower = baseDamage + strength / 2 + weaponBonus;
Formula<int> criticalHit = attackPower * 2;
strength.SetValue(12);
Console.WriteLine($"{attackPower.Value} / {criticalHit.Value}"); // 16 / 32

// 7. A custom Abelian group (XOR)
var xor = new AbelianOp<int>((x, y) => x ^ y, v => v, 0);
Term<int> p = new(5);
Term<int> q = new(2);
Formula<int> masked = Formula<int>.Abelian(p, q, xor);
p.SetValue(3);
Console.WriteLine($"masked = {masked.Value}"); // masked = 1
```
