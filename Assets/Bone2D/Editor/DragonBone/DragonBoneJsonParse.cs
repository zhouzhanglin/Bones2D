using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Bones2D
{
	/// <summary>
	/// parse animation file
	/// author:  bingheliefeng
	/// </summary>
	public class DragonBoneJsonParse {

		public static void ParseTextureAtlas(DragonBoneArmatureEditor armatureEditor , Texture2D texture, TextAsset atlasText )
		{
			Bones2D.JSONClass obj = Bones2D.JSON.Parse(atlasText.text).AsObject;
			Bones2D.JSONArray arr = obj["SubTexture"].AsArray;
			for(int i=0;i<arr.Count;++i){
				Bones2D.JSONClass frameObj = arr[i].AsObject;
				string textureName = frameObj["name"].ToString();
				textureName = textureName.Replace('/','_');

				DragonBoneArmatureEditor.Atlas atlas =new DragonBoneArmatureEditor.Atlas();
				atlas.texture = texture;
				atlas.atlasText = atlasText;
				armatureEditor.atlasKV[textureName] = atlas;
			}
		}


		public static void ParseAnimJsonData(DragonBoneArmatureEditor armatureEditor)
		{
			string str = armatureEditor.animTextAsset.text.Replace("null","\"null\"");
			Bones2D.JSONClass json=Bones2D.JSON.Parse(str.Replace("/","_")).AsObject;
			Bones2D.JSONArray armtureArr = json["armature"].AsArray;

			//all anim clip
			for(int i=0;i<armtureArr.Count;++i){
				Bones2D.JSONClass armtureObj = armtureArr[i].AsObject;
				string armatureName = armtureObj["name"].ToString().Trim();
				armatureEditor.armatureAnimList[armatureName] = new List<string>();

				if(armtureObj.ContainKey("animation")){
					Bones2D.JSONArray anims = armtureObj["animation"].AsArray;
					for(int j=0;j<anims.Count;++j){
						Bones2D.JSONClass animObj = anims[j].AsObject;
						if(animObj.ContainKey("name"))  {
							string animClipName = animObj["name"].ToString().Trim();
							if(armatureEditor.armatureAnimList.ContainsKey(armatureName)){
								armatureEditor.armatureAnimList[armatureName].Add(animClipName);//add animation name
							}
						}
					}
				}
			}


			List<Armature> armatures = new List<Armature>();
			for(int i=0;i<armtureArr.Count;++i)
			{
				armatureEditor.armatureData = new DragonBoneData.ArmatureData();
				GameObject go = new GameObject("DragonBone");
				Armature armature = go.AddComponent<Armature>();
				armature.isUGUI = armatureEditor.isUGUI;
				armatures.Add(armature);
				armatureEditor.armature = go.transform;
				armatureEditor.bonesKV.Clear();
				armatureEditor.slotsKV.Clear();
				armatureEditor.m_slots.Clear();
				armatureEditor.bonesDataKV.Clear();
				armatureEditor.slotsDataKV.Clear();
				armatureEditor.m_bones.Clear();
				armatureEditor.ffdKV.Clear();
				armatureEditor.bonePoseKV.Clear();
				armatureEditor.m_sonArmature.Clear();

				Bones2D.JSONClass armtureObj = armtureArr[i].AsObject;
				if(armtureObj.ContainKey("name")){
					string armatureName = armtureObj["name"].ToString().Trim();
					armatureEditor.armature.name = armatureName;
				}
				if(armtureObj.ContainKey("type")){
					armatureEditor.armatureData.type = armtureObj["type"].ToString();
				}
				if(armtureObj.ContainKey("defaultActions")){
					armatureEditor.armatureData.actions = ParseActionData(armtureObj,"defaultActions");
					//set default anim
					foreach(DragonBoneData.ActionData ad in armatureEditor.armatureData.actions){
						if(ad.key.Equals("gotoAndPlay")){
							if(armatureEditor.armatureAnimList.ContainsKey(armature.name)){
								List<string> anims = armatureEditor.armatureAnimList[armature.name];
								for(int j=0;j<anims.Count;++j){
									if(anims[j].Equals(ad.action)){
										armature.animIndex = j;
										break;
									}
								}
							}
							break;
						}
					}
				}
				if(armtureObj.ContainKey("frameRate")){
					armatureEditor.armatureData.frameRate = armtureObj["frameRate"].AsFloat;
					if(armatureEditor.armatureData.frameRate==0) armatureEditor.armatureData.frameRate = 24;//db默认为24
				}
				ParseArmtureData(armatureEditor,armtureObj);
				armatureEditor.InitShow();
			}

			if(armatureEditor.m_haveSonArmature ){
				List<Armature> copyAllArmatures = new List<Armature>();
				foreach(Armature armature in armatures){
					Armature newArmature = (Armature)GameObject.Instantiate(armature);
					newArmature.name = "_"+armature.name;
					copyAllArmatures.Add(newArmature);
				}
				foreach(Armature armature in armatures){
					if(armature.sonArmatures !=null && armature.sonArmatures.Length>0){
						SetSonArmature( armature, armature.sonArmatures,armatures,copyAllArmatures);
						if(armatureEditor.genPrefab){
							PrefabUtility.ReplacePrefab( armature.gameObject, PrefabUtility.GetPrefabParent( armature.gameObject ), ReplacePrefabOptions.ConnectToPrefab );
						}
					}
				}
				foreach(Armature armature in copyAllArmatures){
					GameObject.DestroyImmediate(armature.gameObject);
				}
			}
		}

		private static void SetSonArmature(Armature parentArmature, Armature[] sonArmatures,List<Armature> allArmatures,List<Armature> copyAllArmatures){
			for(int i=0;i<sonArmatures.Length;++i){
				Armature son = sonArmatures[i];
				Armature prefab = null;
				foreach(Armature temp in copyAllArmatures){
					if(temp.name.Equals("_"+son.name)){
						prefab = temp;
						break;
					}
				}
				if(prefab){
					Armature newArmature = (Armature) GameObject.Instantiate(prefab);
					sonArmatures[i] = newArmature;
					newArmature.parentArmature = parentArmature;
					newArmature.transform.parent = son.transform.parent;
					newArmature.name = son.name;
					newArmature.transform.localScale = son.transform.localScale;
					newArmature.transform.localPosition = son.transform.localPosition;
					newArmature.transform.localRotation = son.transform.localRotation;
					newArmature.animIndex = son.animIndex;
					newArmature.color = son.color;
					newArmature.gameObject.SetActive(son.gameObject.activeSelf);
					newArmature.transform.SetSiblingIndex(son.transform.GetSiblingIndex());
					newArmature.zSpace = parentArmature.zSpace/(newArmature.slots.Length+1);
					newArmature.ResetSlotZOrder();
					GameObject.DestroyImmediate(son.gameObject);
					if(newArmature.sonArmatures!=null && newArmature.sonArmatures.Length>0){
						SetSonArmature(newArmature,newArmature.sonArmatures,allArmatures,copyAllArmatures);
					}
				}
			}
		}

		private static void ParseArmtureData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			//parse bone data
			ParseArmatureBoneData(armatureEditor, armtureObj);
			//parse slot data
			ParseArmatureSlotData(armatureEditor, armtureObj);
			//parse IK data
			ParseArmatureIKData(armatureEditor, armtureObj);
			//parse animation file
			ParseArmatureAnimationData(armatureEditor, armtureObj);
			//parse skin data
			ParseArmatureSkinData(armatureEditor, armtureObj);
		}

		private static void ParseArmatureSkinData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("skin")){
				Bones2D.JSONArray skins = armtureObj["skin"].AsArray;
				DragonBoneData.SkinData[] skinDatas = new DragonBoneData.SkinData[skins.Count];
				for(int i=0;i<skins.Count;++i){
					DragonBoneData.SkinData skinData = new DragonBoneData.SkinData();
					skinDatas[i] = skinData;
					Bones2D.JSONClass skinObj = skins[i].AsObject;
					string skinName = skinObj["name"].ToString();
					skinData.skinName = skinName;
					if(skinObj.ContainKey("slot"))
					{
						Bones2D.JSONArray slots = skinObj["slot"].AsArray;
						skinData.slots = new DragonBoneData.SkinSlotData[slots.Count];
						for(int j=0;j<slots.Count;++j){
							DragonBoneData.SkinSlotData skinSlotData = new DragonBoneData.SkinSlotData();
							Bones2D.JSONClass slot = slots[j].AsObject;
							skinData.slots[j] = skinSlotData;
							if(slot.ContainKey("name")){
								skinSlotData.slotName = slot["name"].ToString();
							}
							skinSlotData.actions = ParseActionData(slot);
							if(slot.ContainKey("display")){
								Bones2D.JSONArray display = slot["display"].AsArray;
								skinSlotData.displays = new DragonBoneData.SkinSlotDisplayData[display.Count];
								for(int k = 0 ;k<display.Count;++k){
									DragonBoneData.SkinSlotDisplayData displayData= new DragonBoneData.SkinSlotDisplayData();
									skinSlotData.displays[k] = displayData;
									Bones2D.JSONClass displayObj = display[k].AsObject;
									if(displayObj.ContainKey("name")) displayData.textureName = displayObj["name"].ToString();
									if(displayObj.ContainKey("path")) displayData.texturePath = displayObj["path"].ToString();
									else displayData.texturePath = displayData.textureName;
									if(displayObj.ContainKey("type")) displayData.type = displayObj["type"].ToString();
									if(displayObj.ContainKey("subType")) displayData.subType = displayObj["subType"].ToString();
									if(displayObj.ContainKey("pivot")) {
										displayData.pivot = new Vector2(displayObj["pivot"].AsObject["x"].AsFloat,displayObj["pivot"].AsObject["y"].AsFloat);
									}
									if(displayObj.ContainKey("transform")){
										Bones2D.JSONClass transformObj = displayObj["transform"].AsObject;
										DragonBoneData.TransformData transData = new DragonBoneData.TransformData();
										if(transformObj.ContainKey("x")) transData.x = transformObj["x"].AsFloat*armatureEditor.unit;
										if(transformObj.ContainKey("y")) transData.y = -transformObj["y"].AsFloat*armatureEditor.unit;
                                        if(transformObj.ContainKey("skY")) transData.rotate = -transformObj["skY"].AsFloat;
                                        if(transformObj.ContainKey("skX")) transData.skew = (transformObj["skX"].AsFloat+transData.rotate)*Mathf.Deg2Rad;
										if(transformObj.ContainKey("scX")) transData.scx = transformObj["scX"].AsFloat;
										if(transformObj.ContainKey("scY")) transData.scy = transformObj["scY"].AsFloat;
										displayData.transform = transData;
									}
									//uv
									if(displayObj.ContainKey("uvs")){
										Bones2D.JSONArray uvsObj = displayObj["uvs"].AsArray;
										int index = 0;
										displayData.uvs=new Vector2[uvsObj.Count/2];
										for(int z =0;z<uvsObj.Count;z+=2){
											Vector2 uv = new Vector2(uvsObj[z].AsFloat,1-uvsObj[z+1].AsFloat);
											displayData.uvs[index] = uv;
											++index;
										}
									}

									//weight
									if(displayObj.ContainKey("weights")){
										Bones2D.JSONArray weightsObj = displayObj["weights"].AsArray;
										displayData.weights=new float[weightsObj.Count];
										for(int z =0;z<weightsObj.Count;++z){
											displayData.weights[z] = weightsObj[z].AsFloat;
										}
									}
									//bonepose
									if(displayObj.ContainKey("bonePose")){
										Bones2D.JSONArray bonePoseObj = displayObj["bonePose"].AsArray;
										displayData.bonePose = new float[bonePoseObj.Count];
										for(int z=0;z<bonePoseObj.Count;z+=7){
											displayData.bonePose[z] = bonePoseObj[z].AsFloat;
											displayData.bonePose[z+1] = bonePoseObj[z+1].AsFloat;//a
											displayData.bonePose[z+2] = bonePoseObj[z+2].AsFloat;//b
											displayData.bonePose[z+3] = bonePoseObj[z+3].AsFloat;//c
											displayData.bonePose[z+4] = bonePoseObj[z+4].AsFloat;//d
											displayData.bonePose[z+5] = bonePoseObj[z+5].AsFloat;//tx
											displayData.bonePose[z+6] = bonePoseObj[z+6].AsFloat;//ty

											BoneMatrix2D m = new BoneMatrix2D(displayData.bonePose[z+1],displayData.bonePose[z+2],
												displayData.bonePose[z+3],displayData.bonePose[z+4],displayData.bonePose[z+5],displayData.bonePose[z+6]);
											armatureEditor.bonePoseKV[ displayData.textureName + armatureEditor.armatureData.boneDatas[ (int)displayData.bonePose[z]].name ] = m;

										}
									}

									BoneMatrix2D slotPoseMat = null;
									//slotpose
									if(displayObj.ContainKey("slotPose")){
										Bones2D.JSONArray slotPoseObj = displayObj["slotPose"].AsArray;
										slotPoseMat = new BoneMatrix2D(slotPoseObj[0].AsFloat,slotPoseObj[1].AsFloat,slotPoseObj[2].AsFloat,
											slotPoseObj[3].AsFloat,slotPoseObj[4].AsFloat,slotPoseObj[5].AsFloat);
									}

									//vertex
									if(displayObj.ContainKey("vertices")){
										Bones2D.JSONArray verticesObj = displayObj["vertices"].AsArray;
										displayData.vertices=new Vector3[verticesObj.Count/2];

										for(int z =0;z<verticesObj.Count;z+=2){
											int vertexIndex = z / 2;
											Vector3 vertex = new Vector3(verticesObj[z].AsFloat,verticesObj[z+1].AsFloat,0f);
											if(slotPoseMat!=null){
												//slotPose转换
												vertex = (Vector3)slotPoseMat.TransformPoint(vertex.x,vertex.y);
											}
											vertex.x*=armatureEditor.unit;
											vertex.y*=-armatureEditor.unit;
											displayData.vertices[vertexIndex] = vertex;
										}
									}
									//triangles
									if(displayObj.ContainKey("triangles")){
										Bones2D.JSONArray trianglesObj = displayObj["triangles"].AsArray;
										displayData.triangles=new int[trianglesObj.Count];
										for(int z =0;z<trianglesObj.Count;z++){
											displayData.triangles[z] = trianglesObj[z].AsInt;
										}
										//dragonBone和unity的z相反
										for(int z =0;z<displayData.triangles.Length;z+=3){
											int f1 = displayData.triangles[z];
											int f3 = displayData.triangles[z+2];
											displayData.triangles[z] = f3;
											displayData.triangles[z+2] = f1;
										}
										ArmatureEditor.ModifyTriangles(displayData.triangles,displayData.vertices);
									}
									//edges 
									if(armatureEditor.genMeshCollider && displayObj.ContainKey("edges")){
										Bones2D.JSONArray edgesObj = displayObj["edges"].AsArray;
										int len=edgesObj.Count;
										List<int> edges = new List<int>();
										for(int z =0;z<len;++z){
											int value = edgesObj[z].AsInt;
											if(edges.Count>0){
												if(edges[edges.Count-1]!=value)	edges.Add(value);
											}else{
												edges.Add(value);
											}
										}
										if(edges.Count>2 && edges[0]==edges[edges.Count-1]) edges.RemoveAt(edges.Count-1); 
										if(edges.Count>2){
											displayData.edges = edges.ToArray();
										}
									}
									//userdeges
								}
							}
						}
					}
				}
				armatureEditor.armatureData.skinDatas = skinDatas;
			}
		}

		private static void ParseArmatureAnimationData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("animation")){
				Bones2D.JSONArray anims = armtureObj["animation"].AsArray;
				DragonBoneData.AnimationData[] animDatas = new DragonBoneData.AnimationData[anims.Count];
				for(int i=0;i<anims.Count;++i){
					Bones2D.JSONClass animObj = anims[i].AsObject;
					DragonBoneData.AnimationData animData=new DragonBoneData.AnimationData();
					if(animObj.ContainKey("name"))	animData.name = animObj["name"].ToString().Trim();
					if(animObj.ContainKey("playTimes"))  animData.playTimes = animObj["playTimes"].AsInt;
					if(animObj.ContainKey("duration"))  animData.duration = animObj["duration"].AsInt;
					if(animData.duration==0) animData.duration =1;
					if(animObj.ContainKey("frame")) {
						ParseAnimFrames(animObj["frame"].AsArray,animData);
					}
					if(animObj.ContainKey("bone")){
						Bones2D.JSONArray bones = animObj["bone"].AsArray;
						animData.boneDatas = new DragonBoneData.AnimSubData[bones.Count];
						ParsetAnimBoneSlot(armatureEditor, bones , animData.boneDatas );
					}
					if(animObj.ContainKey("slot")){
						Bones2D.JSONArray slots = animObj["slot"].AsArray;
						animData.slotDatas = new DragonBoneData.AnimSubData[slots.Count];
						ParsetAnimBoneSlot(armatureEditor, slots , animData.slotDatas );
					}
					//ffd
					if(animObj.ContainKey("ffd")){
						Bones2D.JSONArray ffds = animObj["ffd"].AsArray;
						animData.ffdDatas = new DragonBoneData.AnimSubData[ffds.Count];
						ParsetAnimBoneSlot(armatureEditor, ffds , animData.ffdDatas );
					}
					//zOrder
					if(animObj.ContainKey("zOrder")){
						Bones2D.JSONClass zOrders = animObj["zOrder"].AsObject;
						ParseAnimSortOrder(armatureEditor, zOrders , animData );
					}
					animDatas[i] = animData;
				}
				armatureEditor.armatureData.animDatas = animDatas;
			}
		}

		private static void ParseArmatureIKData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("ik"))
			{
				Bones2D.JSONArray iks = armtureObj["ik"].AsArray;
				DragonBoneData.IKData[] ikDatas = new DragonBoneData.IKData[iks.Count];
				for(int i=0;i<iks.Count;++i)
				{
					Bones2D.JSONClass ikObj = iks[i].AsObject;
					DragonBoneData.IKData ikData = new DragonBoneData.IKData();
					if(ikObj.ContainKey("name")) ikData.name = ikObj["name"].ToString();
					if(ikObj.ContainKey("bone")) ikData.bone = ikObj["bone"].ToString();
					if(ikObj.ContainKey("target")) ikData.target = ikObj["target"].ToString();
					if(ikObj.ContainKey("bendPositive")) ikData.bendPositive = ikObj["bendPositive"].AsBool;
					if(ikObj.ContainKey("chain")) ikData.chain = ikObj["chain"].AsInt;
					if(ikObj.ContainKey("weight")) ikData.weight = ikObj["weight"].AsFloat;
					ikDatas[i] = ikData;
				}
				armatureEditor.armatureData.ikDatas = ikDatas;
			}
		}

		private static void ParseArmatureSlotData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("slot")){
				Bones2D.JSONArray slots = armtureObj["slot"].AsArray;
				DragonBoneData.SlotData[] slotDatas = new DragonBoneData.SlotData[slots.Count];
				bool isMC = armatureEditor.armatureData.type.Equals("MovieClip");
				for(int i=0;i<slots.Count;++i){
					Bones2D.JSONClass slotObj = slots[i].AsObject;
					DragonBoneData.SlotData slotData=new DragonBoneData.SlotData();
					slotDatas[i] = slotData;
					if(slotObj.ContainKey("name"))  slotData.name = slotObj["name"].ToString();
					if(slotObj.ContainKey("parent"))  slotData.parent = slotObj["parent"].ToString();
					if(slotObj.ContainKey("z"))  slotData.z = -slotObj["z"].AsFloat*armatureEditor.zoffset;
					if(!isMC){
						if(slotObj.ContainKey("displayIndex")) slotData.displayIndex = slotObj["displayIndex"].AsInt;
					}
					if(slotObj.ContainKey("scale")) slotData.scale = slotObj["scale"].AsFloat;
					if(slotObj.ContainKey("blendMode")) slotData.blendMode = slotObj["blendMode"].ToString();
					if(slotObj.ContainKey("color"))
					{
						Bones2D.JSONClass colorObj = slotObj["color"].AsObject;
						DragonBoneData.ColorData colorData = new DragonBoneData.ColorData();
						if(colorObj.ContainKey("aM")) {
							colorData.aM = colorObj["aM"].AsFloat*0.01f;
						}
						if(colorObj.ContainKey("a0")){
							colorData.aM+=colorObj["a0"].AsFloat/255f;
						}
						if(colorObj.ContainKey("rM")) {
							colorData.rM = colorObj["rM"].AsFloat*0.01f;
						}
						if(colorObj.ContainKey("r0")){
							colorData.rM+=colorObj["r0"].AsFloat/255f;
						}
						if(colorObj.ContainKey("gM")) {
							colorData.gM = colorObj["gM"].AsFloat*0.01f;
						}
						if(colorObj.ContainKey("g0")){
							colorData.gM+=colorObj["g0"].AsFloat/255f;
						}
						if(colorObj.ContainKey("bM")) {
							colorData.bM = colorObj["bM"].AsFloat*0.01f;
						}
						if(colorObj.ContainKey("b0")){
							colorData.bM+=colorObj["b0"].AsFloat/255f;
						}
						slotData.color = colorData;
					}
					slotData.actions = ParseActionData(slotObj);
					armatureEditor.slotsDataKV[slotData.name]=slotData;
				}
				armatureEditor.armatureData.slotDatas = slotDatas;
			}

		}

		private static DragonBoneData.ActionData[] ParseActionData(Bones2D.JSONClass data , string name="actions"){
			if(data.ContainKey(name)){
				Bones2D.JSONArray actionsArray = data[name].AsArray;
				if(actionsArray.Count>0){
					DragonBoneData.ActionData[] actions = new DragonBoneData.ActionData[actionsArray.Count];
					for(int i=0;i<actionsArray.Count;++i){
						Bones2D.JSONClass actionObj = actionsArray[i].AsObject;
						DragonBoneData.ActionData actionData = new DragonBoneData.ActionData();
						if(actionObj.ContainKey("gotoAndPlay")){
							actionData.key = "gotoAndPlay";
							actionData.action = actionObj["gotoAndPlay"].ToString();
						}
						actions[i] = actionData;
					}
					return actions;
				}
			}
			return null;
		}

		private static void ParseArmatureBoneData(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass armtureObj ){
			if(armtureObj.ContainKey("bone")){
				Bones2D.JSONArray bones = armtureObj["bone"].AsArray;
				DragonBoneData.BoneData[] boneDatas = new DragonBoneData.BoneData[bones.Count];
				for(int i=0;i<bones.Count;++i){
					Bones2D.JSONClass boneObj = bones[i].AsObject;
					DragonBoneData.BoneData boneData = new DragonBoneData.BoneData();
					if(boneObj.ContainKey("length"))  boneData.length = boneObj["length"].AsFloat;
					if(boneObj.ContainKey("name"))  boneData.name = boneObj["name"].ToString();
					if(boneObj.ContainKey("parent"))  boneData.parent = boneObj["parent"].ToString();
					if(boneObj.ContainKey("inheritRotation")) boneData.inheritRotation = boneObj["inheritRotation"].AsInt==1?true:false;
					if(boneObj.ContainKey("inheritScale")) boneData.inheritScale = boneObj["inheritScale"].AsInt==1?true:false;
					if(boneObj.ContainKey("transform")){
						Bones2D.JSONClass transformObj = boneObj["transform"].AsObject;
						DragonBoneData.TransformData transData = new DragonBoneData.TransformData();
						if(transformObj.ContainKey("x")) transData.x = transformObj["x"].AsFloat*armatureEditor.unit;
						if(transformObj.ContainKey("y")) transData.y = -transformObj["y"].AsFloat*armatureEditor.unit;
                        if(transformObj.ContainKey("skY")) transData.rotate = -transformObj["skY"].AsFloat;
						if(transformObj.ContainKey("scX")) transData.scx = transformObj["scX"].AsFloat;
						if(transformObj.ContainKey("scY")) transData.scy = transformObj["scY"].AsFloat;
						boneData.transform = transData;
					}
					else
					{
						boneData.transform = new DragonBoneData.TransformData();
					}
					boneDatas[i] = boneData;
					armatureEditor.bonesDataKV[boneData.name]=boneData;
				}
				armatureEditor.armatureData.boneDatas = boneDatas;
			}
		}

		private static void ParseAnimFrames( Bones2D.JSONArray animFrames ,DragonBoneData.AnimationData animData){
			animData.keyDatas = new DragonBoneData.AnimKeyData[animFrames.Count];
			for(int i=0;i<animFrames.Count;++i){
				Bones2D.JSONClass frameObj = animFrames[i].AsObject;
				DragonBoneData.AnimKeyData keyData = new DragonBoneData.AnimKeyData();
				if(frameObj.ContainKey("event")) keyData.eventName = frameObj["event"].ToString();
				if(frameObj.ContainKey("sound")) keyData.soundName = frameObj["sound"].ToString();
				if(frameObj.ContainKey("duration")) keyData.duration = frameObj["duration"].AsInt;
				if(keyData.duration==0) keyData.duration=1;
				if(frameObj.ContainKey("action")) keyData.actionName = frameObj["action"].ToString();
				animData.keyDatas[i] = keyData;
			}
		}

		private static void ParseAnimSortOrder(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONClass zOrders  ,DragonBoneData.AnimationData animData){
			if(zOrders.ContainKey("frame")){
				//just only one
				DragonBoneData.AnimSubData subData = new DragonBoneData.AnimSubData();
				animData.zOrderDatas = new DragonBoneData.AnimSubData[1]{subData};
				if(zOrders.ContainKey("offset")) subData.offset = zOrders["offset"].AsFloat;

				Bones2D.JSONArray frames = zOrders["frame"].AsArray;
				subData.frameDatas = new DragonBoneData.AnimFrameData[frames.Count];
				for(int i=0;i<frames.Count;++i){
					Bones2D.JSONClass frameObj = frames[i].AsObject;
					DragonBoneData.AnimFrameData frameData = new DragonBoneData.AnimFrameData();
					subData.frameDatas[i] = frameData;

					if(frameObj.ContainKey("duration")) frameData.duration = frameObj["duration"].AsInt;
					if(frameObj.ContainKey("zOrder")){
						Bones2D.JSONArray zs = frameObj["zOrder"].AsArray;
						if(zs!=null){
							frameData.zOrder = new int[zs.Count];
							for(int z=0;z<zs.Count;++z){
								frameData.zOrder[z] = zs[z].AsInt;
							}
							//the last offset is 0
							if(zs.Count>1 && frameData.zOrder[frameData.zOrder.Length-1]==0){
								int lastSlotIdx = frameData.zOrder[frameData.zOrder.Length-2];
								frameData.zOrder[frameData.zOrder.Length-2] = lastSlotIdx + 1;
								frameData.zOrder[frameData.zOrder.Length-1] = -frameData.zOrder.Length/2;
							}
						}
					}
				}
			}
		}

		private static void ParseFrames(DragonBoneArmatureEditor armatureEditor,DragonBoneData.AnimFrameData[] frameDatas, Bones2D.JSONArray frames,string subDataName,DragonBoneData.FrameType type)
		{
			for(int j=0;j<frames.Count;++j){
				Bones2D.JSONClass frameObj = frames[j].AsObject;
				DragonBoneData.AnimFrameData frameData=new DragonBoneData.AnimFrameData();
				frameData.frameType = type;
				if(frameObj.ContainKey("duration")) frameData.duration = frameObj["duration"].AsInt;
				if(frameData.duration==0) frameData.duration=1;
				if(frameObj.ContainKey("displayIndex")) frameData.displayIndex = frameObj["displayIndex"].AsInt;
				if (type == DragonBoneData.FrameType.DisplayFrame) {
					if (frameObj.ContainKey ("value")) {
						frameData.displayIndex = frameObj ["value"].AsInt;
					}
				}
				if(frameObj.ContainKey("z")) frameData.z = -frameObj["z"].AsInt*armatureEditor.zoffset;
				if(frameObj.ContainKey("tweenEasing") && frameObj["tweenEasing"].ToString()!="null") frameData.tweenEasing = frameObj["tweenEasing"].AsFloat;
				if(frameObj.ContainKey("tweenRotate")) frameData.tweenRotate = frameObj["tweenRotate"].AsInt;
				if(frameObj.ContainKey("curve")){
					Bones2D.JSONArray curves = frameObj["curve"].AsArray;
					if(curves.Count>3){
						frameData.curve = new float[4]{
							curves[0].AsFloat,
							curves[1].AsFloat,
							curves[curves.Count-2].AsFloat,
							curves[curves.Count-1].AsFloat
						};
					}
				}
				frameData.actions = ParseActionData(frameObj);
				if(frameObj.ContainKey("transform")){
					Bones2D.JSONClass transformObj = frameObj["transform"].AsObject;
					DragonBoneData.TransformData transData = new DragonBoneData.TransformData();
					if(transformObj.ContainKey("x")) {
						transData.x = transformObj["x"].AsFloat*armatureEditor.unit;
					}
					if(transformObj.ContainKey("y")) {
						transData.y = -transformObj["y"].AsFloat*armatureEditor.unit;
					}
					if(transformObj.ContainKey("skY")) {
						transData.rotate = -transformObj["skY"].AsFloat;
					}
					if(transformObj.ContainKey("scX")) {
						transData.scx = transformObj["scX"].AsFloat;
					}
					if(transformObj.ContainKey("scY")){
						transData.scy = transformObj["scY"].AsFloat;
					}
					frameData.transformData = transData;
				}
				else
				{
					if(frameObj.ContainKey("x")){
						if(frameData.transformData==null)
							frameData.transformData = new DragonBoneData.TransformData();
						if(type==DragonBoneData.FrameType.TranslateFrame){
							frameData.transformData.x = frameObj["x"].AsFloat*armatureEditor.unit;
						}else{
							//scx
							frameData.transformData.scx = frameObj["x"].AsFloat;
						}
					}
					if(frameObj.ContainKey("y")){
						if(frameData.transformData==null)
							frameData.transformData = new DragonBoneData.TransformData();

						if(type==DragonBoneData.FrameType.TranslateFrame){
							frameData.transformData.y = -frameObj["y"].AsFloat*armatureEditor.unit;
						}else{
							//scy
							frameData.transformData.scy = frameObj["y"].AsFloat;
						}
					}
					if(frameObj.ContainKey("rotate")){
						if(frameData.transformData==null)
							frameData.transformData = new DragonBoneData.TransformData();
						frameData.transformData.rotate = -frameObj["rotate"].AsFloat;
					}
				}
				if(frameObj.ContainKey("color"))
				{
					Bones2D.JSONClass colorObj = frameObj["color"].AsObject;
					DragonBoneData.ColorData colorData = new DragonBoneData.ColorData();
					if(colorObj.ContainKey("aM")) {
						colorData.aM = colorObj["aM"].AsFloat*0.01f;
					}
					if(colorObj.ContainKey("a0")){
						colorData.aM+=colorObj["a0"].AsFloat/255f;
					}
					if(colorObj.ContainKey("rM")) {
						colorData.rM = colorObj["rM"].AsFloat*0.01f;
					}
					if(colorObj.ContainKey("r0")){
						colorData.rM+=colorObj["r0"].AsFloat/255f;
					}
					if(colorObj.ContainKey("gM")) {
						colorData.gM = colorObj["gM"].AsFloat*0.01f;
					}
					if(colorObj.ContainKey("g0")){
						colorData.gM+=colorObj["g0"].AsFloat/255f;
					}
					if(colorObj.ContainKey("bM")) {
						colorData.bM = colorObj["bM"].AsFloat*0.01f;
					}
					if(colorObj.ContainKey("b0")){
						colorData.bM+=colorObj["b0"].AsFloat/255f;
					}
					frameData.color = colorData;
				}
				else if (type == DragonBoneData.FrameType.ColorFrame && frameObj.ContainKey("value"))
                {
                    Bones2D.JSONClass colorObj = frameObj["value"].AsObject;
                    DragonBoneData.ColorData colorData = new DragonBoneData.ColorData();
                    if (colorObj.ContainKey("aM"))
                    {
                        colorData.aM = colorObj["aM"].AsFloat * 0.01f;
                    }
                    if (colorObj.ContainKey("a0"))
                    {
                        colorData.aM += colorObj["a0"].AsFloat / 255f;
                    }
                    if (colorObj.ContainKey("rM"))
                    {
                        colorData.rM = colorObj["rM"].AsFloat * 0.01f;
                    }
                    if (colorObj.ContainKey("r0"))
                    {
                        colorData.rM += colorObj["r0"].AsFloat / 255f;
                    }
                    if (colorObj.ContainKey("gM"))
                    {
                        colorData.gM = colorObj["gM"].AsFloat * 0.01f;
                    }
                    if (colorObj.ContainKey("g0"))
                    {
                        colorData.gM += colorObj["g0"].AsFloat / 255f;
                    }
                    if (colorObj.ContainKey("bM"))
                    {
                        colorData.bM = colorObj["bM"].AsFloat * 0.01f;
                    }
                    if (colorObj.ContainKey("b0"))
                    {
                        colorData.bM += colorObj["b0"].AsFloat / 255f;
                    }
                    frameData.color = colorData;
                }
				//ffd animation
				//vertex offset
				bool startFromY = false;
				if(frameObj.ContainKey("offset")){
					startFromY = frameObj["offset"].AsInt%2!=0;//从Y开始
					frameData.offset = frameObj["offset"].AsInt/2;
				}
				if(frameObj.ContainKey("vertices")){ //local vertex
					Bones2D.JSONArray verticesObj = frameObj["vertices"].AsArray;
					int index=0;
					int k= 0;
					if(startFromY) {
						frameData.vertices = new Vector2[verticesObj.Count/2+1];
						frameData.vertices[index]=new Vector2(0,-verticesObj[k].AsFloat*armatureEditor.unit);
						k = 1;
						++index;
					}else{
						frameData.vertices = new Vector2[verticesObj.Count/2];
					}
					for(;k<verticesObj.Count && k+1<verticesObj.Count;k+=2)
					{
						frameData.vertices[index]=new Vector2(verticesObj[k].AsFloat*armatureEditor.unit,-verticesObj[k+1].AsFloat*armatureEditor.unit);
						++index;
					}
					armatureEditor.ffdKV[subDataName] = true;
				}
				frameDatas[j] = frameData;
			}
		}

		private static void ParsetAnimBoneSlot(DragonBoneArmatureEditor armatureEditor, Bones2D.JSONArray animBonesSlots , DragonBoneData.AnimSubData[] animDatas){
			for(int i=0;i<animBonesSlots.Count;++i){
				Bones2D.JSONClass boneSlotObj = animBonesSlots[i].AsObject;
				DragonBoneData.AnimSubData subData = new DragonBoneData.AnimSubData();
				if(boneSlotObj.ContainKey("name")) subData.name = boneSlotObj["name"].ToString();
				if(boneSlotObj.ContainKey("slot")) subData.slot = boneSlotObj["slot"].ToString();
				if(boneSlotObj.ContainKey("scale")) subData.scale = boneSlotObj["scale"].AsFloat;
				if(boneSlotObj.ContainKey("offset")) subData.offset = boneSlotObj["offset"].AsFloat;
				if(boneSlotObj.ContainKey("frame")){ //for 5.3 以下
					Bones2D.JSONArray frames = boneSlotObj["frame"].AsArray;
					if(frames != null && frames.Count>0) {
						subData.frameDatas = new DragonBoneData.AnimFrameData[frames.Count];
						ParseFrames(armatureEditor,subData.frameDatas,frames,subData.name,DragonBoneData.FrameType.Frame);
					}
				}
				else //for 5.5 及以上
				{
					if(boneSlotObj.ContainKey("translateFrame")){
						Bones2D.JSONArray frames = boneSlotObj["translateFrame"].AsArray;
						if(frames != null && frames.Count>0) {
							subData.translateFrameDatas = new DragonBoneData.AnimFrameData[frames.Count];
							ParseFrames(armatureEditor,subData.translateFrameDatas,frames,subData.name,DragonBoneData.FrameType.TranslateFrame);
						}
					}
					if(boneSlotObj.ContainKey("rotateFrame")){
						Bones2D.JSONArray frames = boneSlotObj["rotateFrame"].AsArray;
						if(frames != null && frames.Count>0) {
							subData.rotateFrameDatas = new DragonBoneData.AnimFrameData[frames.Count];
							ParseFrames(armatureEditor,subData.rotateFrameDatas,frames,subData.name,DragonBoneData.FrameType.RotateFrame);
						}
					}
					if(boneSlotObj.ContainKey("scaleFrame")){
						Bones2D.JSONArray frames = boneSlotObj["scaleFrame"].AsArray;
						if(frames != null && frames.Count>0) {
							subData.scaleFrameDatas = new DragonBoneData.AnimFrameData[frames.Count];
							ParseFrames(armatureEditor,subData.scaleFrameDatas,frames,subData.name,DragonBoneData.FrameType.ScaleFrame);
						}
					}

					if(boneSlotObj.ContainKey("colorFrame")){
						Bones2D.JSONArray frames = boneSlotObj["colorFrame"].AsArray;
						if(frames != null && frames.Count>0) {
							subData.colorFrameDatas = new DragonBoneData.AnimFrameData[frames.Count];
							ParseFrames(armatureEditor,subData.colorFrameDatas,frames,subData.name,DragonBoneData.FrameType.ColorFrame);
						}
					}

					if(boneSlotObj.ContainKey("displayFrame")){
						Bones2D.JSONArray frames = boneSlotObj["displayFrame"].AsArray;
						if(frames != null && frames.Count>0) {
							subData.displayFrameDatas = new DragonBoneData.AnimFrameData[frames.Count];
							ParseFrames(armatureEditor,subData.displayFrameDatas,frames,subData.name,DragonBoneData.FrameType.DisplayFrame);
						}
					}
				}
				animDatas[i] = subData;
			}

		}
	}

}