# UValidation attribute showcase

Open `Assets/Scenes/ValidationAttributeShowcase.unity` and select each numbered GameObject.
The Inspector demonstrates supported values, validation failures, and deliberately unsupported
attribute placements.

## Sections

1. `NotNull` — assigned and missing Unity object references, plus string and value-type misuse.
2. `NotEmpty` — populated and empty strings and collections, plus scalar misuse.
3. `HasNoNulls` — valid, null-containing, and empty Unity object collections, plus unsupported types.
4. `HasNoEmpties` — valid, empty-containing, null-containing, and empty string collections, plus misuse.
5. `IsValid` — valid and invalid nested objects, a null nested object, custom nested validation, and ignored types.
6. Combined attributes — useful combinations for required collections and nested objects.
7. `IValidatable` — pass and fail examples of component-level custom validation.

Green/valid values intentionally coexist with red/error and yellow/warning values, so the scene is
intentionally invalid when editor validation is enabled.

## ScriptableObject assets

Browse `Assets/Showcase/ScriptableObjects` in the Project window. The folder contains one valid
asset, focused invalid assets for every validation path, a combined-failures asset, and an assigned
reference asset under `_Shared`.

1. `01 - Valid` — every attribute and the custom validation rule pass.
2. `02 - NotNull Failure` — the required asset reference is missing.
3. `03 - NotEmpty Failure` — the required name and value collection are empty.
4. `04 - HasNoNulls Failure` — the reference collection contains a null element.
5. `05 - HasNoEmpties Failure` — the string collection contains an empty element.
6. `06 - IsValid Failure` — recursively validated nested data is incomplete.
7. `07 - IValidatable Failure` — custom asset-level validation fails.
8. `08 - Combined Failures` — all failure paths coexist in one asset.

With validation enabled, invalid assets are labelled red in the Project window and Unity prevents
their changes from being saved. Select an asset to inspect its field-level HelpBoxes.

## Rebuilding the showcases

Use `HighTower > UValidation > Rebuild Showcase Scene`. The builder temporarily disables save
validation, writes the intentionally invalid scene, restores the previous validation preference,
and selects the overview object.

Use `HighTower > UValidation > Rebuild ScriptableObject Showcase` to recreate every generated
`.asset` file with deterministic values. The builder also temporarily disables save validation and
selects the valid asset when it finishes.

## Expected semantics

- Empty collections pass `HasNoNulls` and `HasNoEmpties`; those attributes validate elements,
  not collection size. Combine them with `NotEmpty` when both guarantees are required.
- `IsValid` alone allows a null nested object. Combine it with `NotNull` when the nested object is
  required.
- Unsupported placements produce Inspector warnings when a property drawer can inspect them.
  Some unsupported placements are runtime no-ops; they are included to make that behavior visible.
- `NotNull` runtime validation accepts any boxed reference/value, while its Inspector drawer is
  intentionally limited to `UnityEngine.Object` references. The misuse examples expose that distinction.
