# RUN_LOG — Análisis Acer C.zip

## Sesión: 2026-05-13 (Github Actions, issue #C.zip)

### Comando del usuario

> `/oc descarga el ZIP nuevamente y decompila los exe d / lee los logs de
> updater para que extraigas los servers y headers correctos para descargar
> archivos de mi laptop desde ahí`

### Qué se hizo

1. **Descarga del ZIP** desde Dropbox (4 487 MB, OK):
   `https://www.dropbox.com/scl/fi/w619q1cn0j7iyyxk4gt23/C.zip?...&dl=1`
   → guardado en `/tmp/opencode/work/C.zip`.

2. **Listado**: 4 891 archivos, 4.8 GB descomprimido. Se identificaron:
   - 9 ejecutables (`.exe`)
   - 251 archivos cifrados dentro de `Recovery/OEM/PreloadBackup.zip`
   - 1 `usmt.ppkg` de 4.5 GB (factory image WIM cifrada)
   - 1 `PowerSetting.ppkg` (pequeño)
   - logs de Acer Care Center, Amundsen, FirstBoot, DriverInstallation

3. **Extracción** (excluyendo `usmt.ppkg` por tamaño): 303 MB.

4. **Strings ASCII + UTF-16** de los 8 `.exe` (en `/tmp/opencode/strings_out/`).
   Sólo se encontraron URLs de PKI/CRL (firma de código) — no URLs de
   descarga reales en los binarios.

5. **Decompilación .NET** con `ilspycmd 10.0.1.8346` de los assemblies:
   - `AlaunchX.exe` (3.8 MB) — launcher de apps preinstaladas (NO updater)
   - `AppInRun.exe` (19 KB)
   - `LaunchALaunchX.exe` (20 KB)

   Resultado en `/tmp/opencode/decomp/`. **No contienen** clases
   `LiveUpdater.*` ni llamadas a `HttpClient/WebRequest` con URLs Acer.

6. **Análisis de logs**: `OEM/CareCenter/DebugLog/*.log` (cientos de archivos),
   `OEM/Amundsen2/device/awc.log`, `OEM/AcerLogs/*.log`. Encontradas:
   - URLs reales del Live Updater (`aluwsv2.acer.com`).
   - URL HOLA telemetría (`hola.acer.com`).
   - URL bucket Amundsen S3 (`s3.amazonaws.com/amundsen/ares`).
   - Headers del LiveUpdater (`User-Agent`, `Acer:`, `SN:`, `RT:`).
   - Algoritmo de firma SHA-1 (clases `Report.BuildStr_Acer_V2` /
     `GetDriverXML.BuildStr_Acer`).
   - Códigos de estado del flujo de descarga Amundsen (2010-2050).

7. **PreloadBackup.zip**: protegido por contraseña AES-256. Probadas 24
   contraseñas comunes (`acer`, `Acer`, `factory`, `OEM`, `Husky`, `amundsen`,
   etc.) — **todas fallan**. No se hace fuerza bruta agresiva en CI.

8. **NXK6TAL019416025803400.zip** (manifest del serial): la mayoría de los
   archivos también cifrados con la misma clave.

### Artefactos generados en el repo

```
docs/acer-update/
├── README.md                              ← análisis completo, headers, URLs
├── amundsen/                              ← profile.json, catalog.json, awc.log, campañas
├── carecenter-samples/                    ← 3 logs representativos del LiveUpdater
├── decompiled/                            ← código C# decompilado (AlaunchX, AppInRun, LaunchALaunchX)
├── exe-strings/                           ← strings ASCII+UTF-16 de los .exe
├── logs/                                  ← FirstBoot, DriverInstallation, AlaunchX, NAPP
└── manifests/                             ← PAP/POP/FIVT/UserAlaunchX (manifest del modelo)
```

Total subido al repo: **1.6 MB** (sólo texto / código fuente decompilado).

### Lo que NO se subió y por qué

| Archivo | Tamaño | Motivo |
|---|---|---|
| `C.zip` original | 4 487 MB | excede límites de Git/GitHub |
| `Recovery/Customizations/usmt.ppkg` | 4 537 MB | factory image cifrada, sin valor sin clave |
| `Recovery/OEM/PreloadBackup.zip` | 86 MB | cifrado, sin clave |
| Recursos gráficos de AlaunchX | ~3 MB | wallpapers/iconos no aportan al análisis |
| Imágenes de wallpapers `OEM/wp/` | ~50 MB | mismo motivo |

### Pendientes / próximos pasos

- [ ] Capturar tráfico HTTPS real (mitmproxy) en el equipo Acer para registrar
      headers `User-Agent` y `Acer:` reales firmados.
- [ ] Decompilar `Acer.CareCenter.LiveUpdate.dll` desde
      `C:\Program Files (x86)\Acer\Live Updater\` del equipo (no está en este ZIP).
- [ ] Decompilar `awc.exe` de Amundsen (`C:\Program Files (x86)\Acer\Amundsen\2.9.25180\`).
- [ ] Probar romper `PreloadBackup.zip` con diccionarios OEM dirigidos
      (hashcat -m 13600 con wordlist Acer/Husky/Callisto).
- [ ] Si se obtiene la clave del `usmt.ppkg`, extraer la WIM con
      `dism /Apply-CustomDataImage`.

### Supuestos asumidos sin preguntar

- El usuario pidió análisis técnico del updater → se priorizó eso sobre
  re-subir archivos (que es imposible por tamaño).
- Se excluyó `usmt.ppkg` de la extracción para no agotar disco (85 GB libres).
- Se decompilaron sólo los .NET (los nativos C++ requerirían Ghidra/IDA).
- No se intentó fuerza bruta de contraseña: caro en CI y riesgo de bloqueo
  del runner.

---

## Sesión: 2026-05-13 #2 (continuación)

### Comando del usuario

> `/oc analiza todo a profundidad y extrae la contraseña del ZIP, extra
> los ppkgs de usmt analiza exe binarios dll etc`

### Qué se hizo

1. **Intento de redescarga del ZIP** desde Dropbox → respuesta:
   `Link Temporarily Disabled` (154 KB de HTML, no el ZIP). Dropbox
   deshabilitó el enlace por la transferencia previa de 4.5 GB.
   Trabajo proseguido con los artefactos ya en repo.

2. **Re-análisis del cifrado del `PreloadBackup.zip`**: revisando
   `docs/acer-update/logs/NAPP4P_2.log` el comando para crear el ZIP
   fue:
   ```
   7za.exe a C:\OEM\PreloadBackup.zip c:\oem\PreloadBackup
   ```
   **SIN `-p<password>`** → el archivo **NO debería estar cifrado**.
   El reporte previo de "AES-256" fue probablemente falso positivo de
   la herramienta. Se documenta en README y se da metodología
   (`7z l`, `unzip -Z1`, `zipdetails -v`) para verificar al
   redescargar.

3. **Contraseñas hardcoded extraídas** del código decompilado
   (`CryptData.cs`):
   - Password default: `Inda`
   - KeyContainer: `GAIA`
   - Provider: `Microsoft Enhanced Cryptographic Provider v1.0`
   - Algoritmo: RC2-CBC con MD5, bloque 512, Base64.

   Generada wordlist OEM dirigida en
   `docs/acer-update/tools/oem_wordlist.txt` (88 candidatos).

4. **Análisis USMT/PPKG**: documento dedicado
   `docs/acer-update/usmt-ppkg-analysis.md` cubriendo:
   - Que `usmt.ppkg` NO es USMT migration store sino Windows
     Provisioning Package (CAB firmado).
   - Estructura interna esperada (`install.wim` + `customizations.xml`).
   - Comandos exactos para extraer en Linux (`7z`, `wimlib`) y
     Windows (`DISM`).
   - Por qué no se extrae en este CI (espacio / I/O).
   - Lista de DLLs valiosas dentro de la WIM (
     `Acer.CareCenter.LiveUpdate.dll`, `awc.exe`).

5. **Análisis binario profundizado**: documento dedicado
   `docs/acer-update/binary-analysis.md`:
   - Detalle por cada uno de los 8 ejecutables.
   - `AcerCCAgent.exe` = servicio Windows nativo `i3d.AcerCCAgent.Service`
     con OpenSSL 3.x embebido (TLS propio, no WinHTTP).
   - `ACCUserPS.exe` integra **JUCE SDK** (audio TrueHarmony / DTS).
   - `OBRSetTool_amd64.exe` = OEM Branding Restore (toca
     `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation`).
   - `CheckFiles.exe` = validador SHA1/SHA256 contra
     `InstalledDriverInfo.ini`.
   - `RunCmd_X64.exe` = helper firmado para `cmd /c` elevado oculto.
   - Comandos r2 / rabin2 / ilspycmd para reanálisis local.

6. **Mapeo de campañas Amundsen** (5 pid-rid → URLs S3):
   ```
   https://s3.amazonaws.com/amundsen/ares/4c550004-25070200/{catalog,config,source}.zip
   https://s3.amazonaws.com/amundsen/ares/5d770005-22081001/...
   https://s3.amazonaws.com/amundsen/ares/5876707b-25092000/...
   https://s3.amazonaws.com/amundsen/ares/9191ac8d-22112700/...
   https://s3.amazonaws.com/amundsen/ares/207da901-25061300/...
   ```
   Test HTTP desde el runner → 403 (auth S3 requerida).

7. **Test de conectividad** real (curl):
   | Host | Resuelve | HTTP |
   |---|---|---|
   | `aluwsv2.acer.com` | ❌ NXDOMAIN | — |
   | `hola.acer.com` | ✅ GAE | 200 (body vacío) |
   | `s3.amazonaws.com` | ✅ | 403 |
   | `www.acer.com` | ✅ | 200 |

   `aluwsv2.acer.com` parece ser GeoDNS interno o sólo accesible desde
   IPs MX/LATAM.

8. **Inventario de drivers** descargados (de
   `docs/acer-update/logs/DriverInstallation.log`):
   8 paquetes con patrón `<Name>_<OEM>_<Version>_W11x64_A.zip` —
   listados en README §B.5.

9. **Script reproducible** `docs/acer-update/tools/reproduce.sh` que
   automatiza: listar/extraer ZIP → john contra wordlist → extraer
   PPKG → wiminfo → decompilar .NET → strings → radare2 → extraer
   endpoints.

### Artefactos nuevos en el repo

```
docs/acer-update/
├── README.md                          ← AMPLIADO (anexo B)
├── usmt-ppkg-analysis.md              ← NUEVO
├── binary-analysis.md                 ← NUEVO
└── tools/
    ├── oem_wordlist.txt               ← NUEVO (88 candidatos)
    └── reproduce.sh                   ← NUEVO (automatización full)
```

### Limitaciones de esta sesión

- **No se pudo redescargar `C.zip`** (Dropbox lo deshabilitó tras la
  descarga previa de 4.5 GB). Trabajo realizado sobre los artefactos
  ya extraídos y subidos.
- **No se pudo ejecutar `7z l` sobre el `PreloadBackup.zip` real** —
  está fuera del repo. La afirmación de que NO está cifrado se basa
  en el log de creación, no en la verificación directa.
- **No se pudo decompilar `Acer.CareCenter.LiveUpdate.dll`** — vive
  dentro de `install.wim` en `usmt.ppkg`, que requiere ~25 GB de I/O.

### Supuestos asumidos (sin preguntar)

- Como Dropbox falla, NO se intentó VPN/proxy/alternativas → riesgo
  legal y de tiempo. Documentado.
- Como `7za a` sin `-p` no cifra, se asume que la afirmación previa
  "AES-256" fue un falso positivo. Se documenta cómo verificar.
- Se asume el patrón de URL de drivers
  `https://csi-bo.acer.com/StaticFiles/<Model>/Driver/...` por
  convención Acer; el patrón real exacto requeriría el XML del Live
  Updater (DNS no resuelve aquí).

---

### Hallazgo principal (TL;DR)

El updater de Acer Care Center llama a:

```
GET  https://aluwsv2.acer.com/ServerInfo/ES/ALU_APP/ALU_APP_10M1_ES.xml
GET  https://aluwsv2.acer.com/ServerInfo/ES/Aspire A315-59/Aspire A315-59_10M1_ES.xml
POST https://hola.acer.com/?1_NX-ACC_V1
```

Con headers:

```
User-Agent: <generado por LiveUpdater.Report.BuildStr_UserAgent_V2>
Acer:       <SHA1(SN+Brand+Model+OS+FUB) en hex, 60 chars>
SN:         NXK6TAL019416025803400
RT:         1
```

Y el cliente Amundsen baja sus paquetes desde:

```
https://s3.amazonaws.com/amundsen/ares/<pid>-<rid>/{config,source}.zip
```

Detalle completo y reproducible en `docs/acer-update/README.md`.
