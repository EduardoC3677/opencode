# Acer Aspire A315-59 - BIOS Advanced Menu Unlock (Insyde H2O)

> Carpeta de salida del análisis solicitado en la issue: descarga del BIOS de
> Acer, extracción, desensamblado de las herramientas de flasheo, extracción de
> módulos UEFI, identificación de patrones que esconden las opciones avanzadas
> y generación del `SREP_CONFIG.cfg` listo para SmokelessRuntimeEFIPatcher.

## Modelo / BIOS analizado

| Campo            | Valor                                                                  |
|------------------|------------------------------------------------------------------------|
| Modelo           | **Acer Aspire A315-59**                                                |
| Plataforma       | Intel **Alder Lake-N** (Insyde H2O)                                    |
| BIOS             | **1.31** (Acer, ZIP `BIOS_Acer_1.31_A_A.zip`)                          |
| Archivo zip      | `BIOS_Acer_1.31_A_A.zip` (~12.7 MB)                                    |
| Installer EXE    | `HH5A4131.exe` (7-Zip SFX, ~12.8 MB)                                   |
| BIOS bin         | `abobios.bin` (37.6 MB, cápsula UEFI Insyde)                           |
| Flasher          | `H2OFFT-Wx64.exe` 6.62.0 (Insyde H2O Flash Firmware Tool)              |
| Firmware Volumes | múltiples FV (UEFIExtract A74 NE)                                      |
| Módulos clave    | `SetupUtility`, `A01ODMDxeDriver`, `A01ODMSmmServiceDriver`            |

## Contenido de esta carpeta

| Archivo                              | Descripción                                                                           |
|--------------------------------------|---------------------------------------------------------------------------------------|
| `SREP_CONFIG.cfg`                    | **Config principal** validado contra el BIOS 1.31 - listo para usar con SREP 0.2.x    |
| `README.md`                          | Este documento                                                                        |
| `../docs/H2OFFT_CLI_REFERENCE.md`    | Referencia completa de la CLI de H2OFFT (extraída por desensamblado con Capstone)     |
| `../docs/BIOS_KEY_COMBINATIONS.md`   | Atajos de teclado de Acer / Insyde H2O para entrar y desbloquear menús del BIOS       |
| `../docs/EXTRACTION_PIPELINE.md`     | Pipeline completo paso-a-paso (descargar, SFX, UEFIExtract, capstone, SREP)           |
| `../analysis/h2offt_strings.txt`     | Strings UTF-16 extraídas (CLI tokens, mensajes, identificadores)                      |
| `../analysis/h2offt_full_help.txt`   | Bloque de ayuda continuo extraído de `.rdata`                                         |
| `../analysis/pattern_matches.txt`    | Matches concretos de cada patrón SREP sobre los módulos extraídos                     |

## Cómo aplicar el desbloqueo

1. **Compila o descarga** SREP 0.2.x desde el fork de Maxinator500:
   <https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS/releases>
2. Copia el archivo `BOOTX64.efi` resultante a `EFI/BOOT/BOOTX64.efi` en un
   pendrive **FAT32** (es UEFI Shell-friendly).
3. Copia este `SREP_CONFIG.cfg` a la **raíz** del pendrive (cualquier nombre
   con extensión `.cfg` sirve - SREP carga el primero que encuentra).
4. **Desactiva Secure Boot** y **establece Supervisor Password** en el BIOS
   (necesario para que Insyde H2O permita ejecutar EFI no firmados).
5. Arranca desde el pendrive (F12 al encender el portátil).
6. SREP cargará `SetupUtility` y `A01ODMDxeDriver` desde el Firmware Volume,
   aplicará los parches en RAM, y finalmente ejecutará `SetupUtilityApp`
   (`Op Exec`) lo que lanzará el setup UEFI con todos los menús desbloqueados.
7. Configura lo que quieras y guarda con F10. Los cambios permanecen en NVRAM.

## Qué desbloquea exactamente

Los 4 patrones efectivos sobre `SetupUtility` son IFR opcodes de Insyde:

* `82 12 06 ...` (`EFI_IFR_SUPPRESS_IF` con expresión "no mostrar si...") se
  sustituye por `82 47` (`EFI_IFR_TRUE` opcode), que evalúa siempre a falso
  para la condición de supresión -> los menús se vuelven visibles.

Submenús que aparecen tras esto en placas Insyde H2O Acer 202x:

* **OC Perf Menu** (Overclocking)
* **Plat Volt Overrides** (Platform Voltages)
* **OC Feature** (habilitar el OC)
* **XTU / FIVR** (Fully Integrated Voltage Regulator, c-state, p-state)
* **CPU / DRAM / PCH / GPU Configuration** (sub-paneles ocultos)
* **Thermal & Power Management** completos
* **Security**, **TPM Configuration** ampliados
* **Boot / CSM** completos

Y el parche sobre `A01ODMDxeDriver` neutraliza el "ODM Default" / "ODM
Single" que Acer usa para mantener escondidos esos menús incluso aunque el
firmware Insyde los soporta.

## Aviso legal

Modificar el BIOS implica riesgos: bloqueos, brick parcial, pérdida de
garantía. Este archivo se publica únicamente con fines educativos y de
investigación. Úsalo bajo tu propia responsabilidad y mantén siempre un
backup completo del flash usando `H2OFFT-Wx64.exe -g backup.fd` antes de
hacer cambios.
