# Documentación de Análisis - Acer Aspire A315-59 (Callisto_ADU)

## Resumen del Equipo

| Campo | Valor |
|-------|-------|
| **Marca** | Acer |
| **Modelo** | Aspire A315-59 |
| **Plataforma** | Callisto_ADU (Notebook) |
| **Sistema Operativo** | Windows 11 (10.0.22621) 64-bit |
| **SKU OS** | Windows Home Single Language (1L) |
| **Región** | México (MX), LatAM |
| **Idioma** | Español (es) |
| **OS Build Date** | 2025-09-25 |
| **Manufacturing Region** | 2023q2.005 |
| **Amundsen DID** | e607nr399pq63y43385048 |
| **AWC Version** | 2.9.25180 |

---

## Archivos Extraídos

### 1. PreloadBackup.zip (87 MB)
Extraído en `/Acer_Extracted/PreloadBackup/OEM/Preload/`
- Contiene los archivos de pre-carga OEM completos
- Algunos archivos en `utility/GCM/` y `utility/` están protegidos con contraseña

### 2. PowerSetting.ppkg (5.4 KB)
Archivo de provisioning package de Windows. Contenido:
- Configuración de plan de energía "Better Battery Life" (DC overlay)
- GUID del esquema: `961cc777-2547-4f9d-8174-7d86181b8a7a`

### 3. usmt.ppkg (4.3 GB)
Archivo USMT (User State Migration Tool) - Contiene 16,708 archivos con backup de estado de usuario y configuraciones de sistema para restauración de fábrica.

---

## Enlaces y URLs Descubiertos

### Acer (Oficiales)

| URL | Descripción |
|-----|-------------|
| `https://www.acer.com/support` | Soporte oficial de Acer |
| `https://www.acer.com/worldwide/` | Lista de oficinas mundiales |
| `https://www.acer.com/legal` | Información legal de Acer |
| `https://www.acer.com/us-en/privacy/california-privacy` | Privacidad California |
| `https://www.acer.com/br-pt/privacy/index.html` | Privacidad Brasil |
| `https://www.acer.com/ac/de/DE/content/data-subject-access-request-form` | Solicitud de acceso a datos (UE) |
| `https://www.acer.com/ac/en/GB/content/privacy` | Políticas de privacidad (UE/UK) |
| `https://www.acer.com/ac/en/GB/content/data-subject-access-request-form` | Solicitud de acceso (Sudáfrica) |
| `https://www.acer.com.cn/chinese_privacy.html` | Privacidad China |
| `https://www.acer-group.com/ag/en/TW/content/office-list` | Lista de oficinas Acer Group |
| `http://www.acer.com/support` | Soporte Acer (IE) |

### Actualizaciones y Drivers (Acer Live Updater)

| URL | Descripción |
|-----|-------------|
| `https://aluwsv2.acer.com/ServerInfo/ES/Aspire A315-59/Aspire A315-59_10M1_ES.xml` | Firmware/Driver updates (ES) |
| `https://aluwsv2.acer.com/ServerInfo/ES/ALU_APP/ALU_APP_10M1_ES.xml` | Aplicaciones (ES) |

### Amundsen (Acer Content Delivery Platform)

| URL | Descripción |
|-----|-------------|
| `https://s3.amazonaws.com/amundsen/ares` | CDN de contenido Amundsen |
| `https://s3.amazonaws.com/amundsen/redirect/19q2/booking.html` | Redirect Booking.com |

### Bing y MSN

| URL | Descripción |
|-----|-------------|
| `http://www.bing.com/search?q={searchTerms}&form=PRACE1&src=IE11TR&pc=ACTE` | Búsqueda predeterminada Bing |
| `http://api.bing.com/qsml.aspx?...` | Bing Suggestions API |
| `http://www.bing.com/favicon.ico` | Bing favicon |
| `http://www.msn.com/?pc=ACTE` | Página de inicio MSN |

### Contacto

| Contacto | Tipo |
|----------|------|
| `privacy.officer@acer.com` | Oficial de privacidad |
| `privacy_officer@acer.com` | Oficial de privacidad (alternativo) |

---

## Software Preinstalado Detectado

### Apps Acer
1. **Acer Care Center** - Centro de soporte y actualizaciones
2. **Acer Registration** (AcerIncorporated.AcerRegistration_48frkmn4z8aw4) - App de registro UWP
3. **AlaunchX** - Lanzador de aplicaciones Acer (post-OOBE)
4. **Amundsen** (v2) - Plataforma de entrega de contenido/partners de Acer
5. **UEIP Framework** (v5.00.3018) - User Experience Improvement Program
6. **PowerSettings** - Gestión de energía Acer

### Software de Terceros Detectado en APBundlePolicy.xml
- **Microsoft Office** (SV2 May CI) - Office Installer
- **McAfee** - Software de seguridad (referenciado en OOBE)
- **Amazon** - Weblink
- **Booking.com** - Weblink
- **Forge of Empires** - Weblink (juego)

### Drivers Pre-cargados (OEM/Preload/Autorun/DRV/)
- Acer Device Enabling Service (FUB)
- Acer Airplane Mode Controller
- Acer Application Base Driver
- Acer Purified Voice Console (Audio)
- Intel DPTF (Dynamic Platform Thermal Framework)
- Intel Gaussian and Neural Accelerator (GNA)
- Intel HID Event Filter
- Intel Rapid Storage Technology (iRST) + HSA + WinPE
- Intel Serial I/O + WinPE
- Intel VGA UMA + Utility
- Intel NB Chipset (Alder Lake)
- Intel TurboBoost / Manageability Engine
- MTK Bluetooth (MTK7663 / MTK7902)
- MTK Wireless LAN (MTK7663 / MTK7902 + WinRE)
- Realtek Audio Codec (ALC256M) + Audio Console UWP
- Realtek LAN (RTL8111H + WinRE)

---

## Configuración de Restauración (Recovery)

### Archivos clave:
- `Unattend.xml` - Configuración de Windows Setup
- `oemsetup.OEMTA.5227.cmd` - Setup de OEM
- `RestoreOEMCustomize.cmd` - Script principal de restauración
- `TaskbarLayoutModification.xml` - Layout de la barra de tareas
- `OOBE/Info/userchoices.xml` - Configuración de registro OOBE
- `UpdateOOBE.cmd` - Actualización de configuración OOBE

### Wallpapers OEM:
- 5 wallpapers Acer en 3840x2400 (JPG, ~3-5 MB cada uno)

### Comandos de Restauración:
- `RestoreDiskSettings.cmd` - Configuración de disco
- `RestoreSCMID_Acer.cmd` - SCM ID
- `RestoreVBSEnablement.cmd` - Virtualization-Based Security
- `RestoreAcerPowerPlan.cmd` - Plan de energía
- `PBR_RestoreCameraFrequency.cmd` - Frecuencia de cámara
- `PBR_RestoreCameraDshowBridges.cmd` - Puentes DirectShow cámara
- `PBR_RestoreVMPSetting.cmd` - Configuración VMP
- `SetRebuildindexForExplorer.cmd` - Reconstruir índice Explorer
- `Sub_SetURL_Acer.cmd` - Configurar URL de soporte en IE
- `EnableVBS.txt` - Habilitar VBS

---

## Análisis de .ppkg

### PowerSetting.ppkg
- **Tipo**: WIM (Windows Imaging Format)
- **Package ID**: `{5d723d27-6365-4425-ac61-c6d6a93a2dcd}`
- **Versión**: 1.0, Altitude 2060
- **Target**: Desktop SKUs
- **Contenido**: Configuración de esquema de energía DC overlay a "BetterBatteryLife"

### usmt.ppkg (4.3 GB)
- **Tipo**: WIM (Windows Imaging Format)
- **Package ID**: `{6BF994D7-552D-4314-8C7D-863CC348878F}`
- **Nombre**: "Initial Configuration Blob"
- **Contenido**: 16,708 archivos en 2,450 directorios
- **Tamaño descomprimido**: ~11.47 GB
- **Propósito**: Backup completo de estado de usuario y sistema para restauración de fábrica

---

## Información del Sistema

| Componente | Detalle |
|------------|---------|
| **CPU** | Intel Alder Lake (12va Gen) |
| **Chipset** | Intel NB Chipset M (Alder Lake) |
| **WiFi/BT** | MediaTek MTK7663 o MTK7902 |
| **Audio** | Realtek ALC256M |
| **Ethernet** | Realtek RTL8111H |
| **Gráficos** | Intel UMA (Integrated) |
| **Almacenamiento** | NVMe (Intel RST) |

---

## Notas

1. **No se encontraron enlaces de descarga de ISO de factory/Windows** - Este equipo usa el sistema de recuperación de Acer basado en la partición de recovery y el archivo `usmt.ppkg`.

2. **Archivos protegidos con contraseña** en PreloadBackup.zip:
   - `utility/GCM/FVMRD.xml`
   - `utility/GCM/GCMT_V.log`
   - `utility/GCM/SMRMMRD.json`
   - `utility/GCM/SMRMMRD_utf8.json`
   - `utility/GCM/TBMRD.xml`
   - `utility/PreloadSystemInfo_*.log`
   - `utility/vols.log`

3. **ProductInfo.enc** está encriptado (contiene información del producto).

4. El equipo fue manufacturado para la región **Latinoamérica (México)** con Windows 11 Home Single Language en español.

5. **Partner Search Code**: ACTS (Acer)
6. **Trusted Image Identifier**: `POP01S0E99X00C01-PAP010ZT99X04C21`