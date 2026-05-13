# Acer Aspire A315-59 - BIOS Unlock Analysis

Analisis y desbloqueo de opciones avanzadas del BIOS Insyde H2O del Acer Aspire A315-59.

## Resumen

Este directorio documenta el analisis completo del BIOS Acer Aspire A315-59 v1.31 y proporciona un archivo `SREP_CONFIG.cfg` listo para usar con SmokelessRuntimeEFIPatcher para desbloquear menus ocultos.

## Estructura del directorio

```
acer-aspire-a315-59-bios-unlock/
  SREP_CONFIG.cfg              # Config para SmokelessRuntimeEFIPatcher (PRINCIPAL)
  README.md                    # Este archivo (overview)
  docs/
    BIOS_DOWNLOAD.md           # Procedimiento de descarga y extraccion del BIOS
    H2OFFT_CLI.md              # Argumentos CLI del flasher H2OFFT-Wx64.exe
    HOTKEYS.md                 # Combinaciones de teclas para desbloquear (resultados)
    BIOS_STRUCTURE.md          # Estructura UEFI y modulos relevantes
    SREP_USAGE.md              # Como usar SmokelessRuntimeEFIPatcher
  scripts/                     # Scripts Python de analisis
  analysis/                    # Strings extraidos de los modulos
```

## Modelo y BIOS analizados

- **Modelo**: Acer Aspire A315-59
- **Plataforma**: Intel Alder Lake mobile (CPU 906A3/906A4)
- **BIOS Vendor**: Insyde H2O
- **Version**: 1.31
- **URL de descarga**: https://global-download.acer.com/GDFiles/BIOS/BIOS/BIOS_Acer_1.31_A_A.zip
- **Hash SHA256 (HH5A4131.exe)**: 8a0884378b24f9817673e4feedadcd1e8631d56d1610ee0728a50a2e217b7d2a
- **Tamano abobios.bin**: 39,427,280 bytes (37 MB)

## Pipeline de analisis ejecutado

1. Descarga del ZIP de BIOS desde Acer (12.6 MB)
2. Descompresion: ZIP -> HH5A4131.exe (PE32+ x86_64 GUI)
3. Identificacion del EXE como 7-Zip SFX (firma \;!@Install@!UTF-8!\ en offset 0x39200)
4. Extraccion del 7z SFX -> 16 archivos incluyendo:
   - `abobios.bin` (37 MB, imagen UEFI completa)
   - `H2OFFT-Wx64.exe` (6.9 MB, flasher Insyde H2O)
   - `platform.ini` (config del flasher)
   - `H2OFFT64.sys` (driver kernel del flasher)
5. Analisis de `H2OFFT-Wx64.exe` con Capstone + extraccion strings -> argumentos CLI
6. UEFIExtract NE A74 sobre `abobios.bin` -> arbol completo (2933 directorios)
7. Identificacion de modulos clave (`SetupUtility`, `A01ODMDxeDriver`, etc)
8. Desensamblado de modulos PE32+ EFI con Capstone (CS_ARCH_X86, CS_MODE_64)
9. Conteo de patrones HII (SUPPRESS_IF TRUE, GRAYOUT_IF TRUE) en SetupUtility
10. Cross-referencia con SREP-Patches existentes -> generacion de SREP_CONFIG.cfg

## Resultados del analisis HII

El modulo `SetupUtility.efi` (FE3542FE-C1D3-4EF8-657C-8048606FF670, 1.4 MB) contiene:

| Patron | Significado | Ocurrencias |
|---|---|---|
| `0A 82 12 06` | SUPPRESS_IF TRUE + subtitle | 493 |
| `0A 82 12 86` | SUPPRESS_IF TRUE + subtitle (variant) | 357 |
| `0A 82 40 84` | SUPPRESS_IF TRUE + (otro) | 160 |
| `0A 82 14 0A` | SUPPRESS_IF TRUE + oneof 10-byte | 77 |
| `0A 82 14 08` | SUPPRESS_IF TRUE + oneof 8-byte | 43 |
| `19 82 ...` | GRAYOUT_IF TRUE (total) | 347 |

**Total: 1327 elementos HII ocultos por SUPPRESS_IF TRUE + 347 grayed-out.**

Estos elementos contienen menus avanzados como:
- OverClocking Performance Menu
- Voltage PLL Trim Controls
- CEP (Current Excursion Protection) Disable Menu
- VR ICCMAX Current Override
- Platform Voltages Overclocking
- Memory Overclocking Menu
- Uncore Overclocking Menu
- Advanced Debug Settings
- HD Audio Advanced Configuration
- PCI Express Configuration (Root Ports detallados)
- Intel Speed Shift Technology
- Race To Halt (RTH)
- ME ALT Disabled / SMBIOS type 130 OEM capabilities

## Verificacion previa para tu BIOS

Antes de aplicar el SREP_CONFIG.cfg, **VERIFICA** que los patrones existen en tu BIOS:

```bash
python3 scripts/verify_patterns.py /ruta/a/tu/SetupUtility.efi
python3 scripts/find_hii.py /ruta/a/tu/SetupUtility.efi
```

Si los conteos cambian respecto a este analisis, ajusta el cfg.

## Limitaciones y advertencias

1. **No se realizaron pruebas en hardware real**: este cfg se genero por analisis estatico.
2. **SREP opera en RAM**: no modifica el SPI flash. No puede brickear, pero parches incorrectos pueden colgar el sistema o causar comportamiento erratico.
3. **Garantia**: el simple acceso a setup avanzado no anula garantia, pero modificar voltajes/multiplicadores puede danar el procesador.
4. **Insyde puede haber agregado verificaciones runtime adicionales** (anti-tamper, BIOS Guard) que esten activas y bloqueen el patcher en SPI/SMM. Si SREP falla, revisa logs.
5. **Los patches HII unicamente desocultan items**: las opciones que dependan de variables NVRAM o EC tendrian que configurarse manualmente despues.

## Quick start (uso real)

1. Compila SREP desde https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS (o descarga release `SREP.efi`)
2. Crea un USB FAT32 con UEFI Shell + SREP.efi + SREP_CONFIG.cfg
3. Arranca el USB en modo UEFI
4. Ejecuta en el shell: `SREP.efi ENG` (para mensajes en ingles)
5. Si no hay errores, SREP cargara SetupUtility, aplicara parches en RAM y lanzara el Setup
6. En el Setup desbloqueado, navega a los menus avanzados (ahora visibles)
7. **NO uses 'Load Defaults' tras parchar**: revertiria los cambios solo si SREP no ha salvado y los menus quedaran inconsistentes.

## Referencias

- SmokelessRuntimeEFIPatcher: https://github.com/SmokelessCPUv2/SmokelessRuntimeEFIPatcher
- SREP-RUS (version usada): https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS
- SREP-Patches (ejemplos): https://github.com/Maxinator500/SREP-Patches
- UEFITool/UEFIExtract: https://github.com/LongSoft/UEFITool
- Capstone: https://www.capstone-engine.org/
- HII Internal Forms Representation Specification (UEFI spec)
