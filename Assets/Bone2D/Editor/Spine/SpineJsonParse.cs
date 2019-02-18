using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Spine json parse.
/// author:bingheliefeng
/// </summary>
namespace Bones2D
{
	public class SpineJsonParse {

		public static void ParseTextureAtlas(SpineArmatureEditor armatureEditor , Texture2D texture, string atlasText )
		{
			using(TextReader reader = new StringReader(atlasText))
			{
				int index = 0;
				string pngName = null;
				while (reader.Peek() != -1)
				{
					string line = reader.ReadLine().Trim();
					if(line.Length>0){

						if(line.LastIndexOf(".png")>-1||line.LastIndexOf(".PNG")>-1)
						{
							if(line.Contains(texture.name)){
								pngName = line;
							}else{
								pngName = null;
							}
						}
						if(pngName!=null && line.IndexOf(":")==-1) {
							SpineArmatureEditor.Atlas atlas =new SpineArmatureEditor.Atlas();
							atlas.texture = texture;
							atlas.atlasText = atlasText;
							armatureEditor.atlasKV[line] = atlas;
						}
					}
					++index;
				}
				reader.Close();
			}

		}


		private static Dictionary<string, EventData> eventPoseKV = null;

		public static void ParseAnimJsonData(SpineArmatureEditor armatureEditor)
		{
			string str = armatureEditor.animTextAsset.text.Replace("null","\"null\"");
			Bones2D.JSONClass json=Bones2D.JSON.Parse(str.Replace("/","_")).AsObject;

			armatureEditor.armatureData = new SpineData.ArmatureData();
			GameObject go = new GameObject(armatureEditor.animTextAsset.name);
			Armature armature = go.AddComponent<Armature>();
			armature.isUGUI = armatureEditor.isUGUI;
			armatureEditor.armature = go.transform;
			armatureEditor.bonesKV.Clear();
			armatureEditor.slotsKV.Clear();
			armatureEditor.bonesDataKV.Clear();
			armatureEditor.slotsDataKV.Clear();
			armatureEditor.slots.Clear();
			armatureEditor.bones.Clear();
			armatureEditor.ffdKV.Clear();
			armatureEditor.animList.Clear();
			eventPoseKV = new Dictionary<string, EventData>();

			ParseArmtureData(armatureEditor,json);
			armatureEditor.InitShow();
		}


		private static void ParseArmtureData(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			ParseBones(armatureEditor,armtureObj);
			ParseSlots(armatureEditor,armtureObj);
			ParseIKs(armatureEditor, armtureObj);
			ParseSkins(armatureEditor,armtureObj);
			ParseEvents(armatureEditor,armtureObj);
			ParseAnimations(armatureEditor,armtureObj);
		}

		private static void ParseEvents(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj){
			if(armtureObj.ContainKey("events"))
			{
				Bones2D.JSONClass eventsObj = armtureObj["events"].AsObject;
				foreach(string name in eventsObj.GetKeys()){
					Bones2D.JSONClass evtObj = eventsObj[name].AsObject;
					EventData evtData = new EventData();
					if(evtObj.ContainKey("string")){
						evtData.stringParam = evtObj["string"].ToString();
					}
					if(evtObj.ContainKey("int")){
						evtData.intParam = evtObj["int"].AsInt;
					}
					if(evtObj.ContainKey("float")){
						evtData.floatParam = evtObj["float"].AsFloat;
					}
					eventPoseKV[name]=evtData;
				}
			}
		}

		private static void ParseIKs(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj){
			if(armtureObj.ContainKey("ik")){
				Bones2D.JSONArray iks = armtureObj["ik"].AsArray;
				SpineData.IKData[] ikDatas = new SpineData.IKData[iks.Count];
				for(int i=0;i<iks.Count;++i){
					Bones2D.JSONClass ikObj = iks[i].AsObject;
					SpineData.IKData ikData=  new SpineData.IKData();
					ikData.name = ikObj["name"].ToString();
					ikData.target = ikObj["target"].ToString();
					if(ikObj.ContainKey("bones")){
						Bones2D.JSONArray bones = ikObj["bones"].AsArray;
						ikData.bones = new string[bones.Count];
						for(int j=0;j<bones.Count;++j){
							ikData.bones[j] = bones[j];
						}
					}
					if(ikObj.ContainKey("mix")){
						ikData.mix = ikObj["mix"].AsFloat;
					}
					if(ikObj.ContainKey("bendPositive")){
						ikData.bendPositive = ikObj["bendPositive"].AsBool;
					}
					ikDatas[i] = ikData;
				}
				armatureEditor.armatureData.iks = ikDatas;
			}
		}

		private static void ParseBones(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("bones")){
				Bones2D.JSONArray bones = armtureObj["bones"].AsArray;
				SpineData.BoneData[] boneDatas = new SpineData.BoneData[bones.Count];
				for(int i=0;i<bones.Count;++i){
					Bones2D.JSONClass boneObj = bones[i].AsObject;
					SpineData.BoneData boneData = new SpineData.BoneData();
					if(boneObj.ContainKey("length"))  boneData.length = boneObj["length"].AsFloat;
					if(boneObj.ContainKey("name"))  boneData.name = boneObj["name"].ToString();
					if(boneObj.ContainKey("parent"))  boneData.parent = boneObj["parent"].ToString();
					if(boneObj.ContainKey("inheritRotation")) boneData.inheritRotation = boneObj["inheritRotation"].AsInt==1?true:false;
					if(boneObj.ContainKey("inheritScale")) boneData.inheritScale = boneObj["inheritScale"].AsInt==1?true:false;
					if(boneObj.ContainKey("x")) boneData.x = boneObj["x"].AsFloat*armatureEditor.unit;
					if(boneObj.ContainKey("y")) boneData.y = boneObj["y"].AsFloat*armatureEditor.unit;
					if(boneObj.ContainKey("scaleX")) boneData.scaleX = boneObj["scaleX"].AsFloat;
					if(boneObj.ContainKey("scaleY")) boneData.scaleY = boneObj["scaleY"].AsFloat;
					if(boneObj.ContainKey("shearX")) boneData.scaleX = boneObj["shearX"].AsFloat;
					if(boneObj.ContainKey("shearY")) boneData.scaleY = boneObj["shearY"].AsFloat;
					if(boneObj.ContainKey("rotation")) boneData.rotation = boneObj["rotation"].AsFloat;
					boneData.index = i;
					boneDatas[i] = boneData;
					armatureEditor.bonesDataKV[boneData.name]=boneData;
				}
				armatureEditor.armatureData.bones = boneDatas;
			}
		}

		private static void ParseSlots(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("slots")){
				Bones2D.JSONArray slots = armtureObj["slots"].AsArray;
				SpineData.SlotData[] slotDatas = new SpineData.SlotData[slots.Count];
				for(int i=0;i<slots.Count;++i){
					Bones2D.JSONClass slotObj = slots[i].AsObject;
					SpineData.SlotData slotData = new SpineData.SlotData();
					slotData.displayIndex = i;
					if(slotObj.ContainKey("name"))  slotData.name = slotObj["name"].ToString();
					if(slotObj.ContainKey("bone"))  slotData.bone = slotObj["bone"].ToString();
					if(slotObj.ContainKey("color"))  slotData.color = SpineArmatureEditor.HexToColor(slotObj["color"].ToString());
					if(slotObj.ContainKey("attachment"))  slotData.attachment = slotObj["attachment"].ToString();
					if(slotObj.ContainKey("blend")) slotData.blend = slotObj["blend"].ToString();					
					slotData.index = i;
					slotDatas[i] = slotData;
					armatureEditor.slotsDataKV[slotData.name]=slotData;
				}
				armatureEditor.armatureData.slots = slotDatas;
			}
		}

		private static void ParseSkins(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("skins")){
				Bones2D.JSONClass skins=armtureObj["skins"].AsObject;
				SpineData.SkinData[] skinDatas = new SpineData.SkinData[skins.Count];
				armatureEditor.armatureData.skins = skinDatas;
				int skinDataCount=0;
				string[] skinNames = new string[skins.Count];
				foreach(string skinName in skins.GetKeys()){
					Bones2D.JSONClass skinSlots = skins[skinName].AsObject;
					SpineData.SkinData skinData= new SpineData.SkinData();
					skinDatas[skinDataCount] = skinData;
					skinData.skinName=skinName;
					skinNames[skinDataCount] = skinName;
					skinData.slots = new Dictionary<string, List<SpineData.SkinAttachment>>();
					foreach(string slotName in skinSlots.GetKeys()){
						Bones2D.JSONClass skinAttments = skinSlots[slotName].AsObject;
						skinData.slots[slotName] = new List<SpineData.SkinAttachment>();
						foreach(string attachmentName in skinAttments.GetKeys()){
							Bones2D.JSONClass attachmentObj = skinAttments[attachmentName].AsObject;
							SpineData.SkinAttachment attachment = new SpineData.SkinAttachment();
							attachment.name = attachmentName;
                            if (attachmentObj.ContainKey("name"))
                                attachment.realName = attachmentObj["name"].ToString();
                            else
                                attachment.realName = attachmentName;
                            if (attachmentObj.ContainKey("path"))
                                attachment.textureName = attachmentObj["path"].ToString();
                            else
                                attachment.textureName = attachment.realName;
							if(attachmentObj.ContainKey("type")) attachment.type = attachmentObj["type"].ToString();//region,mesh,linkedmesh,boundingbox,path
							if(attachment.type=="path" || attachment.type=="linkedmesh") continue;
							skinData.slots[slotName].Add(attachment);
							if(attachmentObj.ContainKey("x")) attachment.x = attachmentObj["x"].AsFloat*armatureEditor.unit;
							if(attachmentObj.ContainKey("y")) attachment.y = attachmentObj["y"].AsFloat*armatureEditor.unit;
							if(attachmentObj.ContainKey("scaleX")) attachment.scaleX = attachmentObj["scaleX"].AsFloat;
							if(attachmentObj.ContainKey("scaleY")) attachment.scaleY = attachmentObj["scaleY"].AsFloat;
							if(attachmentObj.ContainKey("width")) attachment.width = attachmentObj["width"].AsFloat;
							if(attachmentObj.ContainKey("height")) attachment.height = attachmentObj["height"].AsFloat;
							if(attachmentObj.ContainKey("rotation")) attachment.rotation = attachmentObj["rotation"].AsFloat;
							if(attachmentObj.ContainKey("color")) attachment.color = SpineArmatureEditor.HexToColor(attachmentObj["color"].ToString());
							if(attachmentObj.ContainKey("hull")) attachment.hull = attachmentObj["hull"].AsInt;
							if(attachmentObj.ContainKey("uvs")){
								Bones2D.JSONArray uvsObj = attachmentObj["uvs"].AsArray;
								attachment.uvs=new Vector2[uvsObj.Count/2];
								for(int z =0;z<uvsObj.Count;z+=2){
									Vector2 uv = new Vector2(uvsObj[z].AsFloat,1-uvsObj[z+1].AsFloat);
									attachment.uvs[z/2] = uv;
								}
							}
							//triangles
							if(attachmentObj.ContainKey("triangles")){
								Bones2D.JSONArray trianglesObj = attachmentObj["triangles"].AsArray;
								attachment.triangles=new int[trianglesObj.Count];
								for(int z =0;z<trianglesObj.Count;z++){
									attachment.triangles[z] = trianglesObj[z].AsInt;
								}
							}
							//vertex
							if(attachmentObj.ContainKey("vertices")){
								Bones2D.JSONArray verticesObj = attachmentObj["vertices"].AsArray;
								if(attachment.uvs!=null && verticesObj.Count>attachment.uvs.Length*2)
								{
									//have weighted
									List<float> weights = new List<float>();
									for(int i =0;i<verticesObj.Count;++i){
										int boneCount = verticesObj[i].AsInt;
										weights.Add(boneCount);
										for(int j=0;j<boneCount*4;j+=4){
											weights.Add(verticesObj[i+j+1].AsInt);//bone index
											weights.Add(verticesObj[i+j+2].AsFloat*armatureEditor.unit);//relativeBoneX
											weights.Add(verticesObj[i+j+3].AsFloat*armatureEditor.unit);//relativeBoneY
											weights.Add(verticesObj[i+j+4].AsFloat);//weight
										}
										i+=boneCount*4;
									}
									attachment.weights = weights;
								}
								else
								{
									//only vertices
									attachment.vertices = new Vector3[verticesObj.Count/2];
									for(int i =0;i<verticesObj.Count;i+=2){
										Vector3 vertex = new Vector3(verticesObj[i].AsFloat,verticesObj[i+1].AsFloat,0f);
										vertex.x*=armatureEditor.unit;
										vertex.y*=armatureEditor.unit;
										attachment.vertices[i/2] = vertex;
									}
								}
							}
							//edges
							if(armatureEditor.genMeshCollider && attachmentObj.ContainKey("edges")){
								Bones2D.JSONArray edgesObj = attachmentObj["edges"].AsArray;
								int len=edgesObj.Count;
								List<int> edges = new List<int>();
								for(int z =0;z<len;++z){
									int value = edgesObj[z].AsInt/2;
									if(edges.Count>0){
										if(edges[edges.Count-1]!=value)	edges.Add(value);
									}else{
										edges.Add(value);
									}
								}
								if(edges.Count>2 && edges[0]==edges[edges.Count-1]) edges.RemoveAt(edges.Count-1); 
								if(edges.Count>2){
									attachment.edges = edges.ToArray();
								}
							}
						}
					}
					++skinDataCount;
				}
				armatureEditor.armature.GetComponent<Armature>().skins = skinNames;
			}
		}

		private static void ParseAnimations(SpineArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("animations")){
				Bones2D.JSONClass anims = armtureObj["animations"].AsObject;
				List<SpineData.AnimationData> animList = new List<SpineData.AnimationData>();
				foreach(string animName in anims.GetKeys()){
					SpineData.AnimationData animData = new SpineData.AnimationData();
					animData.name=animName;
					animList.Add(animData);

					Bones2D.JSONClass animObj = anims[animName].AsObject;
					if(animObj.ContainKey("bones")){
						Bones2D.JSONClass bonesObj = animObj["bones"].AsObject;
						animData.boneAnims = ParseAnimBones(armatureEditor, bonesObj).ToArray();
					}
					if(animObj.ContainKey("slots")){
						Bones2D.JSONClass slotsObj = animObj["slots"].AsObject;
						animData.slotAnims = ParseAnimSlots(slotsObj).ToArray();
					}
					if(animObj.ContainKey("ik")){
						
					}
					if(animObj.ContainKey("deform")){
						Bones2D.JSONClass deformObj = animObj["deform"].AsObject;
						animData.deforms = ParseAnimDeform(armatureEditor,deformObj).ToArray();
					}else if(animObj.ContainKey("ffd")){
						Bones2D.JSONClass deformObj = animObj["ffd"].AsObject;
						animData.deforms = ParseAnimDeform(armatureEditor,deformObj).ToArray();
					}
					if(animObj.ContainKey("events")){
						Bones2D.JSONArray eventsArray = animObj["events"].AsArray;
						animData.events = ParseAnimEvents(eventsArray).ToArray();
					}
					if(animObj.ContainKey("drawOrder")){
						Bones2D.JSONArray drawOrderArray = animObj["drawOrder"].AsArray;
						animData.drawOrders = ParseDrawOrders(armatureEditor ,drawOrderArray).ToArray();
					}else if(animObj.ContainKey("draworder")){
						Bones2D.JSONArray drawOrderArray = animObj["draworder"].AsArray;
						animData.drawOrders = ParseDrawOrders(armatureEditor ,drawOrderArray).ToArray();
					}

				}
				armatureEditor.armatureData.animations = animList.ToArray();
			}
		}


		private static List<SpineData.AnimationDrawOrderData> ParseDrawOrders(SpineArmatureEditor armatureEditor, Bones2D.JSONArray drawOrdersArray){
			List<SpineData.AnimationDrawOrderData> animDrawOrders = new List<SpineData.AnimationDrawOrderData>();
			if(drawOrdersArray!=null && drawOrdersArray.Count>0){
				for(int i=0;i<drawOrdersArray.Count; ++i){
					Bones2D.JSONClass drawOrderObj = drawOrdersArray[i].AsObject;
					SpineData.AnimationDrawOrderData orderData = new SpineData.AnimationDrawOrderData();

					orderData.time = drawOrderObj["time"].AsFloat;
					if(drawOrderObj.ContainKey("offsets")){
						Bones2D.JSONArray offsetsObj = drawOrderObj["offsets"].AsArray;
						orderData.offsets = new SpineData.DrawOrderOffset[offsetsObj.Count];
						for(int j=0;j<offsetsObj.Count;++j){
							Bones2D.JSONClass offsetObj = offsetsObj[j].AsObject;

							SpineData.DrawOrderOffset offset = new SpineData.DrawOrderOffset();
							offset.slotName = offsetObj["slot"].ToString();
							if(offsetObj.ContainKey("offset")){
								offset.offset = offsetObj["offset"].AsInt;
								//the last offset is 0
								if(offset.offset==0){
									int lastSlotIdx = armatureEditor.slotsDataKV[offset.slotName].index;
									offset.slotName = armatureEditor.armatureData.slots[lastSlotIdx+1].name;
									offset.offset = -offsetsObj.Count;
								}
							}
							orderData.offsets[j]=offset;
						}
					}
					animDrawOrders.Add(orderData);
				}
			}
			return animDrawOrders;
		}

		private static List<SpineData.AnimationEventsData> ParseAnimEvents(Bones2D.JSONArray eventsArray){
			List<SpineData.AnimationEventsData> evts=new List<SpineData.AnimationEventsData>();
			if(eventsArray!=null && eventsArray.Count>0){
				for(int i=0;i< eventsArray.Count ;++i){
					Bones2D.JSONClass eventObj = eventsArray[i].AsObject;
					SpineData.AnimationEventsData animEvtData = new SpineData.AnimationEventsData();

					animEvtData.name = eventObj["name"].ToString();
					animEvtData.time = eventObj["time"].AsFloat;
					EventData evtPoseData = null;
					if( eventPoseKV.ContainsKey(animEvtData.name)){
						evtPoseData = eventPoseKV[animEvtData.name];;
					}

					if(eventObj.ContainKey("string")){
						animEvtData.stringParam = eventObj["string"].ToString();
					}else if(evtPoseData!=null && !string.IsNullOrEmpty(evtPoseData.stringParam)){
						animEvtData.stringParam = evtPoseData.stringParam;
					}

					if(eventObj.ContainKey("int")){
						animEvtData.intParam = eventObj["int"].AsInt;
					}else if(evtPoseData!=null){
						animEvtData.intParam = evtPoseData.intParam;
					}

					if(eventObj.ContainKey("float")){
						animEvtData.floatParam = eventObj["float"].AsFloat;
					}else if(evtPoseData!=null){
						animEvtData.floatParam = evtPoseData.floatParam;
					}

					evts.Add(animEvtData);
				}
			}
			return evts;
		}

		private static List<SpineData.AnimationDeformData> ParseAnimDeform(SpineArmatureEditor armatureEditor,Bones2D.JSONClass deformObj){
			List<SpineData.AnimationDeformData> animationDeformDatas = new List<SpineData.AnimationDeformData>();
			foreach(string skinName in deformObj.GetKeys()){
				Bones2D.JSONClass skinObj = deformObj[skinName].AsObject;
				foreach(string slotName in skinObj.GetKeys()){
					Bones2D.JSONClass slotObj = skinObj[slotName].AsObject;
					foreach(string attachmentName in slotObj.GetKeys()){
						animationDeformDatas.Add(ParseDeformAnimTimeline(armatureEditor,skinName,slotName,attachmentName,slotObj[attachmentName].AsArray));
					}
				}
			}
			return animationDeformDatas;
		}

		private static SpineData.SkinAttachment GetSkinAttachment(SpineArmatureEditor armatureEditor,string skinName,string slotname,string attchmentname)
		{
			foreach(var skin in armatureEditor.armatureData.skins){
				if(skin.skinName == skinName) 
				{
					foreach( var att in skin.slots[slotname]){
						if(att.name == attchmentname){
							return att;
						}
					}
					break;
				}
			}
			return null;
		}

		private static SpineData.AnimationDeformData ParseDeformAnimTimeline(SpineArmatureEditor armatureEditor, string skinName,string slotname,string attchmentname,Bones2D.JSONArray deformAnimObj){
			SpineData.AnimationDeformData animDeformDatas = new SpineData.AnimationDeformData();
			animDeformDatas.slotName = slotname;
			animDeformDatas.skinName = skinName;
			animDeformDatas.timelines = new SpineData.DeformTimeline[deformAnimObj.Count];

			SpineData.SkinAttachment skinAtt = GetSkinAttachment(armatureEditor,skinName,slotname,attchmentname);
			bool haveWeight = ( skinAtt == null || skinAtt.weights == null || skinAtt.weights.Count == 0) ? false : true;

			for(int i=0;i<deformAnimObj.Count;++i){
				SpineData.DeformTimeline timeline = new SpineData.DeformTimeline();
				animDeformDatas.timelines[i] = timeline;
				timeline.attachment = attchmentname;
				Bones2D.JSONClass animObj = deformAnimObj[i].AsObject;

				if(animObj.ContainKey("time")) timeline.time = animObj["time"].AsFloat;
				if(animObj.ContainKey("curve")){
					if(animObj["curve"]=="stepped"){
						timeline.tweenEasing = "stepped";
					}
					else if(animObj["curve"]=="linear"){
						//default
					}
					else{
						timeline.curve = ConvertJsonArrayToFloatArr(animObj["curve"].AsArray);
					}
				}
		
				if(animObj.ContainKey("offset")) {
					timeline.offset = animObj["offset"].AsInt/2;
				}
				if(animObj.ContainKey("vertices")){
					Bones2D.JSONArray verticesObj = animObj["vertices"].AsArray;

					int index=0;
					int k= 0;
					timeline.vertices = new Vector3[verticesObj.Count/2];
					for(;k<verticesObj.Count && k+1<verticesObj.Count;k+=2)
					{
						timeline.vertices[index]=new Vector3(verticesObj[k].AsFloat*armatureEditor.unit,verticesObj[k+1].AsFloat*armatureEditor.unit,0f);
						++index;
					}
					armatureEditor.ffdKV [attchmentname] = true;

					if (haveWeight) {
						CreateBonePose(armatureEditor);
						BoneMatrix2D matrix = new BoneMatrix2D();
						int vertexIndex = 0;
						int offset = timeline.offset;
						int newOffset = 0;
						for (int j = 0; j < skinAtt.weights.Count; ++j) 
						{
							int boneCount = (int)skinAtt.weights[j];
							if(offset<=0)
							{
								Vector3 v = timeline.vertices [vertexIndex];
								Vector3 result = new Vector3 ();
								for(int w = 0; w < boneCount*4; w+=4) {
									int boneIndex = (int)skinAtt.weights [j + w + 1];
									SpineData.BoneData boneData = armatureEditor.armatureData.bones [boneIndex];
									float weight = skinAtt.weights [j + w + 4];

									BoneMatrix2D boneMatrix = armatureEditor.bonePoseKV [boneData.name];
									matrix.Identity ();
									matrix.a = boneMatrix.a;
									matrix.b = boneMatrix.b;
									matrix.c = boneMatrix.c;
									matrix.d = boneMatrix.d;
									matrix.Invert ();//to local

									Vector2 p = matrix.TransformPoint (v.x,v.y);
									result.x += p.x*weight;
									result.y += p.y*weight;
								}
								timeline.vertices [vertexIndex] = result;
								++vertexIndex;
								if (vertexIndex >= timeline.vertices.Length) {
									break;
								}
							}
							else
							{
								++newOffset;
							}
							offset -= boneCount;
							j += boneCount * 4;
						}
						timeline.offset = newOffset;
					}

				}
			}
			return animDeformDatas;
		}


		private static void CreateBonePose(SpineArmatureEditor armatureEditor)
		{
			if(armatureEditor.bonePoseKV==null){
				armatureEditor.bonePoseKV = new Dictionary<string, BoneMatrix2D> ();
				for (int i = 0; i < armatureEditor.armatureData.bones.Length; ++i) {
					SpineData.BoneData boneData = armatureEditor.armatureData.bones [i];
					BoneMatrix2D matrix = new BoneMatrix2D ();
					matrix.Rotate (boneData.rotation);
					matrix.Scale (boneData.scaleX,boneData.scaleY);
					matrix.Translate (boneData.x,boneData.y);
					if (!string.IsNullOrEmpty(boneData.parent)) {
						SpineData.BoneData parentBone = armatureEditor.bonesDataKV[boneData.parent];
						if(parentBone!=null && armatureEditor.bonePoseKV.ContainsKey(parentBone.name)) {
							matrix.Concat (armatureEditor.bonePoseKV [parentBone.name]);
						}
					}
					armatureEditor.bonePoseKV [boneData.name] = matrix;
				}
			}
		}


		private static List<SpineData.AnimationSlotData> ParseAnimSlots(Bones2D.JSONClass slotsObj){
			List<SpineData.AnimationSlotData> animationSlotDatas = new List<SpineData.AnimationSlotData>();
			foreach(string slotName in slotsObj.GetKeys()){
				Bones2D.JSONClass slotAnimObj = slotsObj[slotName].AsObject;
				if(slotAnimObj.ContainKey("attachment")){
					animationSlotDatas.Add(ParseSlotAnimTimeline(slotName,slotAnimObj,"attachment"));
				}
				if(slotAnimObj.ContainKey("color")){
					animationSlotDatas.Add(ParseSlotAnimTimeline(slotName,slotAnimObj,"color"));
				}
			}
			return animationSlotDatas;
		}
		private static SpineData.AnimationSlotData ParseSlotAnimTimeline(string slotName,Bones2D.JSONClass slotAnimObj,string animType){
			Bones2D.JSONArray animObjArr = slotAnimObj[animType].AsArray;

			SpineData.AnimationSlotData spineAnimSlotData = new SpineData.AnimationSlotData();
			spineAnimSlotData.name = slotName;
			spineAnimSlotData.timelines = new SpineData.SlotTimeline[animObjArr.Count];
			for(int i=0;i<animObjArr.Count;++i){
				Bones2D.JSONClass animObj = animObjArr[i].AsObject;
				SpineData.SlotTimeline timeline = new SpineData.SlotTimeline();
				timeline.type = animType;
				spineAnimSlotData.timelines[i] = timeline;

				if(animObj.ContainKey("time")) timeline.time = animObj["time"].AsFloat;
				if(animObj.ContainKey("name")) {
					timeline.attachmentName = animObj["name"].ToString();//attachment name
					if(timeline.attachmentName=="null") timeline.attachmentName = null;
				}
				if(animObj.ContainKey("color")) timeline.color = animObj["color"].ToString();//Keyframes for changing a slot's color.
				if(animObj.ContainKey("curve")){
					if(animObj["curve"]=="stepped"){
						timeline.tweenEasing = "stepped";
					}
					else if(animObj["curve"]=="linear"){
						//default
					}
					else{
						timeline.curve = ConvertJsonArrayToFloatArr(animObj["curve"].AsArray);
					}
				}
			}
			return spineAnimSlotData;
		}



		private static List<SpineData.AnimationBoneData> ParseAnimBones(SpineArmatureEditor armatureEditor, Bones2D.JSONClass bonesObj){
			List<SpineData.AnimationBoneData>  animationBoneDatas = new List<SpineData.AnimationBoneData>();
			foreach(string boneName in bonesObj.GetKeys()){
				Bones2D.JSONClass bonesAnimObj = bonesObj[boneName].AsObject;
				if(bonesAnimObj.ContainKey("rotate")){
					animationBoneDatas.Add(ParseBoneAnimTimeline(armatureEditor,boneName,bonesAnimObj,"rotate"));
				}
				if(bonesAnimObj.ContainKey("translate")){
					animationBoneDatas.Add(ParseBoneAnimTimeline(armatureEditor,boneName,bonesAnimObj,"translate"));
				}
				if(bonesAnimObj.ContainKey("scale")){
					animationBoneDatas.Add(ParseBoneAnimTimeline(armatureEditor,boneName,bonesAnimObj,"scale"));
				}
				if(bonesAnimObj.ContainKey("shear")){
					animationBoneDatas.Add(ParseBoneAnimTimeline(armatureEditor,boneName,bonesAnimObj,"shear"));
				}
			}
			return animationBoneDatas;
		}

		private static SpineData.AnimationBoneData ParseBoneAnimTimeline(SpineArmatureEditor armatureEditor, string boneName, Bones2D.JSONClass bonesAnimObj,string animType){
			Bones2D.JSONArray animObjArr = bonesAnimObj[animType].AsArray;

			SpineData.AnimationBoneData spineAnimBoneData = new SpineData.AnimationBoneData();
	
			spineAnimBoneData.name = boneName;
			spineAnimBoneData.timelines = new SpineData.BoneTimeline[animObjArr.Count];

			for(int i=0;i<animObjArr.Count;++i){
				Bones2D.JSONClass animObj = animObjArr[i].AsObject;
				SpineData.BoneTimeline timeline = new SpineData.BoneTimeline();
				timeline.type = animType;
				spineAnimBoneData.timelines[i] = timeline;

				if(animObj.ContainKey("time")) timeline.time = animObj["time"].AsFloat;
				//The bone's rotation relative to the setup pose
				if(animObj.ContainKey("angle")) timeline.angle = animObj["angle"].AsFloat;
				//The bone's x,y relative to the setup pose
				if(animObj.ContainKey("x")) {
					timeline.x = animObj["x"].AsFloat;
					if(animType=="translate"){
						timeline.x *= armatureEditor.unit;
					}
				}
				if(animObj.ContainKey("y")) {
					timeline.y = animObj["y"].AsFloat;
					if(animType=="translate"){
						timeline.y *= armatureEditor.unit;
					}
				}
				if(animObj.ContainKey("curve")){
					if(animObj["curve"]=="stepped"){
						timeline.tweenEasing = "stepped";
					}
					else if(animObj["curve"]=="linear"){
						//default
					}
					else{
						timeline.curve = ConvertJsonArrayToFloatArr(animObj["curve"].AsArray);
					}
				}
			}
			return spineAnimBoneData;
		}

		private static float[] ConvertJsonArrayToFloatArr(Bones2D.JSONArray jsonArray){
			if(jsonArray !=null && jsonArray.Count>0){
				float[] arr = new float[jsonArray.Count];
				for(int j=0;j<jsonArray.Count;++j){
					arr[j] = jsonArray[j].AsFloat;
				}
				return arr;
			}
			return null;
		}
	}

}