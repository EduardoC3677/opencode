#!/usr/bin/env bash
# Reproductor del análisis del C.zip de Acer Aspire A315-59.
# Uso: ./reproduce.sh <ruta-al-C.zip>
# Requiere: 7z, john, hashcat, wimtools (wimlib-imagex), dotnet/ilspycmd,
#          exiftool, radare2.

set -euo pipefail

ZIP="${1:?Uso: $0 <C.zip>}"
OUT="${2:-./out}"
mkdir -p "$OUT"

echo "[1/9] Listar y verificar cifrado del ZIP"
7z l "$ZIP" | tee "$OUT/zip_listing.txt"
if grep -qE "\\bAES\\b|^\\+\\s*Encrypted" "$OUT/zip_listing.txt"; then
  echo "    ⚠️  ZIP CIFRADO — intentar zip2john + wordlist"
  zip2john "$ZIP" > "$OUT/pb.hash" || true
  john --wordlist="$(dirname "$0")/oem_wordlist.txt" "$OUT/pb.hash" || true
  john --show "$OUT/pb.hash" | tee "$OUT/pb_password.txt"
else
  echo "    ✅ ZIP no cifrado — extraer directo"
fi

echo "[2/9] Extraer (excluyendo usmt.ppkg por tamaño)"
7z x "$ZIP" -o"$OUT/extract" -x'!Recovery/Customizations/usmt.ppkg' -y

echo "[3/9] Localizar usmt.ppkg y obtener metadata"
find "$OUT/extract" -name 'usmt.ppkg' -print0 | xargs -0 -I{} sh -c '
  echo "==> {}"
  ls -lh "{}"
  file "{}"
  7z l "{}" | head -50
' || true

echo "[4/9] Si usmt.ppkg es un CAB/PPKG válido, extraer install.wim e inspeccionar"
PPKG="$(find "$OUT/extract" -name 'usmt.ppkg' | head -1)"
if [ -n "$PPKG" ]; then
  mkdir -p "$OUT/ppkg_extract"
  7z x "$PPKG" -o"$OUT/ppkg_extract" -y || true
  WIM="$(find "$OUT/ppkg_extract" -iname 'install.wim' | head -1)"
  if [ -n "$WIM" ]; then
    wiminfo "$WIM" | tee "$OUT/wim_info.txt"
    wimdir "$WIM" 1 | head -200 > "$OUT/wim_root.txt"
  fi
fi

echo "[5/9] Decompilar binarios .NET"
mkdir -p "$OUT/decomp"
for exe in $(find "$OUT/extract" -name '*.exe'); do
  out="$OUT/decomp/$(basename "$exe" .exe)"
  mkdir -p "$out"
  ilspycmd "$exe" -p -o "$out" 2>/dev/null && echo "    .NET: $exe" || rmdir "$out" 2>/dev/null
done

echo "[6/9] Strings de los .exe y .dll"
mkdir -p "$OUT/strings"
find "$OUT/extract" \( -name '*.exe' -o -name '*.dll' \) -print0 \
  | xargs -0 -I{} sh -c 'base=$(basename "{}"); strings -a    -n 6 "{}" > "$1/${base}.ascii.txt"; strings -a -e l -n 6 "{}" > "$1/${base}.utf16.txt"' _ "$OUT/strings"

echo "[7/9] Análisis radare2 de binarios C++ (no-.NET)"
mkdir -p "$OUT/r2"
for exe in $(find "$OUT/extract" -name 'AcerCCAgent.exe' -o -name 'ACCUserPS.exe' -o -name 'CheckFiles.exe' -o -name 'OBRSetTool_amd64.exe' -o -name 'RunCmd_X64.exe'); do
  base=$(basename "$exe" .exe)
  r2 -qc 'aaa; iI; iE; izq~http; izq~acer; afl' "$exe" > "$OUT/r2/${base}.txt" 2>&1 || true
done

echo "[8/9] Endpoints únicos extraídos"
grep -hoE 'https?://[A-Za-z0-9./_:?=&#%-]+' "$OUT/strings"/*.txt "$OUT/decomp"/*/*.cs 2>/dev/null \
  | sort -u > "$OUT/endpoints.txt"
echo "    -> $OUT/endpoints.txt"

echo "[9/9] Resumen"
wc -l "$OUT/endpoints.txt" "$OUT/strings"/*.txt 2>/dev/null | tail -5
echo "DONE. Resultados en $OUT"
