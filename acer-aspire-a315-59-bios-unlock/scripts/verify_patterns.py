import re, sys
data = open(sys.argv[1], 'rb').read()
# Patterns from Acer_202x.cfg and SFG14-71 cfg
# Use abstract chars (.) translated to regex
patterns = [
    ("OC Perf Menu / Plat Volt Overrides unlock", r"FFFF00[\x00-\xff][\x00-\xff][\x00-\xff]821206[\x00-\xff][\x00-\xff][\x00-\xff]000F0F"),
    ("OC Feature unlock", r"19821408[\x00-\xff][\x00-\xff]01000[\x00-\xff]000591"),
    ("XTU/FIVR unlock", r"2902290229020A821206[\x00-\xff][\x00-\xff][\x00-\xff][\x00-\xff]0[\x00-\xff]00"),
    ("AMD platform check 0F0F", r"\x0F\x0F[\x00-\xff][\x00-\xff]00[\x00-\xff][\x00-\xff]00[\x00-\xff][\x00-\xff]000000FFFF000[\x00-\xff]020F0F"),
    ("Generic SUPPRESS_IF TRUE oneof 8byte", rb"\x0A\x82\x14\x08"),
    ("Generic SUPPRESS_IF TRUE oneof 10byte", rb"\x0A\x82\x14\x0A"),
    ("Generic SUPPRESS_IF TRUE subtitle", rb"\x0A\x82\x12\x06"),
    ("Generic SUPPRESS_IF TRUE end (47)", rb"\x0A\x82\x47\x02"),
]
for name, pat in patterns:
    if isinstance(pat, str):
        pat = pat.encode('latin1')
    matches = list(re.finditer(pat, data))
    print(f"{name}: {len(matches)} match(es)")
    for m in matches[:3]:
        print(f"  @0x{m.start():08x}: {data[m.start():m.start()+24].hex().upper()}")
