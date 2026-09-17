# Formuthorpe

Formuthorpe is a radically simple reactive programming library for C#. Reactive programming allows for variables to *automatically update* when their children expressions change. What does this mean? Let's consider with a small example:

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
Term<int> player_health = 3;
player_health.OnChange(() => Console.WriteLine("Player took damage!"));

// ...

player_health -= 1; (As soon as this finishes "Player took damage!" is printed, you don't worry about it!).
```
