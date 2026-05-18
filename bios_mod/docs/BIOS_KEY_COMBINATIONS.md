# Acer Aspire A315-59 (Insyde H2O) - Combinaciones de teclas del BIOS

> Este documento recopila las **combinaciones de teclas oficiales y no
> documentadas** de los firmwares **Insyde H2O** usados por Acer en los
> Aspire 2020+ (incluido el A315-59 / Alder Lake-N) y, donde aplica, las
> formas de revelar menús avanzados sin tocar el firmware.

## 1. Atajos durante el POST (al encender)

| Tecla / combinación | Función                                                     |
|---------------------|-------------------------------------------------------------|
| **F2**              | Entrar al **BIOS Setup** (Insyde H2O Setup Utility)         |
| **F12**             | **Boot Menu** (selección manual de dispositivo de arranque) |
| **Alt + F10**       | **Acer Recovery (Acer eRecovery / Care Center)**             |
| **F9**              | Mostrar **Diagnostics** Insyde en algunos builds            |
| **Esc**             | Cancelar splash logo y mostrar mensajes POST                |
| **Fn + Esc**        | Algunos modelos: **silent boot toggle**                     |
| **F8**              | Menú "Advanced Boot Options" (Windows)                      |
| **Ctrl + F2**       | Entrar a Setup desde el splash (en algunas revisiones)      |
| **Ctrl + F12**      | EFI Shell en builds antiguos (no en A315-59 stock)          |

> **Importante:** el A315-59 con **Fast Boot** activado puede ignorar F2.
> Para garantizar entrar: enchufar el cargador, mantener **F2** *desde*
> el encendido, o usar **Settings -> Recovery -> Advanced startup ->
> UEFI firmware settings** desde Windows.

## 2. Atajos *dentro* del Setup Utility (Insyde H2O)

| Tecla              | Función                                                     |
|--------------------|-------------------------------------------------------------|
| `↑ / ↓`            | Mover entre opciones                                        |
| `← / →`            | Cambiar de pestaña (Main / Security / Boot / Exit, etc.)    |
| `Enter`            | Abrir submenu / cambiar valor                               |
| `Esc`              | Volver / salir                                              |
| `F1`               | Ayuda contextual                                            |
| `F5 / F6`          | Decrementar / Incrementar valor                             |
| `F9`               | **Load Setup Defaults**                                     |
| `F10`              | **Save & Exit**                                             |
| `+ / -`            | Mover en boot order                                         |

## 3. Atajos *no documentados* para "Advanced" en Insyde H2O

Estas combinaciones provienen del **driver `SetupUtility`** y de
implementaciones histórico-conocidas de Insyde H2O. **NO existen en todas
las versiones** y, en muchos firmwares modernos (incluyendo el del
A315-59 1.31), están **deshabilitadas o sólo desbloquean parcialmente**.
Se documentan aquí como referencia y porque algunos OEM Acer las dejan
parcialmente activas:

| Combinación                  | Efecto histórico                                         |
|------------------------------|----------------------------------------------------------|
| **Fn + Tab**                 | Mostrar "Diagnostics" oculta en algunos Insyde Acer      |
| **Ctrl + S** (en Main page)  | Activa el menú "**Advanced**" (clásico Insyde H2O)       |
| **Ctrl + F1** o **Ctrl + A** | Toggle de "Advanced view" en algunas BIOS Insyde         |
| **Fn + Ctrl + F1**           | Igual que arriba en algunas Acer (versión 2017-2020)     |
| **Shift + Ctrl + Alt + F2**  | "Insyde maintenance mode" - sólo en BIOS de producción   |
| **Ctrl + Alt + F4**          | Algunos OEM: "OEM Diagnostic"                            |
| **Ctrl + R**                 | "Restore Configuration"                                  |
| **A + S** (al encender)      | Modo recovery EC (Insyde crisis recovery)                |

> En el BIOS 1.31 del Aspire A315-59 **el atajo Ctrl+S y similares están
> deshabilitados** por el módulo `A01ODMDxeDriver` de Acer (la rutina
> ejecuta `TEST` + `JE` para retornar `EFI_UNSUPPORTED 0x8000000000000003`,
> como se ve en la dirección de archivo `0x15B2` del PE). Por eso es
> necesario usar **SREP** (ver `../Acer_A315-59/SREP_CONFIG.cfg`) para
> habilitar el menú avanzado en runtime.

## 4. Recuperación / desbloqueo de contraseña

| Combinación                  | Efecto                                                   |
|------------------------------|----------------------------------------------------------|
| **Ctrl + Alt + Esc** en POST | Algunos Acer: borrar Supervisor Password (no en A315-59) |
| Remove CMOS battery 5 min    | **NO funciona** en Acer 2020+: la password está en NVRAM SPI cifrada |
| Acer "PHF" master password   | Generables a partir del HASH mostrado tras 3 intentos    |

## 5. Cómo desbloquear realmente el menú avanzado en el A315-59

Hay **tres opciones**, en orden de seguridad:

1. **Runtime patcher (recomendado - reversible)**:
   Usar `SmokelessRuntimeEFIPatcher` 0.2.x + el `SREP_CONFIG.cfg` de este
   repo. Se ejecuta cada arranque desde un pendrive UEFI. Si algo va mal:
   se quita el pendrive y el BIOS vuelve a ser el de fábrica. **Cero
   modificación permanente.**
   * Carpeta: [`../Acer_A315-59/SREP_CONFIG.cfg`](../Acer_A315-59/SREP_CONFIG.cfg)
   * Tool: <https://github.com/Maxinator500/SmokelessRuntimeEFIPatcher-RUS>
   * Patches base: <https://github.com/Maxinator500/SREP-Patches>

2. **Mod permanente del firmware**:
   Patchear `SetupUtility` + `A01ODMDxeDriver` con UEFITool/`hex` y volver
   a empaquetar la cápsula con `H2OFFT-Wx64.exe`. Riesgo: brick si Boot
   Guard / Capsule Signature está activo. En Insyde H2O moderno, el flash
   exige cápsula firmada -> hace falta o bien:
   * Programar la SPI **off-board** con un CH341A/RPi (lectura
     directa del chip Winbond W25Q256, 32 MB).
   * O usar la **`H2OFFT` IHISI SecureCapsule bypass** que requiere
     SMI privilegiado (no disponible en stock).

3. **EFI variable hack** (no requiere reflash):
   Algunas opciones Insyde están controladas por variables NVRAM con GUID
   `EFI_SETUP_VARIABLE`. Editarlas con `setup_var` (Linux) o `RU.efi`
   permite alternar features individuales. SREP es básicamente la
   automatización de esto + patches IFR.

## 6. Tabla de scancodes que el SetupUtility de Acer reconoce

(Extraídos de las cadenas internas y los IFR opcodes `0F 02 ...` que el
driver registra como "hot keys"):

| Hex | Tecla   | Función registrada                              |
|-----|---------|-------------------------------------------------|
| `0x05` | F1   | Help                                            |
| `0x06` | F2   | Setup entry                                     |
| `0x09` | F5   | Decrement                                       |
| `0x0A` | F6   | Increment                                       |
| `0x0D` | F9   | Load defaults                                   |
| `0x0E` | F10  | Save & exit                                     |
| `0x0F` | F11  | (reserved)                                      |
| `0x10` | F12  | Boot menu                                       |
| `0x17` | ESC  | Cancel / back                                   |
| `0x0D + Ctrl` | C-F9  | Acer "load OEM defaults"                |
| `0x06 + Shift`| S-F2  | Insyde "advanced mode" - **bloqueado en A315-59**, requiere SREP |

## 7. Variables NVRAM relevantes (descubiertas con strings)

| Nombre                  | GUID                                          | Uso                                  |
|-------------------------|-----------------------------------------------|--------------------------------------|
| `Setup`                 | `EC87D643-EBA4-4BB5-A1E5-3F3E36B20DA9`        | Setup variable principal de Insyde   |
| `SetupData`             | `FE612B72-203C-47B1-8560-A66D946EB371`        | Datos extendidos                     |
| `OemSetup`              | `F76B9456-A2FB-4D55-9D86-7CFE2D9EBE0C`        | Configuración OEM Acer               |
| `Custom`                | `B58325AF-77D0-4A3B-BBBB-1105AB8497CA`        | Personalización (FFS GUID)           |
| `AcerSysCfg`            | (interno Acer)                                | Bandera "Advanced Menu" oculta       |

Editando manualmente el offset correcto de `Setup` (vía `RU.efi`,
`UEFI Shell setup_var`, o `H2ORUDK`) se pueden alternar features
individuales sin parchear código. SREP simplemente automatiza esa
edición y, además, neutraliza los bits IFR de "suppress-if".
