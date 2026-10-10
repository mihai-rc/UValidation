# UValidation

Inspector and runtime content validation for Unity.

## Recursive validation

Use `[IsValid]` on an inline custom serializable class, or on an array or `List<T>` whose element
type is a custom serializable class:

```csharp
[Serializable]
public sealed class ItemData
{
    [SerializeField, NotEmpty] private string m_Name;
}

[SerializeField, IsValid] private ItemData m_Item = new();
[SerializeField, IsValid] private List<ItemData> m_Items = new();
```

Every annotated field inside each object is validated recursively. Collection failures include the
element index, such as `m_Items[2].m_Name`. Custom `IValidatable` failures receive the same prefix.
If an `IValidatable` implementation throws while invoked by UValidation, the exception is captured
as a failed diagnostic with that object's path and original stack trace.

`[IsValid]` only controls traversal. A null or empty collection passes, and null elements are
skipped. Add collection constraints separately when they are part of the field's contract.
Unsupported placements, including primitives, Unity object references, and collections of those
types, produce an Inspector warning.

## Runtime fluent validation

Create a validation scope, chain the rules that describe the runtime contract, and report any
accumulated failures before disposal:

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

Collection rules are generic, so value-type collections do not require boxing adapters. Null
checks recognize destroyed Unity objects even when they are exposed as `object`, and nullable
value types have dedicated overloads. Use the predicate form of `IsTrue` when evaluation itself
may throw and should be recorded as a validation failure:

```csharp
validation.IsTrue(nameof(m_Config), m_Config, config => config.CalculateValue() > 0);
```
