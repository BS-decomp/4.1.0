using UnityEngine;

public class vp_Spring
{
	public enum UpdateMode
	{
		Position = 0,
		PositionAdditive = 1,
		Rotation = 2,
		RotationAdditive = 3,
		Scale = 4,
		ScaleAdditive = 5
	}

	protected delegate void UpdateDelegate();

	protected UpdateMode Mode;

	protected bool m_AutoUpdate = true;

	protected UpdateDelegate m_UpdateFunc;

	public Vector3 State = Vector3.zero;

	protected Vector3 m_Velocity = Vector3.zero;

	public Vector3 RestState = Vector3.zero;

	public Vector3 Stiffness = new Vector3(0.5f, 0.5f, 0.5f);

	public Vector3 Damping = new Vector3(0.75f, 0.75f, 0.75f);

	protected float m_VelocityFadeInCap = 1f;

	protected float m_VelocityFadeInEndTime;

	protected float m_VelocityFadeInLength;

	protected Vector3[] m_SoftForceFrame = new Vector3[120];

	public float MaxVelocity = 10000f;

	public float MinVelocity = 1E-07f;

	public Vector3 MaxState = new Vector3(10000f, 10000f, 10000f);

	public Vector3 MinState = new Vector3(-10000f, -10000f, -10000f);

	protected Transform m_Transform;

	public Transform Transform
	{
		set
		{
			m_Transform = value;
			RefreshUpdateMode();
		}
	}

	public vp_Spring(Transform transform, UpdateMode mode, bool autoUpdate = true)
	{
		Mode = mode;
		Transform = transform;
		m_AutoUpdate = autoUpdate;
	}

	public void FixedUpdate()
	{
		if (m_VelocityFadeInEndTime > Time.time)
		{
			m_VelocityFadeInCap = Mathf.Clamp01(1f - (m_VelocityFadeInEndTime - Time.time) / m_VelocityFadeInLength);
		}
		else
		{
			m_VelocityFadeInCap = 1f;
		}
		if (m_SoftForceFrame[0] != Vector3.zero)
		{
			AddForceInternal(m_SoftForceFrame[0]);
			for (int i = 0; i < 120; i++)
			{
				m_SoftForceFrame[i] = ((i >= 119) ? Vector3.zero : m_SoftForceFrame[i + 1]);
				if (m_SoftForceFrame[i] == Vector3.zero)
				{
					break;
				}
			}
		}
		Calculate();
		m_UpdateFunc();
	}

	private void Position()
	{
		m_Transform.localPosition = State;
	}

	private void Rotation()
	{
		m_Transform.localEulerAngles = State;
	}

	private void Scale()
	{
		m_Transform.localScale = State;
	}

	private void PositionAdditive()
	{
		m_Transform.localPosition += State;
	}

	private void RotationAdditive()
	{
		m_Transform.localEulerAngles += State;
	}

	private void ScaleAdditive()
	{
		m_Transform.localScale += State;
	}

	private void None()
	{
	}

	protected void RefreshUpdateMode()
	{
		m_UpdateFunc = None;
		switch (Mode)
		{
		case UpdateMode.Position:
			State = m_Transform.localPosition;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = Position;
			}
			break;
		case UpdateMode.Rotation:
			State = m_Transform.localEulerAngles;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = Rotation;
			}
			break;
		case UpdateMode.Scale:
			State = m_Transform.localScale;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = Scale;
			}
			break;
		case UpdateMode.PositionAdditive:
			State = m_Transform.localPosition;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = PositionAdditive;
			}
			break;
		case UpdateMode.RotationAdditive:
			State = m_Transform.localEulerAngles;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = RotationAdditive;
			}
			break;
		case UpdateMode.ScaleAdditive:
			State = m_Transform.localScale;
			if (m_AutoUpdate)
			{
				m_UpdateFunc = ScaleAdditive;
			}
			break;
		}
		RestState = State;
	}

	protected void Calculate()
	{
		if (!(State == RestState))
		{
			m_Velocity += Vector3.Scale(RestState - State, Stiffness);
			m_Velocity = Vector3.Scale(m_Velocity, Damping);
			m_Velocity = Vector3.ClampMagnitude(m_Velocity, MaxVelocity);
			if (m_Velocity.sqrMagnitude > MinVelocity * MinVelocity)
			{
				Move();
			}
			else
			{
				Reset();
			}
		}
	}

	private void AddForceInternal(Vector3 force)
	{
		force *= m_VelocityFadeInCap;
		m_Velocity += force;
		m_Velocity = Vector3.ClampMagnitude(m_Velocity, MaxVelocity);
		Move();
	}

	public void AddForce(Vector3 force)
	{
		if (Time.timeScale < 1f)
		{
			AddSoftForce(force, 1f);
		}
		else
		{
			AddForceInternal(force);
		}
	}

	public void AddSoftForce(Vector3 force, float frames)
	{
		force /= Time.timeScale;
		frames = Mathf.Clamp(frames, 1f, 120f);
		AddForceInternal(force / frames);
		for (int i = 0; i < Mathf.RoundToInt(frames) - 1; i++)
		{
			m_SoftForceFrame[i] += force / frames;
		}
	}

	protected void Move()
	{
		State += m_Velocity * Time.timeScale;
		State.x = Mathf.Clamp(State.x, MinState.x, MaxState.x);
		State.y = Mathf.Clamp(State.y, MinState.y, MaxState.y);
		State.z = Mathf.Clamp(State.z, MinState.z, MaxState.z);
	}

	public void Reset()
	{
		m_Velocity = Vector3.zero;
		State = RestState;
	}

	public void Stop(bool includeSoftForce = false)
	{
		m_Velocity = Vector3.zero;
		if (includeSoftForce)
		{
			StopSoftForce();
		}
	}

	public void StopSoftForce()
	{
		for (int i = 0; i < 120; i++)
		{
			m_SoftForceFrame[i] = Vector3.zero;
		}
	}

	public void ForceVelocityFadeIn(float seconds)
	{
		m_VelocityFadeInLength = seconds;
		m_VelocityFadeInEndTime = Time.time + seconds;
		m_VelocityFadeInCap = 0f;
	}
}
