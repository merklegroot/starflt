---
description: Coding conventions and standards for this codebase
globs: ["**/*.cs"]
alwaysApply: true
---

# Agent Coding Guidelines

This document outlines C# coding conventions that must be followed when working on this codebase.

## C# Field Conventions

### 1. Private Field Naming

**Private fields must use an underscore prefix.**

✅ **Correct:**
```csharp
private List<StarSystem> _systems;
private Vector2 _cameraOffset;
private int _count;
```

❌ **Incorrect:**
```csharp
private List<StarSystem> systems;
private Vector2 cameraOffset;
private int count;
```

This convention helps distinguish private fields from local variables and parameters, improving code readability.

### 2. Field Initialization

**Fields should be initialized inline when possible.**

✅ **Correct:**
```csharp
private List<StarSystem> _systems = new List<StarSystem>();
private Vector2 _cameraOffset = Vector2.Zero;
private int _count = 0;
```

❌ **Incorrect:**
```csharp
private List<StarSystem> _systems;

public MyClass()
{
    _systems = new List<StarSystem>();
}
```

Inline initialization reduces constructor complexity and ensures fields are always initialized, even if constructors are chained or overloaded.

### 3. Complex Field Initialization

**For complex initialization logic, use a static helper method called inline rather than instance methods.**

✅ **Correct:**
```csharp
private List<StarSystem> _systems = InitializeSystems();

private static List<StarSystem> InitializeSystems()
{
    var systems = new List<StarSystem>
    {
        new StarSystem("Sol", new Vector2(0, 0), Color.YELLOW),
        new StarSystem("Alpha Centauri", new Vector2(200, 150), Color.WHITE)
    };
    // ... additional initialization logic ...
    return systems;
}
```

❌ **Incorrect:**
```csharp
private List<StarSystem> _systems = new List<StarSystem>();

public MyClass()
{
    InitializeSystems();
}

private void InitializeSystems()
{
    _systems.Add(new StarSystem("Sol", new Vector2(0, 0), Color.YELLOW));
    // ... initialization logic ...
}
```

Using static helper methods for complex initialization keeps constructors simple and ensures initialization happens at field declaration time, making the code more predictable and easier to understand.

### 4. Blank Lines After Closing Braces

**Add a blank line after closing braces when followed by another statement, method, or class at the same level.**

✅ **Correct:**
```csharp
public void Method1()
{
    // code
}

public void Method2()
{
    // code
}

if (condition)
{
    // code
}

DoSomething();
```

❌ **Incorrect:**
```csharp
public void Method1()
{
    // code
}
public void Method2()
{
    // code
}

if (condition)
{
    // code
}
DoSomething();
```

**Exceptions - No blank line needed:**
- When a closing brace is immediately followed by `else`, `catch`, or `finally`: `} else {`
- When closing braces are nested: `} }`
- At the end of a file
- When the closing brace is part of a collection initializer or object initializer

✅ **Correct exceptions:**
```csharp
if (condition)
{
    // code
}
else
{
    // code
}

try
{
    // code
}
catch (Exception ex)
{
    // code
}

var list = new List<int> { 1, 2, 3 };
```

This convention improves readability by creating visual separation between logical blocks of code.

### Combined Example

When both conventions are applied together:

✅ **Correct:**
```csharp
private List<StarSystem> _systems = new List<StarSystem>();
```

❌ **Incorrect:**
```csharp
private List<StarSystem> systems;

public MyClass()
{
    systems = new List<StarSystem>();
}
```

## Summary

- Use underscore prefix (`_fieldName`) for all private fields
- Initialize fields inline when possible (`= new Type()` or `= value`)
- For complex initialization, use static helper methods called inline rather than instance methods in constructors
- Remove redundant initialization from constructors when fields are initialized inline
- Add a blank line after closing braces when followed by another statement, method, or class (except before `else`, `catch`, `finally`, or nested closing braces)