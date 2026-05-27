# Acer Aspire A315-59 — BIOS analysis & SREP unlock config

End-to-end automated analysis of the Acer Aspire **A315-59** BIOS version **1.31**
(Insyde H2O), generation of a SREP (`SmokelessRuntimeEFIPatcher`) configuration
file to unhide the advanced BIOS menus, and full documentation of every step.

> ⚠️ **UNTESTED / RESEARCH ONLY** — the generated `SREP_CONFIG.cfg` was produced
> automatically by signature-matching against the actual BIOS modules but has
> **not** been validated on real hardware. Read [SAFETY](#safety) before
> applying anything to a real laptop.

## Files

| File | Purpose |
|---|---|
| `SREP_CONFIG.cfg` | SREP config file with all patches (drop-in for SREP.efi) |
| `ANALYSIS.md` | Full technical analysis of the BIOS and how the patches were derived |
| `CLI_REFERENCE.md` | CLI arguments / inputs documented for `H2OFFT-Wx64.exe`, `FWUpdLcl.exe`, `FlsHook.exe`, `BiosImageProcx64.dll`, `InterToolx64.efi` |
| `HOTKEYS.md` | Findings about hidden hotkeys / key combinations |
| `RUN_LOG.md` | Step-by-step log of the automated pipeline |

## Quick-use (do this **only** if you understand the risks)

1. Build SREP from <https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS>
   (or download a release).
2. Format a USB drive as FAT32, make it UEFI-bootable.
3. Copy `SREP.efi` and `SREP_CONFIG.cfg` to the root of the USB.
4. Boot the laptop into the EFI shell, run `SREP.efi`.
5. SREP patches **runtime memory only** (not the SPI flash). On the next cold
   boot, all unlocks revert. This is intentional — it is the safe way to test.

## Safety

- **Make a full SPI dump** (CH341A + clip on the BIOS chip) before doing
  anything. Without a backup, any mistake is permanent.
- **Do NOT** flash the patched modules permanently. SREP is designed to run at
  boot and modify RAM, leaving SPI untouched.
- The `A01ODMDxeDriver` patches were **deliberately disabled** in the cfg —
  byte signatures from the A315-58/A315-57 SREP configs do not match this
  firmware (different revision); blindly applying them risks corrupting the
  Acer ODM gate that hides/shows hidden submenus.
- You assume **all responsibility**. The authors of this analysis are not
  liable for any damage.

## Source & references

- Insyde H2O BIOS internals + IFR opcode layout
- `SmokelessRuntimeEFIPatcher-RUS` README (Russian fork with extra ops)
- `SREP-Patches` repository, in particular the closely-related Acer Insyde
  unlock configs (`AA315-58`, `AA315-57`, `AV15-51`).
