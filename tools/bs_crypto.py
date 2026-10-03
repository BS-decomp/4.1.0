"""Block Strike 4.1.0 string obfuscation, reimplemented from the game's own code.

Ground truth (decompiled `Assembly-CSharp` of the 4.1.0 APK):

* `Utils.Encrypt/Decrypt` — DES-CBC, PKCS#7, base64.
  - salt/IV source: `Utils.GetIV()` = UTF8 `"IvmD123A12"` (10 bytes).  The blob
    starts with those 10 bytes, DES then uses the first 8 of them as the IV.
  - key: `Rfc2898DeriveBytes(Utils.test, GetIV(), 555).GetBytes(8)`
    (PBKDF2-HMAC-SHA1, 555 iterations, 8-byte key).
  - `Utils.test` is built in `UIFontControl.GenerateFont()` from the byte sizes
    of two files read out of the APK itself:
        bytes.Length            -> "jar:file://" + dataPath + "!/classes.dex"
        www.bytes.Length        -> "jar:file://" + dataPath +
                                   "!/assets/bin/Data/Managed/Assembly-CSharp.dll"
    `Utils.test = bytes.Length + www.bytes.Length.ToString()` (and
    `.Remove(fontSize)` on the second number when `UIFontControl.fontSize != 0`).
    For the shipped APK this is `"8101932" + "1500688"`.
* `AesEncryptor` — AES-128-CBC, key UTF8 `"defaultKeyString"`, IV = first 16
  bytes of the blob (used for the strings inside the C# code).

Nothing here is guessed: the password is recomputed from the APK on every run
and checked against known scene names.
"""

from __future__ import annotations

import base64
import hashlib
import zipfile
from pathlib import Path

IV_SOURCE = b"IvmD123A12"
PBKDF2_ITERATIONS = 555
DEX_ENTRY = "classes.dex"
DLL_ENTRY = "assets/bin/Data/Managed/Assembly-CSharp.dll"


# --------------------------------------------------------------------------- #
# Minimal DES (FIPS 46-3) — the project must not depend on an OpenSSL build
# that still ships the legacy provider.
# --------------------------------------------------------------------------- #

_PC1 = [57, 49, 41, 33, 25, 17, 9, 1, 58, 50, 42, 34, 26, 18, 10, 2, 59, 51, 43, 35, 27,
        19, 11, 3, 60, 52, 44, 36, 63, 55, 47, 39, 31, 23, 15, 7, 62, 54, 46, 38, 30, 22,
        14, 6, 61, 53, 45, 37, 29, 21, 13, 5, 28, 20, 12, 4]
_PC2 = [14, 17, 11, 24, 1, 5, 3, 28, 15, 6, 21, 10, 23, 19, 12, 4, 26, 8, 16, 7, 27, 20,
        13, 2, 41, 52, 31, 37, 47, 55, 30, 40, 51, 45, 33, 48, 44, 49, 39, 56, 34, 53,
        46, 42, 50, 36, 29, 32]
_SHIFTS = [1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1]
_IP = [58, 50, 42, 34, 26, 18, 10, 2, 60, 52, 44, 36, 28, 20, 12, 4, 62, 54, 46, 38, 30,
       22, 14, 6, 64, 56, 48, 40, 32, 24, 16, 8, 57, 49, 41, 33, 25, 17, 9, 1, 59, 51,
       43, 35, 27, 19, 11, 3, 61, 53, 45, 37, 29, 21, 13, 5, 63, 55, 47, 39, 31, 23, 15, 7]
_FP = [40, 8, 48, 16, 56, 24, 64, 32, 39, 7, 47, 15, 55, 23, 63, 31, 38, 6, 46, 14, 54,
       22, 62, 30, 37, 5, 45, 13, 53, 21, 61, 29, 36, 4, 44, 12, 52, 20, 60, 28, 35, 3,
       43, 11, 51, 19, 59, 27, 34, 2, 42, 10, 50, 18, 58, 26, 33, 1, 41, 9, 49, 17, 57, 25]
_E = [32, 1, 2, 3, 4, 5, 4, 5, 6, 7, 8, 9, 8, 9, 10, 11, 12, 13, 12, 13, 14, 15, 16, 17,
      16, 17, 18, 19, 20, 21, 20, 21, 22, 23, 24, 25, 24, 25, 26, 27, 28, 29, 28, 29, 30,
      31, 32, 1]
_P = [16, 7, 20, 21, 29, 12, 28, 17, 1, 15, 23, 26, 5, 18, 31, 10, 2, 8, 24, 14, 32, 27,
      3, 9, 19, 13, 30, 6, 22, 11, 4, 25]
_S = [
    [14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7, 0, 15, 7, 4, 14, 2, 13, 1, 10,
     6, 12, 11, 9, 5, 3, 8, 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0, 15, 12,
     8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13],
    [15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10, 3, 13, 4, 7, 15, 2, 8, 14, 12,
     0, 1, 10, 6, 9, 11, 5, 0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15, 13, 8,
     10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9],
    [10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8, 13, 7, 0, 9, 3, 4, 6, 10, 2, 8,
     5, 14, 12, 11, 15, 1, 13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7, 1, 10,
     13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12],
    [7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15, 13, 8, 11, 5, 6, 15, 0, 3, 4,
     7, 2, 12, 1, 10, 14, 9, 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4, 3, 15,
     0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14],
    [2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9, 14, 11, 2, 12, 4, 7, 13, 1, 5,
     0, 15, 10, 3, 9, 8, 6, 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14, 11, 8,
     12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3],
    [12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11, 10, 15, 4, 2, 7, 12, 9, 5, 6,
     1, 13, 14, 0, 11, 3, 8, 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6, 4, 3,
     2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13],
    [4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1, 13, 0, 11, 7, 4, 9, 1, 10, 14,
     3, 5, 12, 2, 15, 8, 6, 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2, 6, 11,
     13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12],
    [13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7, 1, 15, 13, 8, 10, 3, 7, 4, 12,
     5, 6, 11, 0, 14, 9, 2, 7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8, 2, 1,
     14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11],
]

def _bits(data):
    out = []
    for byte in data:
        for i in range(7, -1, -1):
            out.append((byte >> i) & 1)
    return out


def _bytes(bits):
    out = bytearray()
    for i in range(0, len(bits), 8):
        value = 0
        for b in bits[i : i + 8]:
            value = (value << 1) | b
        out.append(value)
    return bytes(out)


def _permute(bits, table):
    return [bits[i - 1] for i in table]


def _subkeys(key):
    bits = _permute(_bits(key), _PC1)
    c, d = bits[:28], bits[28:]
    keys = []
    for shift in _SHIFTS:
        c = c[shift:] + c[:shift]
        d = d[shift:] + d[:shift]
        keys.append(_permute(c + d, _PC2))
    return keys


def _feistel(right, subkey):
    expanded = _permute(right, _E)
    xored = [a ^ b for a, b in zip(expanded, subkey)]
    out = []
    for box in range(8):
        chunk = xored[box * 6 : box * 6 + 6]
        row = (chunk[0] << 1) | chunk[5]
        col = (chunk[1] << 3) | (chunk[2] << 2) | (chunk[3] << 1) | chunk[4]
        value = _S[box][row * 16 + col]
        out.extend([(value >> i) & 1 for i in range(3, -1, -1)])
    return _permute(out, _P)


def _des_block(block, subkeys):
    bits = _permute(_bits(block), _IP)
    left, right = bits[:32], bits[32:]
    for subkey in subkeys:
        left, right = right, [a ^ b for a, b in zip(left, _feistel(right, subkey))]
    return _bytes(_permute(right + left, _FP))


def des_cbc_decrypt(key, iv, data):
    if len(data) % 8:
        raise ValueError("DES ciphertext length must be a multiple of 8")
    subkeys = list(reversed(_subkeys(key)))
    out = bytearray()
    previous = iv[:8]
    for offset in range(0, len(data), 8):
        block = data[offset : offset + 8]
        plain = _des_block(block, subkeys)
        out.extend(a ^ b for a, b in zip(plain, previous))
        previous = block
    pad = out[-1] if out else 0
    if 1 <= pad <= 8 and bytes(out[-pad:]) == bytes([pad]) * pad:
        del out[-pad:]
    return bytes(out)


def des_cbc_encrypt(key, iv, data):
    subkeys = _subkeys(key)
    pad = 8 - (len(data) % 8)
    data = data + bytes([pad]) * pad
    out = bytearray()
    previous = iv[:8]
    for offset in range(0, len(data), 8):
        block = bytes(a ^ b for a, b in zip(data[offset : offset + 8], previous))
        previous = _des_block(block, subkeys)
        out.extend(previous)
    return bytes(out)


# --------------------------------------------------------------------------- #
# Block Strike specifics
# --------------------------------------------------------------------------- #


def password_from_apk(apk_path, font_size=0):
    """Recompute `Utils.test` exactly as `UIFontControl.GenerateFont()` does."""
    with zipfile.ZipFile(apk_path) as zf:
        try:
            dex = zf.getinfo(DEX_ENTRY).file_size
            dll = zf.getinfo(DLL_ENTRY).file_size
        except KeyError as exc:  # pragma: no cover - defensive
            raise RuntimeError("APK does not contain %s" % exc)
    second = str(dll)
    if font_size:
        second = second[:font_size]
    return "%d%s" % (dex, second), {"classes.dex": dex, "Assembly-CSharp.dll": dll}


def derive_key(password):
    return hashlib.pbkdf2_hmac("sha1", password.encode("utf-8"), IV_SOURCE, PBKDF2_ITERATIONS, 8)


def decrypt_name(encrypted, key):
    """Decrypt one `Utils.Encrypt` blob. `#` is the on-disk escape for `/`."""
    text = encrypted.replace("#", "/")
    blob = base64.b64decode(text + "=" * (-len(text) % 4))
    if not blob.startswith(IV_SOURCE):
        raise ValueError("not a Block Strike encrypted string: %r" % encrypted)
    return des_cbc_decrypt(key, IV_SOURCE[:8], blob[len(IV_SOURCE) :]).decode("utf-8")


def encrypt_name(plain, key):
    blob = IV_SOURCE + des_cbc_encrypt(key, IV_SOURCE[:8], plain.encode("utf-8"))
    return base64.b64encode(blob).decode("ascii").replace("/", "#")


def self_test(key):
    """Round-trip check plus a known sample from the shipped export."""
    known = ("SXZtRDEyM0ExMt6xab3TLQVn", "Bust")
    if decrypt_name(known[0], key) != known[1]:
        raise RuntimeError("DES self-test failed: scene name did not decrypt to 'Bust'")
    for sample in ("Bust", "Military Range", "Shooting Range", "AwakeScene"):
        if decrypt_name(encrypt_name(sample, key), key) != sample:
            raise RuntimeError("DES self-test failed: round-trip broke on %r" % sample)
    return True


def default_apk(root=None):
    root = Path(root or Path(__file__).resolve().parent.parent)
    candidates = sorted((root / "original" / "apk").glob("*.apk"))
    if not candidates:
        raise RuntimeError("no APK in original/apk/")
    return candidates[0]
