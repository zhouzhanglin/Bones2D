using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace Bones2D
{
	/// <summary>
	/// Armature editor.
	/// author:bingheliefeng
	/// </summary>
	public class DragonBoneArmatureEditor : ScriptableWizard {
		[System.Serializable]
		public class Atlas{
			public Texture2D texture;
			public TextAsset atlasText;
		}

		[Header("Setting")]
		public float zoffset = 0.002f;
		public Bone2DSetupEditor.DisplayType displayType = Bone2DSetupEditor.DisplayType.Default;
		[Tooltip("UI and Sprite with the same animation file.")]
		public bool genericAnim = true;
		[Range(0.1f,1f)]
		public float textureScale = 1f;

		[Header("Generate File")]
		public bool genPrefab = false ;
		public bool genAnimations = true;
		public bool genAvatar = false;//generate Avatar and Avatar Mask

		[Header("Colliders")]
		public bool genMeshCollider = false; //Mesh Collider
		public bool genImgCollider = false;// BoxCollider2D , use image size
		public bool genCustomCollider = true;//custom collider

		[Header("Texture And Config")]
		public TextAsset animTextAsset;
		public Texture2D altasTexture;
		public TextAsset altasTextAsset;
		public Atlas[] otherTextures;

		private DragonBoneData.ArmatureData _armatureData;
		public DragonBoneData.ArmatureData armatureData{ 
			get{return _armatureData;} 
			set{_armatureData=value;}
		}

		private Transform _armature;
		public Transform armature{ 
			get{ return _armature;}
			set{_armature = value;}
		}

		public Atlas GetAtlasByTextureName(string textureName){
			if(atlasKV.ContainsKey(textureName)){
				return atlasKV[textureName];
			}
			return null;
		}
		public bool isUGUI{
			get{return (displayType== Bone2DSetupEditor.DisplayType.UGUIDefault || displayType== Bone2DSetupEditor.DisplayType.UGUIImage);}
		}
		public float unit{
			get{return 0.01f;}
		}
		public string namePrefix{
			get{return isUGUI? "_UI":"";}
		}
		public string shader{
			get{return isUGUI? "Bone2D/Simple UI":"Bone2D/Simple";}
		}

		public Dictionary<string,Transform> bonesKV = new Dictionary<string, Transform>();
		public Dictionary<string,DragonBoneData.BoneData> bonesDataKV = new Dictionary<string, DragonBoneData.BoneData>();
		public Dictionary<string,Transform> slotsKV = new Dictionary<string, Transform>();
		public Dictionary<string,DragonBoneData.SlotData> slotsDataKV = new Dictionary<string, DragonBoneData.SlotData>();
		public Dictionary<string,Sprite> spriteKV = new Dictionary<string, Sprite>();//single sprite

		public Dictionary<string , Atlas> atlasKV = new Dictionary<string, Atlas>();
		public Dictionary<string,BoneMatrix2D> bonePoseKV = new Dictionary<string, BoneMatrix2D>() ; //bonePose , key is texturename + bone name
		public Dictionary<string,bool> ffdKV = new Dictionary<string, bool>();//skinnedMesh animation or ffd animation, key is skin name/texture name

		public Dictionary<Material,bool> spriteMeshUsedMatKV = new Dictionary<Material, bool>();

		public Dictionary<string,List<string>> armatureAnimList = new Dictionary<string, List<string>>();//key is armature' name , value is anim name list

		protected internal List<Transform> m_bones = new List<Transform>();
		protected internal List<Slot> m_slots = new List<Slot>();
		protected internal List<Armature> m_sonArmature = new List<Armature>();
		protected internal bool m_haveSonArmature = false;
		protected internal TextureFrames m_TextureFrames ;

		protected List<GameObject> m_prefabs = new List<GameObject>();

		void OnEnable(){
			zoffset = EditorPrefs.GetFloat("bone2d_zoffset",0.002f);
			displayType = (Bone2DSetupEditor.DisplayType)EditorPrefs.GetInt("bone2d_displayType",0);
			genericAnim = EditorPrefs.GetBool("bone2d_genericAnim",true);
			genPrefab = EditorPrefs.GetBool("bone2d_genPrefab",false);
			genAnimations = EditorPrefs.GetBool("bone2d_genAnims",true);
			genAvatar = EditorPrefs.GetBool("bone2d_genAvatar",false);
			genMeshCollider = EditorPrefs.GetBool("bone2d_genMeshCollider",false);
			genImgCollider = EditorPrefs.GetBool("bone2d_genImgCollider",false);
			genCustomCollider = EditorPrefs.GetBool("bone2d_genCustomCollider",true);
		}

		[MenuItem("Assets/Bone2D/##DragonBone Panel (All Functions)",false,15)]
		[MenuItem("Bone2D/##DragonBone Panel (All Functions)",false,15)]
		static void CreateWizard () {
			DragonBoneArmatureEditor editor = ScriptableWizard.DisplayWizard<DragonBoneArmatureEditor>("Create DragonBone", "Create");
			editor.minSize = new Vector2(200,500);
			if(Selection.activeObject != null)
			{
				string dirPath = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
				if(File.Exists(dirPath)){
					dirPath = dirPath.Substring(0,dirPath.LastIndexOf("/"));
				}
				if(Directory.Exists(dirPath)){
					string animJsonPath=null;
					Dictionary<string,string> texturePathKV = new Dictionary<string, string>();
					Dictionary<string,string> textureJsonPathKV = new Dictionary<string, string>();
					foreach (string path in Directory.GetFiles(dirPath))
					{  
						if(path.LastIndexOf(".meta")==-1){
							if( System.IO.Path.GetExtension(path) == ".json" && (path.IndexOf("_tex")>-1 || path.IndexOf("texture")>-1) ){
								int start = path.LastIndexOf("/")+1;
								int end = path.LastIndexOf(".json");
								textureJsonPathKV[path.Substring(start,end-start)] = path;
								continue;
							}
							if( System.IO.Path.GetExtension(path) == ".png" && (path.IndexOf("_tex")>-1 || path.IndexOf("texture")>-1) ){
								int start = path.LastIndexOf("/")+1;
								int end = path.LastIndexOf(".png");
								texturePathKV[path.Substring(start,end-start)] = path;
								continue;
							}
							if ( System.IO.Path.GetExtension(path) == ".json" && (path.IndexOf("_ske")>-1 || path.IndexOf("texture.json")==-1)) {
								animJsonPath = path;
							}
						}
					} 

					if(!string.IsNullOrEmpty(animJsonPath)) editor.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);

					if( texturePathKV.Count>0 && textureJsonPathKV.Count>0){
						List<Atlas> atlasList = new List<Atlas>();
						foreach(string name in texturePathKV.Keys){
							if(textureJsonPathKV.ContainsKey(name)){
								if(editor.altasTexture==null){
									editor.altasTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textureJsonPathKV[name]);
									editor.altasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePathKV[name]);
								}else{
									Atlas atlas = new Atlas();
									atlas.atlasText = AssetDatabase.LoadAssetAtPath<TextAsset>(textureJsonPathKV[name]);
									atlas.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePathKV[name]);
									atlasList.Add(atlas);
								}
							}
						}
						editor.otherTextures = atlasList.ToArray();
					}
				}
			}
		}

		[MenuItem("Assets/Bone2D/DragonBone Default (SpriteFrame-SpriteMesh)",false,16)]
		[MenuItem("Bone2D/DragonBone Default (SpriteFrame-SpriteMesh)",false,16)]
		static void CreateDragbonBoneByDir_SpriteFrame()
		{
			DragonBoneArmatureEditor editor = CreateDragonBoneByDir(Bone2DSetupEditor.DisplayType.Default);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}

		[MenuItem("Assets/Bone2D/DragonBone Default (Sprite-SpriteMesh)",false,17)]
		[MenuItem("Bone2D/DragonBone Default (Sprite-SpriteMesh)",false,17)]
		static void CreateDragbonBoneByDir_UnitySprite()
		{
			DragonBoneArmatureEditor editor = CreateDragonBoneByDir(Bone2DSetupEditor.DisplayType.SpriteRender);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}

		[MenuItem("Assets/Bone2D/DragonBone UGUI (UIFrame-UIMesh)",false,18)]
		[MenuItem("Bone2D/DragonBone UGUI (UIFrame-UIMesh)",false,18)]
		static void CreateDragbonBoneByDir_UIFrame()
		{
			DragonBoneArmatureEditor editor = CreateDragonBoneByDir(Bone2DSetupEditor.DisplayType.UGUIDefault);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}
	
		[MenuItem("Assets/Bone2D/DragonBone UGUI (Image-UIMesh)",false,19)]
		[MenuItem("Bone2D/DragonBone UGUI (Image-UIMesh)",false,19)]
		static void CreateDragbonBoneByDir_UIImage()
		{
			DragonBoneArmatureEditor editor = CreateDragonBoneByDir(Bone2DSetupEditor.DisplayType.UGUIImage);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}


		static DragonBoneArmatureEditor CreateDragonBoneByDir(Bone2DSetupEditor.DisplayType displayType){
			if(Selection.activeObject != null)
			{
				string dirPath = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
				if(File.Exists(dirPath)){
					dirPath = dirPath.Substring(0,dirPath.LastIndexOf("/"));
				}
				if(Directory.Exists(dirPath)){
					string animJsonPath=null;
					Dictionary<string,string> texturePathKV = new Dictionary<string, string>();
					Dictionary<string,string> textureJsonPathKV = new Dictionary<string, string>();
					foreach (string path in Directory.GetFiles(dirPath))
					{  
						if(path.LastIndexOf(".meta")==-1){
							if( System.IO.Path.GetExtension(path) == ".json" && (path.IndexOf("_tex")>-1 || path.IndexOf("texture")>-1) ){
								int start = path.LastIndexOf("/")+1;
								int end = path.LastIndexOf(".json");
								textureJsonPathKV[path.Substring(start,end-start)] = path;
								continue;
							}
							if( System.IO.Path.GetExtension(path) == ".png" && (path.IndexOf("_tex")>-1 || path.IndexOf("texture")>-1) ){
								int start = path.LastIndexOf("/")+1;
								int end = path.LastIndexOf(".png");
								texturePathKV[path.Substring(start,end-start)] = path;
								continue;
							}
							if ( System.IO.Path.GetExtension(path) == ".json" && (path.IndexOf("_ske")>-1 || path.IndexOf("texture.json")==-1)) {
								animJsonPath = path;
							}
						}
					} 
					if(!string.IsNullOrEmpty(animJsonPath) && texturePathKV.Count>0 && textureJsonPathKV.Count>0){
						DragonBoneArmatureEditor instance  = ScriptableObject.CreateInstance<DragonBoneArmatureEditor>();
						instance.displayType = displayType;
						List<Atlas> atlasList = new List<Atlas>();
						foreach(string name in texturePathKV.Keys){
							if(textureJsonPathKV.ContainsKey(name)){
								if(instance.altasTexture==null){
									instance.altasTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textureJsonPathKV[name]);
									instance.altasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePathKV[name]);
								}else{
									Atlas atlas = new Atlas();
									atlas.atlasText = AssetDatabase.LoadAssetAtPath<TextAsset>(textureJsonPathKV[name]);
									atlas.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePathKV[name]);
									atlasList.Add(atlas);
								}
							}
						}
						instance.otherTextures = atlasList.ToArray();
						instance.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);
						if(instance.altasTexture&&instance.altasTextAsset&&instance.animTextAsset){
							return instance;
						}
					}
				}
			}
			return null;
		}

		public void SetAtlasTextureImporter(string atlasPath){
			TextureImporter textureImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
			textureImporter.maxTextureSize = 4096;
			AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);
		}


		public void OnWizardCreate(){
			textureScale = Mathf.Clamp(textureScale,0.1f,1f);
			try{
				Debug.ClearDeveloperConsole();
				if(animTextAsset==null || altasTexture==null || altasTextAsset==null){
					return;
				}
				if(altasTexture && altasTextAsset){
					SetAtlasTextureImporter(AssetDatabase.GetAssetPath(altasTexture));
					DragonBoneJsonParse.ParseTextureAtlas(this,altasTexture,altasTextAsset);
				}
				if(otherTextures!=null){
					foreach(Atlas atlas in otherTextures){
						SetAtlasTextureImporter(AssetDatabase.GetAssetPath(atlas.texture));
						DragonBoneJsonParse.ParseTextureAtlas(this,atlas.texture,atlas.atlasText);
					}
				}
				CreateTextureFramesAndMaterails();
				DragonBoneJsonParse.ParseAnimJsonData(this);
				DragonBoneShowArmature.SplitTextureToSprite();

				if(genPrefab){
					for(int i=0;i<m_prefabs.Count;++i){
						GameObject target = m_prefabs[i];
						var instanceRoot = PrefabUtility.FindRootGameObjectWithSameParentPrefab(target);
						var targetPrefab = UnityEditor.PrefabUtility.GetPrefabParent(instanceRoot);
						PrefabUtility.ReplacePrefab(
							instanceRoot,
							targetPrefab,
							ReplacePrefabOptions.ConnectToPrefab
						);
					}
				}
			}
			finally
			{
				DragonBoneAnimFile.Dispose();
				DragonBoneShowArmature.Dispose();

				bonesKV = null;
				slotsKV = null;
				slotsDataKV = null;
				spriteKV = null;


				atlasKV = null;
				bonePoseKV = null;
				ffdKV = null;

				spriteMeshUsedMatKV = null;
				armatureAnimList = null;
				
				m_prefabs = null;

				m_bones = null;
				m_slots = null;
				m_TextureFrames = null;
				m_sonArmature = null;

				DestroyImmediate(this);
				System.GC.Collect();
			}
		}

		void CreateTextureFramesAndMaterails(){
			//create TextureFrames
			string path = AssetDatabase.GetAssetPath(animTextAsset);
			path = path.Substring(0,path.LastIndexOf('/'));
			m_TextureFrames = AssetDatabase.LoadAssetAtPath<TextureFrames>(path+"/TextureFrames.asset");
			if(m_TextureFrames==null){
				m_TextureFrames = ScriptableObject.CreateInstance<TextureFrames>();
				AssetDatabase.CreateAsset(m_TextureFrames,path+"/TextureFrames.asset");
			}
			List<TextureFrame> frameList = new List<TextureFrame>();
			List<Material> mats=new List<Material>();
			if(altasTexture!=null && altasTextAsset!=null){
				string pathMat = AssetDatabase.GetAssetPath(altasTexture);
				pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+namePrefix+"_Mat.mat";
				Material mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
				if(!mat){
					mat = new Material(Shader.Find(shader));
					AssetDatabase.CreateAsset(mat,pathMat);
				}
				mat.mainTexture = altasTexture;
				mats.Add(mat);
				List<TextureFrame> frames = TextureFrames.ParseDragonBoneAtlasText(altasTextAsset.ToString().Replace('/','_'),mat);
				frameList.AddRange(frames);
				if(isUGUI){
					pathMat = AssetDatabase.GetAssetPath(altasTexture);
					pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+"_Mat.mat";
					mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
					if(mat){
						foreach(TextureFrame frame in frames){
							frame.material = mat;
						}
						mats.Add(mat);
					}
				}
				else
				{
					pathMat = AssetDatabase.GetAssetPath(altasTexture);
					pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+"_UI_Mat.mat";
					mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
					if(mat){
						foreach(TextureFrame frame in frames){
							frame.uiMaterial = mat;
						}
						mats.Add(mat);
					}
				}
			}
			if(otherTextures!=null && otherTextures.Length>0)
			{
				foreach(Atlas atlas in otherTextures){
					string pathMat = AssetDatabase.GetAssetPath(atlas.texture);
					pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+namePrefix+"_Mat.mat";;
					Material mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
					if(!mat){
						mat = new Material(Shader.Find(shader));
						AssetDatabase.CreateAsset(mat,pathMat);
					}
					mat.mainTexture = atlas.texture;
					mats.Add(mat);

					List<TextureFrame> frames = TextureFrames.ParseDragonBoneAtlasText(atlas.atlasText.ToString(),mat);
					frameList.AddRange(frames);
					if(isUGUI){
						pathMat = AssetDatabase.GetAssetPath(atlas.texture);
						pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+"_Mat.mat";
						mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
						if(mat){
							foreach(TextureFrame frame in frames){
								frame.material = mat;
							}
							mats.Add(mat);
						}
					}
					else
					{
						pathMat = AssetDatabase.GetAssetPath(atlas.texture);
						pathMat = pathMat.Substring(0,pathMat.LastIndexOf('.'))+"_UI_Mat.mat";
						mat = AssetDatabase.LoadAssetAtPath<Material>(pathMat);
						if(mat){
							foreach(TextureFrame frame in frames){
								frame.uiMaterial = mat;
							}
							mats.Add(mat);
						}
					}
				}
			}
			m_TextureFrames.frames = frameList.ToArray();
			m_TextureFrames.materials = mats.ToArray();
			AssetDatabase.Refresh();
			EditorUtility.SetDirty(m_TextureFrames);
			AssetDatabase.SaveAssets();

		}

		public int GetAnimIndex(string armatureName,string animName){
			if(armatureAnimList.ContainsKey(armatureName)){
				List<string> anims = armatureAnimList[armatureName];
				for(int i=0;i<anims.Count;++i){
					if(anims[i].Equals(animName)) return i;
				}
			}
			return -1;
		}
		public int GetCurrentArmatureAnimIndex(string animName){
			return GetAnimIndex(armature.name,animName);
		}

		//init
		public void InitShow(){
			DragonBoneShowArmature.AddBones(this);
			DragonBoneShowArmature.AddSlot(this);
			DragonBoneShowArmature.ShowBones(this);
			DragonBoneShowArmature.ShowSlots(this);
			DragonBoneShowArmature.ShowSkins(this);
			DragonBoneShowArmature.SetIKs(this);
			DragonBoneAnimFile.CreateAnimFile(this);

			Armature armature = _armature.GetComponent<Armature>();
			m_prefabs.Add(_armature.gameObject);
			armature.textureFrames = m_TextureFrames;

			//update slot display
			for(int s=0;s<m_slots.Count;++s){
				m_slots[s].displayIndex = m_slots[s].displayIndex;
			}

			if(armature.isUGUI)
			{
				UnityEngine.UI.MaskableGraphic[] renders = _armature.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>(true);
				armature.uiAttachments = renders;
				armature.attachments = new Renderer[0];
			}
			else
			{
				Renderer[] renders = _armature.GetComponentsInChildren<Renderer>(true);
				armature.attachments = renders;
				armature.uiAttachments = new UnityEngine.UI.MaskableGraphic[0];
			}
			armature.slots = m_slots.ToArray();
			armature.bones = m_bones.ToArray();
			armature.zSpace = zoffset;
			armature.sonArmatures = m_sonArmature.ToArray();
			if(armatureAnimList.ContainsKey(armature.name)){
				armature.anims = armatureAnimList[armature.name].ToArray();
			}
			armature.ResetSlotZOrder();

			string path = AssetDatabase.GetAssetPath(animTextAsset);
			path = path.Substring(0,path.LastIndexOf('/'))+"/"+_armature.name;


			//create pose data
			PoseData poseData = AssetDatabase.LoadAssetAtPath<PoseData>(path+"_Pose.asset");
			if(poseData==null){
				poseData = ScriptableObject.CreateInstance<PoseData>();
				AssetDatabase.CreateAsset(poseData,path+"_Pose.asset");
			}
			poseData.slotDatas = new PoseData.SlotData[armature.slots.Length];
			for(int i=0;i<armature.slots.Length;++i){
				poseData.slotDatas[i] = new PoseData.SlotData();
				poseData.slotDatas[i].color = armature.slots[i].color;
				poseData.slotDatas[i].displayIndex = armature.slots[i].displayIndex;
				poseData.slotDatas[i].zorder = armature.slots[i].z;
				armature.slots[i].SendMessage("UpdateSlotByInheritSlot",SendMessageOptions.DontRequireReceiver);
			}
			poseData.boneDatas = new PoseData.TransformData[armature.bones.Length];
			for(int i=0;i<armature.bones.Length;++i){
				poseData.boneDatas[i] = new PoseData.TransformData();
				poseData.boneDatas[i].x = armature.bones[i].localPosition.x;
				poseData.boneDatas[i].y = armature.bones[i].localPosition.y;
				poseData.boneDatas[i].sx = armature.bones[i].localScale.x;
				poseData.boneDatas[i].sy = armature.bones[i].localScale.y;
				poseData.boneDatas[i].rotation = armature.bones[i].localEulerAngles.z;
			}
			if(isUGUI){
				poseData.displayDatas = new PoseData.DisplayData[armature.uiAttachments.Length];
				for(int i=0;i<armature.uiAttachments.Length;++i){
					poseData.displayDatas[i] = new PoseData.DisplayData();
					UnityEngine.UI.MaskableGraphic render = armature.uiAttachments[i];

					UIFrame sf = render.GetComponent<UIFrame>();
					if(sf){
						poseData.displayDatas[i].type= PoseData.AttachmentType.IMG;
						poseData.displayDatas[i].color = sf.color;
					}
					else
					{
						UIMesh sm = render.GetComponent<UIMesh>();
						if(sm){
							poseData.displayDatas[i].type= PoseData.AttachmentType.MESH;
							poseData.displayDatas[i].color = sm.color;
							poseData.displayDatas[i].vertex = (Vector3[])sm.vertices.Clone();
							if(sm.weights==null||sm.weights.Length==0){
								for(int k = 0 ;k<poseData.displayDatas[i].vertex.Length;++k){
									poseData.displayDatas[i].vertex[k]/=100f;
								}
							}
						}
						else
						{
							UnityEngine.UI.Image sr = render.GetComponent<UnityEngine.UI.Image>();
							if(sr){
								poseData.displayDatas[i].type= PoseData.AttachmentType.IMG;
								poseData.displayDatas[i].color = sr.color;
							}
							else
							{
								poseData.displayDatas[i].type= PoseData.AttachmentType.BOX;
							}
						}
					}
					poseData.displayDatas[i].transform = new PoseData.TransformData();
					poseData.displayDatas[i].transform.x = render.transform.localPosition.x;
					poseData.displayDatas[i].transform.y = render.transform.localPosition.y;
					poseData.displayDatas[i].transform.sx = render.transform.localScale.x;
					poseData.displayDatas[i].transform.sy = render.transform.localScale.y;
					poseData.displayDatas[i].transform.rotation = render.transform.localEulerAngles.z;
					render.transform.localScale *= unit;
				}
			}
			else
			{
				poseData.displayDatas = new PoseData.DisplayData[armature.attachments.Length];
				for(int i=0;i<armature.attachments.Length;++i){
					poseData.displayDatas[i] = new PoseData.DisplayData();
					Renderer render = armature.attachments[i];

					SpriteFrame sf = render.GetComponent<SpriteFrame>();
					if(sf){
						poseData.displayDatas[i].type= PoseData.AttachmentType.IMG;
						poseData.displayDatas[i].color = sf.color;
					}
					else
					{
						SpriteMesh sm = render.GetComponent<SpriteMesh>();
						if(sm){
							poseData.displayDatas[i].type= PoseData.AttachmentType.MESH;
							poseData.displayDatas[i].color = sm.color;
							poseData.displayDatas[i].vertex = (Vector3[])sm.vertices.Clone();
						}
						else
						{
							SpriteRenderer sr = render.GetComponent<SpriteRenderer>();
							if(sr){
								poseData.displayDatas[i].type= PoseData.AttachmentType.IMG;
								poseData.displayDatas[i].color = sr.color;
							}
							else
							{
								poseData.displayDatas[i].type= PoseData.AttachmentType.BOX;
							}
						}
					}
					poseData.displayDatas[i].transform = new PoseData.TransformData();
					poseData.displayDatas[i].transform.x = render.transform.localPosition.x;
					poseData.displayDatas[i].transform.y = render.transform.localPosition.y;
					poseData.displayDatas[i].transform.sx = render.transform.localScale.x;
					poseData.displayDatas[i].transform.sy = render.transform.localScale.y;
					poseData.displayDatas[i].transform.rotation = render.transform.localEulerAngles.z;
				}
			}
			armature.poseData = poseData;
			AssetDatabase.Refresh();
			EditorUtility.SetDirty(poseData);
			AssetDatabase.SaveAssets();
			if(armature.textureFrames && armature.textureFrames.materials!=null && armature.textureFrames.materials.Length>0){
				armature.preMultiplyAlpha = armature.textureFrames.materials[0].GetFloat("_BlendSrc")==(int)UnityEngine.Rendering.BlendMode.One;
			}else{
				armature.preMultiplyAlpha = true;
			}

			if(armature.isUGUI){
				GameObject canvas = GameObject.Find("/Canvas");
				if(canvas){
					_armature.SetParent(canvas.transform);
					_armature.localScale = Vector3.one/unit;
					_armature.localPosition = Vector3.zero;
				}
			}

			if(genPrefab){
				string prefabPath = path+".prefab";
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
				if(!prefab){
					PrefabUtility.CreatePrefab(prefabPath,_armature.gameObject,ReplacePrefabOptions.ConnectToPrefab);
				}else{
					PrefabUtility.ReplacePrefab( _armature.gameObject,prefab,ReplacePrefabOptions.ConnectToPrefab);
				}
			}
		}
	}
}