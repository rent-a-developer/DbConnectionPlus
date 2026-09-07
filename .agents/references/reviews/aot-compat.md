# Native AOT / trimming compatibility review

Use this whenever a change touches reflection, dynamic dispatch, expression trees, or generic instantiation in
`src/`. This is a **standing** checklist — AOT support shipped in 4.0.0, and everything here exists to keep it
from regressing.

Read the [Native AOT and Trimming](../../../docs/DESIGN-DECISIONS.md#native-aot-and-trimming) section of
DESIGN-DECISIONS.md before reviewing: it records what is deliberate, and therefore what counts as a regression,
and it carries the measurements behind each decision.

**The design is reflection-based.** Nothing in `src/` generates code at run time, with one exception: the
expression-compiled materializer, which is reachable only from the just-in-time branch of a
`RuntimeFeature.IsDynamicCodeSupported` guard. There is no companion package, no source generator, no
registry of types and no opt-in attribute — a change that introduces one is a finding, not an optimization.

This is a **review**: report findings, do not edit files.

## The highest-severity finding: a broken DAM chain

Missing or **incomplete** `[DynamicallyAccessedMembers]` annotations do not fail loudly. Under trimming,
reflection returns fewer members with no error and the library hands back default-valued entities — measured at
6 columns in, 0 bound, no exception. Treat these as blocking:

- A **new suppressed `IL2xxx`** anywhere in `src/`. These warnings are the only build-time proof the chain is
  complete; suppressing one voids it. Restructure instead.
  (`[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` are *not* suppressions: they propagate the
  requirement to callers. Neither is an `ILLink.Descriptors.xml` entry, which preserves members rather than
  silencing a diagnostic.)

  **There are exactly two sanctioned `IL2xxx` suppressions, and a third one is a finding.** They are what keeps
  `[RequiresUnreferencedCode]` off the query methods, and both are narrow, single-purpose and backed by something
  executable. Verify they still hold rather than assuming; do not use them as precedent for a new one.

  | Site | Code | Why it holds | What guards it |
  |---|---|---|---|
  | `MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod` | `IL2060` | `ValueConverter.ConvertValueToType<TTarget>` declares no DAM on `TTarget`, so the runtime specialization has no requirements to preserve | Adding a DAM to that type parameter invalidates it — check for one |
  | `ValueTupleMaterializerFactory.GetValueTupleConstructors` | `IL2065` | The embedded `ILLink.Descriptors.xml` preserves `System.ValueTuple\`1`-`\`8`, and the caller has rejected any non-value-tuple type | `Trimming/ILLinkDescriptorsTests` plus smoke cases 6 and 12/14 |

  A change that makes either stop holding must restore `[RequiresUnreferencedCode]` on the query methods. Leaving
  an untrue suppression in place is the worst available outcome — it is exactly how silently unpopulated entities
  ship.
- An annotation that is **present but incomplete** — e.g. `PublicParameterlessConstructor` without
  `PublicProperties`. The type still constructs, so nothing fails loudly and every column silently fails to
  bind. Cross-check every DAM against what the reflection call actually needs.
- DAM that does not **flow** from the public entry point down to `EntityHelper.GetEntityTypeMetadata(Type)`.
  Note it does not propagate through `ConcurrentDictionary.GetOrAdd` lambdas.
- Removal or weakening of the **zero-binding guard** — the only defence that does not depend on a human getting
  annotations right.

⚠️ None of this reproduces on the just-in-time compiler, so a green test suite is not evidence. Only the AOT
smoke test is.

## The things the compiler cannot tell you

The tooling misses the emit-based blockers, so text search is not optional here:

- **Emit-based reflection helpers** — `Fasterflect` (`fasterflect.reflect`) and `FastMember` are the two this
  library would plausibly reach for — produce **zero** compile-time IL diagnostics, because they are
  unannotated third-party assemblies. Neither is a dependency: accessors go through
  `System.Reflection.MethodInvoker` and the temporary-table read path through `EnumerableReader`. Adding one is
  a finding; it also puts a permanent `IL2104` roll-up into every consumer's publish.
- **`dynamic`** compiles to Microsoft.CSharp binder calls that need runtime code generation. It must not appear
  in `src/`; the non-generic query methods return `DataRow`.
- **`Expression.Compile()` does not throw under AOT — it silently interprets**, at ~40x the cost and ~15x the
  allocations. Expression-tree code reachable under AOT is a finding even though it "works": it must sit inside
  an `if (RuntimeFeature.IsDynamicCodeSupported)` branch.

Grep for: `Fasterflect`, `FastMember`, `ObjectReader`, `\bdynamic\b`, `MakeGenericMethod`, `MakeGenericType`,
`Activator.CreateInstance`, `Expression\.(Compile|New|Lambda)`, `JsonSerializer`, `GetProperties`,
`GetConstructors?`, `GetFields`, `Type.GetType`.

## Measuring

The analyzers are on permanently via `IsAotCompatible` in `src/Directory.Build.props`, so an ordinary build
surfaces IL diagnostics — and, as warnings-as-errors, fails on them:

```bash
dotnet build DbConnectionPlus.slnx -c Release
```

**The expected count is zero, on both target frameworks**, with no suppressions beyond the sanctioned ones
listed above (`IL2060`, `IL2065`, and the `net8.0`-only `IL3050` on the two `CreateMaterializer` dispatchers).
Any other IL diagnostic in `src/` is a regression; re-measure rather than assuming.

That is necessary but not sufficient. The defect this checklist exists for is invisible at compile time and on
the just-in-time compiler, so a change to any reflection path also needs:

```bash
pwsh -File scripts/verify-package-aot.ps1 -Pack
```

It packs the six shipping projects, publishes `tests/package-consumption/AotConsumer` natively **from those
packages**, gates its IL diagnostics and runs the binary. `-Framework net8.0` checks the documented floor,
which behaves differently from the `net10.0` default. Needs a C++ toolchain: MSVC on Windows, `clang` +
`zlib1g-dev` on Linux. Drop `-Pack` to reuse the packages already in `artifacts/packages`.

The consumer reaches the library through `PackageReference` on purpose: the annotations, the embedded
`ILLink.Descriptors.xml` and the `IsTrimmable` marker have to survive packing, and a project reference would
not prove that.

The gate is **zero diagnostics, from anywhere** — the library, an adapter, a package in the closure, or the smoke
test's own call sites. The generic query methods carry neither `[RequiresUnreferencedCode]` nor
`[RequiresDynamicCode]`, so a consumer publishing with `PublishAot` or `PublishTrimmed` sees nothing for any
supported scenario. Any diagnostic is a regression; one at the smoke test's own call sites specifically means a
public API has gained one of those attributes.

⚠️ Both target frameworks must stay in the gate. `net8.0` needs an `IL3050` suppression on the two
`CreateMaterializer` dispatchers that `net10.0` does not, because only `net9.0`+ annotates
`RuntimeFeature.IsDynamicCodeSupported` as a `[FeatureGuard]`. The `net10.0` build compiling **without** that
suppression is what verifies the reasoning behind it; dropping `net8.0` from the gate, or dropping `net10.0`,
turns a checked fact into folklore.

### Standing check: `Dynamic/DataRow.cs`

`DataRow` supports `dynamic row.Id` by implementing **`IDynamicMetaObjectProvider`**, *not* by deriving from
`DynamicObject`. On `net10.0` the `DynamicObject` **constructor** is `[RequiresDynamicCode]`, so the base class
would push that attribute onto `DataRow`'s constructor and from there onto all 10 non-generic query methods —
penalising AOT consumers who only ever use the string indexer. Reject any change that:

- reintroduces a `DynamicObject` base on `DataRow`;
- calls `Expression.Lambda` or `LambdaExpression.Compile` inside `DataRowMetaObject` (both are
  `[RequiresDynamicCode]`; binding must stay a plain delegate invocation through the row's own indexer);
- adds `[RequiresDynamicCode]` or `[RequiresUnreferencedCode]` to `DataRow` or to a non-generic query method.

The API-snapshot diff is the cheap tripwire: `[RequiresDynamicCode]` must not appear on `DataRow` or on
`Query`/`QueryFirst`/`QueryFirstOrDefault`/`QuerySingle`/`QuerySingleOrDefault`.

### Standing check: `ILLink.Descriptors.xml`

`src/DbConnectionPlus/ILLink.Descriptors.xml` keeps the members of `System.ValueTuple`1`–`8`. It is what makes
a query for a value tuple with **more than seven fields** work under trimming: such a tuple is stored as a
nested tuple, DAM is not recursive, and the trimmer would otherwise remove the inner type's constructor. Reject
any change that drops it, drops its `EmbeddedResource` wiring, narrows the arities, or reverts the nested-tuple
traversal from `Type.GetGenericArguments()` back to `GetFields()` / `FieldInfo.FieldType`.

`ILLinkDescriptorsTests` guards the file's presence; only the smoke test proves it still works.

## Warning-code glossary

| Code | Meaning |
|---|---|
| `IL2026` | Uses a `[RequiresUnreferencedCode]` member (trim-unsafe). |
| `IL2060` | `MakeGenericMethod` that cannot be statically analyzed. |
| `IL2067` | Parameter flowed into a DAM-annotated location without matching annotations. |
| `IL2070` / `IL2075` | Reflection over a type whose members may be trimmed (missing DAM on a parameter / returned value). |
| `IL2093` | DAM annotations on an override don't match the base member. |
| `IL2104` | An entire assembly produced trim warnings (roll-up for unannotated third-party assemblies). |
| `IL3050` | Calls a `[RequiresDynamicCode]` API — unsupported under AOT. |
| `IL3051` | `[RequiresDynamicCode]` mismatch across an override/interface. |
| `IL3053` | An entire assembly produced AOT analysis warnings (the `IL2104` counterpart). |

## Checklist

1. **Annotations.** `Type` parameters and public generic parameters that get reflected over carry
   `[DynamicallyAccessedMembers]`. An internal member that genuinely needs run-time code generation carries
   `[RequiresDynamicCode]` and is reached only from a `RuntimeFeature.IsDynamicCodeSupported` branch — but
   neither `Requires*` attribute belongs on a public API, where it would warn every consumer; if a change seems
   to need one there, that is a finding to report, not an edit to make. Overrides must match their base
   member's annotations exactly
   (`IL2093`/`IL3051`) — for `DbDataReader.GetFieldType` and `GetProviderSpecificFieldType` that is
   `PublicFields | PublicProperties`.
2. **Annotations propagate; a new suppression is a finding.** A `#pragma warning disable IL…` or
   `[UnconditionalSuppressMessage]` that is not one of the sanctioned sites above is a finding — including a
   widened justification on a sanctioned one.
3. **The AOT branch is guarded, not duplicated by accident.** Expression-tree construction must sit inside
   `if (RuntimeFeature.IsDynamicCodeSupported)`; the reflection branch must be the `else`. Verify the
   just-in-time path is byte-for-byte what it was.
4. **Both runtimes keep the full feature set.** A change that makes a feature AOT-only, or that breaks the
   just-in-time path, is a finding.
5. **Behaviour parity.** The reflection and expression paths must agree — including exception types and
   *exception message text*, which the tests assert verbatim.
6. **Shape-keyed caching preserved.** Materializers are keyed by result-set *shape*, not by type. A change that
   keys by `Type` alone is a correctness bug: `SELECT Id, Name` and `SELECT *` need different delegates, and
   the same column arrives as `Int64` on SQLite and `Int32` on SQL Server.
7. **The AOT consumer still covers the change.** A new feature path that reflects over a caller-supplied type
   needs a case in `tests/package-consumption/AotConsumer`, asserting **values** rather than row counts —
   silent trimming damage empties rows, it does not remove them.
8. **Repo conventions still met**: copyright header, `String`/`Object`/`Int32` BCL names, file-scoped
   namespaces, 120-column lines, full XML docs, expression-bodied members, no new analyzer warnings.
9. **Public surface.** A change to any shipping project's public API has to be recorded in that project's
   `PublicAPI.Unshipped.txt` (`scripts/update-public-api.ps1`) — otherwise the build fails with `RS0016` /
   `RS0017` — plus a CHANGELOG entry, prefixed `BREAKING` where applicable. See CONTRIBUTING.md.

## Reporting

Report only findings verified by reading the code or by running the build. For each: file:line, the specific
AOT hazard, and whether it is new or pre-existing.

If the change is clean, say so in one line. Do not pad.
