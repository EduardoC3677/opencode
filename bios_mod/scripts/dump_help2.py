#!/usr/bin/env python3
import pefile
EXE = "/tmp/opencode/acer/bios_extracted/sfx_out/H2OFFT-Wx64.exe"
pe = pefile.PE(EXE)
ib = pe.OPTIONAL_HEADER.ImageBase
rd = next(s for s in pe.sections if s.Name.strip(b'\x00') == b'.rdata')
data = rd.get_data()
base = ib + rd.VirtualAddress

# UTF-16LE extractor (allow space + printable)
strs = []
i = 0
n = len(data) - 1
while i < n:
    if data[i+1] == 0 and (0x20 <= data[i] < 0x7f or data[i] == 9):
        j = i
        chars = []
        while j+1 < len(data) and data[j+1] == 0 and (0x20 <= data[j] < 0x7f or data[j] == 9):
            chars.append(chr(data[j]))
            j += 2
        if len(chars) >= 4 and j+1 < len(data) and data[j] == 0 and data[j+1] == 0:
            strs.append((base + i, "".join(chars)))
        i = j + 2
    else:
        i += 1

# print everything in the help-block VA range based on '-logoupdate:FILENAME  Update logo by input file.'
anchor_idx = None
for k,(va,s) in enumerate(strs):
    if 'logoupdate:FILENAME' in s:
        anchor_idx = k; break
print(f"anchor idx={anchor_idx}")
lo = max(0, anchor_idx - 300)
hi = min(len(strs), anchor_idx + 300)
for va, s in strs[lo:hi]:
    if len(s.strip()) >= 3:
        print(f"{s}")
