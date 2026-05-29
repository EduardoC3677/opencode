#!/usr/bin/env python3
"""Verify SREP Acer_202x.cfg patterns against the extracted SetupUtility / A01ODMDxeDriver."""
import re, os, sys
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_64

SU = "/tmp/opencode/acer/abobios.bin.dump/7 1FD0BACE-6F0A-4085-901E-F6210385CB6F/0 20BC8AC9-94D1-4208-AB28-5D673FD73486/0 EE4E5898-3914-4259-9D6E-DC7BD79403CF/1 Volume image section/0 8C8CE578-8A3D-4F1C-9935-896185C32DD3/266 SetupUtility/2 PE32 image section/body.bin"
ODM_DIR = "/tmp/opencode/acer/abobios.bin.dump/7 1FD0BACE-6F0A-4085-901E-F6210385CB6F/0 20BC8AC9-94D1-4208-AB28-5D673FD73486/0 EE4E5898-3914-4259-9D6E-DC7BD79403CF/1 Volume image section/0 8C8CE578-8A3D-4F1C-9935-896185C32DD3/370 A01ODMDxeDriver"

def find_pe(p):
    for root, dirs, files in os.walk(p):
        if root.endswith("PE32 image section"):
            for f in files:
                if f == "body.bin":
                    return os.path.join(root, f)

ODM = find_pe(ODM_DIR)
print("SetupUtility:", os.path.exists(SU), "size", os.path.getsize(SU))
print("A01ODMDxeDriver:", ODM, "size", os.path.getsize(ODM) if ODM else None)

def hex_pattern_to_regex(pat):
    # SREP patterns: '..' = any byte, '.' = any nibble. Build regex over hex chars.
    # Easier: convert hex pattern with '.' to bytes regex pair by uppercasing and pairing.
    pat = pat.strip().upper()
    out = []
    i = 0
    while i < len(pat):
        c1 = pat[i]; c2 = pat[i+1] if i+1 < len(pat) else '?'
        if c1 == '.' and c2 == '.':
            out.append(b'.')
        elif c1 == '.' or c2 == '.':
            # nibble wildcard: encode as char class for hex chars; but we operate on bytes.
            # Translate: '.X' = byte with low nibble X (need byte regex 0x0X,0x1X,...,0xFX)
            if c1 == '.':
                nib = int(c2,16)
                bs = bytes([(h<<4) | nib for h in range(16)])
                out.append(b'[' + bs + b']')
            else:
                nib = int(c1,16)
                bs = bytes([(nib<<4) | l for l in range(16)])
                out.append(b'[' + bs + b']')
        else:
            out.append(re.escape(bytes.fromhex(c1+c2)))
        i += 2
    return b''.join(out)

# patterns from Acer_202x.cfg
patterns = {
    "SetupUtility": [
        ("Speed AMD check", "0F0F..00..00..000000FFFF000.020F0F..00..00..000000FFFF000.022902018601"),
        ("OC Perf Menu / Plat Volt", "FFFF00......821206......000F0F"),
        ("OC Feature", "19821408....01000.000591"),
        ("XTU/FIVR section", "2902290229020A821206....0.00"),
    ],
    "A01ODMDxeDriver": [
        ("ODM Default", "740C48B80300000000000080"),
        ("ODM Combo Setup Hide", "74..4084CD"),
        ("ODM Combo Setup Hide 2", "7427488D05"),
        ("ODM Combo Setup Hide 3", "4885C078..80"),
        ("ODM Combo TravelMate 1", "85C0745E"),
        ("ODM Combo TravelMate 2", "85C0744F"),
        ("ODM Combo TravelMate 3", "85C0744D"),
        ("ODM Single 1", "4184C.74.."),
        ("ODM Single 2", "..84C974.."),
        ("ODM Single 3", "A801740."),
        ("ODM Single 4", "403ACD75.."),
        ("ODM Single 5", "4584D274.."),
    ],
}

files = {"SetupUtility": SU, "A01ODMDxeDriver": ODM}
md = Cs(CS_ARCH_X86, CS_MODE_64)

for mod, pats in patterns.items():
    fp = files[mod]
    data = open(fp, 'rb').read()
    print(f"\n===== {mod} ({len(data)} bytes) =====")
    for label, pat in pats:
        rgx = hex_pattern_to_regex(pat)
        matches = list(re.finditer(rgx, data, re.DOTALL))
        print(f"  [{label}] {pat}  ->  {len(matches)} match(es)")
        for m in matches[:3]:
            off = m.start()
            print(f"    @0x{off:x}: {data[off:off+len(m.group(0))].hex()}")
