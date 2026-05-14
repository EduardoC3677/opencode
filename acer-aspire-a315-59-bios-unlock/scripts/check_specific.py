import re, sys
data = open(sys.argv[1], 'rb').read()
# Look for variants from SFG (closer to ours, also Intel mobile)
# 0A 82 14 08 XX 00 01 00 01 00
import collections
ctr = collections.Counter()
for m in re.finditer(rb'\x0A\x82\x14\x08', data):
    o = m.start()
    full = data[o:o+10]
    if len(full) < 10: continue
    if full[5:6] == b'\x00' and full[6:8] == b'\x01\x00' and full[8:10] == b'\x01\x00':
        ctr[full[4]] += 1
print("SFG-style SUPPRESS_IF TRUE oneof patterns (0A8214 08 XX 00 01 00 01 00):")
for k, v in sorted(ctr.items()):
    print(f"  XX=0x{k:02X}: {v} matches")
print(f"Total: {sum(ctr.values())}")

# Same for 14 0A pattern
ctr2 = collections.Counter()
for m in re.finditer(rb'\x0A\x82\x14\x0A', data):
    o = m.start()
    full = data[o:o+12]
    if len(full) < 12: continue
    if full[5:6] == b'\x00' and full[6:8] == b'\x02\x00' and full[8:10] == b'\x00\x00' and full[10:12] == b'\x01\x00':
        ctr2[full[4]] += 1
print("\nSFG-style SUPPRESS_IF TRUE oneof 0A82140A XX 00 02 00 00 00 01 00:")
for k, v in sorted(ctr2.items()):
    print(f"  XX=0x{k:02X}: {v} matches")
print(f"Total: {sum(ctr2.values())}")
