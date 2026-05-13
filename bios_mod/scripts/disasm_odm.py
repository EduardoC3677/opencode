#!/usr/bin/env python3
"""Disassemble A01ODMDxeDriver around the SREP patch sites and SetupUtility hot spots."""
import os, pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_64

def disasm_around(path, name, sites):
    pe = pefile.PE(path)
    ib = pe.OPTIONAL_HEADER.ImageBase
    text = next(s for s in pe.sections if s.Name.startswith(b'.text'))
    text_data = text.get_data()
    text_off = text.PointerToRawData  # within file
    # The file offsets are within whole body.bin; match raw offsets vs file
    data = open(path, 'rb').read()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    print(f"\n===== {name} =====")
    for label, file_off in sites:
        print(f"\n-- {label} @ file offset 0x{file_off:x} --")
        # show 32 bytes preceding + 32 after
        lo = max(0, file_off - 8)
        hi = min(len(data), file_off + 32)
        code = data[lo:hi]
        # use a fake address based on file offset for readability
        for ins in md.disasm(code, lo):
            mark = " <--" if ins.address == file_off else ""
            print(f"  0x{ins.address:08x}: {ins.bytes.hex():<20s} {ins.mnemonic} {ins.op_str}{mark}")

ODM = "/tmp/opencode/acer/abobios.bin.dump/7 1FD0BACE-6F0A-4085-901E-F6210385CB6F/0 20BC8AC9-94D1-4208-AB28-5D673FD73486/0 EE4E5898-3914-4259-9D6E-DC7BD79403CF/1 Volume image section/0 8C8CE578-8A3D-4F1C-9935-896185C32DD3/370 A01ODMDxeDriver/1 PE32 image section/body.bin"
disasm_around(ODM, "A01ODMDxeDriver", [
    ("ODM Default 740C", 0x15b2),
    ("ODM Single 1.a 4184C6 740D", 0x5882),
    ("ODM Single 1.b 4184C6 7416", 0x597e),
    ("ODM Single 2 4584C9 743A", 0x4048),
    ("ODM Single 5 4584D2 7415", 0x3cca),
])

SU = "/tmp/opencode/acer/abobios.bin.dump/7 1FD0BACE-6F0A-4085-901E-F6210385CB6F/0 20BC8AC9-94D1-4208-AB28-5D673FD73486/0 EE4E5898-3914-4259-9D6E-DC7BD79403CF/1 Volume image section/0 8C8CE578-8A3D-4F1C-9935-896185C32DD3/266 SetupUtility/2 PE32 image section/body.bin"
# Setup* matches are IFR data, not code. Show raw hex around them.
print("\n===== SetupUtility (IFR data hex dump around matches) =====")
data = open(SU,'rb').read()
for label, off in [("OC Perf Menu #1", 0xc8d5d), ("OC Feature", 0xd02b6), ("XTU/FIVR #1", 0xc990a)]:
    print(f"\n-- {label} @0x{off:x} --")
    print("  " + data[off-8:off+32].hex())
