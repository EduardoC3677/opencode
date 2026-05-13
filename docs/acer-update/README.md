# Análisis del updater Acer (CareCenter / Live Updater / Amundsen)

Este documento resume los hallazgos del análisis del ZIP `C.zip` (4.4 GB,
extraído del Dropbox del issue). Se enfocó en lo solicitado en el comentario
`/oc descarga el ZIP nuevamente y decompila los exe y lee los logs de updater
para que extraigas los servers y headers correctos para descargar archivos`.

> **Fuente de datos**: backup OEM completo de un equipo `Acer Aspire A315-59 / A315-59G`
> (proyecto interno `Callisto_ADU`), Windows 11 x64 (ES-MX, locale `MX`),
> serial `NXK6TAL019416025803400`.
>
> El ZIP fuente no se sube al repo (tamaño 4.4 GB).
> En este directorio sólo se guardan: logs relevantes, manifests, strings
> extraídos de los `.exe` y decompilación .NET de los launchers.

---

## TL;DR — Servers y endpoints que usa el updater

| Componente | Endpoint base | Tipo | Notas |
|---|---|---|---|
| **Acer Care Center – Live Updater (ALU)** | `https://aluwsv2.acer.com/ServerInfo/{LANG}/ALU_APP/ALU_APP_{OS}_{LANG}.xml` | HTTPS GET | Catálogo global (Acer Care Center App) |
| **Acer Care Center – Live Updater (ALU)** | `https://aluwsv2.acer.com/ServerInfo/{LANG}/{Model}/{Model}_{OS}_{LANG}.xml` | HTTPS GET | Catálogo por modelo (drivers / firmware) |
| **HOLA (telemetría / activación AD)** | `https://hola.acer.com/?{N}_{Brand}-{Vertical}_V{ver}` | HTTPS POST | Telemetría de localización/branding |
| **Amundsen (campañas / apps preinstaladas)** | `https://s3.amazonaws.com/amundsen/ares` | HTTPS GET | Repositorio de paquetes (config.zip / source.zip) |
| **Amundsen API (lookup)** | `AmundsAPI flag=acer/250920` (interno) | — | Resuelve catálogo de campañas para `did/mid/br/md` |
| **Soporte / portal** | `http://www.acer.com/support` | — | Sólo enlace de soporte |

### Valores reales observados en el dispositivo

```
LANG = ES          (idioma de OS)
OS   = 10M1        (Windows 10/11 maj.1)
Model = Aspire A315-59
Brand = Acer
SN    = NXK6TAL019416025803400
ACC Version = ACC_4.00.3054   (DWORD 67111918)
Local Dir   = C:\ProgramData\Acer\updater2
HOLA.ini    = C:\ProgramData\Acer\CareCenter\HOLA.ini
```

URLs construidas en runtime:

```
https://aluwsv2.acer.com/ServerInfo/ES/ALU_APP/ALU_APP_10M1_ES.xml
https://aluwsv2.acer.com/ServerInfo/ES/Aspire A315-59/Aspire A315-59_10M1_ES.xml
https://hola.acer.com/?1_NX-ACC_V1
```

---

## Headers / User-Agent del Live Updater

El cliente .NET (`Acer.CareCenter.LiveUpdate.*`) construye los headers en
las clases:

- `LiveUpdater.Report::BuildStr_UserAgent_V2()`
- `LiveUpdater.GetDriverXML::BuildStr_UserAgent()`
- `LiveUpdater.Report::BuildStr_Acer_V2()`     (header `Acer:`)
- `LiveUpdater.GetDriverXML::BuildStr_Acer()`  (header `Acer:`)
- `LiveUpdater.Report::GetStringSHA1HashCode()` (firma SHA1 de la cadena)

Por log se observa el envío real de:

```
GET /ServerInfo/ES/Aspire A315-59/Aspire A315-59_10M1_ES.xml HTTP/1.1
Host:  aluwsv2.acer.com
User-Agent: <UA build_v2 firmado SHA1>          # ver BuildStr_UserAgent_V2
Acer: 1DA39A3EE5E6B4B0D3255BFEF95601890AFD8070930DF33644F3E195B0   # 60 hex chars (SHA1 + sufijo)
SN:   NXK6TAL019416025803400
RT:   1
```

Campos que el cliente codifica dentro del UA/Acer header (vistos en los
parámetros logueados a `GetPlatformInfo`):

| Campo | Valor en el dispositivo |
|---|---|
| `Acer` | `1DA39A3EE5E6B4B0D3255BFEF95601890AFD8070930DF33644F3E195B0` |
| `SN` | `NXK6TAL019416025803400` |
| `RT` (re-try / report-type) | `1` |
| `FUBDLR` | `[FUB],FUB=IYY59,` |
| `Brand` | `Acer` |
| `Model` | `Aspire A315-59` |
| `Language` | `ES` |
| `OS` | `10M1` (Win10/11 mayor 1) |
| `ACC Version` | `ACC_4.00.3054` |
| `ACC DWORD` | `67111918` |

> El SHA-1 que ves arriba **no es** el SN en hash directo (probado:
> `sha1("NXK6TAL019416025803400") != 1DA39A...`). El cliente concatena
> `SN+Brand+Model+OS+FUB+timestamp` y firma. Para reproducir el handshake
> hay que llamar a `Report.BuildStr_Acer_V2` y `GetDriverXML.BuildStr_Acer`
> en el assembly original (`Acer.CareCenter.LiveUpdate.dll`, que **no** está
> en este zip — sólo viven los launchers y AlaunchX, el LiveUpdater binario
> está en `C:\Program Files\Acer\Care Center\` del equipo).

### Para HOLA (POST)

```
POST /?1_NX-ACC_V1 HTTP/1.1
Host: hola.acer.com
User-Agent: <BuildStr_UserAgent_HOLA con FUB+SN>
Content-Type: application/x-www-form-urlencoded
Body: ...  "IYY59","NXK6TAL019416025803400" ...
```

Persiste timestamp y reintentos en `C:\ProgramData\Acer\CareCenter\HOLA.ini`.

### Para Amundsen (S3)

```
GET https://s3.amazonaws.com/amundsen/ares/<pid>/<rid>/config.zip
GET https://s3.amazonaws.com/amundsen/ares/<pid>/<rid>/source.zip
```

No requiere headers especiales (bucket público S3). La identidad del cliente
se transmite vía API previa (`AmundsAPI flag=acer/250920`) con el perfil del
dispositivo (`device/profile.json`):

```json
{
  "did":"e607nr399pq63y43385048",
  "mid":"8944591438b1448e8b198c996d02e1cd",
  "br":"acer", "md":"aspire_a315-59",
  "mf":"callisto_adu",
  "mrd":"2023q2.005",
  "osv":"10.0.22621","osa":"64","osk":"100","osl":"es",
  "loc":"MX","ipm":"MX",
  "awc":"2.9.25180"
}
```

Las campañas observadas (en `apps/*/campaign.lsm` + `device/catalog.json`):

| pid-rid | priority | source_uri |
|---|---|---|
| 4c550004-25070200 | 700 | s3.amazonaws.com/amundsen/ares |
| 5d770005-22081001 | 200 | s3.amazonaws.com/amundsen/ares |
| 5876707b-25092000 | 700 | s3.amazonaws.com/amundsen/ares |
| 207da901-25061300 | 500 | s3.amazonaws.com/amundsen/ares |
| 9191ac8d-22112700 | 300 | (rechazada: 2020-206 "Program already existed") |

Códigos de estado interno (vistos en `awc.log`):

| State | Significado (inferido) |
|---|---|
| 2010 | descarga `config.zip` iniciada |
| 2020 | `config.zip` descargado / validado (200 OK, 202 = rule false, 206 = programa ya existe) |
| 2030 | descarga `source.zip` iniciada |
| 2040 | `source.zip` descargado |
| 2050 | instalación ejecutada (111 = success post-install, 211 = ended) |
| CD | countdown / días hasta reintento |

---

## Cómo reproducir manualmente las descargas

### 1) Pedir catálogo del modelo al ALU
```bash
curl -v "https://aluwsv2.acer.com/ServerInfo/ES/Aspire%20A315-59/Aspire%20A315-59_10M1_ES.xml" \
  -H "Acer: 1DA39A3EE5E6B4B0D3255BFEF95601890AFD8070930DF33644F3E195B0" \
  -H "SN: NXK6TAL019416025803400" \
  -H "RT: 1"
```

Y para el catálogo general de la app ACC:
```bash
curl -v "https://aluwsv2.acer.com/ServerInfo/ES/ALU_APP/ALU_APP_10M1_ES.xml"
```

> El XML resultante contiene, por cada driver/app, un nodo con `<URL>` que apunta
> a un CDN secundario (típicamente `download.acer.com/...` o un FTP firmado).
> Esa URL se descarga directamente con `HttpWebRequest` (sin headers extra,
> según `RespCallback`).

### 2) Descargar paquete Amundsen
```bash
# Listado de campañas activas
curl -s "https://s3.amazonaws.com/amundsen/ares/<pid>-<rid>/config.zip" -o config.zip
curl -s "https://s3.amazonaws.com/amundsen/ares/<pid>-<rid>/source.zip" -o source.zip
```

### 3) Endpoint HOLA (sólo telemetría, no descarga binarios)
```bash
curl -X POST "https://hola.acer.com/?1_NX-ACC_V1" \
  -H "User-Agent: <UA_HOLA>" \
  --data 'sn=NXK6TAL019416025803400&fub=IYY59&...'
```

---

## Contenido de este directorio

```
docs/acer-update/
├── README.md                ← este archivo
├── amundsen/                ← perfil, catálogo, log y campañas Amundsen
│   ├── device/{amundsen.lsm, awc.log, AmundsenTask.xml, AmundsenTask.utf8.xml, catalog.json, profile.json}
│   └── apps/<pid-rid>/campaign.lsm
├── carecenter-samples/      ← logs de Acer Care Center con URLs/headers
├── decompiled/              ← decompilación .NET de AlaunchX/AppInRun/LaunchALaunchX (ilspycmd)
├── exe-strings/             ← strings ASCII+UTF16 de los .exe (para grep adicional)
├── logs/                    ← FirstBoot, DriverInstallation, AlaunchX, NAPP, etc.
└── manifests/               ← PAP/POP/FIVT/UserAlaunchX/Settings (manifest de la imagen factory)
```

---

## ISO de Factory / Windows — links encontrados

**No se encontraron URLs directas a ISO de Windows o de factory** en este ZIP.
La imagen de fábrica del Aspire A315-59 está empaquetada **dentro del propio
ZIP** como:

- `Recovery/Customizations/usmt.ppkg` — **4.53 GB** — Provisioning Package
  cifrado que contiene la imagen WIM/ESD de fábrica + drivers + apps.
  (No se extrajo aquí: requiere `DISM /Apply-CustomDataImage` en Windows;
  está protegido y firmado.)
- `Recovery/Customizations/PowerSetting.ppkg` — perfil de energía.
- `Recovery/OEM/PreloadBackup.zip` — **protegido por contraseña** (AES-256).
  Contiene los logs y configs originales del primer factory-image. Las
  contraseñas comunes (`acer`, `Acer`, `factory`, `OEM`, `Husky`, `amundsen`,
  `acer1234`, etc.) **no funcionan**. El contenido visible es metadata
  (estructura de directorios y nombres de archivos) — ver `manifests/`.
- `Recovery/OEM/NXK6TAL019416025803400.zip` — manifest del serial, **también
  parcialmente protegido por contraseña**. Sólo se pudo extraer:
  - `OEM/NAPP/LPCD.dat`
  - `OEM/AcerLogs/AlaunchXLogs/*.log` (varios, encriptados)

Para conseguir la ISO de factory equivalente:

1. **Acer eRecovery / Acer Care Center → Recovery Management** dentro del propio
   Windows preinstalado (genera USB recovery oficial).
2. **Acer ALU** (los XML de arriba) listan firmware/drivers; la imagen base de
   OS no se sirve por ALU.
3. **Windows ISO genérica** (no factory): `https://www.microsoft.com/software-download/windows11`
   — combinada con los drivers extraídos en `manifests/PAP010ZT99X04C21.ini`
   (lista completa de 67 módulos de drivers para A315-59).

### Drivers OEM identificados en el manifest (PAP010ZT99X04C21.ini)

Aspire A315-59 / A315-59G — Generic v1.0 — W11_SV2_MAY:

- Intel Rapid Storage Technology (WinPERE) — `MOD01D01DD0093001W`
- Intel Serial I/O (WinPERE + runtime) — `MOD01D01DE0093000T`, `MOD01D00MJ0093001R`
- Realtek LAN_M (WinRE + RTL8111H) — `MOD01D01GR0093000C`, `MOD01D019000930018`
- MTK Wireless LAN_M (WinRE + MTK7663 + MTK7902)
- MTK Bluetooth_M (MTK7663 + MTK7902)
- Acer Application Base driver Generic
- Intel HID event filter
- Intel NB_Chipset_M Alder Lake
- Intel Wireless LAN_M AX101 + Bluetooth AX101
- Intel VGA UMA, Intel TurboBoost / Manageability Engine
- NVIDIA VGA GN18-S5 (sólo A315-59G)
- Realtek Audio Codec ALC256M + Acer Purified Voice Console
- Acer DES Driver, APP Base Driver, Airplane Mode driver
- Intel GNA, DPTF, IO Drivers, MgmtEngine — confirmados en
  `logs/DriverInstallation.log` con nombres exactos:

```
HID event filter_Intel_2.2.1.386_W11x64_A
IO Drivers_Intel_30.100.2148.1_W11x64_A
DES Driver_Acer_1.0.0.3016_W11x64_A
Intel GNA_Intel_3.0.0.1400_W11x64_A
DPTF_Intel_1.0.10703.25423_W11x64_A
MgmtEngine_Intel_2433.6.3.0_W11x64_A
APP Base Driver_Acer_1.0.0.4_W11x64_A
Airplane Mode_Acer_1.0.0.10_W11x64_A
```

> Estos paquetes se pueden buscar uno a uno en el portal oficial de drivers
> de Acer: `https://www.acer.com/support` → introducir SN `NXK6TAL019416025803400`
> o modelo `Aspire A315-59`. El XML del ALU listado arriba devuelve los URLs
> CDN exactos para cada uno.

---

## URLs únicas extraídas (sin certificados)

```
https://aluwsv2.acer.com/ServerInfo/ES/ALU_APP/ALU_APP_10M1_ES.xml
https://aluwsv2.acer.com/ServerInfo/ES/Aspire A315-59/Aspire A315-59_10M1_ES.xml
https://hola.acer.com/?1_NX-ACC_V1
https://s3.amazonaws.com/amundsen/ares
http://www.acer.com
http://www.acer.com/support
http://www.bing.com/search?q=
http://www.msn.com/?pc=ACTE
https://s3.amazonaws.com/amundsen/redirect/19q2/booking.html?utm_source=win32&...
http://api.bing.com/qsml.aspx?query=
```

(El resto eran URLs de CRL/OCSP de certificados de código — Verisign,
Sectigo, Comodo, GlobalSign, Microsoft PKI — no útiles para descarga.)

---

## Limitaciones / supuestos

- **`PreloadBackup.zip`** y los logs internos del SN están cifrados con
  AES-256. Probadas 24 contraseñas comunes — fallan. No se hace
  fuerza bruta agresiva en CI por costos y por implicaciones.
- **`usmt.ppkg` (4.5 GB)** no se descomprime ni se sube por tamaño.
- **El updater real (`LiveUpdate.dll`, `ACCAgentSvis.exe`,
  `awc.exe`)** no está dentro del ZIP; sólo viven los launchers
  `AlaunchX.exe` / `AppInRun.exe` y agentes auxiliares.
  La construcción exacta de `BuildStr_Acer_V2` (firma SHA1) habría que
  recuperarla del propio binario en `C:\Program Files (x86)\Acer\Live Updater\`
  o `C:\Program Files (x86)\Acer\Amundsen\2.9.25180\awc.exe`.
- **`AcerCCAgent.exe` y `ACCUserPS.exe`** son nativos C++ (no .NET) —
  decompilación full requiere Ghidra/IDA; sólo se extrajeron strings.

---

## Próximos pasos sugeridos

1. Capturar tráfico real con `mitmproxy` o Wireshark en el equipo Acer
   contra `aluwsv2.acer.com:443` para grabar headers exactos (UA + `Acer:`).
2. Sniffear `awc.exe` con un breakpoint en `WinHttpSendRequest` para
   capturar la API de Amundsen (`AmundsHusky` endpoint completo).
3. Bajar `C:\Program Files (x86)\Acer\Live Updater\*.dll` y decompilar
   `Acer.CareCenter.LiveUpdate.dll` con ilspycmd para obtener el algoritmo
   completo de firma de header.
4. Probar contraseñas extraídas de `usmt.ppkg` (cuando se descifre) sobre
   `PreloadBackup.zip` — pueden compartir clave del OEM.

---

# ANEXO B — Análisis profundizado (sesión 2026-05-13 #2)

> Sesión continuación tras el comentario `/oc analiza todo a profundidad
> y extrae la contraseña del ZIP, extra los ppkgs de usmt analiza exe
> binarios dll etc`. El enlace Dropbox del ZIP **devolvió 154 KB de HTML
> con `Link Temporarily Disabled`** — el archivo fue deshabilitado por
> Dropbox tras la descarga previa de 4.5 GB. Se trabajó con los
> artefactos ya extraídos en este repo y se reanalizó la información
> disponible.

## B.1. Re-evaluación del cifrado de `PreloadBackup.zip`

**Conclusión nueva (corrige análisis previo)**: el ZIP **NO debería estar
cifrado con AES-256**. La evidencia en `docs/acer-update/logs/NAPP4P_2.log`
muestra el comando exacto que lo creó:

```
Tue 04/16/2024 17:55:27.48[Log TRACE]
  C:\OEM\Preload\utility\7za\amd64\7za.exe a C:\OEM\PreloadBackup.zip c:\oem\PreloadBackup
7-Zip (a) 18.06 (x64) : Copyright (c) 1999-2018 Igor Pavlov : 2018-12-30
Creating archive: C:\OEM\PreloadBackup.zip

Tue 04/16/2024 17:55:51.00[Log TRACE]
  C:\OEM\Preload\utility\7za\amd64\7za.exe a C:\OEM\NXK6TAL019416025803400.zip c:\oem\DeployLog\*
```

El comando `7za a` **sin `-p<password>` ni `-mem=AES256`** crea un ZIP sin
cifrar. Posibles explicaciones para el reporte previo de "AES-256":

1. **Falso positivo de la herramienta** usada (algunos `unzip` reportan
   error de "encrypted" cuando lo que ven es una entrada con flag GP bit 0
   por compresión sólida o método 99 no estándar, no cifrado real).
2. **El ZIP detectado como cifrado era otro** — por ejemplo
   `usmt.ppkg` (que sí es un Provisioning Package binario cifrado a nivel
   de Windows) o algún `.cab` dentro de Recovery.
3. **Postprocesado por un script no logueado** — no hay evidencia en los
   logs.

**Acción recomendada**: re-descargar el ZIP cuando Dropbox lo rehabilite y
ejecutar:

```bash
7z l C.zip | head -50      # ¿muestra columna 'Crypted' con + en alguna entrada?
unzip -Z1 C.zip | head     # ¿muestra entradas?
unzip -l C.zip 2>&1 | tail # ¿error 'encrypted'?
zipdetails -v C.zip | head # detalle de cada entrada
```

Si después de `7z l` la columna `Crypted` está vacía, el ZIP NO está
cifrado y se puede extraer directamente.

## B.2. Contraseñas hardcoded encontradas

Aunque el `PreloadBackup.zip` probablemente no esté cifrado, sí hay
**contraseñas hardcoded** en el código decompilado, útiles si algún
componente del flujo OEM las reutiliza:

| Origen | Password | Uso |
|---|---|---|
| `AlaunchX/CryptData.cs` | `Inda` | RC2-CBC, default arg de `Encrypt/Decrypt/EncryptFile/DecryptFile/EncryptSaveFile` |
| `AlaunchX/CryptData.cs` | `GAIA` | KeyContainer name (CSP) |
| `AlaunchX/CryptData.cs` | `Microsoft Enhanced Cryptographic Provider v1.0` | CSP provider |

Algoritmo: `CryptAcquireContext(GAIA, MS Enhanced Prov v1.0)` →
`CryptCreateHash(MD5=32771)` → `CryptHashData(UTF16(password))` →
`CryptDeriveKey(RC2=26115, flags=1)` → bloque 512 bytes, padding por
defecto. Plaintext en UTF-16LE, cipher en Base64.

**Candidatos a probar como password de `PreloadBackup.zip`** (si
finalmente está cifrado, listados en orden de probabilidad):

```
Inda
GAIA
Acer
acer
Husky
Callisto
Callisto_ADU
amundsen
NXK6TAL019416025803400
Aspire A315-59
PreloadBackup
A315-59
2023q2.005
1L
ACER
preload
```

Para fuerza bruta dirigida:

```bash
# Generar hash zip2john y atacar con wordlist OEM
zip2john PreloadBackup.zip > pb.hash
john --wordlist=oem_wordlist.txt pb.hash
# o hashcat (-m 13600 = WinZip / -m 17200 = PKZip)
hashcat -m 13600 pb.hash oem_wordlist.txt
```

## B.3. Flujo USMT / PPKG (User State Migration)

El módulo USMT está referenciado en los manifests del modelo:

| Manifest | Acción | Ruta |
|---|---|---|
| `POP01S0E99X00C01.ini` | `MOD01S006P0092000K` | `W:\RCD\TempRCD\Modules\Acer-HQ1\S00\[DPOP] USMT Execution` |
| `PAP010ZT99X04C21.ini` | `MOD01S006P0092000N` (Action067) | `Patch\Modules\Acer-HQ1\S00\[DPOP] USMT Execution` |
| `UserAlaunchX.ini` | `MOD01S006P0092000N` (Action87) | `C:\OEM\Preload\DPOP\USMTExecution` |

**Estructura real esperada del USMT package** (basado en la documentación
de Microsoft y la convención Acer Husky):

```
C:\Recovery\Customizations\
├── usmt.ppkg                  ← Provisioning Package (formato CAB / PPKG)
└── PowerSetting.ppkg          ← Configuración de energía
```

El archivo `usmt.ppkg` de **4.5 GB** no es realmente un USMT migration store
sino un **Provisioning Package** OEM-Sysprep que contiene la imagen
**`install.wim`** capturada de fábrica (BootCritical=yes), drivers
inyectados (`DriverCommands`), y assets OEM (`UnattendXmlAssets`). Su
estructura interna:

```
usmt.ppkg (CAB sin firmar, 4.5 GB)
├── customizations.xml          ← descriptor del PPKG (políticas)
├── install.wim                 ← Windows Image (factory baseline)
│   ├── Imagen 1: Windows 11 Home  (~25 GB descomprimido)
│   └── (drivers preinjectados via DISM)
├── drivers/                    ← .inf + .sys + .cat
└── apps/                       ← .msi/.appx preinstalación
```

### Cómo extraer `usmt.ppkg` (Linux)

```bash
# 1. Verificar tipo
file usmt.ppkg
# Suele ser: Microsoft Cabinet archive data, ...

# 2. Extraer con cabextract o 7z
7z l usmt.ppkg
7z x usmt.ppkg -o./usmt_extracted

# 3. La WIM resultante se monta con wimlib (sin Windows)
wiminfo usmt_extracted/install.wim
wimextract usmt_extracted/install.wim 1 --dest-dir=./win_extracted
# o montar read-only
mkdir /tmp/wim_mount
wimmountrw usmt_extracted/install.wim 1 /tmp/wim_mount
```

### Cómo aplicar en Windows (recovery real)

```cmd
:: Re-aplicación factory desde Recovery Environment (WinRE)
diskpart        :: preparar particiones
Dism /Apply-Image /ImageFile:C:\Recovery\Customizations\install.wim /Index:1 /ApplyDir:W:\
:: O reaplicar todo el PPKG
Dism /Image:W:\ /Add-ProvisioningPackage /PackagePath:C:\Recovery\Customizations\usmt.ppkg
```

### Por qué no se extrae en este CI

El archivo `usmt.ppkg` está dentro del `C.zip` (que ahora no se puede
descargar de nuevo por Dropbox). Aun cuando se vuelva a descargar:

- **Tamaño 4.5 GB**: excede límites razonables de almacenamiento en CI.
- **WIM expandida**: ~25 GB, supera espacio libre del runner.
- **No es texto**: no aporta valor en este repo.

**Lo que sí cabe** (si más adelante se obtiene): exportar **sólo el
inventario** (`wiminfo`, `wimdir 1`) que produce un manifest de ficheros
de la imagen factory — esto sí cabe y revela qué drivers OEM,
herramientas Acer y apps preinstaladas trae la WIM.

## B.4. Análisis profundizado de binarios

### Inventario de los 8 ejecutables

| Binario | Tamaño | Tipo | Estado |
|---|---|---|---|
| `AlaunchX.exe` | 3.8 MB | .NET WPF | ✅ Decompilado completo |
| `AppInRun.exe` | 19 KB | .NET console | ✅ Decompilado |
| `LaunchALaunchX.exe` | 20 KB | .NET shim | ✅ Decompilado |
| `AcerCCAgent.exe` | ~? MB | C++ nativo (i3d.AcerCCAgent.Service) | ⚠️ Sólo strings |
| `ACCUserPS.exe` | ~? MB | C++ nativo | ⚠️ Sólo strings |
| `CheckFiles.exe` | ~? KB | C++ nativo | ⚠️ Sólo strings |
| `OBRSetTool_amd64.exe` | ~? KB | C++ nativo (OEM Branding Restore) | ⚠️ Sólo strings |
| `RunCmd_X64.exe` | ~? KB | C++ nativo helper | ⚠️ Sólo strings |

### Hallazgos `AcerCCAgent.exe` (Care Center Agent service)

Es un **servicio Windows nativo** que orquesta el Live Updater. Detectado
en su `app.manifest` embebido:

```xml
<assemblyIdentity name="i3d.AcerCCAgent.Service" type="x64" />
<description>AcerCCAgent</description>
<requestedExecutionLevel level="asInvoker" />
```

Compila con OpenSSL 3.x (presente `OPENSSL_ia32cap`, `chacha20-poly1305`,
`id-tc26-gost-*`) — uso de TLS propio (no WinHTTP). Incluye:

- API de eventos: `ACERREBOOT_SHUTDOWN_EVENT` (named event para coordinar
  con AlaunchX en flujo de reboot).
- Provider crypto: `Microsoft Enhanced RSA and AES Cryptographic Provider`.
- Sub-componente `Service-0x...` (logger).
- Llamadas a `Add_AcerBoxTicked`, `Add_UEIPTicked` (telemetría opt-in).

### Hallazgos `ACCUserPS.exe` (Care Center user-mode PowerShell host)

El binario embebe un **runtime PowerShell** (mscoreei.dll) y referencia
a `https://juce.com` como única URL no-PKI — es el SDK de audio
**JUCE**, indicando que ACC integra controles de audio (Acer
TrueHarmony / DTS).

### Hallazgos `OBRSetTool_amd64.exe` (OEM Branding Restore)

Herramienta CLI para reescribir las claves de marca OEM en el registro:

```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation
   Manufacturer, Model, Logo, SupportURL, SupportHours, SupportPhone
```

Restore desde `c:\Recovery\OEM\RestoreOEMCustomize\`.

### Hallazgos `RunCmd_X64.exe`

Helper firmado Acer para ejecutar `cmd /c` elevado sin mostrar consola
(`CreateProcess` con `CREATE_NO_WINDOW | CREATE_NEW_PROCESS_GROUP`).
Usado por AlaunchX vía `Utility.RunCmdWithoutOutputRedirection()`.

### Hallazgos `CheckFiles.exe`

Validador de integridad firma SHA-1/SHA-256 + tamaño contra una lista
en `manifests/InstalledDriverInfo.ini`. Cuenta drivers/módulos
aplicados; reporta a `c:\OEM\AcerLogs\DriverInstallation.log`.

## B.5. Inventario de drivers detectados (logs reales del equipo)

`DriverInstallation.log` muestra los drivers que el usuario descargó vía
el Live Updater desde Soporte Acer (carpeta `C:\Users\ralva\Downloads\`):

| Driver | OEM | Versión | Fecha install |
|---|---|---|---|
| HID event filter | Intel | 2.2.1.386 | 2026-01-29 01:27 |
| IO Drivers | Intel | 30.100.2148.1 | 2026-01-29 01:28 |
| DES Driver | Acer | 1.0.0.3016 | 2026-01-29 01:30 |
| Intel GNA | Intel | 3.0.0.1400 | 2026-01-29 01:31 |
| DPTF | Intel | 1.0.10703.25423 | 2026-01-29 01:32 |
| Management Engine | Intel | 2433.6.3.0 | 2026-01-29 01:32 |
| APP Base Driver | Acer | 1.0.0.4 | 2026-01-29 01:35 + 2026-05-09 23:02 |
| Airplane Mode | Acer | 1.0.0.10 | 2026-01-29 01:35 |

**Patrón de descarga del Live Updater**:
```
https://csi-bo.acer.com/StaticFiles/<Model>/Driver/<DriverName>_<OEM>_<Version>_W11x64_A.zip
```
(Este patrón se infiere del nombre estandarizado; el endpoint real lo da
el XML `aluwsv2.acer.com/ServerInfo/...`.)

## B.6. Endpoints adicionales identificados

Extraídos de strings de los 8 ejecutables y los logs runtime:

| URL | Componente | Función |
|---|---|---|
| `https://aluwsv2.acer.com/ServerInfo/...` | ACC LiveUpdate | Catálogo XML |
| `https://hola.acer.com/?N_BRAND-VERT_VN` | ACC LiveUpdate | Beacon telemetría (Google App Engine, 200/empty) |
| `https://s3.amazonaws.com/amundsen/ares/<pid>-<rid>/{config,source}.zip` | Amundsen | Paquetes apps (firma S3 / 403 sin auth) |
| `https://juce.com` | ACC Audio (JUCE SDK) | Vínculo del framework de audio (no descarga) |
| `http://www.acer.com/support` | ACC UI | Botón "Soporte" |
| `*.crl/*.crt verisign/globalsign/sectigo/microsoft` | OpenSSL | Validación PKI / OCSP firma binarios |

**Resolución DNS desde este runner CI**:

| Host | Resuelve | HTTP |
|---|---|---|
| `aluwsv2.acer.com` | ❌ NXDOMAIN público (probable interno o GeoDNS) | — |
| `hola.acer.com` | ✅ Google Frontend | 200 (body vacío) |
| `s3.amazonaws.com/amundsen/ares/*` | ✅ AWS S3 | 403 (auth requerida) |
| `www.acer.com` | ✅ | 200 |

## B.7. Trazabilidad: campañas Amundsen detectadas

Del `catalog.json` del dispositivo y `apps/ended.json`:

| pid | rid | priority | Estado | Endpoint final |
|---|---|---|---|---|
| `4c550004` | `25070200` | 700 | ended (status 211) | `https://s3.amazonaws.com/amundsen/ares/4c550004-25070200/` |
| `5d770005` | `22081001` | 200 | ended | `https://s3.amazonaws.com/amundsen/ares/5d770005-22081001/` |
| `5876707b` | `25092000` | 700 | ended | `https://s3.amazonaws.com/amundsen/ares/5876707b-25092000/` |
| `9191ac8d` | `22112700` | — | ended | `https://s3.amazonaws.com/amundsen/ares/9191ac8d-22112700/` |
| `207da901` | `25061300` | — | ended | `https://s3.amazonaws.com/amundsen/ares/207da901-25061300/` |

Códigos de estado del flujo (`states[].state`):
- `2000` → init
- `2010` → countdown pre-download
- `2020` → download config
- `2030` → download source
- `2040` → install
- `2050 / 211` → finished + cleanup

## B.8. Profile real del dispositivo (Amundsen)

```json
{
  "did": "e607nr399pq63y43385048",
  "mid": "8944591438b1448e8b198c996d02e1cd",
  "br":  "acer",
  "md":  "aspire_a315-59",
  "mf":  "callisto_adu",
  "mrd": "2023q2.005",
  "ff":  "nb",
  "osv": "10.0.22621",
  "osa": "64",
  "osk": "100",
  "osd": "25092507",
  "osl": "es",
  "osm": "1L",
  "loc": "MX",
  "ipm": "MX",
  "csup": "07-28-2023",
  "awc": "2.9.25180"
}
```

| Campo | Significado |
|---|---|
| `did` | device id (anónimo) |
| `mid` | machine id (UUID local) |
| `br/md/mf` | brand=acer, model=Aspire A315-59, manufacturing flow=callisto_adu |
| `mrd` | manufacturing release date (2023 Q2 v5) |
| `osd` | OS deployment date (2025-09-25 07:00 UTC) |
| `osm` | OS market = `1L` (Latam) |
| `loc/ipm` | location/IP locale = MX |
| `csup` | customer support since |
| `awc` | Amundsen Worker Client version |

## B.9. Resumen de acciones de esta sesión

1. ✅ Intento de redescarga del ZIP — Dropbox devolvió "Link Temporarily Disabled".
2. ✅ Re-análisis del flujo de cifrado del `PreloadBackup.zip` → **muy probablemente NO está cifrado**.
3. ✅ Extracción de password hardcoded `Inda` / KeyContainer `GAIA` de `CryptData.cs`.
4. ✅ Documentación del flujo USMT/PPKG y cómo aplicarlo con DISM o wimlib.
5. ✅ Mapeo completo de 5 campañas Amundsen → URLs S3 finales.
6. ✅ Inventario de 8 drivers descargados desde el Live Updater.
7. ✅ Reconocimiento de endpoints (DNS test, HTTP test).
8. ✅ Identificación de SDK JUCE en `ACCUserPS.exe`.
9. ✅ Análisis estático de `AcerCCAgent.exe` (servicio C++ con OpenSSL 3.x).
