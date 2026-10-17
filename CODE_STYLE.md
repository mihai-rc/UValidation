# Code Style

This file defines shared coding conventions for C# projects. It deliberately separates ordinary .NET guidance from Unity-specific adaptations so the first part can be reused on its own outside Unity.

Repository layout, architecture, package choices, and project workflows belong in the repository's `AGENTS.md`.

## Part I: Plain C# Guidelines

This part is the baseline for ordinary C# libraries and applications.

### Precedence

Apply guidance in this order:

1. The project's `.editorconfig` for mechanical formatting.
2. Project-specific instructions in `AGENTS.md`.
3. The shared conventions in this file.
4. The established style of the file being edited when no written rule applies.

Do not reformat unrelated code while making a focused change.

### General Principles

- Design architecture around clear responsibilities, boundaries, ownership, and expected variation.
- Introduce abstractions when they clarify a responsibility, protect an invariant, isolate a dependency, or support expected variation.
- Keep APIs small and explicit. Add complexity when a demonstrated use case requires it.
- Use guard clauses to reject invalid state early and keep the main flow readable.
- Keep methods focused on one responsibility. Extract a helper when it gives a concept a useful name or removes meaningful duplication.
- Preserve useful failure context. Exceptions and log messages should identify the operation and relevant object, member, or path.
- Follow the surrounding code when two forms are equally clear.

### Files, Namespaces, and Using Directives

- Match the file name to its primary type.
- Keep one primary type per file. Closely related variants, such as generic and non-generic forms of the same API, may share a file.
- Prefer the inline, file-scoped namespace form when it is available.
- Use a block-scoped namespace when the project's language version does not support file-scoped namespaces or when a file must contain multiple namespace declarations.
- Put `using` directives outside the namespace.
- Order `using` directives as follows: `System`, third-party packages, shared framework code, then application code.
- Do not add blank lines between `using` groups.
- Remove unused `using` directives.

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Framework.Controllers;
using Application.Events;

namespace Application.Processing;

// Types.
```

### Naming

| Symbol | Convention | Example |
|---|---|---|
| Class, struct, or record | PascalCase | `ItemQueue`, `ValidationReport` |
| Interface | `I` + PascalCase | `IEventBus`, `IValidatable` |
| Enum type and values | PascalCase; use `None` for the zero value when applicable | `ItemType.None` |
| Generic type parameter | `T` or `T` + descriptive PascalCase | `T`, `TView`, `TContext` |
| Property | PascalCase | `IsAlive`, `CancellationToken` |
| Public or internal field | PascalCase | `public readonly TView View` |
| Private or protected field | `m_` + PascalCase | `m_EventBus`, `m_ContextRef` |
| Public or internal constant | ALL_CAPS with underscores | `MAX_CAPACITY` |
| Private or protected constant | `k_` + PascalCase | `k_InvalidCoordinates` |
| Private or protected static field | `s_` + PascalCase | `s_EmptyResult` |
| Method | PascalCase | `Execute`, `GetNeighbor` |
| Async method | PascalCase + `Async` | `InitializeAsync`, `ProcessAsync` |
| Event handler | `Handle` + event or action | `HandleItemAdded` |
| Reactive property | PascalCase + `Rx` | `HealthRx`, `IsReadyRx` |
| Parameter or local variable | camelCase | `amount`, `nextItem` |

- Choose names that describe domain intent rather than implementation mechanics.
- Avoid abbreviations unless the abbreviation is established in the project or domain.
- Boolean names should read as predicates, usually with `Is`, `Has`, `Can`, or `Should`.

### Constants and Literals

- Keep obvious literals such as `0`, `1`, or `2` inline when their purpose cannot reasonably be misread.
- Extract numbers and strings whose meaning is unclear, lengthy, or repeated into named constants.
- Name a constant after its purpose rather than its value, including the unit when useful.
- Keep a constant `private` in the class that uses it when it is not shared.
- Place values used by multiple classes in a dedicated static constants class for the relevant feature or domain, using the narrowest useful visibility.
- Use `public const` only for stable compile-time values; otherwise use `public static readonly`.

### Access and State

- Default to `private`; widen access only when another type needs it.
- Use `internal` for APIs intended only for the current assembly.
- Use `protected virtual` for optional extension points.
- Use `protected abstract` for required extension points.
- Avoid `public virtual` unless overriding the member is part of the public contract.
- Make fields `readonly` when they are assigned during construction and should not be replaced later.
- Expose mutable state through a deliberate API rather than a public mutable field.
- Use properties for state that callers need to read. Keep setters as narrow as the design allows.
- Prefer constructor injection for required dependencies in ordinary C# classes.

### Class Anatomy

Organize a class in the following order:

1. Constants.
2. Public or internal static members, ordered as fields, properties, then methods.
3. Remaining fields.
4. Remaining properties.
5. Constructors.
6. Protected or internal inherited overrides and framework lifecycle hooks, such as `OnInit`.
7. Remaining methods.

- Within each category, normally order members by access: `public`, `internal`, `protected`, then `private`.
- Keep protected and private static members in their field, property, or method category rather than promoting them above instance members.
- In Unity classes, place Unity event functions at the beginning of the method section, before the remaining methods.

### Formatting and Language Features

- Use four spaces for C# indentation. Do not use tabs.
- Use Allman braces, with each opening brace on its own line.
- Use braces for control-flow bodies, including single-line bodies.
- Keep lines at or below 120 characters where practical.
- Use C# keywords for built-in types: `int`, `string`, `bool`, and so on.
- Do not qualify members with `this.` unless it resolves an ambiguity.
- Use an explicit type for built-in values. Use `var` when the type is apparent from the right-hand side or spelling it adds no useful information.
- Prefer object and collection initializers, target-typed `new`, pattern matching, null propagation, and null coalescing when they make the code shorter without hiding behavior.
- Use expression-bodied properties and methods for a single clear expression. Use a block when validation, branching, cleanup, or multiple operations are involved.
- Prefer a `switch` expression for direct value mapping.
- Align adjacent `switch` arms or short assignments only when it improves scanning and does not create excessive spacing.
- Keep intentional blank lines between logical steps; avoid multiple consecutive blank lines.
- Do not leave trailing whitespace.

```csharp
var direction = maxDot switch
{
    _ when Approximately(maxDot, upDot)   => Direction.Up,
    _ when Approximately(maxDot, downDot) => Direction.Down,
    _ when Approximately(maxDot, leftDot) => Direction.Left,
    _                                     => Direction.Right
};
```

### Null Semantics

- Use `is null` and `is not null` for ordinary managed references when operator overloads should not participate.
- Use `string.IsNullOrEmpty` when both null and empty strings are invalid.
- Use `string.IsNullOrWhiteSpace` only when whitespace is also invalid by contract.
- Use nullable reference annotations when the project enables them, and make nullability match the public contract.

### Control Flow and Errors

- Validate required dependencies and arguments at the boundary where they become necessary.
- Use guard clauses instead of wrapping the successful path in deep nesting.
- Throw when a caller violates a contract and execution cannot continue safely.
- Do not catch an exception merely to ignore it. Catch it when adding context, translating it into the API's failure model, or guaranteeing cleanup.
- Include the affected object, member, path, or operation in diagnostics when available.
- Use `try`/`finally`, `using`, or `using var` for deterministic cleanup.

### Async Code

- Use `Task` as the ordinary return type for asynchronous .NET operations.
- Use `ValueTask` when the API or a measured allocation concern justifies its additional constraints.
- Do not mix async abstractions within one layer without a boundary or concrete benefit.
- Suffix asynchronous methods with `Async`.
- Do not use `async void` for application or library operations. A framework-mandated event handler is the narrow exception, and it must contain its own failure handling.
- Accept `CancellationToken cancellation = default` for cancellable public operations and pass it through every cancellable call.
- Return an existing awaitable directly when no follow-up work or exception boundary requires `await`.
- When `await` is required, make the completion condition clear in the XML `<returns>` documentation.
- Observe failures from every operation, including deliberate fire-and-forget work.

```csharp
public Task SaveAsync(Item item, CancellationToken cancellation = default)
{
    return m_Repository.SaveAsync(item, cancellation);
}
```

### Documentation and Comments

- Add XML documentation to every non-private type and member.
- Public test methods required by the test framework are exempt unless they form a reusable test API.
- Document every public, protected, and internal constructor, including a `<param>` tag for each argument.
- Put the full contract on interface members. Implementations use `/// <inheritdoc />` instead of repeating it.
- Write a `<returns>` tag for every async member. Describe what completion means or what value the awaited operation produces.
- Write public documentation in terms of observable behavior. Keep implementation details out of public contracts.
- Protected and internal documentation may explain lifecycle, ordering, or invariants when consumers need that information.
- Keep `<summary>` blocks on separate lines; do not inline the opening tag, text, and closing tag.
- Write `<param>`, `<returns>`, `<typeparam>`, and similar tags on one line when they fit.
- Put one space inside inline documentation tags: `<param name="value"> The value to use. </param>`.
- End every documentation phrase with a period.
- Put corner cases and usage details in `<remarks>` rather than expanding the summary.
- Use inline comments to explain non-obvious reasons, invariants, or workarounds. Do not narrate what the code already states.
- Do not use block comments in source. Use `//` for a short note and XML documentation for API contracts.

```csharp
/// <summary>
/// Saves the item to the configured repository.
/// </summary>
/// <param name="item"> Item to save. </param>
/// <param name="cancellation"> Token used to cancel the operation. </param>
/// <returns> Completes when the item has been saved. </returns>
public Task SaveAsync(Item item, CancellationToken cancellation = default)
```

### Tests

- Name tests `MemberOrFeature_Scenario_ExpectedResult`.
- Test observable behavior and regressions. Avoid tests that only reproduce an implementation line by line.
- Structure tests using Arrange, Act, Assert (AAA), with the three phases visually separated when the test has multiple steps.
- Give test fixtures the smallest amount of state needed to express the scenario.
- Clean up temporary resources even when an assertion fails. Use teardown hooks or `try`/`finally` as appropriate.

### Repository Hygiene

- Keep changes scoped to the requested behavior.
- Do not combine a behavioral change with unrelated renaming or formatting.
- Remove dead code and stale comments made obsolete by the change.
- Preserve existing public behavior unless the change intentionally revises the contract.
- Run the checks appropriate to the changed layer.

## Part II: Unity-Specific Guidelines

Unity code follows Part I unless a rule below adapts it to Unity's serialization, lifecycle, object model, editor boundaries, or performance characteristics. These adaptations are deliberate framework integration rather than exceptions to code quality.

### Unity Types and Lifecycle

- Treat `MonoBehaviour`, `ScriptableObject`, and other Unity-managed types as framework entry points.
- Do not use constructors to initialize Unity-managed objects. Use serialized state, Unity callbacks, factories, or explicit initialization methods as appropriate.
- Keep Unity callback names and signatures exactly as Unity defines them, including `Awake`, `OnEnable`, `Start`, `Update`, and `OnDisable`.
- Keep lifecycle callbacks focused on wiring and delegation. Put reusable domain behavior in ordinary C# methods or classes.
- Prefer ordinary C# classes with constructor-injected dependencies for logic that does not need Unity serialization or lifecycle participation.
- Keep Unity API access on the main thread unless the API explicitly supports another thread.

### Unity Using Directives

Insert Unity namespaces after `System` and before third-party packages:

```csharp
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;
using Framework.Controllers;
using Gameplay.Events;
```

### Serialization and Inspector Fields

- Prefer a private or protected `[SerializeField]` field when the value is configured in the Inspector and does not need to be exposed.
- Use `[field: SerializeField]` on an auto-property only when other code also needs to read the value.
- Avoid public mutable fields solely for Inspector visibility.
- Keep validation attributes beside `[SerializeField]`.
- Apply validation attributes only to fields Unity serializes.
- Keep attributes and the field on one line when the declaration remains easy to read. Move the attributes to the preceding line when the declaration becomes long.
- Keep runtime-only caches and transient state nonserialized.

```csharp
[SerializeField, NotNull] private GridLayoutGroup m_GridLayout;

[SerializeField, NotEmpty, HasNoNulls]
private MatchCell[] m_Cells;

[field: SerializeField]
public string Id { get; private set; }
```

### Unity Null Semantics

- Use `== null` and `!= null` for values whose compile-time type derives from `UnityEngine.Object`; Unity's overloaded equality detects destroyed objects.
- When an `object`, interface, or generic value may contain a `UnityEngine.Object`, handle both CLR null and Unity's destroyed-object state explicitly.
- Do not replace a Unity-object null check with `is null` unless destroyed objects should remain distinguishable from CLR null.

```csharp
private static bool IsNullReference(object value)
{
    if (value is null)
    {
        return true;
    }

    return value is UnityEngine.Object unityObject && unityObject == null;
}
```

### Unity Performance

- Avoid recurring allocations in `Update`, per-frame callbacks, animation loops, and other measured hot paths.
- Cache stable component and asset references instead of repeatedly searching for them in a hot path.
- Avoid LINQ and allocation-heavy formatting inside hot loops when a direct loop is equally clear.
- Use pooling when object lifetime or profiling data justifies it. Do not introduce ownership complexity to optimize cold setup, editor tooling, or uncommon failure paths.
- Prefer readable code outside hot paths until profiling shows a meaningful cost.
- Keep the successful runtime path inexpensive when validation or diagnostics are expected to fail rarely.

### Async Code in Unity

- Prefer `UniTask` for Unity gameplay operations when the project uses UniTask.
- Keep `Task` or `ValueTask` in engine-independent layers when that preserves a useful .NET API boundary.
- Pass cancellation through every cancellable operation and connect long-running work to the owning object's lifetime.
- Use `UniTaskVoid` only for deliberate fire-and-forget entry points. Route exceptions explicitly or use the project's established `.Forget()` handling.
- Do not make Unity lifecycle callbacks `async void`. Start a named async operation from the callback and handle its lifetime and failures explicitly.
- Return a `UniTask` directly when no follow-up work requires `await`.

```csharp
public UniTask EquipAsync(AccessoryData data, CancellationToken cancellation = default)
{
    return m_View.PlayEquipAnimation(data, cancellation);
}
```

### Runtime and Editor Boundaries

- Keep `UnityEditor` APIs out of runtime assemblies.
- Put editor-only code in an `Editor` folder or an editor-only assembly definition.
- Use `#if UNITY_EDITOR` only when a clean assembly or folder boundary is impractical.
- Keep editor validation and tooling failures descriptive; include the asset, scene, object, component, or serialized field involved.
- For reusable packages, keep runtime, editor, and test assemblies separate so consumers compile only what they need.

### Unity Tests

- Clean up temporary `GameObject` instances, assets, prefabs, and scenes even when an assertion fails.
- Register expected Unity log messages before triggering them, and assert that no unexpected messages remain when logging is part of the contract.
- Cover Unity-specific edge cases when the feature depends on them: destroyed-object null semantics, serialization boundaries, inheritance, prefab contents, sub-assets, and unloaded scenes.
- Use Edit Mode tests for editor and serialization behavior. Use Play Mode tests when behavior depends on the player lifecycle or frame progression.

### Unity Repository Hygiene

- Preserve `.meta` files when moving or renaming Unity assets. Remove the matching `.meta` file when intentionally deleting an asset.
- After changing C# scripts, let Unity recompile and check the Console for new errors before considering the change complete.
- Run the relevant Edit Mode or Play Mode tests for behavioral changes.
