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
	/// author:  bingheliefeng
	/// </summary>
	public class SpineShowArmature {

		private static Transform m_rootBone;
		private static Transform m_rootSlot;

		public static void Dispose(){
			m_rootBone = null;
			m_rootSlot = null;
		}

		public static void AddBones(SpineArmatureEditor armatureEditor){
			m_rootBone = null;
			if(armatureEditor.armatureData.bones!=null)
			{
				armatureEditor.bonesKV.Clear();
				armatureEditor.bones.Clear();
				int len = armatureEditor.armatureData.bones.Length;
				for(int i=0;i<len;++i){
					SpineData.BoneData boneData = armatureEditor.armatureData.bones[i];
					GameObject go = new GameObject(boneData.name);
					armatureEditor.bonesKV[boneData.name]=go.transform;
					if(m_rootBone==null) m_rootBone = go.transform;
					armatureEditor.bones.Add(go.transform);
				}
			}
		}
		public static void AddSlot(SpineArmatureEditor armatureEditor){
			m_rootSlot = null;
			if(armatureEditor.armatureData.slots!=null){
				armatureEditor.slotsKV.Clear();
				int len = armatureEditor.armatureData.slots.Length;
				Armature armature = armatureEditor.armature.GetComponent<Armature>();
				for(int i=0;i<len;++i){
					SpineData.SlotData slotData = armatureEditor.armatureData.slots[i];
					GameObject go = new GameObject(slotData.name);
					armatureEditor.slotsKV[slotData.name]=go.transform;

					Slot slot = go.AddComponent<Slot>();
					slot.zOrder = i;
					slot.armature = armature;
					slot.blendMode = slot.ConvertBlendMode( slotData.blend.ToLower());
					armatureEditor.slots.Add(slot);
					slot.color = slotData.color;
				}
			}
		}


		public static void ShowBones(SpineArmatureEditor armatureEditor){

			foreach(Transform b in armatureEditor.bonesKV.Values)
			{
				SpineData.BoneData boneData= armatureEditor.bonesDataKV[b.name];

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

				b.transform.localPosition = new Vector3(boneData.x,boneData.y,0f);
				b.transform.localScale = new Vector3(boneData.scaleX,boneData.scaleY,1f);
				b.transform.localRotation = Quaternion.Euler(0f,0f,boneData.rotation);

				GameObject inhertGo = null;
				Bone myBone = null;
				if(!boneData.inheritRotation){
					inhertGo = new GameObject("_"+boneData.name);
					inhertGo.transform.parent = armatureEditor.armature;
					inhertGo.transform.localPosition = b.transform.localPosition;
					inhertGo.transform.localRotation = b.transform.localRotation;
					inhertGo.transform.localScale = b.transform.localScale;
					myBone = b.gameObject.AddComponent<Bone>();
					myBone.inheritRotation = inhertGo.transform;
					inhertGo.hideFlags = HideFlags.NotEditable;
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
		public static void ShowSlots(SpineArmatureEditor armatureEditor){
			if(armatureEditor.genericAnim || armatureEditor.isUGUI){
				GameObject rootSlot = new GameObject("slots");
				m_rootSlot = rootSlot.transform;
				m_rootSlot.SetParent(armatureEditor.armature);
				m_rootSlot.localScale = Vector3.one;
				m_rootSlot.localPosition = Vector3.zero;
			}
			foreach(Transform s in armatureEditor.slotsKV.Values)
			{
				Slot slot = s.GetComponent<Slot>();
				SpineData.SlotData slotData = armatureEditor.slotsDataKV[s.name];
				if(!string.IsNullOrEmpty(slotData.bone)){
					if(armatureEditor.bonesKV.ContainsKey(slotData.bone)){
						Transform parent = armatureEditor.bonesKV[slotData.bone];

						if(m_rootSlot){
							s.transform.parent = m_rootSlot;

							GameObject go = new GameObject(s.name);
							go.transform.parent = parent.transform;
							go.transform.localScale = Vector3.one;
							if(armatureEditor.isUGUI){
								go.transform.localPosition = Vector3.zero;
							}else{
								go.transform.localPosition = new Vector3(0,0,-slot.zOrder*armatureEditor.zoffset);
							}
							go.transform.localEulerAngles = Vector3.zero;
							slot.inheritSlot = go.transform;

						}else{
							s.transform.parent = parent.transform;
						}

					}
				}
				s.transform.localScale = Vector3.one;
				if(armatureEditor.isUGUI){
					s.localPosition = Vector3.zero;
				}else{
					s.localPosition = new Vector3(0,0,-slot.zOrder*armatureEditor.zoffset);
				}
				s.transform.localEulerAngles = Vector3.zero;
			}
		}

		public static void ShowSkin(SpineArmatureEditor armatureEditor){
			if(armatureEditor.armatureData.skins!=null)
			{
				Armature armature= armatureEditor.armature.GetComponent<Armature>();

				Dictionary<Texture,List<SpriteMetaData>> metaDatas = new Dictionary<Texture, List<SpriteMetaData>>();
				List<SpriteRenderer> sprites = new List<SpriteRenderer>();
				List<Image> images = new List<Image>();

				int len = armatureEditor.armatureData.skins.Length;
				for(int i=0;i<len;++i){
					SpineData.SkinData skinData = armatureEditor.armatureData.skins[i];
					foreach(string slotName in skinData.slots.Keys){
						Transform slot = armatureEditor.slotsKV[slotName];
						Transform skinParent = slot;
						if(len>1){
							skinParent = slot.Find(skinData.skinName);
							if(!skinParent){
								GameObject skinParentGo = new GameObject(skinData.skinName);
								skinParentGo.transform.parent = slot;
								skinParentGo.transform.localScale = Vector3.one;
								skinParentGo.transform.localPosition = Vector3.zero;
								skinParentGo.transform.localRotation = Quaternion.identity;
								skinParent = skinParentGo.transform;
								skinParent.gameObject.SetActive(i==0);
							}
						}
						SpineData.SlotData slotData = armatureEditor.slotsDataKV[slotName];
						List<SpineData.SkinAttachment> attachmentDataArr = skinData.slots[slotName];
						for(int j=0;j<attachmentDataArr.Count;++j){
							SpineData.SkinAttachment attachmentData = attachmentDataArr[j];
							TextureFrame frame = armatureEditor.m_TextureFrames.GetTextureFrame(attachmentData.textureName);
							if(attachmentData.type=="region")//region,mesh,linkedmesh,boundingBox,path
							{
								if(armatureEditor.displayType== Bone2DSetupEditor.DisplayType.Default){
									ShowSpriteFrame(frame,attachmentData,slot,skinParent,armatureEditor);
								}
								else if(armatureEditor.displayType == Bone2DSetupEditor.DisplayType.SpriteRender
									|| armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIImage)
								{
									SpriteMetaData metaData = new SpriteMetaData();
									metaData.name = attachmentData.textureName;
									metaData.rect = frame.rect;
									metaData.rect.y = frame.texture.height-metaData.rect.y-metaData.rect.height;
									metaData.alignment = (int)SpriteAlignment.Custom;
									metaData.pivot = Vector2.one*0.5f;
									if(!metaDatas.ContainsKey(frame.texture)){
										metaDatas[frame.texture] = new List<SpriteMetaData>();
									}
									metaDatas[frame.texture].Add(metaData);

									if(armatureEditor.displayType == Bone2DSetupEditor.DisplayType.SpriteRender){
										SpriteRenderer sr = ShowUnitySprite(attachmentData,slot,skinParent,metaData,frame);
										if(armatureEditor.genMeshCollider){
											sr.gameObject.AddComponent<BoxCollider2D>();
										}
										sprites.Add(sr);
									}
									else
									{
										Image img = ShowUIImage(attachmentData,slot,skinParent,metaData,frame);
										if(armatureEditor.genMeshCollider){
											img.gameObject.AddComponent<BoxCollider2D>();
										}
										images.Add(img);
									}
								}
								else if(armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIDefault)
								{
									ShowUIFrame(frame,attachmentData,slot,skinParent,armatureEditor,slotData);
								}
							}
							else if(attachmentData.type=="mesh")
							{
								if(frame.rect.width>0 && frame.rect.height>0){
									if(armature.isUGUI){
										ShowUIMesh(frame,attachmentData,slot,skinParent,armatureEditor);
									}else{
										ShowSpriteMesh(frame,attachmentData,slot,skinParent,armatureEditor);
									}
								}
							}
							else if(attachmentData.type=="boundingbox"){
								ShowCustomCollider(attachmentData,slot,skinParent,armatureEditor);
							}

							if(string.IsNullOrEmpty(slotData.attachment)){
								slot.GetComponent<Slot>().displayIndex = -1;
							}
							else
							{
								if(armatureEditor.isUGUI)
								{
									MaskableGraphic[] renders = slot.GetComponentsInChildren<MaskableGraphic>(true);
									for(int p=0;p<renders.Length;++p){
										if(renders[p].name==slotData.attachment){
											slot.GetComponent<Slot>().displayIndex = p;
											break;
										}
									}
								}
								else
								{
									Renderer[] renders = slot.GetComponentsInChildren<Renderer>(true);
									for(int p=0;p<renders.Length;++p){
										if(renders[p].name==slotData.attachment){
											slot.GetComponent<Slot>().displayIndex = p;
											break;
										}
									}
								}
							}
						}
					}
				}

				if(armatureEditor.displayType == Bone2DSetupEditor.DisplayType.SpriteRender
					||armatureEditor.displayType== Bone2DSetupEditor.DisplayType.UGUIImage){
					if(metaDatas.Count>0){
						foreach(Texture k in metaDatas.Keys){
							string textureAtlasPath = AssetDatabase.GetAssetPath(k);
							TextureImporter textureImporter = AssetImporter.GetAtPath(textureAtlasPath) as TextureImporter;
							textureImporter.maxTextureSize = 2048;
							textureImporter.spritesheet = metaDatas[k].ToArray();
							textureImporter.textureType = TextureImporterType.Sprite;
							textureImporter.spriteImportMode = SpriteImportMode.Multiple;
							textureImporter.spritePixelsPerUnit = 100;
							AssetDatabase.ImportAsset(textureAtlasPath, ImportAssetOptions.ForceUpdate);
							Object[] savedSprites = AssetDatabase.LoadAllAssetsAtPath(textureAtlasPath);
							foreach(Object obj in savedSprites){
								Sprite objSprite = obj as Sprite;
								if(objSprite){
									len = sprites.Count;
									for(int i=0;i<len;++i){
										if(sprites[i].name.Equals(objSprite.name)){
											sprites[i].sprite = objSprite;
										}
									}
									len = images.Count;
									for(int i=0;i<len;++i){
										if(images[i].name.Equals(objSprite.name)){
											images[i].sprite = objSprite;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static void ShowCustomCollider(SpineData.SkinAttachment attachmentData,Transform slot, Transform skinParent ,SpineArmatureEditor armatureEditor)
		{
            GameObject go = new GameObject(attachmentData.name);
			go.transform.parent = skinParent;
			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;
			go.transform.localScale = localSc;

			go.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation);

			if(armatureEditor.genCustomCollider){
				PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
				Vector2[] points = new Vector2[attachmentData.vertices.Length];
				int len = points.Length;
				for(int i=0;i<len;++i){
					points[i] = (Vector2)attachmentData.vertices[i];
				}
				collider.SetPath (0,points);
			}
		}

		private static void ShowSpriteFrame(TextureFrame frame,SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,SpineArmatureEditor armatureEditor){
			GameObject go=new GameObject();
			SpriteFrame newFrame =go.AddComponent<SpriteFrame>();
		
			newFrame.CreateQuad();
			newFrame.textureFrames = armatureEditor.m_TextureFrames;
			newFrame.frame = frame;
			newFrame.name = attachmentData.name;
			newFrame.pivot = Vector2.one*0.5f;
			newFrame.transform.parent = skinParent;

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			newFrame.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;

			if(newFrame.frame.isRotated)
			{
				if(attachmentData.width>0 && frame.rect.height>0){
					localSc.x*=attachmentData.width/frame.rect.height;
				}
				if(attachmentData.height>0 && frame.rect.width>0){
					localSc.y*=attachmentData.height/frame.rect.width;
				}
			}else{
				if(attachmentData.width>0 && frame.rect.width>0){
					localSc.x*=attachmentData.width/frame.rect.width;
				}
				if(attachmentData.height>0 && frame.rect.height>0){
					localSc.y*=attachmentData.height/frame.rect.height;
				}
			}

			newFrame.transform.localScale = localSc;
			newFrame.color = attachmentData.color;
			newFrame.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation);

			if(armatureEditor.genImgCollider){
				BoxCollider2D collider = newFrame.gameObject.AddComponent<BoxCollider2D>();
				if(newFrame.frame.isRotated)
				{
					collider.size = new Vector2(newFrame.frame.rect.size.y,newFrame.frame.rect.size.x)*armatureEditor.unit;

					Vector2 center= new Vector2(
						-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
						newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
					collider.offset = center*armatureEditor.unit;
				}
				else
				{
					collider.size = newFrame.frame.rect.size*armatureEditor.unit;

					Vector2 center= new Vector2(
						-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
						newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
					collider.offset = center*armatureEditor.unit;
				}
			}
			newFrame.UpdateVertexColor();
		}


		static void ShowUIFrame(TextureFrame frame,SpineData.SkinAttachment attachmentData,Transform slot ,Transform skinParent,SpineArmatureEditor armatureEditor,SpineData.SlotData slotData){
			GameObject go = new GameObject();
			UIFrame newFrame = go.AddComponent<UIFrame>();
			newFrame.raycastTarget = false;
			newFrame.GetComponent<Graphic>().raycastTarget = false;
			newFrame.CreateQuad();
			newFrame.frame = frame;
			newFrame.name = attachmentData.textureName;
			newFrame.transform.SetParent(skinParent) ;

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;

			if(newFrame.frame.isRotated)
			{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.height;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.width;
				}
			}else{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.width;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.height;
				}
			}
			newFrame.transform.localScale = localSc;
			newFrame.color = attachmentData.color;
			newFrame.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation);

			if(armatureEditor.genImgCollider){
				BoxCollider2D collider = newFrame.gameObject.AddComponent<BoxCollider2D>();
				if(newFrame.frame.isRotated)
				{
					collider.size = new Vector2(newFrame.frame.rect.size.y,newFrame.frame.rect.size.x)*armatureEditor.unit;

					Vector2 center= new Vector2(
						-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
						newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
					collider.offset = center*armatureEditor.unit;
				}
				else
				{
					collider.size = newFrame.frame.rect.size*armatureEditor.unit;

					Vector2 center= new Vector2(
						-newFrame.frame.frameSize.width/2-newFrame.frame.frameSize.x+newFrame.frame.rect.width/2,
						newFrame.frame.frameSize.height/2+newFrame.frame.frameSize.y-newFrame.frame.rect.height/2);
					collider.offset = center*armatureEditor.unit;
				}
			}
			newFrame.UpdateAll();
		}

		private static Image ShowUIImage(SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,SpriteMetaData metaData,TextureFrame frame){
			Sprite sprite = Sprite.Create((Texture2D)frame.texture,metaData.rect,metaData.pivot,100f,0,SpriteMeshType.Tight);
			return ShowUIImageSingle(sprite,attachmentData,slot,skinParent,frame);
		}
		private static Image ShowUIImageSingle( Sprite sprite,SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,TextureFrame frame)
		{
			GameObject go = new GameObject(attachmentData.name);
			Image renderer = go.AddComponent<Image>();
			renderer.sprite = sprite;
			renderer.raycastTarget = false;
			renderer.SetNativeSize();
			renderer.material = frame.uiMaterial;
			go.transform.SetParent(skinParent);
			BoxCollider2D col = go.GetComponent<BoxCollider2D>();
			if(col){
				col.size = renderer.rectTransform.sizeDelta;
			}

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;

			if(frame.isRotated)
			{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.height;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.width;
				}
			}else{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.width;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.height;
				}
			}
			go.transform.localScale = localSc;
			renderer.color = attachmentData.color;
			go.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation+(frame.isRotated?-90f:0f));

			return renderer;
		}

		private static SpriteRenderer ShowUnitySprite(SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,SpriteMetaData metaData,TextureFrame frame){
			Sprite sprite = Sprite.Create((Texture2D)frame.texture,metaData.rect,metaData.pivot,100f,0,SpriteMeshType.Tight);
			return ShowUnitySpriteSingle(sprite,attachmentData,slot,skinParent,frame);
		}

		private static SpriteRenderer ShowUnitySpriteSingle( Sprite sprite,SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,TextureFrame frame)
		{
			GameObject go = new GameObject(attachmentData.name);
			SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
			renderer.material = frame.material;
			renderer.sprite = sprite;
			go.transform.parent = skinParent;

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;

			if(frame.isRotated)
			{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.height;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.width;
				}
			}else{
				if(attachmentData.width>0){
					localSc.x*=attachmentData.width/frame.rect.width;
				}
				if(attachmentData.height>0){
					localSc.y*=attachmentData.height/frame.rect.height;
				}
			}
			go.transform.localScale = localSc;

			renderer.color = attachmentData.color;

			go.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation+(frame.isRotated?-90f:0f));
				
			return renderer;
		}

		//return current mesh bones
		static Transform[] SetMeshVertex<T>(T sm, SpineData.SkinAttachment attachmentData,SpineArmatureEditor armatureEditor)
		{
			if(attachmentData.weights!=null && attachmentData.weights.Count>0){
				List<Vector3> verticesList = new List<Vector3>();
				List<float> weights = new List<float>();
				List<Transform> bones = new List<Transform>();
				Dictionary<int,bool> bonesKV = new Dictionary<int, bool>();
				for(int i=0;i<attachmentData.weights.Count;++i){
					int boneCount = (int)attachmentData.weights[i];
					weights.Add(boneCount);
					Vector3 v=Vector3.zero;
					for(int j=0;j<boneCount*4;j+=4){
						int boneIdx = (int)attachmentData.weights[i+j+1];
						float vx = attachmentData.weights[i+j+2];
						float vy = attachmentData.weights[i+j+3];
						float weight = attachmentData.weights[i+j+4];
						weights.Add(boneIdx);
						weights.Add(weight);
						//convert vertex
						Vector3 tempP = new Vector3(vx,vy,0f);
						Transform bone = armatureEditor.bones[boneIdx];
						tempP = bone.TransformPoint(tempP);
						v.x += tempP.x * weight;
						v.y += tempP.y * weight;
						if(!bonesKV.ContainsKey(boneIdx)){
							bones.Add( armatureEditor.bones[boneIdx]);
							bonesKV[boneIdx] = true;
						}
					}
					verticesList.Add(v);
					i+=boneCount*4;
				}
				attachmentData.vertices = verticesList.ToArray();
				if(sm is SpriteMesh){
					(sm as SpriteMesh).vertices = attachmentData.vertices;
				}
				else if(sm is UIMesh){
					(sm as UIMesh).vertices = attachmentData.vertices;
				}
				attachmentData.weights = weights;
				return bones.ToArray();
			}else{
				if(sm is SpriteMesh){
					(sm as SpriteMesh).vertices = attachmentData.vertices;
				}
				else if(sm is UIMesh){
					(sm as UIMesh).vertices = attachmentData.vertices;
				}
			}
			return null;
		}

		static void ShowUIMesh(TextureFrame frame,SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,SpineArmatureEditor armatureEditor){

			GameObject go = new GameObject(attachmentData.name);
			UIMesh sm = go.AddComponent<UIMesh>();
			sm.raycastTarget = false;
			sm.transform.SetParent(skinParent);

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;
			go.transform.localScale = localSc;

			go.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation);

			Transform[] bones = SetMeshVertex<UIMesh>(sm,attachmentData,armatureEditor);
			sm.uvs = attachmentData.uvs;
			sm.triangles = attachmentData.triangles;
			sm.colors = new Color32[sm.vertices.Length];
			for(int i =0;i<sm.colors.Length;++i){
				sm.colors[i] = Color.white;
			}
			if(armatureEditor.genMeshCollider && attachmentData.edges!=null){
				sm.edges = attachmentData.edges;
			}
			if(attachmentData.weights!=null && attachmentData.weights.Count>0){
				sm.CreateMesh();
				if(armatureEditor.ffdKV.ContainsKey(attachmentData.textureName)){
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

			if(attachmentData.weights!=null&&attachmentData.weights.Count>0){
				List<Armature.BoneWeightClass> boneWeights = new List<Armature.BoneWeightClass>();
				for(int i=0;i<attachmentData.weights.Count;++i)
				{
					int boneCount = (int)attachmentData.weights[i];//骨骼数量
					List<KeyValuePair<int ,float>> boneWeightList = new List<KeyValuePair<int, float>>();
					for(int j=0;j<boneCount*2;j+=2){
						int boneIdx = (int)attachmentData.weights[i+j+1];
						float weight = attachmentData.weights[i+j+2];
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
						}
					}
					boneWeights.Add(bw);
					i+=boneCount*2;
				}
				Matrix4x4[] matrixArray = new Matrix4x4[bones.Length];
				for(int i=0;i<matrixArray.Length;++i){
					Transform bone = bones[i];
					matrixArray[i] = bone.worldToLocalMatrix*armatureEditor.armature.localToWorldMatrix;
					matrixArray[i] *= Matrix4x4.TRS(slot.localPosition,slot.localRotation,slot.localScale);
				}
				sm.bones=bones;
				sm.bindposes = matrixArray;
				sm.weights = boneWeights.ToArray();
			}
			sm.color = attachmentData.color;
			sm.UpdateMesh();
			sm.UpdateVertexColor();
			sm.frame = frame;
		}

		static void ShowSpriteMesh(TextureFrame frame ,SpineData.SkinAttachment attachmentData,Transform slot,Transform skinParent,SpineArmatureEditor armatureEditor){

			GameObject go = new GameObject(attachmentData.name);
			SpriteMesh sm = go.AddComponent<SpriteMesh>();
			sm.transform.parent = skinParent;

			Vector3 localPos = Vector3.zero;
			localPos.x = attachmentData.x;
			localPos.y = attachmentData.y;
			go.transform.localPosition = localPos;

			Vector3 localSc = Vector3.one;
			localSc.x = attachmentData.scaleX;
			localSc.y = attachmentData.scaleY;
			go.transform.localScale = localSc;

			go.transform.localRotation = Quaternion.Euler(0,0,attachmentData.rotation);

			Transform[] bones = SetMeshVertex<SpriteMesh>(sm,attachmentData,armatureEditor);
			sm.uvs = attachmentData.uvs;
			sm.triangles = attachmentData.triangles;
			sm.colors = new Color[sm.vertices.Length];
			for(int i =0;i<sm.colors.Length;++i){
				sm.colors[i] = Color.white;
			}
			if(armatureEditor.genMeshCollider && attachmentData.edges!=null){
				sm.edges = attachmentData.edges;
			}
			if(attachmentData.weights!=null && attachmentData.weights.Count>0){
				sm.CreateMesh();
				if(armatureEditor.ffdKV.ContainsKey(attachmentData.textureName)){
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

			if(attachmentData.weights!=null&&attachmentData.weights.Count>0){
				List<Armature.BoneWeightClass> boneWeights = new List<Armature.BoneWeightClass>();
				for(int i=0;i<attachmentData.weights.Count;++i)
				{
					int boneCount = (int)attachmentData.weights[i];//骨骼数量
					List<KeyValuePair<int ,float>> boneWeightList = new List<KeyValuePair<int, float>>();
					for(int j=0;j<boneCount*2;j+=2){
						int boneIdx = (int)attachmentData.weights[i+j+1];
						float weight = attachmentData.weights[i+j+2];
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
					matrixArray[i] = bone.worldToLocalMatrix*armatureEditor.armature.localToWorldMatrix;
					matrixArray[i] *= Matrix4x4.TRS(slot.localPosition,slot.localRotation,slot.localScale);
				}
				sm.bones=bones;
				sm.bindposes = matrixArray;
				sm.weights = boneWeights.ToArray();
			}
			sm.color = attachmentData.color;
			sm.UpdateMesh();
			sm.UpdateVertexColor();
			sm.frame = frame;
		}

		static int GlobalBoneIndexToLocalBoneIndex( SpineArmatureEditor armatureEditor,int globalBoneIndex,Transform[] localBones){
			Transform globalBone = armatureEditor.bones[globalBoneIndex];
			int len = localBones.Length;
			for(int i=0;i<len;++i){
				if(localBones[i] == globalBone) return i;
			}
			return globalBoneIndex;
		}



		public static void SetIKs(SpineArmatureEditor armatureEditor){
			if(armatureEditor.armatureData.iks!=null){
				int len = armatureEditor.armatureData.iks.Length;
				for(int i=0;i<len;++i){
					SpineData.IKData ikData = armatureEditor.armatureData.iks[i];
					Transform targetIK = armatureEditor.bonesKV[ikData.target];
					Transform startBone =  armatureEditor.bonesKV[ikData.bones[0]];
					SpineData.BoneData endBoneData = armatureEditor.bonesDataKV[ikData.bones[ikData.bones.Length-1]];
					Transform endBone = armatureEditor.bonesKV[ikData.bones[ikData.bones.Length-1]];
					BoneIK bi = startBone.gameObject.AddComponent<BoneIK>();

					Vector3 v = Vector3.right * endBoneData.length*armatureEditor.unit;
					v = endBone.TransformPoint(v);
					GameObject go = new GameObject(ikData.name);
					go.transform.parent = endBone;
					go.transform.position = v;
					go.transform.localRotation = Quaternion.identity;
					go.transform.localScale = Vector3.zero;

					bi.damping = ikData.mix;
					bi.endTransform = go.transform;
					bi.targetIK = targetIK;
					bi.iterations = 20;
					bi.bendPositive = ikData.bendPositive;
					bi.rootBone = armatureEditor.armature;
				}
			}
		}
	}

}