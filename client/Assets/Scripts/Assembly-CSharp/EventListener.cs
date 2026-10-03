using UnityEngine;

public class EventListener : MonoBehaviour
{
	public delegate void IntDelegate(GameObject go, int value);

	public delegate void VoidDelegate(GameObject go);

	public delegate void CollisionDelegate(GameObject go, Collision collision);

	public delegate void CollisionDelegate2D(GameObject go, Collision2D collision);

	public delegate void FloatDelegate(GameObject go, float delta);

	public delegate void Joint2DDelegate(GameObject go, Joint2D joint);

	public delegate void ObjectDelegate(GameObject go, GameObject obj);

	public delegate void ColliderDelegate(GameObject go, Collider collider);

	public delegate void Collider2DDelegate(GameObject go, Collider2D collider);

	public delegate void ControllerColliderHitDelegate(GameObject go, ControllerColliderHit hit);

	public IntDelegate onAnimatorIK;

	public VoidDelegate onAnimatorMove;

	public VoidDelegate onBecameInvisible;

	public VoidDelegate onBecameVisible;

	public CollisionDelegate onCollisionEnter;

	public CollisionDelegate2D onCollisionEnter2D;

	public CollisionDelegate onCollisionExit;

	public CollisionDelegate2D onCollisionExit2D;

	public CollisionDelegate onCollisionStay;

	public CollisionDelegate2D onCollisionStay2D;

	public ControllerColliderHitDelegate onControllerColliderHit;

	public VoidDelegate onDestroy;

	public VoidDelegate onDisable;

	public VoidDelegate onEnable;

	public FloatDelegate onJointBreak;

	public Joint2DDelegate onJointBreak2D;

	public VoidDelegate onMouseDown;

	public VoidDelegate onMouseDrag;

	public VoidDelegate onMouseEnter;

	public VoidDelegate onMouseExit;

	public VoidDelegate onMouseOver;

	public VoidDelegate onMouseUp;

	public VoidDelegate onMouseUpAsButton;

	public ObjectDelegate onParticleCollision;

	public VoidDelegate onParticleTrigger;

	public VoidDelegate onTransformChildrenChanged;

	public VoidDelegate onTransformParentChanged;

	public ColliderDelegate onTriggerEnter;

	public Collider2DDelegate onTriggerEnter2D;

	public ColliderDelegate onTriggerExit;

	public Collider2DDelegate onTriggerExit2D;

	public ColliderDelegate onTriggerStay;

	public Collider2DDelegate onTriggerStay2D;

	public VoidDelegate onWillRenderObject;

	private GameObject mGameObject;

	public GameObject cachedGameObject
	{
		get
		{
			if (mGameObject == null)
			{
				mGameObject = base.gameObject;
			}
			return mGameObject;
		}
	}

	public static EventListener Get(GameObject go)
	{
		EventListener eventListener = go.GetComponent<EventListener>();
		if (eventListener == null)
		{
			eventListener = go.AddComponent<EventListener>();
		}
		return eventListener;
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (onAnimatorIK != null)
		{
			onAnimatorIK(cachedGameObject, layerIndex);
		}
	}

	private void OnAnimatorMove()
	{
		if (onAnimatorMove != null)
		{
			onAnimatorMove(cachedGameObject);
		}
	}

	private void OnBecameInvisible()
	{
		if (onBecameInvisible != null)
		{
			onBecameInvisible(cachedGameObject);
		}
	}

	private void OnBecameVisible()
	{
		if (onBecameVisible != null)
		{
			onBecameVisible(cachedGameObject);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (onCollisionEnter != null)
		{
			onCollisionEnter(cachedGameObject, collision);
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (onCollisionEnter2D != null)
		{
			onCollisionEnter2D(cachedGameObject, collision);
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (onCollisionExit != null)
		{
			onCollisionExit(cachedGameObject, collision);
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (onCollisionExit2D != null)
		{
			onCollisionExit2D(cachedGameObject, collision);
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (onCollisionStay != null)
		{
			onCollisionStay(cachedGameObject, collision);
		}
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (onCollisionStay2D != null)
		{
			onCollisionStay2D(cachedGameObject, collision);
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (onControllerColliderHit != null)
		{
			onControllerColliderHit(cachedGameObject, hit);
		}
	}

	private void OnDestroy()
	{
		if (onDestroy != null)
		{
			onDestroy(cachedGameObject);
		}
	}

	private void OnDisable()
	{
		if (onDisable != null)
		{
			onDisable(cachedGameObject);
		}
	}

	private void OnEnable()
	{
		if (onEnable != null)
		{
			onEnable(cachedGameObject);
		}
	}

	private void OnJointBreak(float breakForce)
	{
		if (onJointBreak != null)
		{
			onJointBreak(cachedGameObject, breakForce);
		}
	}

	private void OnJointBreak2D(Joint2D brokenJoint)
	{
		if (onJointBreak != null)
		{
			onJointBreak2D(cachedGameObject, brokenJoint);
		}
	}

	private void OnMouseDown()
	{
		if (onMouseDown != null)
		{
			onMouseDown(cachedGameObject);
		}
	}

	private void OnMouseDrag()
	{
		if (onMouseDrag != null)
		{
			onMouseDrag(cachedGameObject);
		}
	}

	private void OnMouseEnter()
	{
		if (onMouseEnter != null)
		{
			onMouseEnter(cachedGameObject);
		}
	}

	private void OnMouseExit()
	{
		if (onMouseExit != null)
		{
			onMouseExit(cachedGameObject);
		}
	}

	private void OnMouseOver()
	{
		if (onMouseOver != null)
		{
			onMouseOver(cachedGameObject);
		}
	}

	private void OnMouseUp()
	{
		if (onMouseUp != null)
		{
			onMouseUp(cachedGameObject);
		}
	}

	private void OnMouseUpAsButton()
	{
		if (onMouseUpAsButton != null)
		{
			onMouseUpAsButton(cachedGameObject);
		}
	}

	private void OnParticleCollision(GameObject other)
	{
		if (onParticleCollision != null)
		{
			onParticleCollision(cachedGameObject, other);
		}
	}

	private void OnParticleTrigger()
	{
		if (onParticleTrigger != null)
		{
			onParticleTrigger(cachedGameObject);
		}
	}

	private void OnTransformChildrenChanged()
	{
		if (onTransformChildrenChanged != null)
		{
			onTransformChildrenChanged(cachedGameObject);
		}
	}

	private void OnTransformParentChanged()
	{
		if (onTransformParentChanged != null)
		{
			onTransformParentChanged(cachedGameObject);
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (onTriggerEnter != null)
		{
			onTriggerEnter(cachedGameObject, collider);
		}
	}

	private void OnTriggerEnter2D(Collider2D collider)
	{
		if (onTriggerEnter2D != null)
		{
			onTriggerEnter2D(cachedGameObject, collider);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (onTriggerExit != null)
		{
			onTriggerExit(cachedGameObject, collider);
		}
	}

	private void OnTriggerExit2D(Collider2D collider)
	{
		if (onTriggerExit2D != null)
		{
			onTriggerExit2D(cachedGameObject, collider);
		}
	}

	private void OnTriggerStay(Collider collider)
	{
		if (onTriggerStay != null)
		{
			onTriggerStay(cachedGameObject, collider);
		}
	}

	private void OnTriggerStay2D(Collider2D collider)
	{
		if (onTriggerStay2D != null)
		{
			onTriggerStay2D(cachedGameObject, collider);
		}
	}

	private void OnWillRenderObject()
	{
		if (onWillRenderObject != null)
		{
			onWillRenderObject(cachedGameObject);
		}
	}
}
