# CLI reference — executables inside `HH5A4131.exe`

> The 7-Zip SFX `HH5A4131.exe` itself accepts **standard 7-Zip SFX switches**
> (it is built from `7zS.sfx.exe` v4.57). Useful ones:
>
> | Switch | Meaning |
> |---|---|
> | `-y` | silent; assume "yes" to all prompts |
> | `-o<dir>` | extract to `<dir>` (rarely supported in 7zS builds; use `7z x` instead) |
> | `-gm2` | run extraction in "always-extract" mode |
>
> You can also extract it explicitly with: `7z x HH5A4131.exe`.

---

## `H2OFFT-Wx64.exe`  (Insyde H2O Flash Firmware Tool, Windows GUI)

**Confirmed by PDB path embedded in the binary:**

```
D:\Prog_Working\@Flash\H2OFFT_WINDOWS\FLSUI_R1\x64Release\InsydeFlash.pdb
```

This is the **GUI** variant of InsydeFlash. Its behavior is driven by
`platform.ini` (in the same folder), not by command-line switches. The
relevant `platform.ini` sections include:

```
[CommonFlash]          [AC_Adapter]          [AutoWakeup]
[Bios_Version_Check]   [BIOSVersionFormat]   [CapsuleAudit]
[FactoryCopy]          [FDFile]              [FlashComplete]
[FlashSecureBIOSOverride] [ForceFlash]       [Log_file]
[MessageStringTable]   [MULTI_FD]            [Option]
[Others]               [ParamForBiosReference][PassToBios]
[PasswordCheck]        [PermitFlashConditionalData][PermitFlashVersion]
[Platform_Check]       [PlatformVersion]     [PreFlash]
[Region]               [ReturnCodeDefinition][ReturnErrorCode]
[SecureUpdate]         [UI]                  [UpdateEC]
[UpdateExtraData]      [UpdateOEMME]         [UpdateDeviceFirmware]
[Version]
```

The `[CommonFlash]` `SwitchString` field encodes the run-time protection flags
(parsed by IHISI, **not** by argv):

| Token | Meaning |
|---|---|
| `PTEN` | All protection ENABLED |
| `PTDIS` | All protection DISABLED |
| `ACEN` / `ACDIS` | AC adapter check enable/disable |
| `DCEN` / `DCDIS` | DC + Gas-gauge check enable/disable |
| `RESSEN` / `RESSDIS` | BIOS regression check enable/disable |
| `PJMDEN` / `PJMDDIS` | Project/Model name check enable/disable |
| `FHOS` | After flash, boot back to OS |
| `FHST` | After flash, **shut down** |
| `FHRST` | After flash, **reboot** |
| `CPVER:[N]` | Common-Flash protocol version |

Default switchstring in this BIOS:

```
CPVER:[1] ACEN DCEN RESSEN FHRST PJMDEN
```

**Recognized command-line flags** (extracted by `strings`+disassembly):
the binary does call `CommandLineToArgvW` but only consumes the standard
Insyde GUI options (silent mode, log file path, `/F` to point to a different
firmware). It does **not** expose a real CLI like H2OFFT-Sx64.efi does. The
public Insyde documentation for the **shell** variant (`H2OFFT-Sx64.efi`,
not present here) gives the canonical CLI:

| Switch | Effect |
|---|---|
| `-bios <file>` | flash only BIOS region |
| `-all <file>` | flash full image |
| `-me <file>` | flash ME region |
| `-ec <file>` | flash EC region |
| `-rb` | reboot after flash |
| `-s` | silent |
| `-nr` | no reboot |
| `-nc` | no version check |
| `-cap` | use UEFI capsule path |
| `-pwd <pwd>` | BIOS password |
| `-pa <addr>` | flash at physical address |
| `-q` | quiet |

These ARE the strings that the Windows GUI variant passes internally to the
IHISI flash service after parsing `platform.ini`.

---

## `FWUpdLcl.exe`  (Intel CSME / ME Firmware Update Tool, 32-bit console)

Real CLI — **documented in its own embedded help text**:

```
Usage:
  FWUpdLcl.exe -f <upd.bin>
      Update the FW via MEI with the bin file provided.
  FWUpdLcl.exe -f <upd.bin> -partid <wcod|locl>
      Perform a partial update of the FW via MEI.
  FWUpdLcl.exe -fwver
      Display the FW Version of current FW.
```

Full parameter dictionary (extracted from the parser tables in `.rdata`):

| Param | Type | Purpose |
|---|---|---|
| `-f <file>` / `/f <file>` | path | firmware image |
| `-partid <wcod|locl>` | enum | partial update id |
| `-fwver` | flag | print FW version and exit |
| `-allowsv` | flag | allow same-version update (otherwise error 9) |
| `-verbose` | flag | verbose logging |
| `-save <file>` | path | save current FW image to file |
| `-generic` | flag | use generic OEM signing |
| `-oemid <hex>` | u32 | force OEM identifier |
| `-EXP` (or `/EXP`) | flag | print example usages and exit |

Error codes (selected): `1 invalid usage`, `2 timeout`, `3 internal`,
`4 invalid image`, `5 integrity`, `6 SKU mismatch`, `8 version mismatch`,
`9 same-version requires -allowsv`, `10 last-status read fail`, etc.

---

## `FlsHook.exe`  (pre/post-flash hook)

64-bit PE. Strings show only generic helper text. It is invoked by H2OFFT
between phases (AC detection / FW image validation). It accepts no public
CLI — it is launched programmatically by the flasher.

## `InterToolx64.efi`  (EFI factory recovery tool)

EFI DLL, 1.3 MiB. Launched from the EFI shell or via Insyde's recovery
front-page (`<Fn>+<Esc>` boot path). Recognized strings show no exposed
public CLI — it operates via NVRAM variables set by the BIOS itself.

## `BiosImageProcx64.dll`

Helper DLL used by `H2OFFT-Wx64.exe` for capsule-format / image manipulation
(splitting BIOS / ME / EC regions, computing checksums). Not invoked
directly; exposes a C export surface to its host process.

---

## Disassembly artefacts

Capstone disassemblies for every binary are stored in
`/tmp/opencode/acer-bios/disasm/`:

```
BiosImageProcx64.dll.asm    H2OFormBrowserDxe.efi.asm
FrontPageDxe.efi.asm        H2OFFT-Wx64.exe.asm
FlsHook.exe.asm             InterToolx64.efi.asm
FWUpdLcl.exe.asm            SetupUtility.efi.asm
                            SetupUtilityApp.efi.asm
```

(They are temporary scratch and not committed to the repo because of size.)
