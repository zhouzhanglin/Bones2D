using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//http://wiki.unity3d.com/index.php/EditorAnimationCurveExtension
namespace CurveExtended{

	public static class CurveExtension {

		public static void UpdateAllLinearTangents(this AnimationCurve curve){
			for (int i = 0; i < curve.keys.Length; i++) {
				UpdateTangentsFromMode(curve, i);
			}
		}

		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static void UpdateTangentsFromMode(AnimationCurve curve, int index)
		{
			if (index < 0 || index >= curve.length)
				return;
			Keyframe key = curve[index];
			if (KeyframeUtil.GetKeyTangentMode(key, 0) == TangentMode.Linear && index >= 1)
			{
				key.inTangent = CalculateLinearTangent(curve, index, index - 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtil.GetKeyTangentMode(key, 1) == TangentMode.Linear && index + 1 < curve.length)
			{
				key.outTangent = CalculateLinearTangent(curve, index, index + 1);
				curve.MoveKey(index, key);
			}
			if (KeyframeUtil.GetKeyTangentMode(key, 0) != TangentMode.Smooth && KeyframeUtil.GetKeyTangentMode(key, 1) != TangentMode.Smooth)
				return;
			curve.SmoothTangents(index, 0.0f);
		}

		// UnityEditor.CurveUtility.cs (c) Unity Technologies
		public static float CalculateLinearTangent(AnimationCurve curve, int index, int toIndex)
		{
			return (float) (((double) curve[index].value - (double) curve[toIndex].value) / ((double) curve[index].time - (double) curve[toIndex].time));
		}







		#region custom curve

		public static void OptimizesCurve( AnimationCurve curve){
			if(curve!=null && curve.length>0){
				for(int i=1;i<curve.keys.Length-1;++i){
					Keyframe f1 = curve.keys[i-1];//前一帧
					Keyframe frame = curve.keys[i];
					Keyframe f2 = curve.keys[i+1];//后一帧
					if(frame.value==f1.value && frame.value==f2.value) {
						curve.RemoveKey(i);
						i--;
					}
				}
			}
		}

		public static void ClampCurveRotate360(AnimationCurve rotatecurve,bool rotateCircle = true){
			if(rotatecurve.length>1){
				for(int f=1;f<rotatecurve.length;++f){
					ClampCurveRotate360(rotatecurve,f,rotateCircle);
				}
			}
		}
		public static void ClampCurveRotate360(AnimationCurve rotatecurve,int f,bool rotateCircle){
			float prev = rotatecurve.keys[f-1].value;
			float curr = rotatecurve.keys[f].value;
			if(rotateCircle){
				//rotate beyond 1 circle
				if(curr<-180f || curr>180f) return;
				if(prev<-180f||prev>180f){
					if(f>1) prev = rotatecurve.keys[f-2].value;
				}
			}
			while ((curr - prev) > 180 ){
				curr -= 360;
			}
			while ((curr - prev) < -180){
				curr += 360;
			}
			if (rotatecurve.keys[f].value != curr){
				TangentMode modeIn = KeyframeUtil.GetKeyTangentMode(rotatecurve.keys[f].tangentMode,(int)rotatecurve.keys[f].inTangent);
				TangentMode modeOut = KeyframeUtil.GetKeyTangentMode(rotatecurve.keys[f].tangentMode,(int)rotatecurve.keys[f].outTangent);
				if(f==rotatecurve.length-1) modeIn = TangentMode.Linear;
				rotatecurve.MoveKey(f, KeyframeUtil.GetNew(rotatecurve.keys[f].time , curr,modeIn,modeOut ));
			}
		}

		// p0, p3 - start, end points
		// p1, p2 - conrol points
		// t - value on x [0,1]
		public static Vector2 GetBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t){
			float y = (1 - t) * (1 - t) * (1 - t) * p0.y +
				3 * t * (1 - t) * (1 - t) * p1.y +
				3 * t * t * (1 - t) * p2.y +
				t * t * t * p3.y;
			return new Vector2(p0.x + t * (p3.x - p0.x) ,y);
		}

		// a - start point
		// b - on t= 1/3
		// c - on t = 2/3
		// d - end point
		// c1,c2 control points of bezier.
		public static void CalcControlPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 c1, out Vector2 c2){
			c1 = (-5 * a + 18 * b - 9 * c + 2 * d)/6;
			c2 = ( 2 * a - 9 * b + 18 * c - 5 * d)/6;	
		}

		public static void SetCustomTangents(AnimationCurve curve, int i, int nextI, float[] tangentArray){
			float diffValue = curve[nextI].value - curve[i].value;
			float diffTime = curve[nextI].time - curve[i].time;
			if (diffValue == 0)
				return; 


			float cx1 = tangentArray[0];
			float cy1 = tangentArray[1];
			float cx2 = tangentArray[2];
			float cy2 = tangentArray[3];

			Vector2 p0     = new Vector2(0  , curve[i].value);
			Vector2 p3     = new Vector2(diffTime  , curve[nextI].value);
			Vector2 cOrig1 = new Vector2(diffTime * cx1, curve[i].value);
			cOrig1.y += diffValue > 0 ? diffValue * cy1 : -1.0f * Mathf.Abs(diffValue * cy1);

			Vector2 cOrig2 = new Vector2(diffTime * cx2, curve[i].value);
			cOrig2.y += diffValue > 0 ? diffValue * cy2 : -1.0f * Mathf.Abs(diffValue * cy2);

			Vector2 p1 = GetBezierPoint(p0, cOrig1, cOrig2, p3, 1.0f / 3.0f);
			Vector2 p2 = GetBezierPoint(p0, cOrig1, cOrig2, p3, 2.0f / 3.0f);


			Vector2 c1tg, c2tg, c1, c2;
			CalcControlPoints(p0,p1,p2,p3, out c1, out c2);

			c1tg = c1 - p0;
			c2tg = c2 - p3;

			float outTangent = c1tg.y / c1tg.x;
			float inTangent  = c2tg.y / c2tg.x;


			object thisKeyframeBoxed = curve[i];
			object nextKeyframeBoxed = curve[nextI];


			if (!KeyframeUtil.isKeyBroken(thisKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(thisKeyframeBoxed, true);

			TangentMode mode = TangentMode.Editable;
			if(cx1==0f&&cy1==0f) mode=TangentMode.Linear;
			KeyframeUtil.SetKeyTangentMode(thisKeyframeBoxed, 1, mode);

			if (!KeyframeUtil.isKeyBroken(nextKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(nextKeyframeBoxed, true);	

			mode = TangentMode.Editable;
			if(cx2==1f&&cy2==1f) mode=TangentMode.Linear;
			KeyframeUtil.SetKeyTangentMode(nextKeyframeBoxed, 0, mode);

			Keyframe thisKeyframe = (Keyframe)thisKeyframeBoxed;
			Keyframe nextKeyframe = (Keyframe)nextKeyframeBoxed;

			thisKeyframe.outTangent = outTangent;
			nextKeyframe.inTangent  = inTangent;

			curve.MoveKey(i, 	 thisKeyframe);
			curve.MoveKey(nextI, nextKeyframe);

			//* test method
			float startTime = thisKeyframe.time;

			for (float j=0; j < 25f; j++) {
				float t  = j/25.0f;
				curve.Evaluate(startTime + diffTime * t);			
			}

		}

		public static bool NearlyEqual(float a, float b, float epsilon)
		{
			float absA = Mathf.Abs(a);
			float absB = Mathf.Abs(b);
			float diff = Mathf.Abs(a - b);

			if (a == b)
			{ // shortcut, handles infinities
				return true;
			} 
			else if (a == 0 || b == 0 || diff < System.Double.MinValue) 
			{
				// a or b is zero or both are extremely close to it
				// relative error is less meaningful here
				return diff < (epsilon * System.Double.MinValue);
			}
			else
			{ // use relative error
				return diff / (absA + absB) < epsilon;
			}
		}
		#endregion
	}
}