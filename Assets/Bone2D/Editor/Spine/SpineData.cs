using UnityEngine;
using System.Collections.Generic;

namespace Bones2D
{
	/// <summary>
	/// Spine data.
	/// author:bingheliefeng
	/// </summary>
	public class SpineData {

		public class BoneData {
			public int index;
			public string name = null;//The bone name. This is unique for the skeleton.
			public string parent = null;//parent bone name
			public float x = 0f;
			public float y = 0f;
			public float scaleX = 1f;
			public float scaleY = 1f;
			public float rotation = 0;
			public float length = 0;
			public bool inheritRotation=true;
			public bool inheritScale=true;
		}

		public class SlotData {
			public int index = 0;
			public string name = null; //The slot name. This is unique for the skeleton.
			public string bone = null; //The name of the bone that this slot is attached to
			public Color color = Color.white;
			public string attachment = null;
			public string blend="normal";//normal, additive, multiply, or screen.
			public int displayIndex=0;
		}

		public class SkinAttachment{
			public string type="region";//region,mesh,linkedmesh,boundingbox,path
			public string name = null;//attachment name
            public string realName = null;//real attachment name
			public string textureName =null;//texture path
			public float x = 0f;
			public float y = 0f;
			public float scaleX = 1f;
			public float scaleY = 1f;
			public float width = 0f;
			public float height = 0f;
			public float rotation = 0;
			public Color color = Color.white;
			public bool deform = true;//is ffd
			public Vector2[] uvs;
			public List<float> weights;//boneCount,boneIndex,vertexRelativBoneX,vertexRelativBoneY,weight
			public Vector3[] vertices;
			public int[] triangles;
			public int[] edges;
			public int hull;//triangles count

		}
		public class SkinData{
			public string skinName;
			public Dictionary<string,List<SkinAttachment>>  slots; //key is slot name
		}
		public class IKData{
			public string name; //The constraint name. This is unique for the skeleton.
			public string[] bones = null; //A list of 1 or 2 bone names whose rotation will be controlled by the constraint.
			public string target ; //The name of the target bone.
			public float mix = 1f;//weight
			public bool bendPositive = false;
		}
		public class SpineEvent{
			public string name;
			public int i = 0 ;
			public float f = 0f;
			public string s = null;
		}
		public class AnimationData{
			public string name;//animation name
			public AnimationBoneData[] boneAnims;
			public AnimationSlotData[] slotAnims;
			public AnimationDeformData[] deforms;
			public AnimationEventsData[] events;
			public AnimationDrawOrderData[] drawOrders;
			public AnimationIKData[] ikDatas;
		}
		public class AnimationBoneData{
			public string name;//boneName
			public BoneTimeline[] timelines;
		}
		public class AnimationSlotData{
			public string name;//slotName
			public SlotTimeline[] timelines;
		}
		public class AnimationIKData{
			
		}
		public class AnimationDeformData{
			public string skinName = null;
			public string slotName = null ;
			public DeformTimeline[] timelines;
		}
		public class AnimationEventsData{
			public float time = 0f;
			public string name;
			public string stringParam = null;
			public int intParam = 0;
			public float floatParam=0f;
		}
		public class AnimationDrawOrderData{
			public float time = 0f;
			public DrawOrderOffset[] offsets;
		}
		public class BaseTimeline{
			public float time=0f;
			public float[] curve = null;
			public string tweenEasing="linear";//linear or stepped
		}
		public class BoneTimeline:BaseTimeline{
			public string type="";//rotate,scale,translate,shear

			public float angle = float.NaN;//for rotate
			public float x = float.NaN;//for translate,scale,shear
			public float y = float.NaN;//for translate,scale,shear
		}
		public class SlotTimeline:BaseTimeline{
			public string type="";//attachment ,color
			public string color = null; //hex
			public string attachmentName = null;
		}
		public class DeformTimeline:BaseTimeline{
			public string attachment;//mesh name
			public int offset=0;//vertices offset
			public Vector3[] vertices=null;//changed vertices
		}
		public class DrawOrderOffset{
			public string slotName;
			public int offset=0;//default pose
		}

		public class ArmatureData{
			public string name;//Armature name = file name
			public BoneData[] bones;
			public SlotData[] slots;
			public IKData[] iks;
			public SkinData[] skins;
			public AnimationData[] animations;
			public float frameRate = 60;
		}
	}

}