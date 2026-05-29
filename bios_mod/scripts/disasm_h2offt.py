#!/usr/bin/env python3
"""Disassemble H2OFFT-Wx64.exe with Capstone and locate CLI argument handling.

We:
1. Load the PE.
2. Disassemble .text.
3. Extract all ASCII strings referenced via RIP-relative LEAs that look like
   short CLI tokens (start with '-' or '/').
4. Print disassembly windows around the references for context.
5. Try to find a usage/help string and dump it.
"""
import sys, re, struct
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_64

EXE = sys.argv[1] if len(sys.argv) > 1 else "/tmp/opencode/acer/bios_extracted/sfx_out/H2OFFT-Wx64.exe"
pe = pefile.PE(EXE)
image_base = pe.OPTIONAL_HEADER.ImageBase

# Map of all sections raw data, and helper to read VA bytes
sections = []
for s in pe.sections:
    sections.append({
        "name": s.Name.decode(errors='ignore').strip('\x00'),
        "va": image_base + s.VirtualAddress,
        "vsz": s.Misc_VirtualSize,
        "raw": s.get_data()
    })

def read_va(va, n):
    for s in sections:
        if s["va"] <= va < s["va"] + len(s["raw"]):
            off = va - s["va"]
            return s["raw"][off:off+n]
    return None

# Pull .text and .rdata
text = next(s for s in sections if s["name"] == ".text")
rdata = next(s for s in sections if s["name"] == ".rdata")

# 1) Collect ASCII strings in .rdata with their VAs.
strs = {}
data = rdata["raw"]
i = 0
while i < len(data):
    j = i
    while j < len(data) and 0x20 <= data[j] < 0x7f:
        j += 1
    if j - i >= 2 and j < len(data) and data[j] == 0:
        s = data[i:j].decode('ascii', errors='replace')
        va = rdata["va"] + i
        strs[va] = s
        i = j + 1
    else:
        i += 1

# Also collect UTF-16LE strings
i = 0
while i < len(data) - 2:
    if data[i+1] == 0 and 0x20 <= data[i] < 0x7f:
        j = i
        chars = []
        while j+1 < len(data) and data[j+1] == 0 and 0x20 <= data[j] < 0x7f:
            chars.append(chr(data[j]))
            j += 2
        if len(chars) >= 3 and j+1 < len(data) and data[j] == 0 and data[j+1] == 0:
            s = "".join(chars)
            va = rdata["va"] + i
            strs[va] = "W:" + s
            i = j + 2
            continue
    i += 1

# 2) Disassemble .text and find LEA reg, [rip+disp] -> string addr
md = Cs(CS_ARCH_X86, CS_MODE_64)
md.detail = True
ref_to_str = {}  # ins_addr -> (string_va, string)
addr2ins = {}    # addr -> mnemonic + op_str

for ins in md.disasm(text["raw"], text["va"]):
    addr2ins[ins.address] = (ins.mnemonic, ins.op_str, ins.size)
    if ins.mnemonic == "lea" and "rip" in ins.op_str:
        # Resolve target VA: next ip + disp
        # Capstone provides ins.operands with mem.disp; but easier: parse op_str like "rax, [rip + 0x1234]"
        m = re.search(r"\[rip ([+\-]) 0x([0-9a-fA-F]+)\]", ins.op_str)
        if m:
            sign = 1 if m.group(1) == "+" else -1
            disp = int(m.group(2), 16) * sign
            target = ins.address + ins.size + disp
            if target in strs:
                ref_to_str[ins.address] = (target, strs[target])

# Filter CLI-like tokens
def looks_cli(s):
    s = s[2:] if s.startswith("W:") else s
    if s.startswith(("-", "/")) and 2 <= len(s) <= 30 and " " not in s:
        return True
    return False

cli_refs = [(a, t, s) for a, (t, s) in ref_to_str.items() if looks_cli(s)]
print(f"Found {len(cli_refs)} CLI-like string refs")
seen = set()
for a, t, s in sorted(cli_refs):
    if s in seen: continue
    seen.add(s)
    print(f"  ins=0x{a:x} str@0x{t:x} -> {s!r}")

# 3) Look for "Usage" / "usage:" strings and dump big blocks
print("\n--- Usage-like strings ---")
for va, s in sorted(strs.items()):
    cs = s[2:] if s.startswith("W:") else s
    if re.search(r"^(usage|Usage|USAGE|H2OFFT|-[A-Z]\s)", cs) or " -" in cs[:60] and len(cs) > 40:
        if len(cs) > 25 or cs.startswith("Usage"):
            print(f"  0x{va:x}: {cs!r}")

# 4) Dump big multi-line help blob from .rdata if any (look for "\\n" or "  -" pattern)
print("\n--- Possible help blob lines ---")
for va, s in sorted(strs.items()):
    cs = s[2:] if s.startswith("W:") else s
    if len(cs) > 40 and (cs.count(":") >= 1) and (cs.startswith("-") or cs.startswith("/") or " : " in cs or "filename" in cs.lower() or "force" in cs.lower() or "bios" in cs.lower()):
        print(f"  0x{va:x}: {cs!r}")
