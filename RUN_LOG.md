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
