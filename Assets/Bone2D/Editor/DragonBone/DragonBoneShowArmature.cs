using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

namespace Bones2D
{
	/// <summary>
	/// Show Unity transforms
	/// author:bingheliefeng
	/// </summary>
	public class DragonBoneShowArmature {

		private static Dictionary<Texture,List<SpriteMetaData>> m_metaDatas ;
		private static List<SpriteRenderer> m_sprites ;
		private static List<Image> m_images ;

		private static Transform m_rootBone;
		private static Transform m_rootSlot;
		public static void Dispose(){
			m_rootBone = null;
			m_rootSlot = null;
			m_metaDatas = null;
			m_sprites = null;
			m_images = null;
		}

		public static void AddBones(DragonBoneArmatureEditor armatureEditor){
			m_rootBone = null;
			if(armatureEditor.armatureData.boneDatas!=null)
			{
				armatureEditor.bonesKV.Clear();
				armatureEditor.m_bones.Clear();
				int len = armatureEditor.armatureData.boneDatas.Length;
				for(int i=0;i<len;++i){
					DragonBoneData.BoneData boneData = armatureEditor.armatureData.boneDatas[i];
					GameObject go = new GameObject(boneData.name);
					armatureEditor.bonesKV[boneData.name]=go.transform;
					if(m_rootBone==null) m_rootBone = go.transform;
					armatureEditor.m_bones.Add(go.transform);
				}
			}
		}
		public static void AddSlot(DragonBoneArmatureEditor armatureEditor){
			m_rootSlot = null;
			if(armatureEditor.armatureData.slotDatas!=null){
				armatureEditor.slotsKV.Clear();
				Armature armature = armatureEditor.armature.GetComponent<Armature>();
				int len = armatureEditor.armatureData.slotDatas.Length;
				for(int i=0;i<len;++i){
					DragonBoneData.SlotData slotData = armatureEditor.armatureData.slotDatas[i];
					GameObject go = new GameObject(slotData.name);
					armatureEditor.slotsKV[slotData.name]=go.transform;

					Slot slot = go.AddComponent<Slot>();
					slot.zOrder = i;
					slot.armature = armature;
					slot.blendMode = slot.ConvertBlendMode( slotData.blendMode.ToLower());
					if(slotData.color!=null){
						slot.color = slotData.color.ToColor();
					}
					armatureEditor.m_slots.Add(slot);
				}
			}
		}
		public static void ShowBones(DragonBoneArmatureEditor armatureEditor){
			foreach(Transform b in armatureEditor.bonesKV.Values)
			{
				DragonBoneData.BoneData boneData= armatureEditor.bonesDataKV[b.name];

				if(!string.IsNullOrEmpty(boneData.parent)){
					if(armatureEditor.bonesKV.ContainsKey(boneData.parent)){
						Transform parent = armatureEditor.bonesKV[boneData.parent];
						b.transform.parent = parent.transform;
					}
				}
				else
				{
					b.transform.parent = armatureEditor.armature;
				}
				if(boneData.transform!=null){

					Vector3 localPos = Vector3.zero;
					if(!float.IsNaN(boneData.transform.x)) localPos.x = boneData.transform.x;
					if(!float.IsNaN(boneData.transform.y)) localPos.y = boneData.transform.y;
					b.transform.localPosition = localPos;

					Vector3 localSc = Vector3.one;
					if(!float.IsNaN(boneData.transform.scx)) localSc.x = boneData.transform.scx;
					if(!float.IsNaN(boneData.transform.scy)) localSc.y = boneData.transform.scy;

					b.transform.localScale = localSc;

					if(!float.IsNaN(boneData.transform.rotate))
					{
						b.transform.localRotation = Quaternion.Euler(0,0,boneData.transform.rotate);
					}

				}else{
					b.transform.localScale = Vector3.one;
					b.transform.localPosition = Vector3.zero;
				}

				GameObject inhertGo = null;
				Bone myBone = null;
				if(!boneData.inheritRotation){
					inhertGo = new GameObject("_"+boneData.name);
					inhertGo.transform.parent = armatureEditor.armature;
					inhertGo.transform.localPosition = b.transform.localPosition;
					inhertGo.transform.localRotation = b.transform.localRotation;
					inhertGo.transform.localScale = b.transform.localScale;
					inhertGo.hideFlags = HideFlags.NotEditable;
					myBone = b.gameObject.AddComponent<Bone>();
					myBone.inheritRotation = inhertGo.transform;
				}
				if(!boneData.inheritScale){
					if(inhertGo==null){
						inhertGo = new GameObject("_"+boneData.name);
						inhertGo.transform.parent = armatureEditor.armature;
						inhertGo.transform.localPosition = b.transform.localPosition;
						inhertGo.transform.localRotation = b.transform.localRotation;
						inhertGo.transform.localScale = b.transform.localScale;
						inhertGo.hideFlags = HideFlags.NotEditable;
						myBone = b.gameObject.AddComponent<Bones2D.Bone>();
					}
					myBone.inheritScale = inhertGo.transform;
				}
			}
		}
		public static void ShowSlots(DragonBoneArmatureEditor armatureEditor){
			
			if(armatureEditor.genericAnim || armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIDefault 
				|| armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIImage){
				GameObject rootSlot = new GameObject("slots");
				m_rootSlot = rootSlot.transform;
				m_rootSlot.SetParent(armatureEditor.armature);
				m_rootSlot.localScale = Vector3.one;
				m_rootSlot.localPosition = Vector3.zero;
			}

			foreach(Transform s in armatureEditor.slotsKV.Values)
			{
				DragonBoneData.SlotData slotData = armatureEditor.slotsDataKV[s.name];

				if(!string.IsNullOrEmpty(slotData.parent)){
					if(armatureEditor.bonesKV.ContainsKey(slotData.parent)){
						Transform parent = armatureEditor.bonesKV[slotData.parent];

						if(m_rootSlot){
							s.transform.parent = m_rootSlot;

							GameObject go = new GameObject(s.name);
							go.transform.parent = parent.transform;
							go.transform.localScale = new Vector3(slotData.scale,slotData.scale,1f);
							if(armatureEditor.isUGUI){
								go.transform.localPosition = Vector3.zero;
							}else{
								go.transform.localPosition = new Vector3(0,0,slotData.z);
							}
							go.transform.localEulerAngles = Vector3.zero;

							Slot slot = s.GetComponent<Slot>();
							slot.inheritSlot = go.transform;

						}else{
							s.transform.parent = parent.transform;
						}

					}
				}
				s.transform.localScale = new Vector3(slotData.scale,slotData.scale,1f);
				if(armatureEditor.isUGUI){
					s.transform.localPosition = Vector3.zero;
				}else{
					s.transform.localPosition = new Vector3(0,0,slotData.z);
				}
				s.transform.localEulerAngles = Vector3.zero;
			}
		}
		public static void ShowSkins(DragonBoneArmatureEditor armatureEditor){
			if(armatureEditor.armatureData.skinDatas!=null && armatureEditor.armatureData.skinDatas.Length>0){
				Armature armature= armatureEditor.armature.GetComponent<Armature>();

				if(m_metaDatas==null) m_metaDatas = new Dictionary<Texture, List<SpriteMetaData>>();
				if(m_sprites==null) m_sprites = new List<SpriteRenderer>();
				if(m_images==null) m_images = new List<Image>();

				int len = armatureEditor.armatureData.skinDatas.Length;
				for(int i=0;i<len;++i){
					DragonBoneData.SkinData skinData = armatureEditor.armatureData.skinDatas[i];
					for(int j=0;j<skinData.slots.Length;++j){
						DragonBoneData.SkinSlotData skinSlotData = skinData.slots[j];
						Transform slot = armatureEditor.slotsKV[skinSlotData.slotName];
						DragonBoneData.SlotData slotData = armatureEditor.slotsDataKV[skinSlotData.slotName];
						if(slot && skinSlotData.displays!=null && skinSlotData.displays.Length>0){
							for(int k=0;k<skinSlotData.displays.Length;++k){
								DragonBoneData.SkinSlotDisplayData displayData= skinSlotData.displays[k];
								if(displayData.type=="armature"){
									//子骨架
									armatureEditor.m_haveSonArmature = true;
									ShowSonArmature(skinSlotData.actions, displayData,slot,armatureEditor,slotData);
								}

								if(displayData.type.Equals("boundingBox"))
								{
									if(displayData.subType=="polygon")
									{
										ShowCustomCollider(displayData,slot,armatureEditor,slotData);
									}
									continue;
								}
								if(displayData.type!="image" && displayData.type!="mesh")  continue;

								TextureFrame frame = armatureEditor.m_TextureFrames.GetTextureFrame(displayData.texturePath);
								if(displayData.type =="image"){
									if(armatureEditor.displayType== Bone2DSetupEditor.DisplayType.Default){
										ShowSpriteFrame(frame,displayData,slot,armatureEditor,slotData);
									}
									else if(armatureEditor.displayType == Bone2DSetupEditor.DisplayType.SpriteRender
										|| armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIImage)
									{
										SpriteMetaData metaData = new SpriteMetaData();
										metaData.name = displayData.textureName;
										metaData.rect = frame.rect;
										metaData.rect.y = frame.texture.height-metaData.rect.y-metaData.rect.height;
										if(displayData.pivot.x!=0 || displayData.pivot.y!=0 ){
											metaData.alignment = (int)SpriteAlignment.Custom;
											metaData.pivot = new Vector2((displayData.pivot.x+metaData.rect.width/2)/metaData.rect.width,(displayData.pivot.y+metaData.rect.height/2)/metaData.rect.height);
										}
										if(!m_metaDatas.ContainsKey(frame.texture)){
											m_metaDatas[frame.texture] = new List<SpriteMetaData>();
										}
										if(CheckMetadata(m_metaDatas[frame.texture],metaData)){
											m_metaDatas[frame.texture].Add(metaData);
										}

										if(armatureEditor.displayType == Bone2DSetupEditor.DisplayType.SpriteRender){
											SpriteRenderer sr = ShowUnitySprite(armatureEditor,frame,displayData,slot,metaData,slotData);
											if(armatureEditor.genMeshCollider){
												sr.gameObject.AddComponent<BoxCollider2D>();
											}
											m_sprites.Add(sr);
										}
										else
										{
											Image img = ShowUIImage(armatureEditor,frame,displayData,slot,metaData,slotData);
											if(armatureEditor.genMeshCollider){
												img.gameObject.AddComponent<BoxCollider2D>();
											}
											m_images.Add(img);
										}
									}
									else if(armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIDefault)
									{
										ShowUIFrame(frame,displayData,slot,armatureEditor,slotData);
									}
								}
								else if(displayData.type=="mesh")
								{
									if(armature.isUGUI){
										ShowUIMesh(frame,displayData,slot,armatureEditor,slotData);
									}else{
										ShowSpriteMesh(frame,displayData,slot,armatureEditor,slotData);
									}
								}
							}
							Slot s = slot.GetComponent<Slot>();
							if(s){
								s.displayIndex = slotData.displayIndex;
							}
						}
					}
				}

			}
		}

		static bool CheckMetadata(List<SpriteMetaData> metaDatas, SpriteMetaData data){
			int len = metaDatas.Count;
			for(int i=0;i<len;++i){
				SpriteMetaData d = metaDatas[i];
				if(d.name.Equals(data.name)) return false;
			}
			return true;
		}

		public static void SplitTextureToSprite()
		{
			if(m_metaDatas.Count>0){
				foreach(Texture k in m_metaDatas.Keys){
					string textureAtlasPath = AssetDatabase.GetAssetPath(k);
					TextureImporter textureImporter = AssetImporter.GetAtPath(textureAtlasPath) as TextureImporter;
					textureImporter.maxTextureSize = 2048;
					textureImporter.spritesheet = m_metaDatas[k].ToArray();
					textureImporter.textureType = TextureImporterType.Sprite;
					textureImporter.spriteImportMode = SpriteImportMode.Multiple;
					textureImporter.spritePixelsPerUnit = 100;
					AssetDatabase.ImportAsset(textureAtlasPath, ImportAssetOptions.ForceUpdate);
					Object[] savedSprites = AssetDatabase.LoadAllAssetsAtPath(textureAtlasPath);
					foreach(Object obj in savedSprites){
						Sprite objSprite = obj as Sprite;
						if(objSprite){
							int len = m_sprites.Count;
							for(int i=0;i<len;++i){
								if(m_sprites[i].name.Equals(objSprite.name)){
									m_sprites[i].sprite = objSprite;
								}
							}
							len = m_images.Count;
							for(int i=0;i<len;++i){
								if(m_images[i].name.Equals(objSprite.name)){
									m_images[i].sprite = objSprite;
								}
							}
						}
					}
				}
			}
		}

		static void ShowSonArmature(DragonBoneData.ActionData[] actions, DragonBoneData.SkinSlotDisplayData displayData,Transform slot ,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData){
			GameObject go = new GameObject(displayData.textureName);
			go.transform.parent = slot;
			Armature armature = go.AddComponent<Armature>();
			armature.isUGUI = armatureEditor.isUGUI;
			armatureEditor.m_sonArmature.Add(armature);

			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			go.transform.localScale = localSc;

			armature.color = slot.GetComponent<Slot>().color;

			if(!float.IsNaN(displayData.transform.rotate))
				go.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);

			if(actions!=null){
				foreach(DragonBoneData.ActionData ad in actions){
					if(ad.key.Equals("gotoAndPlay")){
						armature.animIndex = armatureEditor.GetAnimIndex(armature.name, ad.action);
						break;
					}
				}
			}
			if(slotData.actions!=null){
				foreach(DragonBoneData.ActionData ad in slotData.actions){
					if(ad.key.Equals("gotoAndPlay")){
						armature.animIndex = armatureEditor.GetAnimIndex(armature.name, ad.action);
						break;
					}
				}
			}
		}

		static void ShowCustomCollider(DragonBoneData.SkinSlotDisplayData displayData,Transform slot ,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData)
		{
			GameObject go = new GameObject(displayData.textureName);
			go.transform.parent = slot;
			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			localSc.x/= armatureEditor.textureScale;
			localSc.y/= armatureEditor.textureScale;
			go.transform.localScale = localSc;

			if(!float.IsNaN(displayData.transform.rotate))
			{
				go.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);
			}

			if(armatureEditor.genCustomCollider){
				PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
				Vector2[] points = new Vector2[displayData.vertices.Length];
				int len = points.Length;
				for(int i=0;i<len;++i){
					points[i] = (Vector2)displayData.vertices[i];
				}
				collider.SetPath (0,points);
			}
		}

		static void ShowSpriteFrame(TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot ,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData){
			GameObject go = new GameObject();
			SpriteFrame newFrame = go.AddComponent<SpriteFrame>();
			newFrame.CreateQuad();
			newFrame.textureFrames = armatureEditor.m_TextureFrames;
			newFrame.frame = frame;
			newFrame.name = displayData.textureName;
			newFrame.pivot = displayData.pivot;
			newFrame.transform.parent = slot;

			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			newFrame.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			localSc.x/= armatureEditor.textureScale;
			localSc.y/= armatureEditor.textureScale;
			newFrame.transform.localScale = localSc;

			newFrame.color = slot.GetComponent<Slot>().color;

			if(!float.IsNaN(displayData.transform.rotate))
				newFrame.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);

            newFrame.skew = displayData.transform.skew;
			
			if(armatureEditor.genImgCollider){
				BoxCollider2D collider = newFrame.gameObject.AddComponent<BoxCollider2D>();
				collider.size = newFrame.frame.rect.size*armatureEditor.unit;
				Vector2 center= new Vector2(
					-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
					newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
				collider.offset = center*armatureEditor.unit;
			}
			newFrame.UpdateVertexColor();
		}

		static void ShowUIFrame(TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot ,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData){
			GameObject go = new GameObject();
			UIFrame newFrame = go.AddComponent<UIFrame>();
			newFrame.raycastTarget = false;
			newFrame.GetComponent<Graphic>().raycastTarget = false;
			newFrame.CreateQuad();
			newFrame.frame = frame;
			newFrame.name = displayData.textureName;
			newFrame.GetComponent<RectTransform>().pivot = displayData.pivot;
			newFrame.transform.SetParent(slot) ;

			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			newFrame.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			localSc.x/= armatureEditor.textureScale;
			localSc.y/= armatureEditor.textureScale;
			newFrame.transform.localScale = localSc;

			newFrame.color = slot.GetComponent<Slot>().color;

			if(!float.IsNaN(displayData.transform.rotate))
				newFrame.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);

			if(armatureEditor.genImgCollider){
				BoxCollider2D collider = newFrame.gameObject.AddComponent<BoxCollider2D>();
				collider.size = newFrame.frame.rect.size*armatureEditor.unit;
				Vector2 center= new Vector2(
					-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
					newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
				collider.offset = center*armatureEditor.unit;
			}
			newFrame.UpdateAll();
		}

		static SpriteRenderer ShowUnitySprite(DragonBoneArmatureEditor armatureEditor, TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,SpriteMetaData metaData,DragonBoneData.SlotData slotData){
			Sprite sprite = Sprite.Create((Texture2D)frame.texture,metaData.rect,metaData.pivot,100f,0,SpriteMeshType.Tight);
			return ShowUnitySpriteSingle(armatureEditor,frame,sprite,displayData,slot,slotData);
		}

		static SpriteRenderer ShowUnitySpriteSingle(DragonBoneArmatureEditor armatureEditor, TextureFrame frame, Sprite sprite,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,DragonBoneData.SlotData slotData)
		{
			GameObject go = new GameObject(displayData.textureName);
			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.sprite = sprite;
			go.transform.parent = slot;

			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			localSc.x/= armatureEditor.textureScale;
			localSc.y/= armatureEditor.textureScale;
			go.transform.localScale = localSc;

			renderer.color = slot.GetComponent<Slot>().color;
			renderer.material = frame.material;

			if(!float.IsNaN(displayData.transform.rotate))
				go.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);
			return renderer;
		}

		static Image ShowUIImage(DragonBoneArmatureEditor armatureEditor,TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,SpriteMetaData metaData,DragonBoneData.SlotData slotData){
			Sprite sprite = Sprite.Create((Texture2D)frame.texture,metaData.rect,metaData.pivot,100f,0,SpriteMeshType.Tight);
			return ShowUIImageSingle(armatureEditor,frame,sprite,displayData,slot,slotData);
		}

		static Image ShowUIImageSingle(DragonBoneArmatureEditor armatureEditor,TextureFrame frame, Sprite sprite,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,DragonBoneData.SlotData slotData)
		{
			GameObject go = new GameObject(displayData.textureName);
			Image renderer = go.AddComponent<Image>();
			renderer.sprite = sprite;
			renderer.raycastTarget = false;
			renderer.SetNativeSize();
			BoxCollider2D col = go.GetComponent<BoxCollider2D>();
			if(col){
				col.size = renderer.rectTransform.sizeDelta;
			}
			go.transform.SetParent(slot) ;

			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(displayData.transform.x)) localPos.x = displayData.transform.x;
			if(!float.IsNaN(displayData.transform.y)) localPos.y = displayData.transform.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(displayData.transform.scx)) localSc.x = displayData.transform.scx;
			if(!float.IsNaN(displayData.transform.scy)) localSc.y = displayData.transform.scy;
			localSc.x/= armatureEditor.textureScale;
			localSc.y/= armatureEditor.textureScale;
			go.transform.localScale = localSc;

			renderer.color = slot.GetComponent<Slot>().color;
			renderer.material = frame.uiMaterial;

			if(!float.IsNaN(displayData.transform.rotate))
				go.transform.localRotation = Quaternion.Euler(0,0,displayData.transform.rotate);
			return renderer;
		}

		static void ShowUIMesh(TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData){
			armatureEditor.spriteMeshUsedMatKV[frame.uiMaterial] = true;
			GameObject go = new GameObject(displayData.textureName);
			UIMesh sm = go.AddComponent<UIMesh>();
			sm.raycastTarget = false;
			sm.vertices = displayData.vertices;
			sm.uvs = displayData.uvs;
			sm.triangles = displayData.triangles;
			sm.colors = new Color32[sm.vertices.Length];
			for(int i =0;i<sm.colors.Length;++i){
				sm.colors[i] = Color.white;
			}
			if(armatureEditor.genMeshCollider && displayData.edges!=null){
				sm.edges = displayData.edges;
			}
			if(displayData.weights!=null && displayData.weights.Length>0){
				sm.CreateMesh();
				if(armatureEditor.ffdKV.ContainsKey(displayData.textureName)){
					//Vertex controller
					sm.vertControlTrans = new Transform[sm.vertices.Length];
					for(int i=0;i<sm.vertices.Length;++i){
						GameObject gov = new GameObject(go.name+"_v"+i);
						gov.transform.parent = go.transform;
						gov.transform.localPosition = sm.vertices[i];
						gov.transform.localScale = Vector3.zero;
						sm.vertControlTrans[i] = gov.transform;
						gov.SetActive(false);
					}
				}
			}
			else
			{
				sm.CreateMesh();
				if(displayData.bonePose==null){
					//Vertex controller
					sm.vertControlTrans = new Transform[sm.vertices.Length];
					for(int i=0;i<sm.vertices.Length;++i){
						GameObject gov = new GameObject(go.name+"_v"+i);
						gov.transform.parent = go.transform;
						gov.transform.localPosition = sm.vertices[i];
						gov.transform.localScale = Vector3.zero;
						sm.vertControlTrans[i] = gov.transform;
						gov.SetActive(false);
					}
				}
			}
			sm.transform.SetParent(slot);
			sm.frame = frame;

			if(displayData.bonePose!=null){
				if(displayData.weights!=null&&displayData.weights.Length>0){
					Transform[] bones = new Transform[displayData.bonePose.Length/7];
					for(int i=0;i<displayData.bonePose.Length;i+=7)
					{
						int index = i/7;
						int boneIndex = (int)displayData.bonePose[i];
						bones[index] = armatureEditor.m_bones[boneIndex];
					}

					List<Armature.BoneWeightClass> boneWeights = new List<Armature.BoneWeightClass>();
					for(int i=0;i<displayData.weights.Length;++i)
					{
						int boneCount = (int)displayData.weights[i];//骨骼数量

						List<KeyValuePair<int ,float>> boneWeightList = new List<KeyValuePair<int, float>>();
						for(int j=0;j<boneCount*2;j+=2){
							int boneIdx = (int)displayData.weights[i+j+1];
							float weight = displayData.weights[i+j+2];
							boneWeightList.Add(new KeyValuePair<int, float>(boneIdx,weight));
						}
						//sort boneWeightList，desc
						boneWeightList.Sort(delegate(KeyValuePair<int, float> x, KeyValuePair<int, float> y) {
							if(x.Value==y.Value) return 0;
							return x.Value<y.Value? 1: -1;
						});
						Armature.BoneWeightClass bw = new Armature.BoneWeightClass();
						for(int j=0;j<boneWeightList.Count;++j){
							if(j==0){
								bw.boneIndex0 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight0 = boneWeightList[j].Value;
							}else if(j==1){
								bw.boneIndex1 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight1 = boneWeightList[j].Value;
							}else if(j==2){
								bw.boneIndex2 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight2 = boneWeightList[j].Value;
							}else if(j==3){
								bw.boneIndex3 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight3 = boneWeightList[j].Value;
							}else if(j==4){
								bw.boneIndex4= GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight4 = boneWeightList[j].Value;
								break;
							}
						}
						boneWeights.Add(bw);
						i+=boneCount*2;
					}
					Matrix4x4[] matrixArray = new Matrix4x4[bones.Length];
					for(int i=0;i<matrixArray.Length;++i){
						Transform bone = bones[i];
						Vector3 bonePos = bone.localPosition;
						Quaternion boneRotate = bone.localRotation;

						BoneMatrix2D m2d= armatureEditor.bonePoseKV[displayData.textureName + bone.name];
						bone.position = new Vector3(m2d.tx*armatureEditor.unit,-m2d.ty*armatureEditor.unit,bone.position.z);
						bone.rotation = Quaternion.Euler(0f,0f,-m2d.GetAngle());

						matrixArray[i] = bone.worldToLocalMatrix*armatureEditor.armature.localToWorldMatrix;
						matrixArray[i] *= Matrix4x4.TRS(slot.localPosition,slot.localRotation,slot.localScale);

						bone.localPosition = bonePos;
						bone.localRotation = boneRotate;
					}
					sm.bones=bones;
					sm.bindposes = matrixArray;
					sm.weights = boneWeights.ToArray();
				}
			}
			DragonBoneData.TransformData tranform = displayData.transform ;
			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(tranform.x)) localPos.x = tranform.x;
			if(!float.IsNaN(tranform.y)) localPos.y = tranform.y;
			sm.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(tranform.scx)) localSc.x = tranform.scx;
			if(!float.IsNaN(tranform.scy)) localSc.y = tranform.scy;
			sm.transform.localScale = localSc;

			sm.color = slot.GetComponent<Slot>().color;
			sm.UpdateMesh();
			sm.UpdateVertexColor();
			sm.transform.localRotation = Quaternion.Euler(0,0,tranform.rotate);
		}

		static void ShowSpriteMesh(TextureFrame frame,DragonBoneData.SkinSlotDisplayData displayData,Transform slot,DragonBoneArmatureEditor armatureEditor,DragonBoneData.SlotData slotData){
			armatureEditor.spriteMeshUsedMatKV[frame.material] = true;
			GameObject go = new GameObject(displayData.textureName);
			SpriteMesh sm = go.AddComponent<SpriteMesh>();
			sm.vertices = displayData.vertices;
			sm.uvs = displayData.uvs;
			sm.triangles = displayData.triangles;
			sm.colors = new Color[sm.vertices.Length];
			for(int i =0;i<sm.colors.Length;++i){
				sm.colors[i] = Color.white;
			}
			if(armatureEditor.genMeshCollider && displayData.edges!=null){
				sm.edges = displayData.edges;
			}
			if(displayData.weights!=null && displayData.weights.Length>0){
				sm.CreateMesh();
				if(armatureEditor.ffdKV.ContainsKey(displayData.textureName)){
					//Vertex controller
					sm.vertControlTrans = new Transform[sm.vertices.Length];
					for(int i=0;i<sm.vertices.Length;++i){
						GameObject gov = new GameObject(go.name+"_v"+i);
						gov.transform.parent = go.transform;
						gov.transform.localPosition = sm.vertices[i];
						gov.transform.localScale = Vector3.zero;
						sm.vertControlTrans[i] = gov.transform;
						gov.SetActive(false);
					}
				}
			}
			else
			{
				sm.CreateMesh();
				if(displayData.bonePose==null){
					//Vertex controller
					sm.vertControlTrans = new Transform[sm.vertices.Length];
					for(int i=0;i<sm.vertices.Length;++i){
						GameObject gov = new GameObject(go.name+"_v"+i);
						gov.transform.parent = go.transform;
						gov.transform.localPosition = sm.vertices[i];
						gov.transform.localScale = Vector3.zero;
						sm.vertControlTrans[i] = gov.transform;
						gov.SetActive(false);
					}
				}
			}
			sm.transform.SetParent(slot);
			sm.frame = frame;

			if(displayData.bonePose!=null){
				if(displayData.weights!=null&&displayData.weights.Length>0){
					Transform[] bones = new Transform[displayData.bonePose.Length/7];
					for(int i=0;i<displayData.bonePose.Length;i+=7)
					{
						int index = i/7;
						int boneIndex = (int)displayData.bonePose[i];
						bones[index] = armatureEditor.m_bones[boneIndex];
					}

					List<Armature.BoneWeightClass> boneWeights = new List<Armature.BoneWeightClass>();
					for(int i=0;i<displayData.weights.Length;++i)
					{
						int boneCount = (int)displayData.weights[i];//骨骼数量

						List<KeyValuePair<int ,float>> boneWeightList = new List<KeyValuePair<int, float>>();
						for(int j=0;j<boneCount*2;j+=2){
							int boneIdx = (int)displayData.weights[i+j+1];
							float weight = displayData.weights[i+j+2];
							boneWeightList.Add(new KeyValuePair<int, float>(boneIdx,weight));
						}
						//sort boneWeightList，desc
						boneWeightList.Sort(delegate(KeyValuePair<int, float> x, KeyValuePair<int, float> y) {
							if(x.Value==y.Value) return 0;
							return x.Value<y.Value? 1: -1;
						});
						Armature.BoneWeightClass bw = new Armature.BoneWeightClass();
						for(int j=0;j<boneWeightList.Count;++j){
							if(j==0){
								bw.boneIndex0 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight0 = boneWeightList[j].Value;
							}else if(j==1){
								bw.boneIndex1 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight1 = boneWeightList[j].Value;
							}else if(j==2){
								bw.boneIndex2 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight2 = boneWeightList[j].Value;
							}else if(j==3){
								bw.boneIndex3 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight3 = boneWeightList[j].Value;
							}else if(j==4){
								bw.boneIndex4 = GlobalBoneIndexToLocalBoneIndex(armatureEditor, boneWeightList[j].Key,bones);
								bw.weight4 = boneWeightList[j].Value;
								break;
							}
						}
						boneWeights.Add(bw);
						i+=boneCount*2;
					}
					Matrix4x4[] matrixArray = new Matrix4x4[bones.Length];
					for(int i=0;i<matrixArray.Length;++i){
						Transform bone = bones[i];
						Vector3 bonePos = bone.localPosition;
						Quaternion boneRotate = bone.localRotation;

						BoneMatrix2D m2d= armatureEditor.bonePoseKV[displayData.textureName + bone.name];
						bone.position = new Vector3(m2d.tx*armatureEditor.unit,-m2d.ty*armatureEditor.unit,bone.position.z);
						bone.rotation = Quaternion.Euler(0f,0f,-m2d.GetAngle());

						matrixArray[i] = bone.worldToLocalMatrix*armatureEditor.armature.localToWorldMatrix;
						matrixArray[i] *= Matrix4x4.TRS(slot.localPosition,slot.localRotation,slot.localScale);

						bone.localPosition = bonePos;
						bone.localRotation = boneRotate;
					}
					sm.bones=bones;
					sm.bindposes = matrixArray;
					sm.weights = boneWeights.ToArray();
				}
			}
			DragonBoneData.TransformData tranform = displayData.transform ;
			if(tranform==null) return;
			Vector3 localPos = Vector3.zero;
			if(!float.IsNaN(tranform.x)) localPos.x = tranform.x;
			if(!float.IsNaN(tranform.y)) localPos.y = tranform.y;
			sm.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			if(!float.IsNaN(tranform.scx)) localSc.x = tranform.scx;
			if(!float.IsNaN(tranform.scy)) localSc.y = tranform.scy;
			sm.transform.localScale = localSc;

			sm.color = slot.GetComponent<Slot>().color;
			sm.UpdateMesh();
			sm.UpdateVertexColor();
			sm.transform.localRotation = Quaternion.Euler(0,0,tranform.rotate);
		}

		static int GlobalBoneIndexToLocalBoneIndex( DragonBoneArmatureEditor armatureEditor,int globalBoneIndex,Transform[] localBones){
			Transform globalBone = armatureEditor.m_bones[globalBoneIndex];
			int len = localBones.Length;
			for(int i=0;i<len;++i){
				if(localBones[i] == globalBone) return i;
			}
			return globalBoneIndex;
		}

		public static void SetIKs(DragonBoneArmatureEditor armatureEditor){
			if(armatureEditor.armatureData.ikDatas!=null){
				int len = armatureEditor.armatureData.ikDatas.Length;
				for(int i=0;i<len;++i){
					DragonBoneData.IKData ikData = armatureEditor.armatureData.ikDatas[i];
					Transform ikTrans = armatureEditor.bonesKV[ikData.target];
					Transform targetBone = armatureEditor.bonesKV[ikData.bone];
					DragonBoneData.BoneData targetBoneData = armatureEditor.bonesDataKV[ikData.bone];
					Transform parentBone = targetBone;
					int y = ikData.chain;
					while(--y>-1){
						parentBone = parentBone.parent;
					}
					BoneIK bi = parentBone.gameObject.AddComponent<BoneIK>();

					Vector3 v = Vector3.right * targetBoneData.length*armatureEditor.unit;
					v = targetBone.TransformPoint(v);
					GameObject go = new GameObject(ikData.name);
					go.transform.parent = targetBone;
					go.transform.position = v;
					go.transform.localRotation = Quaternion.identity;
					go.transform.localScale = Vector3.zero;

					bi.damping = ikData.weight;
					bi.endTransform = go.transform;
					bi.targetIK = ikTrans;
					bi.iterations = 20;
					bi.bendPositive = ikData.bendPositive;
					bi.rootBone = armatureEditor.armature;
				}
			}
		}
	}

}