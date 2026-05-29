# Estructura interna del BIOS y modulos analizados

## SetupUtility.efi (FE3542FE-C1D3-4EF8-657C-8048606FF670)

Modulo principal del menu BIOS Setup en Insyde H2O. Contiene:
- Codigo del navegador HII (text-mode)
- Formularios HII (VFR compilado) embebidos como recursos
- Logica de password (supervisor / user)
- Handlers de boot configuration, security, etc.

### Layout del PE32+

| Seccion | VA | VSize | Raw | Funcion |
|---|---|---|---|---|
| .text | 0x280 | 0x49234 | 0x49240 | Codigo ejecutable |
| .data | 0x494C0 | 0x109958 | 0x109960 | Variables + VFR compilado |
| (unnamed) | 0x152E20 | 0x2094 | 0x20A0 | Datos auxiliares |
| .xdata | 0x154EC0 | 0x1428 | 0x1440 | Exception unwind |
| .reloc | 0x156300 | 0x1F8 | 0x200 | Relocaciones PE |

### Codigo HII embebido

El VFR (Visual Forms Representation) compilado vive en `.data`. Los opcodes relevantes para el desbloqueo son:

| Opcode | Nombre | Descripcion |
|---|---|---|
| 0x0A | SUPPRESS_IF | Oculta un item si la condicion es verdadera |
| 0x19 | GRAYOUT_IF | Item visible pero no seleccionable |
| 0x0E | NO_SUBMIT_IF | No envia cambios si condicion es verdadera |
| 0x82 | TRUE | Constante booleana TRUE |
| 0x47 | END_IF | Cierre del bloque IF |
| 0x12 | SUBTITLE | Item de seccion (titulo) |
| 0x14 | ONE_OF | Item desplegable de opciones |
| 0x05 | FORM | Definicion de un sub-formulario |

Cuando vemos un patron como `0A 82 14 08 XX 00 ...`, significa:
- `0A 82` = SUPPRESS_IF TRUE  (siempre verdadero -> siempre suprime)
- `14 08` = ONE_OF opcode, 8 bytes de configuracion siguiente
- `XX 00` = ID del item

El parche reemplaza esto por `0A 82 47 08 ...` (47 = END_IF -> el item queda sin SUPPRESS) o por NOPs equivalentes para mantener alineamiento.

## A01ODMDxeDriver.efi

Driver DXE especifico de Acer ODM (Original Design Manufacturer). Verifica el modelo y branding al inicio para habilitar/deshabilitar features.

### Patron clave encontrado

En offset `0x15B2`:
```asm
74 0C                je      +0x0C        ; salto a la rama 'desbloqueada'
48 B8 03 00 00 00 00 00 00 80  movabs rax, 0x8000000000000003
```

Parchado por SREP:
```asm
EB 0C                jmp     +0x0C        ; salto INCONDICIONAL
48 B8 03 00 00 00 00 00 00 80  ...
```

Esto fuerza el path 'desbloqueado' sin importar el resultado del check ODM previo.

## SetupUtilityApp.efi (vs SetupUtility.efi)

`SetupUtilityApp` es un *EFI Application* tiny (10 KB) que sirve como **entry point** que dispara el formulario Setup. `SetupUtility` es el *Driver* DXE que registra los protocolos.

SREP termina ejecutando `SetupUtilityApp` (`Op LoadFromFV` + `Op Exec`) DESPUES de parchear `SetupUtility`, asi cuando se invoque el Form Browser, los items SUPPRESS_IF ya estaran desactivados.

## Otros modulos analizados

### H2OFormBrowserDxe.efi (347 KB)
Implementacion Insyde del HII Form Browser Protocol. Interpreta los opcodes HII y renderiza la UI. No requiere parchado para nuestro objetivo.

### H2OFormDialogDxe.efi (80 KB)
Dialog popups (confirmaciones, password prompts).

### H2ODisplayEngineLocalTextDxe.efi (157 KB)
Renderizado text-mode en pantalla.

### H2OKeyDescDxe.efi (9 KB)
Solo descripciones de teclas para la UI (key name -> string).

### HddPassword.efi
Logica de password de disco duro (ATA Security). No relevante para nuestro objetivo.
