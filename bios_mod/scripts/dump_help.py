#!/usr/bin/env python3
import pefile, re
EXE = "/tmp/opencode/acer/bios_extracted/sfx_out/H2OFFT-Wx64.exe"
pe = pefile.PE(EXE)
ib = pe.OPTIONAL_HEADER.ImageBase
rd = next(s for s in pe.sections if s.Name.strip(b'\x00') == b'.rdata')
data = rd.get_data()
base = ib + rd.VirtualAddress

# UTF-16LE strings
strs = []
i = 0
n = len(data) - 2
while i < n:
    # printable wchar (low byte printable, high byte 0)
    if data[i+1] == 0 and (0x20 <= data[i] < 0x7f or data[i] == 9):
        j = i
        chars = []
        while j+1 < len(data) and data[j+1] == 0 and (0x20 <= data[j] < 0x7f or data[j] == 9):
            chars.append(chr(data[j]))
            j += 2
        if len(chars) >= 3 and j+1 < len(data) and data[j] == 0 and data[j+1] == 0:
            strs.append((base + i, "".join(chars)))
        i = j + 2
    else:
        i += 1

# anchor
for k, (va, s) in enumerate(strs):
    if '-logoupdate' in s:
        # print wide window
        lo = max(0, k-50)
        hi = min(len(strs), k+250)
        for va2, s2 in strs[lo:hi]:
            print(f"0x{va2:08x}: {s2}")
        break
