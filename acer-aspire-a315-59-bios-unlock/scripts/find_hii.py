#!/usr/bin/env python3
"""Find HII SUPPRESS_IF/GRAY_OUT_IF patterns that hide menu items.
0x0A = SUPPRESS_IF opcode
0x19 = GRAY_OUT_IF opcode  
0x82 = TRUE opcode (always-true expression following)
Pattern 0A 82 XX YY ... = SUPPRESS_IF TRUE ...  (item ALWAYS hidden)
"""
import sys, re, struct
data = open(sys.argv[1], 'rb').read()
# SUPPRESS_IF TRUE patterns (always-hide)
patterns = [
    (b'\x0A\x82', 'SUPPRESS_IF TRUE'),
    (b'\x19\x82', 'GRAYOUT_IF TRUE'),
    (b'\x0E\x82', 'NO_SUBMIT_IF TRUE'),
]
counts = {}
for sig, name in patterns:
    n = 0
    for m in re.finditer(re.escape(sig), data):
        n += 1
    counts[name] = n
print(f"File: {sys.argv[1]}  size={len(data)}")
for k,v in counts.items():
    print(f"  {k}: {v} occurrences")

# Show first few suppress_if with context
print("\nFirst 10 SUPPRESS_IF TRUE with 16 bytes context:")
i = 0
for m in re.finditer(re.escape(b'\x0A\x82'), data):
    if i >= 10: break
    off = m.start()
    end = min(off + 16, len(data))
    print(f"  @0x{off:08x}: {data[off:end].hex().upper()}")
    i += 1
