"""Shared parsing helpers for Unity 4.7.2f1 static-batch data in the 4.1.0 export.

Everything here reads only data that is present in the AssetRipper export of the
ground-truth APK (`original/apk/com.rexetstudio.blockstrike-780.apk`): scene YAML
objects, Transform hierarchies and `Mesh` assets in the Unity 4 serialized layout
(`serializedVersion: 8`, uncompressed interleaved `_typelessdata`, 16-bit
`m_IndexBuffer`).

No value is invented: submesh ranges, vertex layouts and renderer subset indices
are taken verbatim from the exported assets.  Anything the layout does not cover
raises `UnsupportedLayout` instead of being guessed.
"""

from __future__ import annotations

import math
import re
import struct
from dataclasses import dataclass, field
from pathlib import Path

BLOCK_RE = re.compile(r"^--- !u!(\d+) &(\d+)(.*)$", re.M)

CLASS_GAMEOBJECT = 1
CLASS_TRANSFORM = 4
CLASS_MESH_RENDERER = 23
CLASS_MESH_FILTER = 33
CLASS_MESH_COLLIDER = 64
CLASS_SKINNED_MESH_RENDERER = 137

# Unity 4 vertex channel order inside `m_Channels`.
CH_VERTEX, CH_NORMAL, CH_COLOR, CH_UV0, CH_UV1, CH_TANGENT = range(6)

# Unity 4 YAML numeric formats seen in this export: 0 = float32, 1 = float16,
# 2 = unorm8 (colour, dimension 1 == 4 bytes).
FORMAT_SIZES = {0: 4, 1: 2, 2: 4}
FORMAT_CODES = {0: "f", 1: "e"}


class UnsupportedLayout(RuntimeError):
    """Raised when an asset uses a layout this tool has not verified."""


# --------------------------------------------------------------------------- #
# YAML-ish object splitting (the export is plain Unity YAML, but huge: the hex
# payloads make a real YAML parser far too slow, so blocks are handled as text)
# --------------------------------------------------------------------------- #


@dataclass
class UnityObject:
    class_id: int
    file_id: int
    header: str
    body: str


@dataclass
class UnityDocument:
    preamble: str
    objects: list

    def by_id(self):
        return {o.file_id: o for o in self.objects}

    def dumps(self):
        out = [self.preamble]
        for obj in self.objects:
            out.append(obj.header + "\n")
            out.append(obj.body)
        return "".join(out)


def parse_document(text):
    marks = list(BLOCK_RE.finditer(text))
    if not marks:
        raise UnsupportedLayout("no Unity objects found")
    objects = []
    for i, m in enumerate(marks):
        end = marks[i + 1].start() if i + 1 < len(marks) else len(text)
        body = text[m.end() + 1 : end]
        objects.append(UnityObject(int(m.group(1)), int(m.group(2)), m.group(0), body))
    return UnityDocument(text[: marks[0].start()], objects)


def field_text(body, name, default=None):
    m = re.search(r"^[ \t]*" + re.escape(name) + r":[ \t]*(.*)$", body, re.M)
    if m is None:
        if default is None:
            raise UnsupportedLayout("missing field %r" % name)
        return default
    return m.group(1).strip()


def unquote(value):
    """Strip the YAML quoting Unity adds to names containing `:` or `#`."""
    value = value.strip()
    for quote in ("'", '"'):
        if len(value) >= 2 and value.startswith(quote) and value.endswith(quote):
            return value[1:-1].replace(quote * 2, quote)
    return value


def parse_ptr(value):
    """Parse `{fileID: 123, guid: ..., type: 2}` into (file_id, guid)."""
    fid = re.search(r"fileID:\s*(-?\d+)", value)
    guid = re.search(r"guid:\s*([0-9a-fA-F]{32})", value)
    return (int(fid.group(1)) if fid else 0, guid.group(1) if guid else None)


def parse_vector(value):
    return [float(x) for x in re.findall(r"[xyzw]:\s*([-\d.eE+]+)", value)]


# --------------------------------------------------------------------------- #
# Transforms
# --------------------------------------------------------------------------- #


def identity4():
    return [[1.0, 0, 0, 0], [0, 1.0, 0, 0], [0, 0, 1.0, 0], [0, 0, 0, 1.0]]


def mat_mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def trs_matrix(pos, rot, scale):
    x, y, z, w = rot
    n = math.sqrt(x * x + y * y + z * z + w * w)
    if n == 0:
        raise UnsupportedLayout("zero quaternion")
    x, y, z, w = x / n, y / n, z / n, w / n
    r = [
        [1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
        [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
        [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)],
    ]
    m = identity4()
    for i in range(3):
        for j in range(3):
            m[i][j] = r[i][j] * scale[j]
        m[i][3] = pos[i]
    return m


def mat_inverse(m):
    """General 4x4 inverse (Gauss-Jordan); transforms here are well conditioned."""
    a = [row[:] + [1.0 if i == j else 0.0 for j in range(4)] for i, row in enumerate(m)]
    for col in range(4):
        pivot = max(range(col, 4), key=lambda r: abs(a[r][col]))
        if abs(a[pivot][col]) < 1e-12:
            raise UnsupportedLayout("singular transform matrix")
        a[col], a[pivot] = a[pivot], a[col]
        p = a[col][col]
        a[col] = [v / p for v in a[col]]
        for r in range(4):
            if r == col:
                continue
            f = a[r][col]
            if f:
                a[r] = [v - f * w for v, w in zip(a[r], a[col])]
    return [row[4:] for row in a]


def transform_point(m, p):
    x, y, z = p
    return (
        m[0][0] * x + m[0][1] * y + m[0][2] * z + m[0][3],
        m[1][0] * x + m[1][1] * y + m[1][2] * z + m[1][3],
        m[2][0] * x + m[2][1] * y + m[2][2] * z + m[2][3],
    )


def transform_direction(m, v):
    x, y, z = v
    return (
        m[0][0] * x + m[0][1] * y + m[0][2] * z,
        m[1][0] * x + m[1][1] * y + m[1][2] * z,
        m[2][0] * x + m[2][1] * y + m[2][2] * z,
    )


def transform_normal_world_to_local(world_matrix, n):
    """local normal = normalize(M_linear^T * world normal)."""
    x, y, z = n
    out = (
        world_matrix[0][0] * x + world_matrix[1][0] * y + world_matrix[2][0] * z,
        world_matrix[0][1] * x + world_matrix[1][1] * y + world_matrix[2][1] * z,
        world_matrix[0][2] * x + world_matrix[1][2] * y + world_matrix[2][2] * z,
    )
    length = math.sqrt(sum(c * c for c in out))
    return tuple(c / length for c in out) if length else out


def is_identity_linear(m, eps=1e-6):
    for i in range(3):
        for j in range(3):
            if abs(m[i][j] - (1.0 if i == j else 0.0)) > eps:
                return False
    return True


class SceneGraph:
    """GameObject/Transform lookups for one scene document."""

    def __init__(self, doc):
        self.doc = doc
        self.objects = doc.by_id()
        self.transform_of_go = {}
        self.go_of_transform = {}
        self.components = {}
        for obj in doc.objects:
            if obj.class_id == CLASS_GAMEOBJECT:
                comps = [
                    (int(c), int(f))
                    for c, f in re.findall(r"- (\d+): \{fileID: (\d+)\}", obj.body)
                ]
                self.components[obj.file_id] = comps
                for cls, fid in comps:
                    if cls == CLASS_TRANSFORM:
                        self.transform_of_go[obj.file_id] = fid
            elif obj.class_id == CLASS_TRANSFORM:
                gid, _ = parse_ptr(field_text(obj.body, "m_GameObject"))
                self.go_of_transform[obj.file_id] = gid
        self._world_cache = {}

    def go_name(self, go_id):
        return unquote(field_text(self.objects[go_id].body, "m_Name"))

    def component(self, go_id, class_id):
        for cls, fid in self.components.get(go_id, ()):
            if cls == class_id:
                return fid
        return None

    def world_matrix(self, transform_id):
        if transform_id in self._world_cache:
            return self._world_cache[transform_id]
        body = self.objects[transform_id].body
        pos = parse_vector(field_text(body, "m_LocalPosition"))
        rot = parse_vector(field_text(body, "m_LocalRotation"))
        scale = parse_vector(field_text(body, "m_LocalScale"))
        local = trs_matrix(pos, rot, scale)
        father, _ = parse_ptr(field_text(body, "m_Father"))
        result = local if father == 0 else mat_mul(self.world_matrix(father), local)
        self._world_cache[transform_id] = result
        return result


# --------------------------------------------------------------------------- #
# Mesh assets
# --------------------------------------------------------------------------- #


@dataclass
class SubMesh:
    first_byte: int
    index_count: int
    topology: int
    first_vertex: int
    vertex_count: int
    center: tuple
    extent: tuple


@dataclass
class MeshAsset:
    path: Path
    text: str
    name: str
    submeshes: list
    index_bytes: bytes
    vertex_count: int
    stride: int
    channels: list
    raw: bytes
    data_size: int
    sections: dict = field(default_factory=dict)

    def submesh_indices(self, i):
        s = self.submeshes[i]
        return struct.unpack_from("<%dH" % s.index_count, self.index_bytes, s.first_byte)


SUBMESH_RE = re.compile(
    r"  - serializedVersion: 2\n"
    r"    firstByte: (\d+)\n"
    r"    indexCount: (\d+)\n"
    r"    topology: (\d+)\n"
    r"    firstVertex: (\d+)\n"
    r"    vertexCount: (\d+)\n"
    r"    localAABB:\n"
    r"      m_Center: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}\n"
    r"      m_Extent: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}\n"
)


def load_mesh(path):
    text = Path(path).read_text(encoding="utf-8", errors="strict")
    if "  m_SubMeshes:\n" not in text or "  m_VertexData:\n" not in text:
        raise UnsupportedLayout("%s: not a Unity 4 serialized Mesh" % path)
    sub_section = text.split("  m_SubMeshes:\n", 1)[1].split("  m_Shapes:", 1)[0]
    submeshes = []
    for m in SUBMESH_RE.finditer(sub_section):
        g = m.groups()
        submeshes.append(
            SubMesh(
                int(g[0]),
                int(g[1]),
                int(g[2]),
                int(g[3]),
                int(g[4]),
                tuple(float(v) for v in g[5:8]),
                tuple(float(v) for v in g[8:11]),
            )
        )
    if sub_section.count("- serializedVersion: 2") != len(submeshes):
        raise UnsupportedLayout("%s: unrecognised submesh layout" % path)

    index_bytes = bytes.fromhex(field_text(text, "m_IndexBuffer", ""))
    vertex_section = text.split("  m_VertexData:\n", 1)[1].split("  m_CompressedMesh:", 1)[0]
    vertex_count = int(field_text(vertex_section, "m_VertexCount"))
    data_size = int(field_text(vertex_section, "m_DataSize"))

    channels = [
        tuple(int(x) for x in m.groups())
        for m in re.finditer(
            r"    - stream: (\d+)\n\s+offset: (\d+)\n\s+format: (\d+)\n\s+dimension: (\d+)",
            vertex_section.split("    m_Channels:\n", 1)[1].split("    m_Streams:", 1)[0],
        )
    ]
    streams = re.split(
        r"    - channelMask: ",
        vertex_section.split("    m_Streams:\n", 1)[1].split("    m_DataSize:", 1)[0],
    )[1:]
    if not streams or any(int(s.splitlines()[0]) != 0 for s in streams[1:]):
        raise UnsupportedLayout("%s: multi-stream vertex data is not supported" % path)
    stride = int(field_text(streams[0], "stride"))
    raw = bytes.fromhex(field_text(vertex_section, "_typelessdata", ""))
    if len(raw) != data_size or (vertex_count and len(raw) != vertex_count * stride):
        raise UnsupportedLayout("%s: vertex buffer size mismatch" % path)
    if int(field_text(text, "m_MeshCompression", "0")) != 0:
        raise UnsupportedLayout("%s: compressed mesh is not supported" % path)

    return MeshAsset(
        path=Path(path),
        text=text,
        name=unquote(field_text(text, "m_Name")),
        submeshes=submeshes,
        index_bytes=index_bytes,
        vertex_count=vertex_count,
        stride=stride,
        channels=channels,
        raw=raw,
        data_size=data_size,
    )


def is_combined_mesh(mesh):
    return mesh.name.startswith("Combined Mesh")


def decode_channel(raw, stride, channel, vertex_index):
    stream, offset, fmt, dimension = channel
    if fmt not in FORMAT_CODES:
        raise UnsupportedLayout("channel format %d is not numeric" % fmt)
    return struct.unpack_from(
        "<" + FORMAT_CODES[fmt] * dimension, raw, vertex_index * stride + offset
    )


def encode_channel(buffer, stride, channel, vertex_index, values):
    stream, offset, fmt, dimension = channel
    struct.pack_into(
        "<" + FORMAT_CODES[fmt] * len(values), buffer, vertex_index * stride + offset, *values
    )


def unity_float(value):
    """Format a float the way Unity writes YAML scalars (compact, round-trip safe)."""
    value = struct.unpack("<f", struct.pack("<f", value))[0]
    if value == 0:
        return "0"
    if value == int(value) and abs(value) < 1e7:
        return str(int(value))
    text = repr(float(struct.unpack("<f", struct.pack("<f", value))[0]))
    for digits in range(6, 10):
        candidate = "%.*g" % (digits, value)
        if struct.pack("<f", float(candidate)) == struct.pack("<f", value):
            text = candidate
            break
    return text


def guid_bytes_sorted(values):
    return "".join("%08x" % v for v in values)


def parse_subset_indices(hex_text):
    data = bytes.fromhex(hex_text)
    if len(data) % 4:
        raise UnsupportedLayout("m_SubsetIndices length is not a multiple of 4")
    return list(struct.unpack("<%dI" % (len(data) // 4), data))


def format_subset_indices(indices):
    return struct.pack("<%dI" % len(indices), *indices).hex()


def iter_scenes(project_root):
    levels = Path(project_root) / "Assets" / "Levels"
    return sorted(p for p in levels.rglob("*.unity"))


def build_guid_map(project_root):
    guids = {}
    for meta in (Path(project_root) / "Assets").rglob("*.meta"):
        try:
            head = meta.read_text(encoding="utf-8", errors="replace")[:400]
        except OSError:
            continue
        m = re.search(r"^guid: ([0-9a-f]{32})", head, re.M)
        if m:
            guids[m.group(1)] = meta.with_suffix("")
    return guids
