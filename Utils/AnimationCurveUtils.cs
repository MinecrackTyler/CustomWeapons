using UnityEngine;

namespace CustomWeapons.Utils
{
	public static class AnimationCurveUtils
	{
		public static float EvaluateNormalizedTime(this AnimationCurve curve, float normalizedTime)
		{
			if(curve.length <= 0)
			{
				//Debug.LogError("Given curve has 0 keyframes!");
				return float.NaN;
			}

			// get the time of the first keyframe in the curve
			var start = curve[0].time;

			if(curve.length == 1) 
			{
				//Debug.LogWarning("Given curve has only 1 single keyframe!");
				return start;
			}

			// get the time of the last keyframe in the curve
			var end = curve[curve.length - 1].time;
       
			// get the duration fo the curve
			var duration = end - start;
        
			// get the de-normalized time mapping the input 0 to 1 onto the actual time range 
			// between start and end
			var actualTime = start + Mathf.Clamp(normalizedTime, 0, 1) * duration;

			// finally use that calculated time to actually evaluate the curve
			return curve.Evaluate(actualTime);
		}
	}
}