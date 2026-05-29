# Pipeline completo de extracción y análisis del BIOS

Este documento describe **paso a paso, reproducible** todo lo que se hizo
para llegar desde el ZIP del BIOS de Acer hasta el `SREP_CONFIG.cfg`.
Todo se ejecutó en `linux x86_64`.

## 0. Dependencias

```bash
sudo apt install -y unzip p7zip-full python3-pip
pip3 install capstone pefile
# UEFITool / UEFIExtract (release A74, Linux x64):
curl -L -o uefiextract.zip \
  https://github.com/LongSoft/UEFITool/releases/download/A74/UEFIExtract_NE_A74_x64_linux.zip
unzip uefiextract.zip
chmod +x uefiextract
```

## 1. Descargar el BIOS

```bash
curl -L -A "Mozilla/5.0" -o bios.zip \
  "https://global-download.acer.com/GDFiles/BIOS/BIOS/BIOS_Acer_1.31_A_A.zip?acerid=638876442878329689&Step1=&Step2=&Step3=ASPIRE%20A315-59&OS=ALL&LC=es&BC=ACER&SC=EMEA_11y"
unzip bios.zip      #  -> HH5A4131.exe
```

`HH5A4131.exe` es **PE32+ GUI x86-64**, internamente un **SFX 7-Zip**:

```
;!@Install@!UTF-8!
RunProgram="H2OFFT-Wx64.exe -sfx7z %%S "
```

## 2. Extraer el contenido del instalador

```bash
7z x HH5A4131.exe -osfx_out
```

Resultado (16 ficheros, 51 MB descomprimidos):

| Archivo                       | Tipo                                          |
|-------------------------------|-----------------------------------------------|
| `H2OFFT-Wx64.exe`             | Insyde Flash Tool v6.62 (PE32+ x86-64)        |
| `H2OFFT64.sys`                | Driver kernel firmado WHQL                    |
| `H2OFFT.inf` / `H2OFFT.cat`   | Instalador del driver                         |
| `FlsHook.exe` / `FWUpdLcl.exe`| Helpers (FW update local, hook)               |
| `InterToolx64.efi`            | "Intermediate Tool" UEFI                      |
| `BiosImageProcx64.dll`        | Procesado de imagen OEM                       |
| `abobios.bin`                 | **BIOS / cápsula firmware** (37.6 MB)         |
| `platform.ini`                | Config completa Insyde (1319 líneas, muy útil) |
| `mfc90u.dll`, `msvcp/r90.dll` | Runtime MSVC 2008                             |
| `Ding.wav`                    | Sonido de fin de flash                        |

## 3. Desensamblar `H2OFFT-Wx64.exe` con Capstone

Objetivo: documentar la **CLI real** del flasher, no la que aparece en
"H2OFFT-Wx64.exe -h" (truncada y traducida).

```python
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_64

pe = pefile.PE("H2OFFT-Wx64.exe")
md = Cs(CS_ARCH_X86, CS_MODE_64)
text = next(s for s in pe.sections if s.Name.startswith(b'.text'))
ib = pe.OPTIONAL_HEADER.ImageBase
for ins in md.disasm(text.get_data(), ib + text.VirtualAddress):
    # ... busca `lea reg, [rip+disp]` cuyo target cae dentro de .rdata
    #     y es una UTF-16LE empezando por '-' o '/'
```

**Las strings son UTF-16LE** (MFC). Hay que decodificar pares
`<byte> 0x00 <byte> 0x00 ...` para encontrar los tokens. Resultado: ver
[H2OFFT_CLI_REFERENCE.md](H2OFFT_CLI_REFERENCE.md).

## 4. Extraer el BIOS con UEFIExtract

```bash
./uefiextract abobios.bin
# Genera:
#   abobios.bin.dump/            <- árbol completo
#   abobios.bin.report.txt       <- mapa FIT + microcode + BIOS Startup
#   abobios.bin.guids.csv        <- todos los GUIDs encontrados
```

Hallazgos clave (de `.report.txt`):

```
00000000FFC00060h | Microcode | CpuSignature: 00090671h (Alder Lake-N)
00000000FFC2E060h | Microcode | CpuSignature: 000906A1h
00000000FFC5B060h | Microcode | CpuSignature: 000906A2h
00000000FFC8D860h | Microcode | CpuSignature: 000906A3h
00000000FFD3D000h | BIOS Startup Module
...
```

> CpuSignature `00090671h` confirma **Intel Alder Lake-N** (familia 6,
> modelo 0xBE) -> consistente con A315-59.

Módulos críticos localizados:

```
abobios.bin.dump/7 1FD0BACE-.../0 20BC8AC9-.../0 EE4E5898-.../1 Volume.../0 8C8CE578-.../
    128 SetupUtilityApp/             # launcher
    266 SetupUtility/                # menú Setup (1.4 MB - el que se patchea)
    311 TrustedDeviceSetupApp/
    370 A01ODMDxeDriver/             # ODM hide rules (30 KB - el que se patchea)
    371 A01ODMSmmServiceDriver/      # ODM SMM service
```

## 5. Identificar patrones IFR / código a patchear

### 5.1 SetupUtility (IFR data)

Las opciones del Setup están en formato **HII / IFR** (UEFI Forms). El
opcode clave es `EFI_IFR_SUPPRESS_IF` (`0x0A 0x82` en builds Insyde
modernos donde se usa la combinación `0A 82 14 08` o `0A 82 12 06`).

El **truco SREP** consiste en sustituir la expresión interna por
`EFI_IFR_TRUE_OP (0x47)`, que convierte la condición de supresión en
"true / no suprimir nunca". Patrones:

| Patrón a buscar (regex hex)                          | Sustituir por           | Qué desbloquea          |
|------------------------------------------------------|-------------------------|-------------------------|
| `FFFF00......821206......000F0F`                     | `FFFF0001000A8247`      | OC Perf Menu, Plat Volt |
| `19821408....01000.000591`                           | `198247`                | OC Feature              |
| `2902290229020A821206....0.00`                       | `2902290229020A8247`    | XTU / FIVR              |

Validación con regex sobre `SetupUtility/2 PE32 image section/body.bin`:

```
[OC Perf Menu]  -> 12 matches  (first @ 0xC8D5D)
[OC Feature]    ->  1 match    (@ 0xD02B6)
[XTU / FIVR]    -> 13 matches  (first @ 0xC990A)
```

### 5.2 A01ODMDxeDriver (código)

Acer añade verificaciones "ODM" que retornan `EFI_UNSUPPORTED 0x80...03`
para esconder menús. Capstone confirma:

```
0x000015aa: e8 35 00 00 00    call    short_func
0x000015b0: 3c 01             cmp     al, 1
0x000015b2: 74 0c             je      0x15c0   <-- ODM Default JE
0x000015b4: 48 b8 03 00 00 00 movabs  rax, 0x8000000000000003   ; EFI_UNSUPPORTED
            00 00 00 80
0x000015be: eb 25             jmp     0x15e5
0x000015c0: ...continúa con la rama "permitido"...
```

El **patch** `Op FastPatch 740C... -> EB0C...` cambia el `JE` por un
`JMP` incondicional al destino "permitido". Limpio y reversible.

Otros patches confirmados (ver `../Acer_A315-59/SREP_CONFIG.cfg`):

```
@0x5882: test r14b,al ; je +0x0D    -> patch ODM Single 1
@0x597E: test r14b,al ; je +0x16    -> patch ODM Single 1 (otra rama)
@0x4048: test r9b,r9b ; je +0x3A    -> patch ODM Single 2
@0x3CCA: test r10b,r10b; je +0x15   -> patch ODM Single 5
```

## 6. Generar el `SREP_CONFIG.cfg`

Base usada: `Maxinator500/SREP-Patches NewConfigs/Acer/Acer_202x.cfg`
(es el config "Acer laptops since 2020 InsydeH2O" del autor, el más
moderno y abarca toda la serie).

Validado en este BIOS con `verify_patterns.py` (capstone + regex). Ver
[../Acer_A315-59/SREP_CONFIG.cfg](../Acer_A315-59/SREP_CONFIG.cfg).

## 7. Clonado de los repos de SREP

```bash
git clone --depth=1 https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS.git
git clone --depth=1 https://github.com/Maxinator500/SREP-Patches.git
```

El primer repo es el **patcher** (driver UEFI/Shell que aplica los hex
patches en memoria antes de saltar al SetupUtility original).

El segundo repo es la **biblioteca de configs** por modelo. Su README
lista el config `Acer_202x.cfg` como válido para "Acer laptops since
2020 (InsydeH2O)" - **categoría a la que pertenece el A315-59**.

## 8. Resumen del flujo SREP_CONFIG

```
+--------------------------+
| UEFI Shell ejecuta SREP  |
+-----------+--------------+
            v
Op Loaded SetupUtility            <- buscar módulo cargado en RAM
            v
Op Patch  (FFFF...821206... -> FFFF0001000A8247)
Op Patch  (19821408...591  -> 198247)
Op Patch  (2902...821206... -> 2902...8247)
            v
Op Loaded A01ODMDxeDriver         <- buscar driver ODM en RAM
            v
Op FastPatch (740C...80 -> EB0C...80)
Op Patch     (...84C974.. -> ...84C97400)  [batch]
Op Patch     (4584D274.. -> 4584D27400)
            v
Op LoadFromFV SetupUtilityApp     <- carga app de menú
            v
Op Exec                           <- arranca el menú ya desbloqueado
```

## 9. Scripts utilizados (incluidos en el repo)

* [`../scripts/disasm_h2offt.py`](../scripts/disasm_h2offt.py) - desensamblado
  Capstone + extracción de tokens CLI.
* [`../scripts/dump_help.py`](../scripts/dump_help.py) - volcado del bloque
  de ayuda continuo.
* [`../scripts/verify_patterns.py`](../scripts/verify_patterns.py) -
  validación de los patrones SREP contra el BIOS.
* [`../scripts/disasm_odm.py`](../scripts/disasm_odm.py) - desensamblado
  alrededor de los sitios de patch del módulo ODM.

Todos son auto-contenidos (`python3 script.py`).
