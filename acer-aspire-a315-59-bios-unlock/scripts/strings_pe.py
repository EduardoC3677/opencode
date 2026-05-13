#!/usr/bin/env python3
"""Extract ASCII/UTF-16 strings from PE binary, used for CLI args / hotkeys / patterns."""
import sys, re, pefile

def extract(path, min_len=4):
    data = open(path, 'rb').read()
    asc = re.findall(rb'[\x20-\x7e]{%d,}' % min_len, data)
    uni = []
    for m in re.findall(rb'(?:[\x20-\x7e]\x00){%d,}' % min_len, data):
        try:
            uni.append(m.decode('utf-16le'))
        except:
            pass
    return [s.decode('latin1') for s in asc], uni

if __name__ == '__main__':
    a, u = extract(sys.argv[1], int(sys.argv[2]) if len(sys.argv) > 2 else 4)
    for s in a:
        print('A:', s)
    for s in u:
        print('U:', s)
