# `bios_mod/` - Mod del BIOS del Acer Aspire A315-59

Resultado completo del trabajo solicitado en la issue **"Mod acer"**:

> Descargar el BIOS oficial del Acer Aspire A315-59 (versión 1.31), extraer
> el instalador `HH5A4131.exe`, sacar la cápsula UEFI `abobios.bin`,
> desensamblar `H2OFFT-Wx64.exe` con **Capstone** para documentar sus
> argumentos CLI, extraer los módulos del BIOS con **UEFIExtract A74**,
> desensamblar los módulos clave con Capstone para encontrar la forma de
> activar las opciones avanzadas y las combinaciones de teclas, clonar y
> estudiar **SmokelessRuntimeEFIPatcher-RUS** + **SREP-Patches**, y crear
> un `SREP_CONFIG.cfg` validado con parches hexadecimales que desbloquee
> el menú avanzado, todo documentado y subido al repo.

## Resumen de hallazgos

| Tema                                 | Resultado                                                              |
|--------------------------------------|------------------------------------------------------------------------|
| **Modelo**                           | Acer Aspire A315-59 (Intel Alder Lake-N, Insyde H2O)                   |
| **BIOS analizado**                   | v1.31 (`HH5A4131.exe` -> `abobios.bin`, 37.6 MB)                       |
| **Flasher**                          | `H2OFFT-Wx64.exe` v6.62 (Insyde Flash Firmware Tool)                   |
| **Tokens CLI documentados**          | 47 tokens (`-h`, `-b`, `-s`, `-n`, `-g`, `-pwd:`, `-l=`, ... ) - todos extraídos por desensamblado |
| **Microcodes Intel**                 | `00090671h`, `000906A1h`, `000906A2h`, `000906A3h` (Alder Lake-N)      |
| **Patrones SREP validados**          | 4 efectivos en `SetupUtility` + 4 efectivos en `A01ODMDxeDriver`       |
| **Match count total**                | 12 + 1 + 13 + 1 + 2 + 1 + 1 = 31 sitios concretos a parchear            |
| **Combinaciones teclas BIOS**        | Documentadas (POST, Setup, scancodes IFR, atajos avanzados)            |
| **Variables NVRAM relevantes**       | `Setup`, `SetupData`, `OemSetup`, `Custom` con GUIDs Insyde Acer       |
| **Config SREP final**                | [`Acer_A315-59/SREP_CONFIG.cfg`](Acer_A315-59/SREP_CONFIG.cfg)         |

## Estructura

```
bios_mod/
├── README.md                              <- este archivo
├── Acer_A315-59/
│   ├── SREP_CONFIG.cfg                    <- ★ archivo principal entregable
│   └── README.md                          <- cómo usarlo + qué desbloquea
├── docs/
│   ├── H2OFFT_CLI_REFERENCE.md            <- CLI completa del flasher
│   ├── BIOS_KEY_COMBINATIONS.md           <- atajos teclado y scancodes
│   └── EXTRACTION_PIPELINE.md             <- pipeline reproducible paso a paso
├── scripts/
│   ├── disasm_h2offt.py                   <- capstone + xref strings UTF-16
│   ├── dump_help.py / dump_help2.py       <- extracción del bloque help
│   ├── disasm_odm.py                      <- desensamblado del módulo ODM
│   └── verify_patterns.py                 <- validación regex de patrones
└── analysis/
    ├── h2offt_full_help.txt               <- help completo
    ├── h2offt_help_dump.txt               <- volcado ASCII secundario
    ├── h2offt_strings_excerpt.txt         <- extracto de strings UTF-16
    ├── pattern_matches.txt                <- offsets reales encontrados
    ├── abobios_uefitool_report.txt        <- report A74 de UEFIExtract
    └── abobios_guids.csv                  <- todos los GUIDs del BIOS
```

## Sobre el `SREP_CONFIG.cfg`

Es la **entrega principal**. Funciona con el patcher
[`Maxinator500/SmokelessRuntimeEFIPatcher-RUS`](https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS)
versión 0.2.x, está basado en el patrón
[`SREP-Patches NewConfigs/Acer/Acer_202x.cfg`](https://github.com/Maxinator500/SREP-Patches/tree/main/NewConfigs/Acer)
(que el autor del repo declara compatible con "Acer laptops since 2020
InsydeH2O"), y se ha **validado individualmente cada patrón** contra los
módulos extraídos por UEFIExtract:

```
SetupUtility (1,402,112 bytes):
  [OC Perf Menu / Plat Volt] FFFF00......821206......000F0F   -> 12 matches
  [OC Feature]               19821408....01000.000591         ->  1 match
  [XTU/FIVR section]         2902290229020A821206....0.00     -> 13 matches

A01ODMDxeDriver (30,784 bytes):
  [ODM Default]   740C48B80300000000000080  -> 1 match  (@ 0x15B2)
  [ODM Single 1]  4184C.74..                -> 2 matches (@0x5882, @0x597E)
  [ODM Single 2]  ..84C974..                -> 1 match  (@ 0x4048)
  [ODM Single 5]  4584D274..                -> 1 match  (@ 0x3CCA)
```

Los patrones AMD / TravelMate del config base **no encuentran nada** (lo
cual es lo correcto: el A315-59 es Intel y no es TravelMate). Se han
dejado de todas formas para preservar la compatibilidad cruzada con
otros Acer de la misma familia (el config sirve para varios modelos).

## Reproducir todo desde cero

```bash
# 1) Descargar BIOS
curl -L -A "Mozilla/5.0" -o bios.zip \
  "https://global-download.acer.com/GDFiles/BIOS/BIOS/BIOS_Acer_1.31_A_A.zip?acerid=638876442878329689&Step1=&Step2=&Step3=ASPIRE%20A315-59&OS=ALL&LC=es&BC=ACER&SC=EMEA_11y"
unzip bios.zip                            # -> HH5A4131.exe
7z x HH5A4131.exe -osfx_out               # -> abobios.bin + H2OFFT-Wx64.exe

# 2) UEFIExtract A74 Linux x64
curl -L -o uefiextract.zip \
  https://github.com/LongSoft/UEFITool/releases/download/A74/UEFIExtract_NE_A74_x64_linux.zip
unzip uefiextract.zip && chmod +x uefiextract
./uefiextract sfx_out/abobios.bin         # -> abobios.bin.dump/

# 3) Análisis con Capstone
pip3 install capstone pefile
python3 bios_mod/scripts/disasm_h2offt.py sfx_out/H2OFFT-Wx64.exe
python3 bios_mod/scripts/verify_patterns.py

# 4) Listo: SREP_CONFIG.cfg ya generado en bios_mod/Acer_A315-59/
```

Ver el detalle completo en [`docs/EXTRACTION_PIPELINE.md`](docs/EXTRACTION_PIPELINE.md).
