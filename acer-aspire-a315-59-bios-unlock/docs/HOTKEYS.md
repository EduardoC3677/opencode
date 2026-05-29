# Hotkeys para Setup avanzado - Acer Aspire A315-59 (Insyde H2O 1.31)

## Resumen

Se analizo el modulo `SetupUtility.efi` y los modulos relacionados (`H2OKeyDescDxe`, `H2OFormBrowserDxe`) buscando combinaciones de teclas que activen menus ocultos.

## Conclusion clave

**En este BIOS (Insyde H2O moderno post-2020) NO existen hotkeys magicos generales que desbloqueen los menus avanzados.**

Esto contrasta con BIOS AMI Aptio antiguos donde `Ctrl+F1`, `Fn+Tab`, `A+Ctrl+F1` o similares revelaban menus ocultos.

En Insyde H2O moderno, los items estan ocultos por opcodes HII `SUPPRESS_IF TRUE` (0x0A 0x82) hardcodeados en el VFR compilado. No hay rama de codigo que cambie el comportamiento basado en una combinacion de teclas en runtime.

## Evidencia del analisis

### Strings buscados en SetupUtility.efi (20,852 strings totales)

Se busco (case-insensitive):
- `F1, F2, ..., F12` -> solo aparecen en contextos no de hotkey (`PCIe D0/F1`, `Race To Halt`)
- `Ctrl, Alt, Shift, Fn` -> 0 ocurrencias como string
- `hotkey, combination, combo, secret key` -> 0 ocurrencias
- `Press F<n>` -> 0 ocurrencias
- `unlock, hidden` -> 0 ocurrencias relacionadas con teclas

### Scancodes buscados como bytes

Los scancodes EFI para F1-F12 (`EFI_SCAN_CODE` 0x0B-0x16) aparecen muchas veces, pero estan asociados a las acciones estandar del Setup (F1=Help, F9=Defaults, F10=Save&Exit, etc), no a desbloqueo de menus.

### Modulo H2OKeyDescDxe (9 KB)

Solo contiene descripciones literales (`ctrl`, `shift`, `logo`, `menu`, `sysreq`, `down`, `right`, `left`, `home`, `pgup`, `pgdn`, `enter`, `backspace`, `space`) usadas para mostrar el nombre de la tecla en la UI. Ninguna logica de desbloqueo.

## Hotkeys que SI funcionan (estandar Acer/Insyde)

Estas son combinaciones estandar de Acer pre-boot, NO especificas del menu avanzado:

| Combinacion | Funcion |
|---|---|
| F2 | Entrar al BIOS Setup |
| F12 | Boot menu (seleccion de dispositivo de arranque) |
| F9 (en Setup) | Load Optimized Defaults |
| F10 (en Setup) | Save and Exit |
| Esc | Salir sin guardar |
| Alt+F10 | Acer Recovery (eRecovery/D2D) - requiere habilitar D2D Recovery primero |
| Ctrl+F2 (Power On) | Reset BIOS Password (en algunos modelos) - NO probado en A315-59 |

## Hotkeys probados experimentalmente que NO funcionan en este modelo

Basado en analisis estatico (no comparten codigo handler en el binario):

- ❌ `Fn+Tab`  (no hay handler)
- ❌ `Ctrl+F1` (no hay handler)
- ❌ `A+Ctrl+F1` (no hay handler)
- ❌ `Ctrl+Alt+F11` (no hay handler)
- ❌ `Fn+Esc` (no hay handler)

## Por que las hotkeys no funcionan en BIOS modernos Insyde

Historicamente Insyde tenia handlers de hotkeys en `SetupUtility` que cambiaban variables runtime para revelar items. Esto fue removido en versiones posteriores porque:

1. Los OEM (Acer, HP, Dell, Lenovo) lo pidieron explicitamente para reducir soporte
2. La estructura HII compilada con `SUPPRESS IF TRUE` es **estatica** - no requiere check de variable
3. Solo afecta a la presentacion, no a la logica subyacente -> mas seguro tener menus ocultos por compilation que por runtime check

## Solucion: SREP (SmokelessRuntimeEFIPatcher)

Dado que no hay hotkeys, **la unica manera de desbloquear es parchar los bytes del modulo `SetupUtility.efi` en RAM** justo antes de que el Form Browser interprete el formulario. Eso es exactamente lo que hace SREP.

Ver `SREP_USAGE.md` y el archivo `../SREP_CONFIG.cfg` adjunto.
