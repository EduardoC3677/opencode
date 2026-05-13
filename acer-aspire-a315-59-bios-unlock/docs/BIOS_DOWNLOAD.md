# Descarga y extraccion del BIOS Acer Aspire A315-59

## 1. Descarga

```bash
curl -L -o bios.zip 'https://global-download.acer.com/GDFiles/BIOS/BIOS/BIOS_Acer_1.31_A_A.zip?acerid=638876442878329689&Step1=&Step2=&Step3=ASPIRE%20A315-59&OS=ALL&LC=es&BC=ACER&SC=EMEA_11'
```
Tamano: 12.6 MB

## 2. Descomprimir ZIP

```bash
unzip bios.zip
```
Contenido: `HH5A4131.exe` (12.8 MB)

Identificacion: `file HH5A4131.exe` -> PE32+ executable (GUI) x86-64, 5 sections

## 3. Identificar tipo de instalador

El PE tiene un overlay grande (13 MB) despues del fin de secciones. Examinando los primeros bytes del overlay (offset 0x39200):

```
3b2140496e7374616c6c402155544638  -> ';!@Install@!UTF-8!'
```

**Firma de 7-Zip SFX**.

## 4. Extraer el 7z SFX

```bash
7z x HH5A4131.exe -osfx/ -y
```

Esto extrae 16 archivos:

| Archivo | Tamano | Funcion |
|---|---|---|
| **abobios.bin** | 37 MB | **Imagen UEFI completa del BIOS** |
| H2OFFT-Wx64.exe | 6.9 MB | Flasher Insyde H2O |
| H2OFFT64.sys | 47 KB | Driver kernel del flasher |
| H2OFFT.inf | 6.5 KB | Driver install info |
| H2OFFT.cat | 10 KB | Driver catalog (firma) |
| InterToolx64.efi | 1.3 MB | Tool EFI auxiliar |
| BiosImageProcx64.dll | 280 KB | DLL de procesamiento BIOS image |
| FWUpdLcl.exe | 220 KB | Intel FW Update Tool (ME firmware) |
| FlsHook.exe | 41 KB | Hook de flash |
| platform.ini | 65 KB | Config del flasher (modelos compatibles, regiones) |
| mfc90u.dll, msvcp90.dll, msvcr90.dll | - | Runtime VC9 |
| Microsoft.VC90.CRT.manifest, MFC.manifest | - | Manifests SxS |
| Ding.wav | 103 KB | Sonido notificacion |

## 5. Verificar abobios.bin

```bash
file abobios.bin
# data

# Buscar la firma de Flash Descriptor o FV header
head -c 16 abobios.bin | xxd
```

## 6. Extraccion UEFI con UEFIExtract

```bash
wget https://github.com/LongSoft/UEFITool/releases/download/A74/UEFIExtract_NE_A74_x64_linux.zip
unzip UEFIExtract_NE_A74_x64_linux.zip
chmod +x uefiextract
./uefiextract abobios.bin all
```

Genera:
- `abobios.bin.dump/` (arbol completo del firmware, 2,933 directorios)
- `abobios.bin.report.txt` (reporte con descripcion de cada nodo)
- `abobios.bin.guids.csv` (base de datos de GUIDs encontrados)

## 7. Estructura UEFI encontrada (alto nivel)

El BIOS Insyde sigue la layout estandar Intel firmware con Flash Descriptor + ME + GbE + BIOS region. El arbol relevante:

```
abobios.bin.dump/
  0 Padding
  7 1FD0BACE-6F0A-4085-901E-F6210385CB6F   <- BIOS region (FFS)
    0 20BC8AC9-94D1-4208-AB28-5D673FD73486 <- FV principal
      0 EE4E5898-3914-4259-9D6E-DC7BD79403CF
        1 Volume image section
          0 8C8CE578-8A3D-4F1C-9935-896185C32DD3 <- FV de DXE drivers (cientos de modulos)
            128 SetupUtilityApp
            266 SetupUtility           <- GUID FE3542FE-C1D3-4EF8-657C-8048606FF670
            156 H2OFormBrowserDxe
            99  H2OKeyDescDxe
            370 A01ODMDxeDriver        <- ODM checks (Acer-specific)
            371 A01ODMSmmServiceDriver
            380 AcerBIOSConfigurationToolDxe
            381 AcerBIOSConfigurationToolSmm
            384 AcerDeviceEnabling
            489 AcerExtensionVirtualDevice
  12 CF1406C5-3FEC-47EB-A6C3-B71A3EE00B95 <- PEI volume
```

## 8. Extraccion de modulos PE32+ EFI

Cada modulo tiene un subdirectorio `X PE32 image section/body.bin` que contiene el ejecutable PE32+ x86_64. Para extraer los modulos clave:

```bash
BASE="abobios.bin.dump/7 1FD0BACE-6F0A-4085-901E-F6210385CB6F/0 20BC8AC9-94D1-4208-AB28-5D673FD73486/0 EE4E5898-3914-4259-9D6E-DC7BD79403CF/1 Volume image section/0 8C8CE578-8A3D-4F1C-9935-896185C32DD3"
for d in '266 SetupUtility' '128 SetupUtilityApp' '156 H2OFormBrowserDxe' '370 A01ODMDxeDriver'; do
  name=$(echo "$d" | awk '{print $2}')
  cp "$BASE/$d/2 PE32 image section/body.bin" "modules/${name}.efi" 2>/dev/null || \
  cp "$BASE/$d/1 PE32 image section/body.bin" "modules/${name}.efi"
done
```

Tamanos resultantes:
- SetupUtility.efi: 1,402,112 bytes (1.4 MB)
- SetupUtilityApp.efi: 10,240 bytes
- H2OFormBrowserDxe.efi: 347,072 bytes
- H2OKeyDescDxe.efi: 9,152 bytes
- A01ODMDxeDriver.efi: 30,784 bytes
