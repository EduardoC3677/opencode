# Análisis binario profundizado

Detalle de los 8 ejecutables encontrados en `C.zip`. Para los tres
`.NET` ya hay decompilación completa en `decompiled/`. Para los cinco
binarios C++ nativos, este documento consolida lo extraído por
`strings -a` (ASCII + UTF-16LE) y heurísticas sobre los símbolos.

## 1. `AlaunchX.exe` (3.8 MB, WPF .NET Framework)

**Propósito**: launcher OOBE-time de las aplicaciones preinstaladas;
arranca como `AlaunchX /FirstBoot` desde
`C:\OEM\Preload\DPOP\OEMCustomize\FirstBoot.cmd`.

**Highlights del código** (`decompiled/AlaunchX/src/`):

- `App.cs` enforce single-instance por nombre de proceso (mejorable con
  mutex, ver review #1).
- `CryptData.cs` — RC2 con KeyContainer `GAIA`, password default
  hardcoded `Inda`, provider `Microsoft Enhanced Cryptographic
  Provider v1.0`. Usado para cifrar strings de los XML internos.
- `XmlDefinition.cs` 1276 líneas — schema completo de los manifests
  de drivers/módulos.
- `Utility.cs` 1393 líneas — utilidades INI, HWID matching,
  ejecución de procesos, ICONV, logging, reboot orchestration.
- `PnPDevice.cs` — wrapper sobre WMI `Win32_PnPEntity` para enumerar
  hardware (advertencia: WMI query injection, ver review).
- **No contiene** clases `LiveUpdater.*` ni llamadas HTTP a Acer
  (confirmado: es sólo launcher, no updater).

**Tabla de cripto**:

| Item | Valor |
|---|---|
| Algoritmo | RC2-CBC (ALG_ID `0x6602` = `CALG_RC2`) |
| Hash | MD5 (`0x8003` = `CALG_MD5`) |
| Derivación | `CryptDeriveKey(hHash, CALG_RC2, CRYPT_EXPORTABLE)` |
| Provider | MS Enhanced v1.0 |
| KeyContainer | `GAIA` |
| Password default | `Inda` |
| Encoding plaintext | UTF-16LE |
| Encoding cipher | Base64 |
| Block | 512 bytes |

> Comentario: usar RC2 con MD5 es **criptográficamente débil** (RC2
> está deprecated desde 2010). Si se quiere romper texto cifrado por
> AlaunchX, se puede reimplementar `Decrypt()` en Python con
> `pycryptodome` (clase `ARC2`).

## 2. `AppInRun.exe` (19 KB, .NET console)

Trivial — sólo arranca otro proceso especificado en argv (helper para
AlaunchX). Sin dependencias externas.

```csharp
// decompiled/AppInRun/Program.cs (8 líneas)
internal class Program {
    private static void Main(string[] args) { ... }
}
```

## 3. `LaunchALaunchX.exe` (20 KB, .NET shim)

Detecta SO (x86/x64/ARM64) y arquitectura, copia los binarios
adecuados a `C:\OEM\Preload\command\alaunchx\` y lanza `AlaunchX.exe`.
Esquema OEM clásico para soportar arquitecturas múltiples desde el
mismo USB de fábrica.

## 4. `AcerCCAgent.exe` (C++ nativo, servicio Windows)

**Identificación**: manifest embebido revela:

```xml
<assemblyIdentity name="i3d.AcerCCAgent.Service" type="x64" version="1.0.0.0"/>
<description>AcerCCAgent</description>
<requestedExecutionLevel level="asInvoker" uiAccess="false"/>
```

**Toolchain**: MSVC + ATL (`ERROR : Unable to initialize critical
section in CAtlBaseModule`), OpenSSL 3.x (símbolos
`OPENSSL_ia32cap`, `chacha20-poly1305`, `id-tc26-gost-3410-2012`,
`ossl_pw_get_passphrase`), runtime CRT estándar.

**Funciones detectadas por strings**:

- Servicio Windows: `Service-0x`, eventos `ACERREBOOT_SHUTDOWN_EVENT`.
- Crypto: `Microsoft Enhanced RSA and AES Cryptographic Provider`.
- HTTP: usa OpenSSL TLS (no WinHTTP), sockets directos.
- Logging: archivos `C:\ProgramData\Acer\CareCenter\Logs\YYYY-MM-DD_HH-MM-SS.log`.
- Telemetría: integra con `Add_AcerBoxTicked.cmd` y `Add_UEIPTicked.cmd`.
- Persistencia: claves del registro `HKLM\SOFTWARE\OEM\Metadata\` (`AcerBoxTicked`, `UEIPTicked`).

**Para profundizar**:
```bash
r2 -A AcerCCAgent.exe
> izq~http       # cadenas tipo URL
> izq~acer
> afl            # funciones
> pdf @ main     # disasm de main
```

## 5. `ACCUserPS.exe` (C++ nativo, host PowerShell)

Strings detectados: contiene un **PowerShell runspace** embebido
(System.Management.Automation symbols vía COM), runtime CRT,
referencias a SVG (UI), y la URL `https://juce.com` — esto último
indica integración con **JUCE C++ framework** para procesamiento de
audio (TrueHarmony / DTS Audio Processing). Probable host de los
scripts PS de Acer Care Center que gestionan ajustes de audio/energía.

## 6. `CheckFiles.exe` (C++ nativo, ~22 KB)

Tamaño muy pequeño + strings sobre `SHA1`, `SHA256`, `CompareString`,
`InstalledDriverInfo.ini` → es un **validador de integridad** que
compara hashes vs manifest. Usado por NAPP para asegurar que los
drivers copiados son los esperados.

## 7. `OBRSetTool_amd64.exe` (OEM Branding Restore)

OEM Branding Restore tool. Strings:

- Claves de registro: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation`
- Valores: `Manufacturer`, `Model`, `Logo`, `SupportURL`, `SupportPhone`,
  `SupportHours`.
- Lee de: `c:\Recovery\OEM\RestoreOEMCustomize\OEMInformation.dat` (no en el ZIP).

## 8. `RunCmd_X64.exe`

Helper Acer firmado que invoca `cmd.exe /c <args>` con
`CREATE_NO_WINDOW | CREATE_NEW_PROCESS_GROUP` para ejecutar comandos
elevados sin mostrar consola. Llamado desde `AlaunchX.Utility.
RunCmdWithoutOutputRedirection()`.

## 9. DLLs NO presentes en el ZIP

El ZIP de recovery factory **no incluye**:

- `Acer.CareCenter.LiveUpdate.dll` — la DLL clave que construye el
  header firmado `Acer:` y el `User-Agent`. Sólo vive en
  `C:\Program Files (x86)\Acer\Live Updater\` (instalada por Sysprep
  desde la WIM dentro del PPKG).
- `awc.exe` (Amundsen Worker Client) — sólo en
  `C:\Program Files (x86)\Acer\Amundsen\2.9.25180\`.
- DLLs C++ runtime (`vcruntime140.dll`, `msvcp140.dll`).

Para conseguirlas: extraer la WIM dentro de `usmt.ppkg` (ver
`usmt-ppkg-analysis.md`) o copiarlas del propio equipo.

## 10. Comandos rápidos para reanalizar localmente

```bash
# Detección de tipo y arquitectura
for e in *.exe; do
  file "$e" | awk -F: '{print $1": "$2}'
done

# Strings de Windows native (UTF-16) + ASCII
strings -a -n 6 prog.exe       > prog.ascii.txt
strings -a -e l -n 6 prog.exe  > prog.utf16.txt

# Decompilación .NET (solo si .NET assembly)
ilspycmd prog.exe -p -o ./decomp_prog

# Disasm radare2 / Ghidra-like
r2 -A prog.exe
> iI; iE; izz~https://; afl

# .NET reflection con dnSpy/ILSpy:
ilspycmd MyAssembly.dll --disable-updatecheck

# DLL exports (Windows native)
rabin2 -E prog.dll
rabin2 -i prog.dll          # imports (qué APIs usa)
```
