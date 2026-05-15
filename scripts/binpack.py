#!/usr/bin/env python3
import argparse
import os
import shutil
from pathlib import Path

def iter_files(root: Path):
    for p in root.rglob("*"):
        if p.is_file():
            yield p

def sizeof(p: Path) -> int:
    return p.stat().st_size

def ensure_dir(p: Path):
    p.mkdir(parents=True, exist_ok=True)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True, help="Directorio fuente ya extraído")
    ap.add_argument("--dst", required=True, help="Directorio destino para shards")
    ap.add_argument("--max-gb", required=True, type=float, help="Máx GB por shard")
    args = ap.parse_args()

    src = Path(args.src).resolve()
    dst = Path(args.dst).resolve()
    max_bytes = int(args.max_gb * 1024 * 1024 * 1024)

    if not src.exists():
        raise SystemExit(f"src no existe: {src}")

    # Recolectar archivos
    files = [(p, sizeof(p)) for p in iter_files(src)]
    files.sort(key=lambda x: x[1], reverse=True)

    # Greedy bin packing
    bins = []  # list of (total_bytes, [files])
    for p, sz in files:
        placed = False
        for bi in range(len(bins)):
            total, arr = bins[bi]
            if total + sz <= max_bytes:
                arr.append((p, sz))
                bins[bi] = (total + sz, arr)
                placed = True
                break
        if not placed:
            bins.append((sz, [(p, sz)]))

    # Crear shards y mover preservando estructura
    if dst.exists():
        shutil.rmtree(dst)
    ensure_dir(dst)

    for idx, (total, arr) in enumerate(bins, start=1):
        part_dir = dst / f"part{idx:02d}"
        ensure_dir(part_dir)
        for p, sz in arr:
            rel = p.relative_to(src)
            target = part_dir / rel
            ensure_dir(target.parent)
            shutil.move(str(p), str(target))

    # (Opcional) limpiar src vacío
    # Eliminar dirs vacíos
    for d in sorted([p for p in src.rglob("*") if p.is_dir()], reverse=True):
        try:
            d.rmdir()
        except OSError:
            pass

    print(f"Creado(s) {len(bins)} shard(s) en {dst}")
    for idx, (total, _) in enumerate(bins, start=1):
        print(f"  part{idx:02d}: {total/ (1024**3):.2f} GB")

if __name__ == "__main__":
    main()
  
