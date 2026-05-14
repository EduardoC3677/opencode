# H2OFFT-Wx64.exe - Argumentos CLI

Flasher de BIOS Insyde H2O FlashTool, version Windows x64.

**Archivo**: H2OFFT-Wx64.exe (6,935,328 bytes)
**Compilado**: 2023-09-06
**Origen**: extraido de HH5A4131.exe (BIOS A315-59 v1.31)

## Metodologia de obtencion

Se extrajeron strings ASCII y UTF-16LE del PE con un script propio (`scripts/strings_pe.py` usando pefile + regex), y se filtraron strings de ayuda (`-X command used`, `-X        Description`).

El EXE esta parcialmente ofuscado (basura en .text con strings tipo `-Xy.D`) pero las strings de help permanecen intactas en `.rdata` UTF-16.

## Argumentos CLI documentados (de la ayuda interna -h)

| Flag | Descripcion |
|---|---|
| `-h` | Muestra la ayuda del flasher |
| `-s` | Ejecuta en modo silencioso |
| `-r` | Reinicia el sistema tras flashear |
| `-n` | NO reinicia tras flashear |
| `-b` | Suspende BitLocker forzosamente |
| `-f` | Ignora errores de soft dependency / no check BIOS version |
| `-g` | Lee la ROM actual y la guarda a archivo (DUMP) |
| `-iv` | Muestra version IHISI soportada por utility y BIOS onboard |
| `-mfg` | Indica a BIOS que se esta en modo manufactura |
| `-pi` | Consulta el BVDT protection/private region MAP en el input file |
| `-pq` | Consulta el BIOS protection region MAP en la ROM actual |
| `-pr` | Consulta el external region MAP en la ROM actual |
| `-priv` | Consulta el BIOS private region MAP en la ROM actual |
| `-pw` | Consulta el whole region MAP en la ROM actual |
| `-OemCus` | Indica a BIOS hacer OEM customization feature |
| `-ecp` | Update non-share EC block por bloque |
| `-forceit` | Skip BIOS version check (downgrade) y skip AC/DC check |
| `-forcetype` | Skip model name check (cross-model flashing) |
| `-nopause` | No prompt al usuario por input |
| `-noconfirm` | No popup de confirmacion de flash |
| `-extrfd OUT_PATH` | Extrae el archivo BIOS del package a OUT_PATH (solo modo package) |
| `-edt#@:"VALUE"` | Edita un campo. `#` es Type ID (4-C), `@` posicion, VALUE el valor |
| `-dbgndt` | Debug: no usar datetime (flag interno) |
| `-sdbg` | Modo debug verbose |
| `-secondlogo` | Flashea segundo logo (boot logo OEM) |
| `-generic` | Modo flash generico (sin checks de modelo) |
| `-allowsv` | Permitir flashear secure variant a non-secure |

## Modos de uso comunes

### Extraer el .fd (BIOS body) sin flashear
```cmd
HH5A4131.exe -extrfd C:\extracted_bios\
```
(Equivalente a lo que hicimos manualmente con 7z, pero usando el flasher oficial)

### Hacer dump de la ROM actual del SPI flash
```cmd
HH5A4131.exe -g current_bios.bin
```

### Flashear forzando (peligroso)
```cmd
HH5A4131.exe -s -r -nopause -forceit -forcetype
```
Esto saltea TODOS los checks (modelo, version, AC). **NUNCA usar a menos que sepas lo que haces**.

### Flashear normalmente con reboot automatico
```cmd
HH5A4131.exe -s -r
```

## Mensajes de error decodificados

Extraidos de strings UTF-16 en .rdata:

| Token interno | Significado |
|---|---|
| ArgumentInvalidCommand | Invalid H2OFFT-W parameters |
| ArgumentForPackageOnly | Comando -extrFD solo en modo package |
| ArgumentInvalidCusCommand | -OemCus parse failed |
| ArgumentInvalidEdtTypeCommand | -edt parse failed |
| ArgumentInvalidEdtFormatCode | -edt formato incorrecto (esperado `-edt#@:\"Value\"`) |
| ArgumentInvalidFilename | Filename no valido |
| ArgumentFileNotFound | Archivo no encontrado |
| ArgumentBlockCommand | Comando bloqueado por politica |
| SecurityNotInsydeBios | BIOS no es Insyde H2O |
| SecureNotAllowSecureFlash | Plataforma no permite secure flash |
| SecureCannotSecureFlash | BIOS image invalida para Secure Flash |
| NoBiosFileFound | No se encontro archivo BIOS |
| NormalBiosUseSecureFilename | Filename reservado (isflash.bin) usado en BIOS no firmado |
| ImageBiosSizeSmallerThanBoard | Nueva BIOS mas pequena que la actual |
| ImageBiosSizeLargerThanBoard | Nueva BIOS mas grande que la actual |
| OemBiosNotSupportAbct | BIOS no soporta ABCT BIOS structure |

## Notas de seguridad

- El flasher carga un driver kernel firmado (`H2OFFT64.sys`) que se instala como servicio Windows
- Solo flashea BIOS firmados por Insyde/Acer (Secure Flash habilitado en esta plataforma)
- Para flashear un BIOS modificado seria necesario:
  1. Bypass de Secure Flash (no posible por software con esta version)
  2. Flashing externo via SPI programmer (CH341A + clip SOIC8)
  3. **Por eso SREP es la unica via runtime viable para este BIOS**
