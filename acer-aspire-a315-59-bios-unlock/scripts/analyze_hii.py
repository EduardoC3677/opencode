import sys, re, collections
data = open(sys.argv[1], 'rb').read()
# Classify by 3rd-4th byte (the suppressed opcode that follows SUPPRESS_IF TRUE)
# Standard HII opcodes after 0A 82:
# 12 06 = subtitle (small)
# 14 08 = oneof option (8-byte value)
# 14 0A = oneof option (10-byte)
# 14 8A = oneof option (with conditional)
# 46 02 = (uncertain)
# 47 02 = (NOP)
suffix_count = collections.Counter()
for m in re.finditer(re.escape(b'\x0A\x82'), data):
    o = m.start()
    suffix = data[o+2:o+4].hex().upper()
    suffix_count[suffix] += 1
print("Top 20 suffixes after 0A82 (SUPPRESS_IF TRUE):")
for s, c in suffix_count.most_common(20):
    print(f"  0A82{s}: {c}")
print()
# Show all 0A82 with 12-byte context
print("\nClassify by 6-byte signature (0A82 + 4):")
sig6 = collections.Counter()
for m in re.finditer(re.escape(b'\x0A\x82'), data):
    o = m.start()
    sig = data[o:o+6].hex().upper()
    sig6[sig] += 1
for s, c in sig6.most_common(30):
    print(f"  {s}: {c}")
