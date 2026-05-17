# Technical analysis — Acer Aspire A315-59 BIOS 1.31

## 1. Acquisition

```
URL: https://global-download.acer.com/GDFiles/BIOS/BIOS/BIOS_Acer_1.31_A_A.zip
SHA-size: 13,278,126 bytes
ZIP content: HH5A4131.exe  (13,410,600 bytes)
```

`HH5A4131.exe` is a **7-Zip SFX (self-extracting)** PE32+ x86-64 wrapper around
the actual Insyde flash payload:

```
7-Zip 23.01 listing:
  Comment:  7zS.sfx.exe  -  ProductName: 7-Zip  -  FileVersion: 4.57
  Inner Path = [0]  Type = 7z  Size = 13,175,272
```

Extracted with `7z x HH5A4131.exe` -> 16 files (51 MB total):

| File | Size | Role |
|---|---:|---|
| `H2OFFT-Wx64.exe` | 6.6 MiB | **Insyde H2O Flash Firmware Tool (Windows, GUI)** — the actual flasher (PDB path: `D:\Prog_Working\@Flash\H2OFFT_WINDOWS\FLSUI_R1\x64Release\InsydeFlash.pdb`) |
| `H2OFFT64.sys` | 47 KiB | kernel driver used by H2OFFT to access SPI |
| `H2OFFT.inf` / `H2OFFT.cat` | — | driver installation metadata |
| `FlsHook.exe` | 41 KiB | Pre/post-flash hook (HID/AC checks) |
| `FWUpdLcl.exe` | 220 KiB | **Intel CSME / ME local firmware update tool** (separate from BIOS flash) |
| `InterToolx64.efi` | 1.3 MiB | EFI variant of Insyde InterTool (factory/recovery) |
| `BiosImageProcx64.dll` | 280 KiB | helper DLL used by H2OFFT for image processing |
| `abobios.bin` | **37.6 MiB** | **the actual BIOS image** (the SPI flash content) |
| `platform.ini` | 65 KiB | declarative flash config (sections, error codes, switches) |
| `Microsoft.VC90.*`, `mfc90u.dll`, `msvcr90.dll`, `msvcp90.dll` | — | VC++ 2008 runtime |
| `Ding.wav` | 103 KiB | UI sound |

The flasher uses `platform.ini` to negotiate with the IHISI BIOS interface; the
real CLI of `H2OFFT-Wx64.exe` is **driven by `platform.ini`** at runtime, not
by hardcoded argv flags (it is the InsydeFlash GUI). See `CLI_REFERENCE.md`.

## 2. Disassembly (Capstone)

Capstone 5.0.7 was installed and used to disassemble the entry point and the
first 4 KiB of `.text` for every PE inside the payload (see
`/tmp/opencode/acer-bios/disasm/*.asm`). For the firmware modules extracted
in §3 the same script is used. Capstone setup:

```python
from capstone import Cs, CS_ARCH_X86, CS_MODE_64
md = Cs(CS_ARCH_X86, CS_MODE_64)
md.detail = False
```

PE parsing was done via `pefile`. The script lives at
`/tmp/opencode/acer-bios/disasm_pe.py`.

## 3. BIOS extraction (UEFIExtract NE alpha 74)

```
UEFIExtract abobios.bin all
```

Produces `abobios.bin.dump/` with 2,933 directories. The image is a normal
Insyde-style firmware:

- Volume `7BBB3E42-...` — NVRAM
- Volume `FFF12B8D-...` — PEI volume
- Volume `1FD0BACE-...` — main DXE volume, containing the compressed DXE
  archive (LZMA + EE4E5898 GUIDed section), which when expanded contains:
  - **`SetupUtility`** (1.4 MiB PE32+) — the BIOS Setup form
  - **`SetupUtilityApp`** (10 KiB) — the EFI launcher invoked to enter Setup
  - **`H2OFormBrowserDxe`** (347 KiB) — Insyde HII form browser
  - **`FrontPageDxe`** (51 KiB) — boot-time front page
  - **`A01ODMDxeDriver`** (30 KiB) — Acer ODM customization (hides menu items
    based on machine model)
- 4× microcode entries (Alder Lake ULP family `0x906A1/2/3/4`)

This confirms the platform is **Intel Alder Lake-U** (12th-gen) and the BIOS
is **Insyde H2O**.

## 4. IFR / HII analysis

The Setup form in Insyde firmware is built from IFR opcodes inside
`SetupUtility.efi`. Items are hidden using `SuppressIf` (`0x0A`) followed by
a comparison against an `AccessLevel` token. The general pattern is:

```
0A 82 14 08 XX 00 01 00 01 00     ; SuppressIf  AccessLevel(byte) == 0xXX
0A 82 14 0A XX 00 02 00 00 00 01 00 ; SuppressIf  AccessLevel(word) == 0xXX
```

By replacing the comparison with a `TrueOpcode` of equal length:

```
0A 82 47 08 00 00 00 00 00 00
0A 82 47 0A 00 00 00 00 00 00 00 00
```

… the `SuppressIf` becomes `SuppressIf(TRUE)` -> never suppressed -> the item
becomes visible.

### Quantitative findings in our `SetupUtility.efi`

```
Module size: 1,402,112 bytes

Pattern                                      Occurrences
0A 82 14 08 (byte-EqVal SuppressIf)          43
0A 82 14 0A (word-EqVal SuppressIf)          77
0A 82 12 06 (generic SuppressIf)            493
0A 82 12 86 (SuppressIf w/ word StringRef)  357
0A 82 46 02 (SuppressIf w/ Or)               22
19 82 14 08 (GrayoutIf AccessLevel)           1
```

Unique AccessLevel IDs discovered (byte form):
`01 02 03 10 11 1A 1B 1C 1D 1E 1F 20 21 23 24 25 26 27 29 2A 2B 2C 2D 2E 39 3A 3B 3C 3D 3E 3F 40`

Unique AccessLevel IDs discovered (word form):
`04 07 0B 0D 0F 21 24 25 26 27 28 29 2A 2B 2C 2D 2E 2F 30 31 32 33 34 35 36 37 38 41 42 43 44 45 46 47 48 49`

Each of these has been emitted as an `Op Patch` block in `SREP_CONFIG.cfg`.

### Cross-validation with AA315-58 SREP config

47 out of 67 byte-exact patterns from
`SREP-Patches/Configs/Acer/AA315-58(514-54, 515-56)&EX215-54_Insyde_BiosUnlock.cfg`
match this BIOS verbatim (the A315-58 is the previous generation; many
AccessLevel IDs are stable, others shifted). The non-matching 20 patterns are
either:

- Renumbered AccessLevels (e.g. A315-58 used `08`, `18`, `19`, `22`, `28`,
  `38` — we instead see `01`, `02`, `03`, `10`, `11`, `1F` and additional
  `20–28`)
- Different string-ref encoded items (the `0A 82 12 86 ?? ?? ?? ?? 17 02`
  family — the StringRef IDs differ between SKUs)

The patches in our generated cfg are the **superset that actually matches
this firmware**.

## 5. A01ODMDxeDriver

The A01ODM DXE driver is Acer's ODM customization gate. In AA315-58 it is
patched with three families of signatures:

| Family | Generic pattern | Matches in A315-58 | Matches in A315-59 |
|---|---|---:|---:|
| ODM Default | `000084C0740C` -> `000084C0EB0C` | yes | **0** |
| ODM Combo (Setup hide) | `74??4084CD` | yes | **0** |
| ODM Combo (TravelMate) | `85C0745E`, `85C0744F`, `85C0744D` | yes | **0** |
| ODM Single | `4184C?74??`, `?484C974??`, `A80174??`, `403ACD75??`, `4584D274??` | yes | partial (`A801 74??` = 2, others ≠) |

The compiler/build of the A01ODM driver in v1.31 evidently changed enough
that the byte signatures from older revisions no longer apply. For safety
**the cfg leaves this section commented out**. Anyone wanting to enable it
must:

1. Disassemble `modules/A01ODMDxeDriver.efi` with Capstone/IDA.
2. Find the function that returns the "is hidden menu enabled" flag.
3. Patch the conditional jump after the boolean test.

## 6. Tooling

| Tool | Source | Used for |
|---|---|---|
| **Capstone** (`pip install capstone`) | <https://www.capstone-engine.org/> | x86-64 disassembly of all PEs/EFI modules |
| **UEFIExtract NE A74** | <https://github.com/LongSoft/UEFITool/releases/tag/A74> | BIOS tree dump |
| **7-Zip 23.01** | system | unpack SFX |
| `pefile`, `binascii`, `re` (Python stdlib) | — | pattern hunting |
| **SREP** | <https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS> | runtime-apply the patches |
| **SREP-Patches** examples | <https://github.com/Maxinator500/SREP-Patches> | reference patterns |
