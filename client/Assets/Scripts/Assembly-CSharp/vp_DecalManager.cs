using System.Collections.Generic;
using UnityEngine;

public sealed class vp_DecalManager
{
	public static readonly vp_DecalManager instance;

	private static List<GameObject> m_Decals;

	private static float m_MaxDecals;

	private static float m_FadedDecals;

	private static float m_NonFadedDecals;

	private static float m_FadeAmount;

	public static float MaxDecals
	{
		get
		{
			return m_MaxDecals;
		}
		set
		{
			m_MaxDecals = value;
			Refresh();
		}
	}

	public static float FadedDecals
	{
		get
		{
			return m_FadedDecals;
		}
		set
		{
			if (value > m_MaxDecals)
			{
				Debug.LogError("FadedDecals can't be larger than MaxDecals");
				return;
			}
			m_FadedDecals = value;
			Refresh();
		}
	}

	private vp_DecalManager()
	{
	}

	static vp_DecalManager()
	{
		instance = new vp_DecalManager();
		m_Decals = new List<GameObject>();
		m_MaxDecals = 100f;
		m_FadedDecals = 20f;
		m_NonFadedDecals = 0f;
		m_FadeAmount = 0f;
		Refresh();
	}

	public static void Add(GameObject decal)
	{
		if (m_Decals.Contains(decal))
		{
			m_Decals.Remove(decal);
		}
		Color color = decal.GetComponent<Renderer>().material.color;
		color.a = 1f;
		decal.GetComponent<Renderer>().material.color = color;
		m_Decals.Add(decal);
		FadeAndRemove();
	}

	private static void FadeAndRemove()
	{
		if ((float)m_Decals.Count > m_NonFadedDecals)
		{
			for (int i = 0; (float)i < (float)m_Decals.Count - m_NonFadedDecals; i++)
			{
				if (m_Decals[i] != null)
				{
					Color color = m_Decals[i].GetComponent<Renderer>().material.color;
					color.a -= m_FadeAmount;
					m_Decals[i].GetComponent<Renderer>().material.color = color;
				}
			}
		}
		if (m_Decals[0] != null)
		{
			if (m_Decals[0].GetComponent<Renderer>().material.color.a <= 0f)
			{
				vp_Utility.Destroy(m_Decals[0]);
				m_Decals.Remove(m_Decals[0]);
			}
		}
		else
		{
			m_Decals.RemoveAt(0);
		}
	}

	private static void Refresh()
	{
		if (m_MaxDecals < m_FadedDecals)
		{
			m_MaxDecals = m_FadedDecals;
		}
		m_FadeAmount = m_MaxDecals / m_FadedDecals / m_MaxDecals;
		m_NonFadedDecals = m_MaxDecals - m_FadedDecals;
	}

	private static void DebugOutput()
	{
		int num = 0;
		int num2 = 0;
		foreach (GameObject decal in m_Decals)
		{
			if (decal.GetComponent<Renderer>().material.color.a == 1f)
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		Debug.Log("Decal count: " + m_Decals.Count + ", Full: " + num + ", Faded: " + num2);
	}
}
