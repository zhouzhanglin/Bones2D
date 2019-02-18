using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Bones2D
{
	public class PoseData : ScriptableObject {
		public enum AttachmentType
		{
			IMG,MESH,LINKED_MESH,BOX
		}
		[System.Serializable]
		public class TransformData{
			public float x,y;
			public float rotation;
			public float sx,sy;
		}
		[System.Serializable]
		public class SlotData{
			public Color color;
			public int displayIndex;
			public int zorder;
		}
		[System.Serializable]
		public class DisplayData{
			public Color color;
			public Vector3[] vertex;
			public TransformData transform;
			public AttachmentType type;
		}
	
		public TransformData[] boneDatas;
		public SlotData[] slotDatas;
		public DisplayData[] displayDatas;
	}

}