# DividingMon1Test engineering standard

This file is the canonical implementation contract for this repository.

## 1. Product boundary

Build a small Windows 11 user-mode utility that makes one physical secondary display behave like four logical 2x2 work zones for window snapping.

The app MUST:
- use C# and .NET 8;
- use WinForms only for tray UI, preview overlays, and the interactive Snap Assist picker;
- use documented Win32/DWM APIs through narrow P/Invoke wrappers when Windows APIs are required;
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
- dragging on displays other than the configured target is not modified;
- by default, a physical-edge guard reserves the real outer target-display edges for Windows Snap while our outer logical edges are activated on inset virtual rails.

After a half or quarter snap, optional Snap Assist suggestions MUST stay inside that same logical submonitor:
- half snap -> offer windows for the opposite half only;
- quarter snap -> offer windows sequentially for the other three quarters only;
- full-zone snap -> no suggestions;
- clicking a suggestion is an explicit user action authorizing that selected window to move into the shown slot;
- suggestions must never fill another logical submonitor automatically.

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

The utility may reposition only:
- the window the user is actively moving; or
- a top-level window the user explicitly clicks in the Snap Assist picker.

## 4. Code quality hard constraints

Every handwritten source file MUST be at most 377 lines.

Also mandatory:
- nullable reference types enabled;
- warnings treated as errors;
- .NET analyzers enabled;
- no god files, god classes, or grab-bag utility classes;
- no hidden mutable global state;
- no business/geometry logic inside Forms;
- Win32/DWM declarations isolated from product logic;
- pure snapping geometry separated from native window manipulation;
- deterministic geometry with no unexplained magic numbers;
- explicit disposal of native hooks, DWM thumbnails, timers, forms, menus, and tray icons;
- no empty catch blocks;
- no TODO/placeholder behavior presented as complete;
- no third-party dependency unless the standard library/Win32 cannot reasonably solve the problem.

Prefer small explicit code over clever abstractions.

## 5. Architecture

Keep these concerns separate:
- Geometry: pure 2x2 partitioning, snap-target calculation, and companion-slot calculation.
- Display selection: enumerate/select the physical target display.
- Native interop: Win32/DWM declarations and constants only.
- Drag controller: observe move lifecycle and coordinate preview/snap.
- Window snapper: validate and reposition an explicitly authorized window.
- Overlay: render drag preview; never decide geometry.
- Snap Assist: enumerate candidate top-level windows, render a picker, and fill only companion slots.
- Tray application context: user controls, lifecycle, display selection.

UI code must call product logic, not duplicate it.

## 6. DPI and coordinates

The process must be Per-Monitor-V2 DPI aware.

All cursor, window, monitor, preview, picker, thumbnail, and SetWindowPos coordinates must stay in the correct coordinate space. Test with:
- negative monitor coordinates;
- target display to the left/right/above/below primary;
- non-100% DPI on either display;
- odd monitor widths/heights without one-pixel gaps.

## 7. Gesture rules

Do not snap on ordinary resize operations.

A move/resize lifecycle event alone is not enough to prove a move. Classify obvious border-resize starts conservatively and skip them.

Never keep snapping after the move ends.

Starting a new drag cancels any open Snap Assist session.

Windows Snap compatibility is enabled by default:
- the real outer edge guard of the target display MUST return no DividingMon snap target;
- outer half/corner activation MUST use inset virtual rails beyond that guard;
- internal logical-submonitor edges stay active normally;
- the primary display and global Windows Snap settings MUST NOT be changed;
- no registry or undocumented shell toggles may be used to suppress Windows Snap per monitor.

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
- half Snap Assist fills only the opposite half;
- quarter Snap Assist fills only the other three quarters of the same logical submonitor;
- cancel Snap Assist with Escape;
- move back to laptop without interference;
- disconnect/reconnect target display;
- exit while hooks/picker are active;
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
