---
description: "Use when creating or modifying SpacetimeDB server module files. Enforces modular structure with feature-based organization."
applyTo: "spacetimedb/**/*.cs"
---
# SpacetimeDB Module Structure

## Organization Pattern

- **Base.cs**: Core constants, shared utilities, and fundamental game configuration
- **Feature files**: One file per game entity/feature (Player.cs, Ball.cs, etc.)
- All files are `partial class Module`

## Rules

1. **Create new features as separate files** — don't add unrelated functionality to existing files
2. **Keep features self-contained** — each file should have its own tables and reducers for that theme
3. **Use Base.cs for shared state** — game tick rates, constants used across features
4. **Name files after the primary entity** — Player.cs for Player table, Ball.cs for Ball table

## Example Structure

```csharp
// Base.cs
public static partial class Module
{
    private static readonly TimeDuration _gametick = TimeDuration.FromMilliseconds(10);
}

// Player.cs
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Player", Public = true)]
    public partial struct Player { /* ... */ }
    
    [SpacetimeDB.Reducer]
    public static void JoinGame(ReducerContext ctx, string playerName) { /* ... */ }
    
    [SpacetimeDB.Reducer]
    public static void MovePlayer(ReducerContext ctx, float angle) { /* ... */ }
}
```

When adding a new game feature, create a new .cs file in spacetimedb/ following this pattern.
