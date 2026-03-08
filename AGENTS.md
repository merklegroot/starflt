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
- Remove redundant initialization from constructors when fields are initialized inline
