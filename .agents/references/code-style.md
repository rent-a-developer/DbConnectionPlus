# Code style details

Background for the rules in [AGENTS.md](../../AGENTS.md#code-style-formatting-and-ordering). Read this when a
tool does something you did not expect, or when you are about to write a type name in a place the build does
not check.

## The concerns, and the tool that owns each

| Concern | Tool | Configured in |
|---|---|---|
| Formatting — whitespace, line breaks, wrapping | **CSharpier** | `.editorconfig` (`max_line_length`, `indent_size`) |
| Style — `var`, `=>`, `this.`, null checks, usings | **Roslyn analyzers** | `.editorconfig` |
| Ordering — types and their members | **ReSharper** applies it, **NewStyleCop** checks *part* of it | `DbConnectionPlus.slnx.DotSettings` and `stylecop.json` |

The word *part* is load-bearing. StyleCop checks kind, access, constant, static and readonly — `SA1201`,
`SA1202`, `SA1203`, `SA1204`, `SA1214`. It has no notion of alphabetical order **within** one of those
groups, which the ReSharper file layout applies and nothing checks. A member that is in the right group but
the wrong place inside it compiles, passes the analyzers, and is only visible by running the pipeline and
looking at what it moves.

Each tool owns its concern completely, and every one of them is a build error rather than a warning, in `tests/` and
`benchmarks/` as much as in `src/`. Two different mechanisms, both in the root `Directory.Build.props`:
`EnforceCodeStyleInBuild=true` with `TreatWarningsAsErrors=true` covers style and ordering, and the
`CSharpier.MsBuild` package covers formatting. It runs in check mode, so a build never rewrites your files —
an unformatted file fails the build and names itself. `-p:CSharpier_Bypass=true` skips it.

## Where the build misses a BCL type name

The rule is: write `string`, `object?`, `int`, `bool`, `nint`, `nuint` — never `String`, `Object?`, `Int32`,
`Boolean`, `IntPtr`, `UIntPtr`. `dotnet_style_predefined_type_for_*` is `true:error`, and `IDE0049` enforces
most of it.

`IDE0049` has two blind spots. Neither produces a diagnostic, so `TreatWarningsAsErrors` has nothing to fail
on. Both were verified against the analyzer:

- **`nint`/`nuint` are not on its list.** They arrived in C# 9 as their own feature and only became aliases
  for `IntPtr`/`UIntPtr` in C# 11; the analyzer was never extended. `IntPtr` is invisible to it.
- **It never looks inside `nameof(...)`.** With good reason: `nameof(int)` does not compile at all
  (CS1525 — a keyword is not a name), so a blanket skip is the safe choice. Where `nameof` names a CLR type
  on purpose, as in `EnumerableReaderTests`, leave it and say why in a comment.

A third gap is not the analyzer's fault: **`tests/package-consumption/` is not in `DbConnectionPlus.slnx`**,
so `dotnet format` and `dotnet build` on the solution never see it, and its deliberately empty
`Directory.Build.props` means it gets no style gate even when `verify-package-aot.ps1` builds it. If you touch
those files, apply the style by hand.

## Primary constructor parameters

Every primary constructor parameter is assigned to a `private readonly` backing field, and members read
`this.field` rather than the parameter:

```csharp
internal class SqliteEntityManipulator(SqliteDatabaseAdapter databaseAdapter) : IEntityManipulator
{
    private readonly SqliteDatabaseAdapter databaseAdapter = databaseAdapter;
```

Using the parameter directly is shorter and looks equivalent. It is not. A parameter that a member body reads
is *captured*, and the field the compiler generates for it carries no `readonly` — so the value becomes
reassignable from inside the type, where the explicit field would have made that a compile error. C# has no way
to mark a primary constructor parameter `readonly`, so the backing field is the only way to keep the guarantee.

There is no diagnostic for this. `IDE0290` asks for the primary constructor and stops there; the `this.`
rules cannot see a parameter at all. It is on the author.

The field costs nothing at runtime: a parameter used only in a field initializer is not captured, so the
explicit field and the compiler's capture field are one field, not two.

## When a tool surprises you

- **`cleanupcode` re-indents the content of raw string literals and CSharpier puts it back.** Neither is
  idempotent alone; the pair is. Always let CSharpier run last — `tidy-code.ps1` does.
- **Explicit interface implementations sort first — except events.** StyleCop counts an explicit property,
  indexer or method as public, so each of those has its own "Explicit interface …" entry in the file layout
  putting it at the front of its group. An explicit **event** is counted as private, so it stays where
  ReSharper puts it by default, at the end; giving events an entry breaks the build. The entries match
  `ImplementsInterface` **and** `Access Is="Private"` — without the access test they would also catch implicit
  implementations and pull `Equals(T)` away from `Equals(object)`.
- **Constructors sort static-first; everything else sorts by access first.** The file layout gives the
  constructors entry `<Static />` ahead of `<Access />`, and only that entry. It is the one place where the
  two order definitions had to be reconciled by hand: `stylecop.json` lists `accessibility` before `static`,
  which for constructors would put a `public` instance constructor ahead of the static one. `Benchmarks.cs`
  has both, in that order, and the build is green — so this is load-bearing, not an oversight. Do not
  "regularise" it.
- **Static fields are not reordered past each other.** The file layout sets
  `StaticFieldReorderingPolicy="Strict"`, so ReSharper leaves a static field where it found it relative to the
  other static fields of its type. Moving one can change the order its initializer runs in, and that is a
  behaviour change no formatter is allowed to make. Order them by hand if you need them ordered.
- **Overloads with the same name have no defined order between them.** Two methods called `Equals` tie on
  every key the file layout sorts by — kind, access, static, readonly and name — and ReSharper's sort is
  stable, so it leaves them in whatever order it found them. Reordering the same file starting from two
  different states can therefore give two different results, and both are correct: StyleCop accepts either,
  and moving a method declaration changes nothing at runtime. CI does not flap over it, because a stable sort
  leaves the committed order alone. There is no fix available — ReSharper's `SortBy` offers `<Name />` but
  nothing that separates overloads by signature. Put new overloads next to their siblings and it will never
  come up.

## XML documentation comments

XML docs go on all public and most internal members of the **shipping** projects — `<param>`, `<returns>`,
`<exception>`, `<remarks>`. Match the density of the file you are editing; `DbConnectionExtensions.QueryFirst.cs`
sets the bar. Invalid XML docs break the docfx workflow.

The benchmarks are the exception: nothing consumes them as an API, so they use plain `//` comments and switch
`RCS1181` off for that reason.

## Member order

StyleCop's order, applied by ReSharper. Write a new member straight into the right place rather than relying
on the fixer — and note that only the leading keys below are checked by an analyzer:

    constants → fields → constructors → finalizers → delegates → events → enums → interfaces
    → properties → indexers → conversion operators → operators → methods → nested structs → nested classes

Within each of those groups: public before internal before protected before private, static before instance,
readonly before mutable, and **alphabetical** after that. The kind, access, constant, static and readonly keys
are `SA1201`/`SA1202`/`SA1203`/`SA1204`/`SA1214` and are build errors. Alphabetical order is applied by
ReSharper and checked by nothing.

Note that fields go at the **top** of a type, and that explicit interface implementations sort ahead of the
ordinary methods — with the exception for events described above.

## Rules the build fully enforces

These need no attention beyond letting the tools run — they are listed here so that the rule set in AGENTS.md
stays short, not because they are optional:

- **Primary constructors** wherever `IDE0290` asks for one.
- **`var` where the type is obvious** — a built-in type, or a right-hand side that names the type. Both are
  errors. Everywhere else the preference is the explicit type, stated in `.editorconfig` as
  `csharp_style_var_elsewhere = false`, but it is *not* enforced: it is a preference the build does not fail
  on, so an existing `var` in that position is not a defect.
- **Expression-bodied members** are `error`-severity for methods, constructors, operators, properties, indexers,
  accessors, lambdas and local functions. Use `=>` wherever a member is a single expression.
- **File-scoped namespaces**, with usings outside the namespace.
- **Max line length is 120**, set once in `.editorconfig` and read from there by CSharpier. Do not wrap lines by
  hand — write it on one line and let CSharpier break it.
- Nullable and `ImplicitUsings` are enabled; common namespaces come from `GlobalUsings.cs`.
