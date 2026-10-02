# DividingMon1Test engineering standard

This file is the canonical implementation contract for this repository.

## 1. Product boundary

Build a small Windows 11 user-mode utility that makes one physical secondary display behave like four logical 2x2 work zones for window snapping.

The app MUST:
- use C# and .NET 8;
- use WinForms only for the tray UI and non-activating preview overlay;
- use documented Win32 APIs through narrow P/Invoke wrappers when Windows APIs are required;
- remain a normal user-mode desktop process;
- require no administrator rights;
- install no driver, service, scheduled task, shell extension, or kernel component;
- make no network requests and include no telemetry;
- write no registry settings;
- leave the primary/laptop display untouched unless the user explicitly chooses it as the target.

The app MUST NOT pretend the four zones are real Windows monitors. They are logical snapping regions only.

## 2. Required behavior

The selected physical display is divided into four equal logical submonitors:

1. top-left;
2. top-right;
3. bottom-left;
4. bottom-right.

While the user drags a normal top-level window:
- dropping in the middle of a submonitor fills that submonitor;
- touching its left/right edge previews and snaps to its left/right half;
- touching its top/bottom edge previews and snaps to its top/bottom half;
- touching one of its corners previews and snaps to that quarter;
- a visible non-activating overlay shows the exact destination before release;
- dragging on displays other than the configured target is not modified.

If the target display disappears, the window handle becomes invalid, a native call fails, or the gesture cannot be classified safely, do nothing.

## 3. Safety constraints

Prefer failure with no action over surprising desktop changes.

Do not:
- inject keyboard or mouse input;
- install low-level keyboard hooks;
- modify other processes' memory;
- use undocumented kernel APIs;
- request UIAccess;
- elevate;
- terminate or suspend other processes;
- download or execute external binaries;
- use PowerShell/cmd as part of runtime behavior.

Global observation must be minimal. A WinEvent hook for move/size lifecycle plus polling the cursor only while a move is in progress is acceptable.

The utility may reposition only the window the user is actively moving.

## 4. Code quality hard constraints

Every handwritten source file MUST be at most 377 lines.

Also mandatory:
- nullable reference types enabled;
- warnings treated as errors;
- .NET analyzers enabled;
- no god files, god classes, or grab-bag utility classes;
- no hidden mutable global state;
- no business/geometry logic inside Forms;
- Win32 declarations isolated from product logic;
- pure snapping geometry separated from native window manipulation;
- deterministic geometry with no unexplained magic numbers;
- explicit disposal of native hooks, timers, forms, menus, and tray icons;
- no empty catch blocks;
- no TODO/placeholder behavior presented as complete;
- no third-party dependency unless the standard library/Win32 cannot reasonably solve the problem.

Prefer small explicit code over clever abstractions.

## 5. Architecture

Keep these concerns separate:
- Geometry: pure 2x2 partitioning and snap-target calculation.
- Display selection: enumerate/select the physical target display.
- Native interop: Win32 declarations and constants only.
- Drag controller: observe move lifecycle and coordinate preview/snap.
- Window snapper: validate and reposition the tracked window.
- Overlay: render preview; never decide geometry.
- Tray application context: user controls, lifecycle, display selection.

UI code must call product logic, not duplicate it.

## 6. DPI and coordinates

The process must be Per-Monitor-V2 DPI aware.

All cursor, window, monitor, preview, and SetWindowPos coordinates must stay in the same coordinate space. Test with:
- negative monitor coordinates;
- target display to the left/right/above/below primary;
- non-100% DPI on either display;
- odd monitor widths/heights without one-pixel gaps.

## 7. Gesture rules

Do not snap on ordinary resize operations.

A move/resize lifecycle event alone is not enough to prove a move. Classify obvious border-resize starts conservatively and skip them.

Never keep snapping after the move ends.

At physical display edges Windows Snap may also appear; our final SetWindowPos may override the result only when our own preview target was active at release.

## 8. Verification

The repository must contain a dependency-free geometry self-test executable.

Before claiming completion:
1. restore;
2. build Debug;
3. run geometry self-tests;
4. build Release when the environment supports it;
5. report anything that could not be run.

Manual verification on Windows should cover:
- drag from laptop to each of the four TV zones;
- full-zone snap;
- left/right half;
- top/bottom half;
- four quarter snaps;
- move back to laptop without interference;
- disconnect/reconnect target display;
- exit while hooks are active;
- elevated target window fails harmlessly;
- DPI scaling and negative display coordinates.

## 9. Repository hygiene

Commit:
- source;
- project/solution files;
- app manifest;
- build scripts;
- README;
- this engineering standard.

Do not commit:
- bin/ or obj/;
- IDE user state;
- secrets;
- generated publish output;
- binaries unless explicitly requested.

## 10. Completion definition

READY FOR USER PULL means:
- the repository is coherent and buildable from a clean clone with the .NET 8 SDK;
- no source file exceeds 377 lines;
- no driver/admin/network/telemetry behavior was introduced;
- self-tests exist and pass when build verification is available;
- README describes limitations honestly, especially that these are not real monitors;
- any unverified item is stated explicitly instead of being guessed.
