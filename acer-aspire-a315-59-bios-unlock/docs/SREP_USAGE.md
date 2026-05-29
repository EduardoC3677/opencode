# Uso de SmokelessRuntimeEFIPatcher (SREP)

SREP es una aplicacion EFI que carga modulos UEFI, les aplica parches en memoria y luego transfiere control al modulo parchado (ej. `SetupUtilityApp`).

## Funcionamiento (analisis del codigo)

1. SREP arranca en EFI Shell
2. Lee el primer archivo `*.cfg` que encuentre en el mismo directorio
3. Parsea las operaciones `Op <Name>` linea por linea
4. Para cada modulo (`Op Loaded` o `Op LoadFromFV`):
   a. Si esta cargado: lo encuentra en la memoria via ImageHandle
   b. Si no esta cargado: lo carga desde el FV (Firmware Volume) actual
5. Aplica los patches secuencialmente
6. Llama `Op Exec` para transferir control al modulo (entry point)

## Operaciones soportadas (resumen de la documentacion del autor)

| Op | Descripcion |
|---|---|
| `Op Loaded` | Busca un modulo ya cargado en RAM por nombre o GUID |
| `Op LoadFromFV` | Carga un modulo desde el FV (Firmware Volume) actual |
| `Op LoadGUIDandSavePE` | Carga PE seccion por GUID y la guarda como archivo |
| `Op LoadGUIDandSaveFreeform` | Carga FREEFORM seccion por GUID |
| `Op NonamePE` | Busca PE en RAM por GUID (modulos sin UI section) |
| `Op NonameTE` | Busca TE en RAM por GUID |
| `Op Patch` | Aplica patch reemplazando TODAS las ocurrencias del pattern |
| `Op FastPatch` | Aplica patch solo a la PRIMERA ocurrencia |
| `Op Skip <N>` | Salta N lineas del cfg (control de flujo condicional) |
| `Op UninstallProtocol` | Desinstala un protocolo EFI por GUID |
| `Op Compatibility` | Configura FilterProtocol para compatibilidad |
| `Op HandleIndex <N>` | Selecciona EFI_HANDLE por indice |
| `Op UpdateHiiPackage` | Update package HII por GUID |
| `Op Exec` | Transfiere control al ultimo modulo cargado |
| `Op End` | Marca fin de un bloque de operaciones para un modulo |

## Sintaxis de Op Patch

```
Op Patch
<Modo>            # uno de: Pattern, Offset, RelNegOffset, RelPosOffset
<Argumento1>      # depende del modo (pattern hex, offset hex, etc)
<Argumento2>      # patch hex bytes
```

Wildcards en pattern (solo en SREP-RUS \>=0.2.x):
- `.` (punto) - cualquier nibble
- Clases regex segun https://gist.github.com/kaigouthro/e8bad6a2c8df6ff13b8716027a172dc0#3-character-types

Ejemplo:
```
Op Patch
Pattern
0A8214 08 .. 0001000100         # XX = cualquier byte
0A8247 08 00 0000000000         # reemplazo: NOP
```

## Preparacion del USB para SREP

1. Formato FAT32 (UEFI requirement)
2. Estructura:
```
USB:
  EFI/
    BOOT/
      BOOTX64.EFI    # UEFI Shell o el SREP renombrado
  SREP.efi           # binario compilado
  SREP_CONFIG.cfg    # config de parches (este repo)
```

3. (Opcional) Crear `startup.nsh` con:
```
fs0:
SREP.efi ENG
```

## Configuracion de la BIOS antes de bootear SREP

1. Entrar BIOS Setup (F2)
2. Deshabilitar **Secure Boot** (Security > Secure Boot Mode > Disabled)
3. Asegurar **CSM Disabled** (Boot > CSM Support: Disabled) -> arranque UEFI puro
4. (Opcional) Deshabilitar **TPM** si causa problemas
5. Guardar y reiniciar
6. Pulsar F12 para Boot Menu
7. Seleccionar el USB

## Que pasa si los parches no coinciden

Si un patron de `Op Patch` no se encuentra:
- SREP escribe en log: \"Pattern not found\"
- No se aplica el patch
- Continua con el siguiente `Op Patch`

**No** se brickea nada porque SREP solo escribe en RAM. Tras reiniciar, la memoria se limpia y el BIOS vuelve al estado original.

## Logs

SREP escribe un log `SREP.log` en el USB que puede consultarse despues. La version RUS tiene logs en ruso por defecto; usar `SREP.efi ENG` para ingles.

## Persistencia

**SREP NO persiste**. Cada arranque hay que volver a ejecutarlo. Si quieres persistencia:

1. Modificar el orden de boot para arrancar SREP por defecto
2. O usar un `StdLib` DXE para reemplazar el SetupUtility original (avanzado)
3. O hacer un flashing modificado del SPI (requiere programador externo; brickea si falla)
