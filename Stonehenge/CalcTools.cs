using UnityEngine;

namespace CustomWeapons.Stonehenge
{
	public class CalcTools
	{
		public static float TargetPosLead(Vector3 pos, Vector3 vel, GameObject gun, float muzzleVel, float dragCoef,
			int iterations)
		{
			float num = muzzleVel;
			float lead = 0f;
			for (int i = 0; i < iterations; i++)
			{
				float num3 = Vector3.Distance(pos + vel * lead, gun.transform.position);
				lead = (Mathf.Pow(2.71828f, dragCoef * num3 / num) - 1f) / dragCoef;
				if (float.IsInfinity(lead) || lead > 120f)
				{
					return 120f;
				}
			}
			return lead;
		}
	}
}