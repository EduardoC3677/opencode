# H2OFFT-Wx64.exe - referencia de CLI

> Documento extraído mediante **desensamblado con Capstone** y volcado de
> strings UTF-16 de `.rdata` del binario `H2OFFT-Wx64.exe` v6.62 que viene
> dentro del instalador `HH5A4131.exe` del BIOS 1.31 del Acer Aspire A315-59.

## Identificación del binario

| Campo             | Valor                                                |
|-------------------|------------------------------------------------------|
| Tipo              | `PE32+ executable (GUI) x86-64, for MS Windows`      |
| Compilador        | MSVC, MFC (mfc90u.dll), MSVCR90                      |
| ImageBase         | `0x140000000`                                        |
| EntryPoint        | `0x14001A6BC`                                        |
| Sections          | `.text 0x231D8`, `.rdata 0xEB42`, `.data 0x3C08`, `.pdata 0x3384`, `.rsrc 0x1650` |
| Versión           | `H2OFFT (Flash Firmware Tool) Version 6.62`          |
| Driver kernel     | `H2OFFT64.sys` (Insyde, firmado WHQL)                |
| Manifest          | `requireAdministrator`                               |
| Distribución      | SFX 7-Zip `7zS.sfx` (`;!@Install@!UTF-8! RunProgram="H2OFFT-Wx64.exe -sfx7z %%S "`) |

## Metodología de extracción de la CLI

1. Cargamos el PE con `pefile`.
2. Extraemos todas las **cadenas Unicode UTF-16LE** de `.rdata` (la app es
   MFC y las constantes son `L"..."`).
3. Desensamblamos `.text` con `Capstone` (`CS_ARCH_X86 / CS_MODE_64`) y
   buscamos `LEA reg, [rip + disp]` cuyas referencias apuntan a esas
   cadenas.
4. Filtramos las que empiezan por `-`, `/` o terminan en `:` -> tokens CLI.
5. Localizamos la función `ProcessArgument` / `ShowUsage` / `ShowOemDUsage`
   (sus nombres aparecen como literales en `.rdata` y los usa la
   propia app para emitir errores).

Scripts utilizados (incluidos en este repo en `/tmp/opencode/` durante el
análisis, reproducibles):

* `disasm_h2offt.py` - desensambla `.text`, resuelve referencias LEA -> string,
  filtra tokens CLI.
* `dump_help.py` / `dump_help2.py` - extrae el bloque continuo de ayuda
  ASCII/UTF-16LE de `.rdata`.

## Resumen del help oficial (extraído del propio binario)

```
H2OFFT-W   v6.62   (Insyde H2O Flash Firmware Tool)

-h                Show help.
-b                Force suspend BitLocker.
-ecp              Update non-share EC block by block.
-edtcdw:"0x12345678"  Verify/inspect data using edt (Engineering Debug Tool).
-extrfd OUT_PATH  Extract BIOS file from single package to OUT_PATH.
-forceit          Skip BIOS version check.
-forcetype        Skip model name check.
-g [FILE]         Read current ROM and save to file. (a.k.a. dump/backup)
-logoupdate:FILENAME  Update logo by input file.
-mfg              Tell BIOS current run is in manufacture mode.
-n                Do not reboot after flash.
-noconfirm        Do not popup flash confirm dialog.
-OemCus           Tell BIOS to do OEM customization feature.
-pq               Query BIOS protection region MAP in current ROM.
-pr               Query external region MAP in current ROM.
-priv             Query BIOS private region MAP in current ROM.
-pw               Query whole region MAP in current ROM.
-pwd:PASSWORD     Input password for features that need a password.
                  NOTE: Requires -s option. Only available with -s.
-s                Run as silent mode (no UI, used by service installers).
-l=LOG_PATH       Define a specific path for log file.

(internal / undocumented flags discovered by string + xref scan):
-dbg              Enable debug log.
-dbgndt           Enable debug log w/o date-time.
-sdbg             Sticky/super debug log.
-db               Database / debug toggle.
-ini:FILE         Override platform.ini path.
-noini            Do NOT load any .ini config.
-iv               Show IHISI version (Insyde SMI version).
-ecver:VERSION    Override EC version string.
-extec:FILE       Update external EC firmware from FILE.
-pbi:FILE         Provide partial BIOS image.
-dfAC:VAL         "DC offset / AC" debug flag.
-secondlogo       Update secondary OEM logo.
-alp              Enable additional logging path.
-unvs             Unconditional NVS / variable store update.
-ppriv            Private region update.
-p=...            Generic param (mirrors -pwd in some builds).
-dumpwriterom     Dump the buffer the tool would have written to ROM.
-qatest           QA / regression test mode.
-sfx7z PATH       Internal flag used by the 7-Zip SFX wrapper.
-base:HEX         Together with -size, defines a region for -edt.
-size:HEX         Region size for -edt.
-edt##:           Edt sub-command (e.g. -edtcdw, -edtcdb, -edtcdb).
-ft:TYPE          Force flash TYPE (protected region byte value, hex).
-OemCus           Run OEM-specific custom flow.
-ec_crisis        EC crisis recovery flow.
-ft:TYPE          Forced flash type (TYPE is hex byte of the protected region).
-all              Flash ALL regions.
-pi               Query (privileged info? - used internally).
-pr               Query external region MAP.
-priv             Query private region MAP.
-pq               Query protection MAP.
-pw               Query whole MAP.
```

## Tabla completa de tokens CLI detectados

Tokens encontrados como cadenas literales y referenciados por `LEA rip+disp`
desde `.text` (filtrados a aquellos que están en la cadena de `ProcessArgument`
y `CheckArgumentWithCommandFilter`):

```
-h            -b            -s            -n            -g
-mfg          -all          -iv           -ecp          -pq
-pi           -pr           -pw           -priv         -ppriv
-noconfirm    -nopause      -forceit      -forcetype    -extrfd
-extec:       -ecver:       -ft:          -edt          -edt##:
-edtcdw:      -base         -base:        -size         -size:
-pbi:         -dfAC:        -logoupdate:  -OemCus       -pwd:
-l=           -p=           -ini:         -noini        -dbg
-dbgndt       -sdbg         -db           -secondlogo   -alp
-unvs         -dumpwriterom -qatest       -sfx7z        -ec_crisis
```

## Ejemplos de uso prácticos para este BIOS

```cmd
:: Backup del flash entero (recomendado antes de cualquier modificación):
HH5A4131.exe -g backup_A315-59_v131.fd

:: Flasheo silencioso (requiere password si la NVRAM lo tiene):
H2OFFT-Wx64.exe abobios.bin -s -pwd:"MiPassword" -forceit

:: Extraer el .fd del paquete sin flashear:
H2OFFT-Wx64.exe -extrfd .\out abobios.bin

:: Query del mapa de regiones de la flash:
H2OFFT-Wx64.exe -pw
H2OFFT-Wx64.exe -pr      # solo region externa
H2OFFT-Wx64.exe -priv    # region privada (Insyde)

:: Modo log + log path personalizado + sin reboot:
H2OFFT-Wx64.exe abobios.bin -n -l=C:\temp\h2offt.log -dbg
```

## SFX wrapper

`HH5A4131.exe` es un **7-Zip SFX** con header:

```
;!@Install@!UTF-8!
RunProgram="H2OFFT-Wx64.exe -sfx7z %%S "
;!@InstallEnd@!
```

`%S` recibe los parámetros adicionales que se le pasen al EXE de Acer, los
prefija con `-sfx7z` y se los entrega tal cual a `H2OFFT-Wx64.exe`. Por
tanto, **cualquier argumento documentado arriba** se puede pasar
directamente al EXE de Acer y funcionará igual:

```cmd
HH5A4131.exe -g backup.fd       :: backup
HH5A4131.exe -extrfd .\out      :: extraer
HH5A4131.exe -s -forceit -n     :: flasheo silencioso
```

## Anatomía interna (relevant para mods)

* GUID Insyde-Flash : `F93936B2-EFE2-4dd8-8479-FC9356C8921F`
* Eventos Windows usados por el flasher:
  * `WinFlashEvent_StopAp     {8BB6122C-34EA-4723-86AD-0C4DFBFE863F}`
  * `WinFlashEvent_EnableHook {EF24BA92-762A-4d2d-BE04-AD4F9840CDB9}`
  * `WinFlashEvent_DisableHook{91A786CA-94A7-442a-9E2A-EC0561C8B930}`
* IHISI SMI commands implementados:
  `Ihisi_48h`, `Ihisi_4Dh`, `Ihisi_80h`, `Ihisi_81h`, `Ihisi_82h`,
  `Ihisi_83h`, `Ihisi_84h` (auth lock/unlock, capsule flash, etc.).
* Helpers documentados (descubrir flujo "secure capsule"):
  `SMI_SecureCapsuleFlash_New`, `SMI_SpecificCheckByBios`,
  `SMI_PassCapsuleBlock`, `SMI_IhisiAuth*`.
* Hooks externos: `FwUpdLcl.exe` (`-f %s -generic -allowsv`) -> usado para
  actualizar Intel ME via la herramienta de Intel.
* BiosImageProc DLL : `BiosImageProcx64.dll`, expuesta para procesar la
  imagen antes de pasar al SMI; sirve para custom OEM logic (Acer la usa).
* Acceso a BitLocker (Windows): suspende/resume via PowerShell
  (`Suspend-BitLocker -MountPoint "%c:"`).

Estos identificadores son útiles si en lugar de SREP en runtime se quiere
hacer un mod permanente (re-firmar la cápsula, parchear módulos in-flash).
