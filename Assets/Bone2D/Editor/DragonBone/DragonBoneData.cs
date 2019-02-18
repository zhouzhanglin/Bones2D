using UnityEngine;
using System.Collections.Generic;

namespace Bones2D
{
	/// <summary>
	/// Dragon bone data.
	/// author:bingheliefeng
	/// </summary>
	public class DragonBoneData {
		public class ActionData{
			public string key="";//gotoAndPlay
			public string action;//anim name
		}
		public class BoneData {
			public string name;
			public string parent;
			public TransformData transform = new TransformData();
			public float length;
			public bool inheritRotation=true;
			public bool inheritScale=true;
		}
		public class SlotData {
			public string name;
			public string parent;
			public int displayIndex=0;
			public float z;
			public float scale =1f;//缩放值，默认为1
			public string blendMode="normal";
			public ColorData color;
			public ActionData[] actions;
		}
		public class IKData{
			public string name;//ik名称
			public string bone;//ik绑定骨骼名称
			public string target = null;//IK约束的目标骨骼名称
			public bool bendPositive = true;//弯曲方向，默认为true
			public int chain=0;//影响的骨骼数量
			public float weight=1f;//权重
		}
		public class TransformData {
			public float x = 0f;
			public float y = 0f;
			public float rotate = 0f;
            public float skew = 0f;
			public float scx = 1f;
			public float scy = 1f;
			public TransformData Add( TransformData data){
				x+=data.x;
				y+=data.y;
				rotate+=data.rotate;
				scx+=data.scx;
				scy+=data.scy;
				return this;
			}
			public override string ToString ()
			{
				return "x:"+x+" y:"+y+" rotate:"+rotate+" sc:"+scx;
			}
		}
		public class ColorData{
			public float aM=1f,rM=1f,gM=1f,bM = 1f; //颜色叠加 0-1
			public float a0=0f,r0=0f,g0=0f,b0 =0f;//颜色偏移-1 - 1
			public Color ToColor(){
				Color c = Color.white;
				c.a = aM+a0;
				c.r = rM+r0;
				c.g = gM+g0;
				c.b = bM+b0;
				return c;
			}
		}

		public class AnimationData {
			public string name;//动画名称
			public int playTimes=1;//播放次数，0为循环播放 
			public int duration =1; // 动画帧长度 (可选属性 默认: 1)
			public AnimKeyData[] keyDatas;
			public AnimSubData[] boneDatas;
			public AnimSubData[] slotDatas;
			public AnimSubData[] ffdDatas;
			public AnimSubData[] zOrderDatas;
		}
		public class AnimKeyData{ //此动画包含的关键帧数据
			public int duration = 1;
			public string eventName;
			public string soundName;
			public string actionName;
		}
		public class AnimSubData{
			public string name;//slotname , bone name
			public string slot;//如果有slot，就用slot
			public float scale=1f;
			public float offset=0f;
			public AnimFrameData[] frameDatas;//for 5.3

			//for 5.5
			public AnimFrameData[] translateFrameDatas;
			public AnimFrameData[] rotateFrameDatas;
			public AnimFrameData[] scaleFrameDatas;
			public AnimFrameData[] colorFrameDatas;
			public AnimFrameData[] displayFrameDatas;

		}

		public enum FrameType{
			Frame,
			TranslateFrame,
			RotateFrame,
			ScaleFrame,
			ColorFrame,
			DisplayFrame
		}

		public class AnimFrameData { //此动画包含的关键帧列表
			public int duration = 1;
			public float[] curve;
			public int[] zOrder;
			public float tweenEasing=float.PositiveInfinity;
			public int tweenRotate = 0 ;//转的圈数
			public int displayIndex=0;
			public float z;
			public TransformData transformData = new TransformData();
			public ColorData color = new ColorData();
			public ActionData[] actions;
			public FrameType frameType = FrameType.Frame;

			//网格动画
			public int offset=0;//顶点坐标索引偏移
			public Vector2[] vertices;//顶点位置,顶点坐标相对位移
		}


		public class SkinData {
			public string skinName;
			public SkinSlotData[] slots;
		}
		public class SkinSlotData{
			public string slotName;
			public ActionData[] actions;
			public SkinSlotDisplayData[] displays;
		}
		public class SkinSlotDisplayData{
			public string textureName;
			public string texturePath;
			public string type = "image";//armature,mesh,boundingBox
			public string subType="polygon";
			public Vector2 pivot = new Vector2(0.5f,0.5f);
			public TransformData transform = new TransformData();

			//网格变化
			public Vector3[] vertices;
			public Vector2[] uvs;
			public int[] triangles;
			public int boneIndex;
			public int vertexIndex;
			public float[] bonePose;
			public float[] weights;//[vertex index, bone index, weight, ...]
			public int[] edges;
		}
		public class ArmatureData{
			public string name;
			public int isGlobal = 0 ;
			public float frameRate = 24 ;
			public string type="Armature";//MovieClip
			public ActionData[] actions;
			public BoneData[] boneDatas;
			public SlotData[] slotDatas;
			public AnimationData[] animDatas;
			public SkinData[] skinDatas;
			public IKData[] ikDatas;
		}
	}
}