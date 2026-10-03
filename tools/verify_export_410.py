#!/usr/bin/env python3
"""Conservative integrity checks for the Block Strike 4.1.0 AssetRipper export.
This verifier never rewrites project files and does not assume Unity 2021 data formats.
"""
from pathlib import Path
import argparse, re, sys

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('project', nargs='?', default='client'); args=ap.parse_args()
    root=Path(args.project); assets=root/'Assets'
    errors=[]
    pv=(root/'ProjectSettings/ProjectVersion.txt').read_text(errors='replace') if (root/'ProjectSettings/ProjectVersion.txt').exists() else ''
    scenes=list(assets.rglob('*.unity'))
    if not scenes: errors.append('no Unity scenes')
    bad_yaml=[p for p in scenes if not p.read_text(errors='replace').startswith('%YAML')]
    if bad_yaml: errors.append(f'{len(bad_yaml)} scenes do not start with YAML header')
    metas={m.read_text(errors='replace').split('guid:',1)[1].split()[0] for m in assets.rglob('*.meta') if 'guid:' in m.read_text(errors='replace')}
    refs=set()
    for p in assets.rglob('*'):
        if p.is_file() and p.suffix in {'.unity','.prefab','.asset','.mat','.controller'}:
            refs.update(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(errors='replace')))
    missing=refs-metas-{'0000000000000000e000000000000000','0000000000000000f000000000000000'}
    if missing: errors.append(f'{len(missing)} unresolved GUIDs: {sorted(missing)[:5]}')
    missing_scripts=[]
    for p in scenes:
        t=p.read_text(errors='replace')
        if 'Missing (Mono Script)' in t or re.search(r'm_Script:\s*\{fileID: 0\}',t): missing_scripts.append(p)
    if missing_scripts: errors.append(f'{len(missing_scripts)} scenes contain missing scripts')
    shaders=list(assets.rglob('*.shader')); dummy=[p for p in shaders if 'DummyShaderTextExporter' in p.read_text(errors='replace')]
    static=[p for p in scenes if re.search(r'm_StaticBatchRoot:\s*\{fileID: [1-9]',p.read_text(errors='replace'))]
    print(f'Project: {root}\nEditor: {pv.strip() or "unknown"}\nScenes: {len(scenes)}\nShaders: {len(shaders)} (dummy: {len(dummy)})\nGUID refs: {len(refs)} (unresolved: {len(missing)})\nStatic batch roots: {len(static)}\nMissing-script scenes: {len(missing_scripts)}')
    if dummy: print('DUMMY SHADERS:'); print('\n'.join(map(str,dummy)))
    if errors:
        print('FAIL:'); print('\n'.join(errors)); return 1
    print('OK: no structural integrity errors found.'); return 0
if __name__=='__main__': sys.exit(main())
