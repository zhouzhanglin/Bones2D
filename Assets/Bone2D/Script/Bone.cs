using UnityEngine;
using System.Collections;

namespace Bones2D
{
	[ExecuteInEditMode,DisallowMultipleComponent]
	public class Bone : MonoBehaviour {

		public Transform inheritRotation;
		public Transform inheritScale;

		void LateUpdate(){
			if(inheritRotation){
				transform.rotation = inheritRotation.rotation;
			}
			if(inheritScale && inheritScale.parent.localScale.x!=0 && inheritScale.parent.localScale.y!=0){
				Transform parent = transform.parent;
				transform.parent = inheritScale.parent;
				transform.localScale = inheritScale.localScale;
				transform.parent = parent;
			}
		}
	}
}
