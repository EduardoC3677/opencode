# RUN_LOG.md — automated pipeline

Workspace (scratch): `/tmp/opencode/acer-bios/`
Workspace (final artefacts): `acer-a315-59-bios-mod/` (this directory)

## Steps performed

1. ✅ Created scratch directory `/tmp/opencode/acer-bios/`.
2. ✅ Downloaded `BIOS_Acer_1.31_A_A.zip` (12.6 MiB) from
   `global-download.acer.com`.
3. ✅ Unzipped → obtained `HH5A4131.exe` (13.4 MiB).
4. ✅ Identified `HH5A4131.exe` as **7-Zip SFX (7zS v4.57)** wrapper.
5. ✅ Extracted inner archive with `7z x` → 16 files including
   `abobios.bin`, `H2OFFT-Wx64.exe`, `FWUpdLcl.exe`, etc.
6. ✅ Installed **Capstone 5.0.7** + **pefile 2024.8.26** via `pip`.
7. ✅ Wrote `/tmp/opencode/acer-bios/disasm_pe.py` (Capstone PE
   disassembler) and ran it on every PE/EFI binary →
   `/tmp/opencode/acer-bios/disasm/*.asm`.
8. ✅ Mined `strings -n 3` and `strings -el` (UTF-16) for CLI tokens;
   correlated with `platform.ini` config sections → documented in
   `CLI_REFERENCE.md`.
9. ✅ Downloaded **UEFIExtract NE alpha 74 (Linux x64)** from LongSoft
   GitHub release.
10. ✅ Ran `uefiextract abobios.bin all` → 2,933 directories of dumped
    tree.
11. ✅ Located key DXE modules: `SetupUtility`, `SetupUtilityApp`,
    `H2OFormBrowserDxe`, `FrontPageDxe`, `A01ODMDxeDriver`. Extracted
    their PE32+ bodies into `/tmp/opencode/acer-bios/modules/*.efi`.
12. ✅ Disassembled each module with Capstone (entry-point window).
13. ✅ Searched the modules for hotkey strings (`Ctrl+`, `Alt+`, `Fn+`,
    `F[0-9]+`, `Tab`, `Press`, `Advanced`, `AccessLevel`, …) — see
    `HOTKEYS.md`. Conclusion: **no hotkey unlock exists** on this BIOS;
    advanced items are gated by `SuppressIf [AccessLevel]` IFR opcodes.
14. ✅ Enumerated all `SuppressIf AccessLevel` IFR patterns in
    `SetupUtility.efi`:
    - 32 distinct byte-EqVal levels (0x01–0x40)
    - 36 distinct word-EqVal levels (0x04–0x49)
15. ✅ Cloned reference repositories:
    - `SmokelessRuntimeEFIPatcher-RUS` — read README + INF spec
    - `SREP-Patches` — examined ~30 Acer Insyde cfg files
16. ✅ Cross-validated against
    `Configs/Acer/AA315-58(514-54, 515-56)&EX215-54_Insyde_BiosUnlock.cfg`:
    **47 out of 67 patterns matched byte-exact** → confirms the same IFR
    structure with a few renumbered AccessLevel IDs.
17. ✅ Generated **`SREP_CONFIG.cfg`** containing the **full set of
    patches that actually match this BIOS** (32 byte-level + 36 word-level
    `SuppressIf -> TrueOpcode` substitutions).
18. ✅ Verified that all generated patterns exist exactly in
    `SetupUtility.efi`.
19. ✅ Inspected `A01ODMDxeDriver.efi` — concluded that the byte
    signatures used for older A315-58 cfg do NOT match this v1.31 build.
    The ODM section in `SREP_CONFIG.cfg` is therefore **left commented
    out** (documented as a known TODO requiring manual disassembly).
20. ✅ Wrote documentation (`README.md`, `ANALYSIS.md`,
    `CLI_REFERENCE.md`, `HOTKEYS.md`, this `RUN_LOG.md`).
21. ✅ All artefacts ready under `acer-a315-59-bios-mod/`.

## Assumptions made (no user input was requested per CI rules)

- Working scratch directory: `/tmp/opencode/acer-bios/` (NOT in repo).
- Final artefacts uploaded into the current repository
  (`/home/runner/work/opencode/opencode/acer-a315-59-bios-mod/`) — pushing
  to third-party repos was intentionally NOT performed (those repos belong
  to `Maxinator500` and require their credentials).
- The generated `SREP_CONFIG.cfg` is explicitly labelled **UNTESTED**.
- The `A01ODMDxeDriver` patch is left disabled for safety.

## Pending / future work

- Hardware test on a real A315-59 (only viable safe step is to boot SREP
  from a USB key and verify the Advanced menu appears after F2).
- Reverse-engineer the new `A01ODMDxeDriver.efi` to derive the correct
  byte signature for this firmware revision.
- Extend cfg with patches for the `0A 82 12 86` / `0A 82 46 02`
  StringRef-gated `SuppressIf`s if specific submenu items remain hidden
  after Stage 1.

## Files generated (in this repo)

```
acer-a315-59-bios-mod/
├── SREP_CONFIG.cfg     # The actual SREP patch file (drop on FAT32 USB)
├── README.md           # Top-level overview + how to use
├── ANALYSIS.md         # BIOS internals + pattern derivation
├── CLI_REFERENCE.md    # CLI of H2OFFT / FWUpdLcl / FlsHook / etc.
├── HOTKEYS.md          # Hotkey research + advanced-unlock conclusion
└── RUN_LOG.md          # This file
```

## Files generated (scratch, not committed)

```
/tmp/opencode/acer-bios/
├── bios.zip            # Original download
├── extracted/          # Unzip of bios.zip
│   ├── HH5A4131.exe    # 7z SFX wrapper
│   └── inner/          # 7z payload (abobios.bin + H2OFFT + ...)
├── abobios.bin         # 39 MiB firmware image
├── abobios.bin.dump/   # UEFIExtract output (2,933 dirs)
├── modules/            # Isolated PE32+ bodies of key modules
├── disasm/             # Capstone disassemblies
├── uefi_tool/          # UEFIExtract binary
├── SREP-Patches/       # Cloned reference patches
├── SmokelessRuntimeEFIPatcher-RUS/  # Cloned SREP source
└── disasm_pe.py        # Capstone disassembler helper script
```
