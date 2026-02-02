using UnityEngine;

namespace CustomWeapons.Stonehenge
{
	public class StonehengePiston : MonoBehaviour
	{
		public Transform targetA; // First joint (e.g. on crank/hinge)
		public Transform targetB; // Second joint (e.g. on piston head)

		void LateUpdate()
		{
			if (targetA == null || targetB == null) return;

			// Make A look at B
			targetA.LookAt(targetB, Vector3.up);

			// Make B look at A
			targetB.LookAt(targetA, Vector3.up);
		}
	}
}