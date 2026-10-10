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

`[IsValid]` only controls traversal. A null or empty collection passes, and null elements are
skipped. Add collection constraints separately when they are part of the field's contract.
Unsupported placements, including primitives, Unity object references, and collections of those
types, produce an Inspector warning.
