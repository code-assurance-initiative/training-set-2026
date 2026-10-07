# 2. JavaScript interop through typed wrappers

Date: 2026-10-03

## Status

Accepted

## Context

Blazor calls JavaScript by string name. A renamed script function is not a compile error; it fails at run time, and
only on the page that calls it.

## Decision

Interop goes through small wrapper classes in `Interop/` (clipboard, dialogs, map). Components call the wrappers, not
`IJSRuntime`. A unit test reads the names the wrappers call and checks each against the scripts in `wwwroot/js`, and
checks that every imported module exists.

## Consequences

- Renaming a script function used by a wrapper fails the build's tests.
- A call made directly from a component is not covered by the test.
- The cost: every script needs a wrapper class registered in the container, and a component that needs a new
  JavaScript function waits for a wrapper method (and its test) instead of calling `IJSRuntime` on the spot — more
  code and one more hop to read for each call. For a portal with three scripts the trade is cheap; it would be
  revisited if the interop surface grew to dozens of functions (a generated binding layer would then pay off).
