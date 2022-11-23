## Project file-structure

- Try to keep size of individual files below 1.5-2K LOC <!--([Helpers](src/Helpers/) folder is an exception)-->
- Try to keep file count per namespace folder below 15 <!--([Helpers](src/Helpers/) folder is an exception)-->
- Keep the number of classes in main namespace to a minimum

## Code style

- Use `#region` directives liberally, nest if needed
- Fields/properties/constants preferably at the top
- Nested types preferably at the bottom (`#region nested`)
- Static members in non-static types preferably at the bottom (`#region statics`)
- Keep your code as "flat" as possible, avoid making any single line too long, unless doing that means turning expression into unreadable mess

### Naming

- Try to make class and member names as short as possible without making them counterintuitive (`GetVar` > `GetVariable`, `EnsureAndGet` > `SetIfMissingAndGet`)
- Using type and namespace aliases for long external names is encouraged. Type aliases should preferably be all-caps acronyms (`TXT` <- `System.Text.RegularExpressions`).
- Project-wide usings, including type aliases, are in [the Prelude file](src/Helpers/Prelude.cs) 

## Functionality distribution

| Namespace		| Functionality	|
| ---			| ---			|
| Atmo			| Mod core class, API, mod-interop |
| Atmo.Body		| Lifeblood. Classes the execution flows through |
| Atmo.Gen		| Populating Body with behaviours |
| Atmo.Data		| Handling user input, data storage and serialization |
| Atmo.Helpers	| Additional utilities not specifically belonging to any of the above |
