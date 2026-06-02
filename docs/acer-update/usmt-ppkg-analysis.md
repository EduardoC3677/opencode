# Análisis USMT / PPKG (Provisioning Package OEM)

> Este documento amplía el análisis del `usmt.ppkg` (4.5 GB) y
> `PowerSetting.ppkg` (pequeño) que vienen en
> `Recovery/Customizations/` del backup factory. NO se sube el binario
> al repo (excede límites). Aquí se documenta su estructura, contenido
> esperado y cómo extraerlo.

## 1. Qué es un PPKG en este contexto

`usmt.ppkg` **no es** un USMT migration store (estructura
`USMTData/*.mig` de Microsoft User State Migration Tool). El nombre
es engañoso: es un **Windows Provisioning Package** con extensión
arbitraria — Acer reutiliza el sufijo `.ppkg` para empaquetar la
*OEM baseline image*.

Microsoft Docs:
https://learn.microsoft.com/windows/configuration/provisioning-packages/provisioning-packages

Verificación: un PPKG real es un **archivo CAB firmado** con cabecera
`MSCF`. Inspeccionable con:

```bash
file usmt.ppkg
# Microsoft Cabinet archive data, ... extended header ...
hexdump -C usmt.ppkg | head -2
# 00000000  4d 53 43 46 00 00 00 00 ...   ← "MSCF"
```

## 2. Estructura interna esperada

Para una imagen factory Acer Aspire (Windows 11 OEM):

```
usmt.ppkg (CAB ~4.5 GB)
├── customizations.xml          ← descriptor XML (ICD / Windows Configuration Designer)
├── install.wim                 ← imagen WIM principal
│   └── [Index 1] Windows 11 Home Single Language MX
│       ├── Windows\           (sistema)
│       ├── Program Files\     (apps preinstaladas: Acer ACC, McAfee, Norton, etc.)
│       ├── Program Files (x86)\Acer\Live Updater\
│       │   └── Acer.CareCenter.LiveUpdate.dll   ← clave para reproducir headers
│       ├── Program Files (x86)\Acer\Amundsen\
│       │   └── 2.9.25180\awc.exe
│       └── Recovery\OEM\
├── OEMDefaultAssociations.xml  (file associations)
├── unattend.xml                (Sysprep specialize)
└── (assets PPKG firmados)
```

## 3. Cómo verificar y extraer

### En Linux (sin Windows)

```bash
# 1. Tipo del archivo
file usmt.ppkg

# 2. Listar contenido (CAB)
7z l usmt.ppkg
# o
cabextract -l usmt.ppkg

# 3. Extracción a directorio
mkdir usmt_out
7z x usmt.ppkg -ousmt_out
# o
cabextract -d usmt_out usmt.ppkg

# 4. La WIM principal:
ls -lh usmt_out/install.wim
wiminfo usmt_out/install.wim          # info de imagen
wimdir usmt_out/install.wim 1 | head  # raíz de la imagen
wimextract usmt_out/install.wim 1 \
    --dest-dir=./win_root \
    'Program Files (x86)/Acer/Live Updater/*'   # solo lo de Acer
```

### En Windows (WinRE / Sysprep)

```cmd
REM Inspeccionar
DISM /Get-ProvisioningPackageInfo /PackagePath:C:\Recovery\Customizations\usmt.ppkg

REM Aplicar al sistema actual (NO ejecutar sin entender el efecto)
DISM /Online /Add-ProvisioningPackage /PackagePath:C:\Recovery\Customizations\usmt.ppkg

REM O en una imagen montada offline
DISM /Image:W:\ /Add-ProvisioningPackage /PackagePath:...

REM Aplicar la WIM extraída a una partición
DISM /Apply-Image /ImageFile:C:\path\install.wim /Index:1 /ApplyDir:W:\
```

## 4. Espacios necesarios

| Estado | Tamaño aprox. |
|---|---|
| `usmt.ppkg` cifrado/comprimido | 4.5 GB |
| CAB extraído (sin WIM aún) | ~30 MB texto + WIM |
| `install.wim` (comprimido LZMS) | ~12-15 GB |
| Imagen aplicada (Windows + apps) | ~25-40 GB |

Por eso este CI **no la extrae**: el runner tiene 89 GB libres pero
los I/O sostendrían 30+ minutos para descomprimir.

## 5. Lo verdaderamente importante adentro

Para retomar el análisis del updater (el objetivo del issue #47), lo
relevante dentro de `install.wim` es:

| Path en la WIM | Por qué importa |
|---|---|
| `Program Files (x86)\Acer\Live Updater\Acer.CareCenter.LiveUpdate.dll` | Contiene `BuildStr_Acer_V2`, `BuildStr_UserAgent_V2` — algoritmo exacto del header `Acer:` firmado SHA1. |
| `Program Files (x86)\Acer\Live Updater\Acer.CareCenter.LiveUpdate.exe.config` | Servidor ALU configurado (`aluwsv2.acer.com`). |
| `Program Files (x86)\Acer\Amundsen\2.9.25180\awc.exe` | Cliente Amundsen, lógica de bajada S3, headers. |
| `Program Files (x86)\Acer\Amundsen\2.9.25180\policy.json` | Política de campañas. |
| `Program Files\Acer\TrueHarmony\*.dll` | Audio (JUCE). |
| `Windows\System32\OEM\` | Branding restore. |
| `Recovery\OEM\eRecovery_tool.log` | Log de eRecovery. |

## 6. Si `usmt.ppkg` resulta cifrado

Algunos OEMs sí firman/cifran sus PPKGs con la clave OEM (registrada en
`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\OEM`). En ese
caso `7z l` devuelve error de firma. Soluciones:

1. **Confiar la firma en una VM Acer real** — Sysprep validará y aplicará
   normalmente.
2. **Extraer la clave OEM del registro** del equipo y firmar manualmente
   con `Add-ProvisioningPackageCertificate`.
3. **No descifrarlo**: extraer la WIM internamente (la firma sólo
   protege el manifest, no necesariamente el blob `install.wim`).

## 7. Estado en este repo

| Archivo | Subido | Razón |
|---|---|---|
| `usmt.ppkg` | ❌ | 4.5 GB, no aporta texto |
| `PowerSetting.ppkg` | ❌ | binario, sólo configura plan de energía |
| `wim_info.txt` (sería el output de `wiminfo`) | ❌ | requiere extracción no realizable este run |
| Script `reproduce.sh` para hacer todo | ✅ | `docs/acer-update/tools/reproduce.sh` |
