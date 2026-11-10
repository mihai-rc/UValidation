# UValidation

UValidation is a Unity package for catching invalid serialized content while it is still being
authored. It adds field-level validation messages to the Inspector, marks invalid objects in the
Hierarchy and Project windows, and it can optionally block invalid saves, entering Play Mode, quitting the Editor, or building the application.

The package also includes a small fluent API for validating values that need a more complex validation scenario or do not come from Unity serialization.

The intention behind this library was historically to reduce the iteration time by making the editor aggressively notify when something is misconfigured to the point it can block the user from moving forward until the misconfiguration is addressed. As a consequence to that, by trusting a framework like this, it can enable a _negative space_ coding style allowing the code to focus less on checking and handling all the invalid states the serialized fields could be in and more on the actual logic.

## Features

- Inspector validation attributes for required references, strings, collections, and nested data.
- Immediate error messages beside the field that failed validation.
- Validation feedback in the Hierarchy and Project windows.
- Recursive validation of serializable classes, structs, collections, and `[SerializeReference]`
  values.
- Custom rules through `IValidatable`.
- Fluent runtime validation with source location and Unity object context in diagnostics.
- Optional project-wide enforcement for saves, Play Mode, Editor quit, and player builds.
- Correct handling of destroyed `UnityEngine.Object` references, inherited serialized fields,
  prefabs, scenes, ScriptableObjects, sub-assets, and missing scripts.

## Installation

### Requirements

- Unity 6.3 or newer.

### OpenUPM

Open **Edit > Project Settings > Package Manager** and add this scoped registry:

| Field | Value |
| --- | --- |
| Name | `OpenUPM` |
| URL | `https://package.openupm.com` |
| Scope | `com.hightower` |

Then open **Window > Package Manager**, select **Install package by name**, enter
`com.hightower.uvalidation`, and leave the version empty to install the latest published release.

### Git tag

To install the first release directly from GitHub, select **Install package from git URL** and enter:

```text
https://github.com/mihai-rc/UValidation.git?path=/com.hightower.uvalidation#v0.0.1
```

The equivalent `Packages/manifest.json` dependency is:

```json
{
  "dependencies": {
    "com.hightower.uvalidation": "https://github.com/mihai-rc/UValidation.git?path=/com.hightower.uvalidation#v0.0.1"
  }
}
```

### Compatibility
- The legacy Hierarchy is supported in Unity 6.3 and newer.
- The UI Toolkit Hierarchy integration uses Unity's official extension API and is available in
  Unity 6.6 and newer.

## Quick start

Add validation attributes to fields that Unity serializes:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UValidation;

public sealed class EnemyConfig : MonoBehaviour
{
    [SerializeField, NotNull] private Transform m_Target;
    [SerializeField, NotEmpty] private string m_Id;
    [SerializeField, NotEmpty, HasNoNulls] private List<GameObject> m_SpawnPoints;
    [SerializeField, HasNoEmpties] private List<string> m_Tags;
    [SerializeField, IsValid] private Stats m_Stats;

    [Serializable]
    private sealed class Stats
    {
        [SerializeField, NotEmpty] private string m_DisplayName;
    }
}
```

Invalid fields display an error in the Inspector. The owning component, GameObject, or asset also receives validation feedback in the relevant Editor window.

## Editor feedback

Validation failures appear beside the affected field in the Inspector. Invalid GameObjects have red labels in the Hierarchy. Invalid scenes and supported assets show an error badge in the Project window at any icon zoom level, making invalid content visible without opening every object.

## Attribute reference

### `[NotNull]`

Use `[NotNull]` for a serialized `UnityEngine.Object` reference that must be assigned, such as a
target `Transform`, prefab, material, or configuration asset. A missing or destroyed Unity object
fails validation.

```csharp
[SerializeField, NotNull] private Transform m_Target;
```

<img src="docs/images/not-null-inspector.png" width="411" alt="Unity Inspector showing a NotNull failure on an unassigned Transform">

### `[NotEmpty]`

Use `[NotEmpty]` for strings that must contain text or a collections that must contain at least one effective element. It supports strings, arrays, lists, and Unity 6.6+ dictionaries.

```csharp
[SerializeField, NotEmpty] private string m_Id;
[SerializeField, NotEmpty] private GameObject[] m_SpawnPoints;
[SerializeField, NotEmpty] private List<int> m_WaveIds;
[SerializeField, NotEmpty] private Dictionary<string, GameObject> m_Views;
```

**Strings, arrays, and lists:**

<img src="docs/images/not-empty-inspector.png" width="411" alt="Unity Inspector showing NotEmpty failures on empty strings and collections">

**Unity 6.6+ dictionaries:**

<img src="docs/images/dictionary-not-empty-inspector.png" width="411" alt="Unity Inspector showing expanded NotEmpty dictionary examples and ignored null-key rows">

### `[HasNoNulls]`

Use `[HasNoNulls]` when every entry in a collection of `UnityEngine.Object` references must be assigned. It supports arrays and lists of object
references, plus Unity 6.6+ dictionaries with object-reference values. Empty collections pass; combine it with `[NotEmpty]` when at least one entry is also required.

```csharp
[SerializeField, HasNoNulls] private GameObject[] m_Prefabs;
[SerializeField, HasNoNulls] private List<Transform> m_Targets;
[SerializeField, HasNoNulls] private Dictionary<string, GameObject> m_Views;
```

**Arrays and lists:**

<img src="docs/images/has-no-nulls-inspector.png" width="411" alt="Unity Inspector showing a HasNoNulls failure and the null collection element">

**Unity 6.6+ dictionary values:**

<img src="docs/images/dictionary-has-no-nulls-inspector.png" width="411" alt="Unity Inspector showing expanded HasNoNulls dictionary values including a missing object reference">

### `[HasNoEmpties]`

Use `[HasNoEmpties]` for a collection of strings where every entry must contain text, such as tags,
localization keys, or item identifiers. It supports string arrays and lists, plus Unity 6.6+
dictionaries with string values. Empty collections pass; add `[NotEmpty]` if the collection itself
must also contain an entry.

```csharp
[SerializeField, HasNoEmpties] private string[] m_Layers;
[SerializeField, HasNoEmpties] private List<string> m_Tags;
[SerializeField, HasNoEmpties] private Dictionary<int, string> m_Labels;
```

**Arrays and lists:**

<img src="docs/images/has-no-empties-inspector.png" width="411" alt="Unity Inspector showing HasNoEmpties failures and an empty string element">

**Unity 6.6+ dictionary values:**

<img src="docs/images/dictionary-has-no-empties-inspector.png" width="411" alt="Unity Inspector showing expanded HasNoEmpties dictionary values including empty and null strings">

### `[IsValid]`

Use `[IsValid]` for fields of custom serialized types whose own attributed fields and custom validation rules must pass. It supports serializable classes and structs, arrays and lists of those types, Unity 6.6+ dictionary values, and individual or collected `[SerializeReference]` values. A summary message remains visible above a collapsed invalid field so the nested failure is easy to locate.

```csharp
// Serializable class and struct
[SerializeField, IsValid] private ItemData m_Item;
[SerializeField, IsValid] private StatsData m_Stats;

// Arrays, lists, and Unity 6.6+ dictionaries of serializable values
[SerializeField, IsValid] private ItemData[] m_Items;
[SerializeField, IsValid] private List<ItemData> m_ItemList;
[SerializeField, IsValid] private Dictionary<string, ItemData> m_ItemMap;

// Individual, array, and list managed references
[SerializeReference, IsValid] private IRule m_Rule;
[SerializeReference, IsValid] private IRule[] m_RuleArray;
[SerializeReference, IsValid] private List<IRule> m_Rules;
```

**Inline nested objects and custom `IValidatable` values:**

<img src="docs/images/is-valid-inspector.png" width="411" alt="Unity Inspector showing expanded nested objects and recursive IsValid failures">

**Unity 6.6+ dictionary values:**

<img src="docs/images/dictionary-is-valid-inspector.png" width="411" alt="Unity Inspector showing expanded recursively validated dictionary values and their nested failures">

Combine collection attributes when the contract requires more than one guarantee. For example,
`[NotEmpty, HasNoNulls]` requires at least one element and rejects null elements.

### Serialized dictionaries

On Unity 6.6 and newer, validation attributes support fields declared exactly as
`Dictionary<TKey, TValue>` and serialized with `[SerializeField]`. Dictionary validation is
value-oriented: `[HasNoNulls]`, `[HasNoEmpties]`, and `[IsValid]` inspect `TValue`; keys remain
under Unity's built-in null-key and duplicate-key validation. `[NotEmpty]` counts only entries that
Unity includes in the live dictionary, so duplicate and null-key placeholder rows do not satisfy it.

```csharp
[SerializeField, NotEmpty, HasNoNulls]
private Dictionary<string, GameObject> m_Views;

[SerializeField, IsValid]
private Dictionary<string, ItemData> m_Items;
```

Unity does not serialize public dictionaries implicitly, and `[SerializeReference]` is not valid on
dictionary fields. Earlier Unity versions continue to treat dictionary fields as nonserialized.

Unsupported attribute placements produce an Inspector warning instead of silently promising a rule
that Unity cannot serialize or inspect.

## Recursive validation

Use `[IsValid]` on an inline custom serializable class or struct, or on an array or `List<T>` whose
element type is a custom serializable class or struct:

```csharp
[Serializable]
public sealed class ItemData
{
    [SerializeField, NotEmpty] private string m_Name;
}

[SerializeField, IsValid] private ItemData m_Item;
[SerializeField, IsValid] private List<ItemData> m_Items;
```

Every annotated field inside each object is validated recursively. An invalid `[IsValid]` target
shows one summary message above the outer field even while it is collapsed; expand it to see the
specific nested field messages. Collection failures include the element index, such as
`m_Items[2].m_Name`.

`[IsValid]` also supports `[SerializeReference]` fields declared as an interface, abstract class, or
`object`, including arrays and lists. Each assigned value is validated according to its runtime
type:

```csharp
[SerializeReference, IsValid] private IRule m_Rule;
[SerializeReference, IsValid] private List<IRule> m_Rules = new();
```

For a `[SerializeReference]` array or list declared with a class element type, Unity requires the
declared class to have `[Serializable]`. Interface and `object` collection declarations do not
require it, but every concrete assigned value must still be serializable.

`[IsValid]` controls traversal only. It does not require the nested value or collection to exist,
and it does not require a collection to contain elements. Add `[NotEmpty]` or another constraint
when that is part of the field's contract.

## Custom validation

Implement `IValidatable` when a type needs a rule that cannot be expressed by field attributes:

```csharp
using UnityEngine;
using UValidation;

public sealed class DamageConfig : MonoBehaviour, IValidatable
{
    [SerializeField] private int m_MinDamage;
    [SerializeField] private int m_MaxDamage;

    public void Validate(Validation validation)
    {
        validation.IsTrue(nameof(m_MinDamage), m_MinDamage <= m_MaxDamage);
    }
}
```

Custom validation is also invoked for nested values reached through `[IsValid]`. Their failures
receive the same nested path prefix as attribute failures. If an implementation throws, UValidation
records the exception as a failed diagnostic with the original stack trace.

## Runtime fluent validation

The `Validation` API can validate ordinary runtime values, including values that Unity does not
serialize:

```csharp
using var validation = new Validation(this);

validation
    .IsNotNull(nameof(m_Target), m_Target)
    .IsNotEmpty(nameof(m_Name), m_Name)
    .IsNotEmpty(nameof(m_ItemIds), m_ItemIds)
    .HasNoNulls(nameof(m_Views), m_Views)
    .HasNoEmpties(nameof(m_Tags), m_Tags)
    .IsTrue(nameof(m_MinDamage), m_MinDamage <= m_MaxDamage);

validation.Report();
```

Rules accumulate failures so one validation pass can report the complete problem set. Collection
rules are generic, destroyed Unity objects are recognized even when exposed as `object`, and
nullable value types have dedicated overloads.

Use the predicate overload when evaluation itself may throw and should be captured as a validation
failure:

```csharp
validation.IsTrue(nameof(m_Config), m_Config, config => config.CalculateValue() > 0);
```

Dispose the validation after reading `Passed` or `Failed`, reporting the result, or allowing the
`using` scope to end.

## Editor feedback and enforcement

Inspector messages and Hierarchy and Project window feedback are always active. Blocking policies
are configured separately under **Project Settings > UValidation**:

<img src="docs/images/project-settings.png" width="898" alt="Unity Project Settings showing UValidation enforcement options">

| Policy | Behavior |
| --- | --- |
| **Block Invalid Saves** | Blocks invalid scene, prefab, and ScriptableObject saves when Unity exposes the contents being written. |
| **Block Play Mode** | Prevents entering Play Mode while the active scene is invalid. |
| **Block Editor Quit** | Prevents quitting while a dirty ScriptableObject asset is invalid. |
| **Fail Player Builds** | Fails a player build when a processed scene contains invalid data. |

All enforcement policies are disabled in a fresh project. Settings are stored in
`ProjectSettings/UValidationSettings.asset`, allowing a team to commit and share them.

### Save behavior

Unity does not expose the same information for every save operation. UValidation avoids blocking a
write based on stale or unrelated data:

- A newly created scene, prefab, or ScriptableObject is allowed to complete its initial save. The
  imported asset is validated immediately afterward so it can be configured normally.
- Prefab Mode exposes the actual save root, so an invalid Prefab Mode save can be blocked before
  Unity writes it.
- Operations such as **Apply Overrides**, `PrefabUtility.SaveAsPrefabAsset`, and direct prefab-asset
  saves expose only the destination path. UValidation allows the write, then validates and
  highlights the imported prefab.
- Saving an open scene to its own path is blocked when invalid. **Save As** over a different existing
  scene is allowed and the written scene is validated after import because Unity's callback exposes
  the old destination contents rather than the scene being written.

## Showcase project

The repository includes `UValidationSandbox`, a Unity project linked to the local package source.
Open it with the Unity version recorded in
`UValidationSandbox/ProjectSettings/ProjectVersion.txt`.

Useful examples include:

- `Assets/Scenes/ValidationAttributeShowcase.unity`
- `Assets/Scenes/ValidationInheritanceShowcase.unity`
- `Assets/Showcase/ScriptableObjects`

The examples intentionally contain both valid and invalid values. See
`UValidationSandbox/Assets/Showcase/README.md` for the expected result of each object and asset.

## Development and tests

Clone the repository and open `UValidationSandbox` in Unity. Its project manifest references the
package through `file:../../com.hightower.uvalidation`, so edits under
`com.hightower.uvalidation` are compiled directly.

Run the package tests from **Window > General > Test Runner > EditMode**. The suite covers runtime
rules, drawers, inherited fields, recursive and `[SerializeReference]` validation, scene and prefab
save enforcement, ScriptableObject assets and sub-assets, build processing, and Editor feedback.

Issues and focused pull requests are welcome. When changing behavior, include a regression test and
update the relevant documentation or showcase example.

## License

UValidation is available under the [MIT License](LICENSE).
