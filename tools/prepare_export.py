"""Prepare Block Strike 4.1.0 APK data for AssetRipper; Python 3 standard library only.

This only extracts APK payloads and reassembles split files. Decryption is deliberately
not applied automatically until the 4.1.0 APK proves that its DLL uses the 3.7.0 wrapper."""
import argparse
from pathlib import Path
import re
import struct
import zipfile


def decrypt(data):
    if not data.endswith(b'<J3Tech>') or (len(data)-8) % 8:
        raise ValueError('Not the expected TEA wrapper')
    output = bytearray()
    for a,b in struct.iter_unpack('>II', data[:-8]):
        total = 0xc6ef3720
        for _ in range(32):
            b = (b-(((a<<4)+3)^(a+total)^((a>>5)+4))) & 0xffffffff
            a = (a-(((b<<4)+1)^(b+total)^((b>>5)+2))) & 0xffffffff
            total = (total-0x9e3779b9) & 0xffffffff
        output.extend(struct.pack('>II',a,b))
    padding = output[-1]
    if not 1 <= padding <= 8 or output[-padding:] != bytes([padding])*padding:
        raise ValueError('Invalid padding')
    output = bytes(output[:-padding])
    pe = struct.unpack_from('<I', output, 0x3c)[0]
    if output[:2] != b'MZ' or output[pe:pe+4] != b'PE\0\0':
        raise ValueError('Invalid recovered PE')
    return output


def prepare(apk, output, decrypt_tea=False):
    if output.exists():
        raise ValueError('Choose an output directory that does not exist')
    with zipfile.ZipFile(apk) as z:
        names = [n for n in z.namelist() if n.startswith(('assets/','lib/')) and not n.endswith('/')]
        groups = {}
        for n in names:
            if '..' in Path(n).parts:
                raise ValueError('Unsafe path')
            m = re.fullmatch(r'(.+)\.split(\d+)', n)
            if m: groups.setdefault(m[1], {})[int(m[2])] = n
        for base, parts in groups.items():
            if sorted(parts) != list(range(max(parts)+1)) or base in names:
                raise ValueError('Invalid split group: '+base)
        for n in names:
            m = re.fullmatch(r'(.+)\.split(\d+)', n)
            if m and int(m[2]): continue
            dest = m[1] if m else n
            data = b''.join(z.read(groups[dest][i]) for i in sorted(groups[dest])) if m else z.read(n)
            if decrypt_tea and dest.endswith('/Managed/Assembly-CSharp.dll'): data = decrypt(data)
            p = output/dest
            p.parent.mkdir(parents=True,exist_ok=True)
            p.write_bytes(data)
    print('Prepared',output)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk',type=Path)
    p.add_argument('--output',type=Path,default=Path('recovered/4.1.0/export-input'))
    p.add_argument('--decrypt-tea',action='store_true', help='Only if 4.1.0 binary analysis confirms the 3.7.0 TEA wrapper')
    a=p.parse_args()
    prepare(a.apk,a.output,a.decrypt_tea)
