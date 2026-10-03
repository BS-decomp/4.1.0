using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkin : TimerBehaviour
{
	public bool isPlayerActive;

	public Team PlayerTeam;

	public int Health = 100;

	private bool SyncHealth;

	private bool StartDamage;

	public ControllerManager Controller;

	public Renderer PlayerRenderer;

	public Animator PlayerAnimator;

	public PlayerSkinRagdoll PlayerRagdoll;

	public SkinnedMeshAtlas PlayerAtlas;

	public Transform PlayerWeaponRoot;

	public Transform PlayerTwoWeaponRoot;

	public AudioClip[] PlayerFoosteps;

	public AudioSource m_AudioSource;

	public Rigidbody PlayerRigidbody;

	public GameObject PlayerRootIK;

	public Transform PlayerSpectatePoint;

	public SkinnedMeshAtlas PlayerSkinnedMesh;

	public PlayerSounds Sounds;

	private bool Sound = true;

	private bool ShowDamage;

	public float PhotonSpeed = 10f;

	[Disabled]
	public Vector3 PhotonPosition = Vector3.zero;

	[Disabled]
	public Quaternion PhotonRotation = Quaternion.identity;

	[Disabled]
	public bool Dead;

	private float Move;

	private float Rotate;

	private float RotateLast;

	private bool Grounded = true;

	private bool Foostep;

	[HideInInspector]
	public int PlayerSkinID = -1;

	[HideInInspector]
	public TPWeaponShooter SelectWeapon;

	private List<TPWeaponShooter> WeaponsList = new List<TPWeaponShooter>();

	private Transform m_Transform;

	private static int MoveHash = Animator.StringToHash("Move");

	private static int RotateHash = Animator.StringToHash("Rotate");

	private static int GroundedHash = Animator.StringToHash("Grounded");

	private static int WeaponHash = Animator.StringToHash("Weapon");

	private static int ReloadHash = Animator.StringToHash("Reload");

	private void Start()
	{
		Controller = base.transform.root.GetComponent<ControllerManager>();
		m_Transform = base.transform;
		base.addTimer = TimerManager.In(0.1f, -1, 0.1f, CheckPosition);
		base.addTimer = TimerManager.In(0.12f, -1, 0.12f, UpdateMove);
		EventManager.AddListener("UpdateSettings", UpdateSettings);
		UpdateSettings();
	}

	private void UpdateSettings()
	{
		Sound = Settings.Audio;
		ShowDamage = Settings.ShowDamage;
		SyncHealth = Settings.SyncHealth;
	}

	private void OnEnable()
	{
		Foostep = false;
		isPlayerActive = true;
		if (PlayerSkinID != -1)
		{
			UpdateSkin();
		}
		CameraManager.Targets.Add(this);
	}

	private void OnDisable()
	{
		isPlayerActive = false;
		CameraManager.Targets.Remove(this);
	}

	private void CheckPosition()
	{
		if (PlayerRenderer.isVisible && isPlayerActive && (m_Transform.position - PhotonPosition).sqrMagnitude > 3f)
		{
			PlayerRigidbody.MovePosition(PhotonPosition);
		}
	}

	private Vector3 VectorLerp(Vector3 from, Vector3 to, float t)
	{
		return new Vector3(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t);
	}

	private void LateUpdate()
	{
		if (PlayerRenderer.isVisible)
		{
			PlayerRigidbody.MovePosition(VectorLerp(m_Transform.position, PhotonPosition, Time.deltaTime * PhotonSpeed));
			PlayerRigidbody.MoveRotation(Quaternion.Lerp(m_Transform.rotation, PhotonRotation, Time.deltaTime * PhotonSpeed));
			RotateLast = Mathf.Lerp(RotateLast, Rotate, Time.deltaTime * PhotonSpeed);
			if (RotateLast != Rotate)
			{
				PlayerAnimator.SetFloat(RotateHash, RotateLast);
			}
		}
		else
		{
			PlayerRigidbody.MovePosition(PhotonPosition);
			PlayerRigidbody.MoveRotation(PhotonRotation);
			RotateLast = Rotate;
		}
	}

	private void UpdateMove()
	{
		if (isPlayerActive)
		{
			if (PlayerRenderer.isVisible && PlayerAnimator.GetFloat(MoveHash) != Move)
			{
				PlayerAnimator.SetFloat(MoveHash, Move);
			}
			if (!Foostep && Sound && Mathf.Abs(Move) >= 0.3f)
			{
				StartCoroutine(UpdateFoosteps());
			}
		}
	}

	public void SetMove(float move)
	{
		Move = move;
	}

	public void SetRotate(float rotate)
	{
		Rotate = rotate;
	}

	public void SetGrounded(bool grounded)
	{
		if (isPlayerActive && Grounded != grounded)
		{
			PlayerAnimator.SetBool(GroundedHash, grounded);
			Grounded = grounded;
		}
	}

	public void SetWeapon(WeaponData weaponType, int skinID, int fireStat, byte[] stickers)
	{
		PlayerAnimator.SetInteger(WeaponHash, (int)weaponType.Animation);
		if (SelectWeapon != null && SelectWeapon.name == weaponType.Name)
		{
			return;
		}
		if (SelectWeapon != null)
		{
			SelectWeapon.Deactive();
		}
		TPWeaponShooter tPWeaponShooter = ContainsWeapon(weaponType.Name);
		if (tPWeaponShooter == null)
		{
			GameObject gameObject = Utils.AddChild(weaponType.TpsPrefab, PlayerWeaponRoot, weaponType.TpsPrefab.transform.position, weaponType.TpsPrefab.transform.rotation);
			SelectWeapon = gameObject.GetComponent<TPWeaponShooter>();
			SelectWeapon.name = weaponType.Name;
			WeaponsList.Add(SelectWeapon);
			SelectWeapon.Init(weaponType.ID, skinID, this);
			if (fireStat > 0)
			{
				SelectWeapon.SetFireStat(fireStat);
			}
			SelectWeapon.SetStickers(stickers);
		}
		else
		{
			SelectWeapon = tPWeaponShooter;
			if (fireStat > 0)
			{
				SelectWeapon.UpdateFireStat(fireStat);
			}
		}
		SelectWeapon.Active();
		Sounds.Stop();
	}

	private TPWeaponShooter ContainsWeapon(string weaponName)
	{
		for (int i = 0; i < WeaponsList.Count; i++)
		{
			if (weaponName == WeaponsList[i].name)
			{
				return WeaponsList[i];
			}
		}
		return null;
	}

	public void Fire()
	{
		if (SelectWeapon != null)
		{
			SelectWeapon.Fire(PlayerRenderer.isVisible);
		}
	}

	public void Reload()
	{
		if (SelectWeapon != null)
		{
			PlayerAnimator.SetBool(ReloadHash, true);
			SelectWeapon.Reload();
			base.addTimer = TimerManager.In(0.1f, () =>
			{
				PlayerAnimator.SetBool(ReloadHash, false);
			});
		}
	}

	public void Damage(DamageInfo damageInfo)
	{
		if ((PlayerTeam != damageInfo.AttackerTeam || GameManager.GetFriendDamage()) && !Dead)
		{
			if (ShowDamage)
			{
				UIToast.Show(Localization.Get("Damage") + ": " + damageInfo.Damage, 2f);
			}
			if (SyncHealth && !StartDamage && Health - damageInfo.Damage <= 0)
			{
				Vector3 ragdollForce = Utils.GetRagdollForce(PlayerInput.instance.PlayerTransform.position, damageInfo.AttackPosition);
				PlayerRagdoll.Active(ragdollForce, damageInfo.HeadShot);
				Sounds.Stop();
			}
			UICrosshair.Hit();
			Controller.Damage(damageInfo);
		}
	}

	private IEnumerator UpdateFoosteps()
	{
		Foostep = true;
		AudioClip clip = PlayerFoosteps[Random.Range(0, PlayerFoosteps.Length)];
		m_AudioSource.pitch = Random.Range(1f, 1.5f);
		m_AudioSource.clip = clip;
		m_AudioSource.Play();
		yield return new WaitForSeconds(0.3f);
		Foostep = false;
	}

	public void SetPosition(Vector3 pos)
	{
		PhotonPosition = pos;
		if (m_Transform == null)
		{
			m_Transform = base.transform;
		}
		m_Transform.position = PhotonPosition;
	}

	public void SetRotation(Vector3 rot)
	{
		PhotonRotation = Quaternion.Euler(rot);
		if (m_Transform == null)
		{
			m_Transform = base.transform;
		}
		m_Transform.rotation = PhotonRotation;
	}

	public void UpdateSkin()
	{
		string playerSkin = (int)PlayerTeam + "-" + PlayerSkinID;
		if (PlayerAtlas.mSpriteName != playerSkin)
		{
			base.addTimer = TimerManager.In(0.01f, () =>
			{
				PlayerAtlas.spriteName = playerSkin;
			});
		}
	}

	public void SetActiveObject(bool active)
	{
		PlayerRenderer.gameObject.SetActive(active);
		PlayerRootIK.SetActive(active);
	}

	public void StartDamageTime()
	{
		if (SyncHealth && GameManager.isStartDamage())
		{
			StartDamage = true;
			base.addTimer = TimerManager.In(GameManager.GetStartDamageTime(), () =>
			{
				StartDamage = false;
			});
		}
	}
}
