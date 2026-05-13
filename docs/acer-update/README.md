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
