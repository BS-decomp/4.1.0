# Decompiler artifacts in the recovered scripts

The 4.1.0 C# comes from a decompiled `Assembly-CSharp`, and the decompiler
rewrites Unity 4 APIs mechanically. Some of those rewrites are **wrong** and
break gameplay at runtime. They are tracked here and fixed with
`tools/fix_decompiler_artifacts.py`, which only touches patterns it can prove
are artifacts.

## #1 `RaycastHit.collider.GetComponent<Collider>()` — fixed, 35 sites

Unity 4 had `Component.collider`, so the decompiler turns every `.collider`
into `.GetComponent<Collider>()`. For a `Component` that is correct. For
`RaycastHit`, `ControllerColliderHit` and `Collision` it is not: their
`.collider` **is already a `Collider`**, and the extra call

* throws `NullReferenceException` when the cast missed (`collider == null`)
  instead of simply yielding null, and
* returns the *first* collider on the hit GameObject instead of the one that was
  actually hit, when an object carries several colliders.

Symptoms this caused in the editor:

```
NullReferenceException: Object reference not set to an instance of an object
vp_FPController.FixedMove () (at Assets/Scripts/Assembly-CSharp/vp_FPController.cs:483)   ×971
```

`vp_FPController.FixedMove()` line 483 is ground detection:

```csharp
// recovered (broken)
m_Grounded = m_GroundHit.collider.GetComponent<Collider>() != null;
// original
m_Grounded = m_GroundHit.collider != null;
```

Every frame the player was airborne the sphere cast missed, the line threw, and
the rest of `FixedMove()` never ran — no ground state, no gravity handling, no
step offset. That is exactly the reported "jump and fly away, no physics".

The same artifact affected shooting and UI:

| File | Sites | What it broke |
| --- | --- | --- |
| `vp_FPController.cs` | 2 | ground detection / physics (NRE every FixedUpdate) |
| `PlayerWeapons.cs` | 5 | `PlayerSkin` / `DamageObject` / `IgnoreDecal` / `RigidbodyObject` / `PaintObject` tag checks on the hit — hits and decals landing nowhere |
| `vp_HitscanBullet.cs` | 2 | bullet impact handling |
| `vp_FPWeaponMeleeAttack.cs` | 3 | melee hits |
| `vp_FPInteractManager.cs` | 5 | interaction raycasts and `ControllerColliderHit` |
| `vp_FPCamera.cs`, `vp_FPWeapon.cs`, `vp_3DUtility.cs`, `vp_Placement.cs`, `vp_SimpleAITurret.cs` | 7 | camera collision, weapon placement, AI turret |
| `UICamera.cs`, `InputToEvent.cs`, `UINameManager.cs`, `SimpleDrag.cs` | 11 | NGUI input raycasts (buttons that "do nothing") |

All 35 were on hit structs, so all 35 were rewritten to plain `.collider`; the
tool reports (and leaves alone) anything whose receiver type it cannot prove.

```
$ python3 tools/fix_decompiler_artifacts.py --report
35 `.collider.GetComponent<Collider>()` sites: 35 on hit structs, 0 left alone
```

Patterns that were checked and **do not** occur in this export:
`hit.transform.GetComponent<Transform>()`, `.rigidbody.GetComponent<Rigidbody>()`,
`.camera.GetComponent<Camera>()`, `.renderer.GetComponent<Renderer>()`,
`.audio.GetComponent<AudioSource>()`, `.animation.GetComponent<Animation>()`,
`.light.GetComponent<Light>()`, `.gameObject.GetComponent<GameObject>()`.

## Still open

* `TODO: unverified` — `Connect() to 'ns.exitgames.com' failed:
  DllNotFoundException: PhotonSocketPlugin`. The APK only ships the Android
  native socket plugin, so any *online* connection attempt from the Windows
  editor fails. It does not affect offline play; a managed-socket fallback for
  the editor has not been investigated yet.
* `Occlusion culling data is out of date` — the scenes still carry the Unity 4
  PVS data; it needs a rebake or removal.
