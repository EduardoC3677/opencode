# Hotkey research — Acer Aspire A315-59 (BIOS 1.31)

This document enumerates the **key combinations and POST hotkeys** documented
inside the BIOS modules `FrontPageDxe.efi`, `SetupUtility.efi` and
`H2OFormBrowserDxe.efi`.

## 1. Documented POST hotkeys

These are the only key bindings that the firmware itself prompts the user
about (via UTF-16 strings inside `FrontPageDxe.efi`):

| Key | Function | Source |
|---|---|---|
| `F2` | Enter BIOS Setup (`SetupUtilityApp`) | standard (no string banner — invoked silently) |
| `F12` | Boot menu | `H2OFormBrowserDxe.efi` — "Press F12 to display Boot menu" |
| `Esc` | Boot options menu | `FrontPageDxe.efi` — `Esc is pressed. Go to boot options.` |
| `F9` | ME Remote Assistance | `FrontPageDxe.efi` — `F9 is pressed. Go to ME Remote Assistance.` |
| `F10` | MEBx (ME BIOS Extension) | `FrontPageDxe.efi` — `F10 is pressed. Go to MEBx.` |

> Note: the F9/F10 -> ME / MEBx hotkeys are gated by `MeBxHotKey` being
> enabled in the IFR. The string is always shipped, but the keypress is
> ignored unless the underlying NVRAM bit is set.

## 2. Recovery / OEM hotkeys

| Key | Function |
|---|---|
| `Alt+F10` (during POST) | **Acer disc-to-disc recovery** (D2D). Only meaningful on systems with the Acer Recovery partition (not a BIOS-unlock hotkey). |
| `Fn+Esc` (some Acer firmwares) | Boot to Insyde InterTool / factory recovery — present here as `InterToolx64.efi`, but the firmware does NOT bind a documented hotkey to it. |
| `Fn` + `F1..F12` (action keys) | Toggle between media keys and function keys (configurable in Setup -> Main -> "Function key behavior"). |

## 3. Hotkeys to UNLOCK advanced menus — research conclusion

**No "secret" key combination was found** that would, on its own, expose the
hidden advanced menus on this firmware. Specifically the following were
checked and are **not present** in any of the analyzed modules:

- `A`, `Ctrl+A`, `Ctrl+F1`, `Ctrl+Alt+F1` — historical Acer/Phoenix unlock combos
- `Tab`-while-booting, `Fn+Tab` — historical Award/AMI combos
- `Shift+F2`, `Alt+F2`, `Ctrl+F2` — InsydeH2O private-build combos
- `winwin`, `bios`, `service` — Insyde service-mode strings

The reason is structural: in InsydeH2O the visibility of every Setup item is
hard-coded in the **IFR** (the binary HII form). Items are hidden by
`SuppressIf [AccessLevel == X]` opcodes. There is no runtime hotkey that
toggles the `AccessLevel` from "User" to "Manufacturer". The only ways to
expose those items on the actual hardware are:

1. **Patch the IFR opcodes** (this is what `SREP_CONFIG.cfg` does, at boot).
2. **Set the Supervisor / Manufacturer password to a known value** (sometimes
   the AccessLevel is gated by the password type — but on Acer Aspire BIOSes
   this gate is *also* the AccessLevel form, so it does not actually unlock
   additional items).
3. **Write the `AccessLevel` NVRAM variable** directly from an EFI shell
   (requires knowing the GUID — `8BE4DF61-93CA-11D2-AA0D-00E098032B8C` for
   `gEfiGlobalVariableGuid`, but the actual variable is in the Insyde
   `Setup` variable namespace and is rejected by SMM if the request comes
   from outside Setup).
4. **Permanently modify SetupUtility.efi in the SPI flash** (dangerous;
   breaks Secure Boot signing of the BIOS region — recommended **only** with
   an SPI flasher backup).

## 4. Setup-utility internal hotkeys (within the BIOS menu)

These are active **once you are already inside** the Setup utility (after
F2). They were extracted from the IFR string tables:

| Key | Function |
|---|---|
| `F1` | General help (overlay window) |
| `F2` | Previous tab |
| `F3` | Next tab |
| `F5` / `F6` | Decrease / Increase the value of the current item (also used for "move up / down" in boot order) |
| `F9` | Load Setup Defaults |
| `F10` | Save and Exit |
| `Esc` | Exit / leave submenu |
| `Enter` | Open submenu / confirm |
| `+` / `-` | Same as F6 / F5 in numeric fields |
| `Tab` | Move between fields inside a date/time picker |

## 5. Conclusion

For the Acer Aspire A315-59 BIOS v1.31:

- The **advanced/hidden menus cannot be unlocked with a hotkey**.
- The supported method is the **`SREP_CONFIG.cfg`** in this repository, which
  patches the SuppressIf opcodes in `SetupUtility.efi` at runtime, exposing
  every item that the OEM hid behind `AccessLevel != User`.
