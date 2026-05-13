#!/usr/bin/env python3
"""Disassemble UEFI PE32+ module with Capstone."""
import sys, pefile, capstone

def disasm(path, out=None):
    pe = pefile.PE(path)
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
    md.detail = True
    base = pe.OPTIONAL_HEADER.ImageBase
    fh = open(out, 'w') if out else sys.stdout
    fh.write(f"# PE {path}\n# ImageBase=0x{base:x} EP=0x{pe.OPTIONAL_HEADER.AddressOfEntryPoint:x}\n")
    for s in pe.sections:
        name = s.Name.decode(errors='ignore').strip('\0')
        fh.write(f"\n## Section {name} VA=0x{s.VirtualAddress:x} VSize=0x{s.Misc_VirtualSize:x}\n")
        if name not in ('.text',):
            continue
        data = s.get_data()
        addr = base + s.VirtualAddress
        for ins in md.disasm(data, addr):
            fh.write(f"0x{ins.address:08x}: {ins.mnemonic:<8} {ins.op_str}\n")
    if out:
        fh.close()

if __name__ == '__main__':
    disasm(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
