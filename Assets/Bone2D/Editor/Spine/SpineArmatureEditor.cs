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
	public class SpineArmatureEditor : ScriptableWizard {
		[System.Serializable]
		public class Atlas{
			public Texture2D texture;
			public string atlasText;
		}

		[Header("Setting")]
		public float zoffset = 0.002f;
		public Bone2DSetupEditor.DisplayType displayType = Bone2DSetupEditor.DisplayType.Default;
		[Tooltip("UI and Sprite with the same animation file.")]
		public bool genericAnim = true;

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
		public string altasTextAsset;
		public Atlas[] otherTextures;

		private SpineData.ArmatureData _armatureData;
		public SpineData.ArmatureData armatureData{ 
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
			get{ return 0.01f; }
		}
		public string namePrefix{
			get{return isUGUI? "_UI":"";}
		}
		public string shader{
			get{return isUGUI? "Bone2D/Simple UI":"Bone2D/Simple";}
		}

		public Dictionary<string,Transform> bonesKV = new Dictionary<string, Transform>();
		public Dictionary<string,SpineData.BoneData> bonesDataKV = new Dictionary<string, SpineData.BoneData>();
		public Dictionary<string,Transform> slotsKV = new Dictionary<string, Transform>();
		public Dictionary<string,SpineData.SlotData> slotsDataKV = new Dictionary<string, SpineData.SlotData>();
		public Dictionary<string,Sprite> spriteKV = new Dictionary<string, Sprite>();//single sprite

		public Dictionary<string , Atlas> atlasKV = new Dictionary<string, Atlas>();
		public Dictionary<string,bool> ffdKV = new Dictionary<string, bool>();//skinnedMesh animation or ffd animation, key is skin name/texture name

		public Dictionary<Material,bool> spriteMeshUsedMatKV = new Dictionary<Material, bool>();

		public Dictionary<string,BoneMatrix2D> bonePoseKV;

		protected internal List<string> animList = new List<string>(){};
		protected internal List<Transform> bones = new List<Transform>();
		protected internal List<Slot> slots = new List<Slot>();
		protected internal TextureFrames m_TextureFrames ;

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

		[MenuItem("Assets/Bone2D/##Spine Panel (All Function)",false,64)]
		[MenuItem("Bone2D/##Spine Panel (All Function)",false,64)]
		static void CreateWizard () {
			SpineArmatureEditor editor = ScriptableWizard.DisplayWizard<SpineArmatureEditor>("Create Spine", "Create");
			editor.minSize = new Vector2(200,500);
			if(Selection.activeObject != null)
			{
				string dirPath = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
				if(File.Exists(dirPath)){
					dirPath = dirPath.Substring(0,dirPath.LastIndexOf("/"));
				}
				if(Directory.Exists(dirPath)){
					string animJsonPath=null,textureFilePath=null;
					List<string> texturePaths = new List<string>();
					foreach (string path in Directory.GetFiles(dirPath))  
					{  
						if(path.IndexOf(".atlas")>-1 && path.LastIndexOf(".meta")==-1 ){
							textureFilePath = path;
							continue;
						}
						if(path.IndexOf(".png")>-1 && path.LastIndexOf(".meta")==-1){
							texturePaths.Add(path);
							continue;
						}
						if (path.IndexOf(".json")>-1 && path.LastIndexOf(".meta")==-1)  
						{  
							animJsonPath = path;
						}
					} 
					string texturePath = null;
					if(texturePaths.Count>0) texturePath = texturePaths[0];
					if(!string.IsNullOrEmpty(animJsonPath) && !string.IsNullOrEmpty(texturePath) && !string.IsNullOrEmpty(textureFilePath)){
						editor.altasTextAsset = LoadAtlas( Application.dataPath + "/" + textureFilePath.Substring(6) );
						editor.altasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
						editor.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);

						if(texturePaths.Count>1) editor.otherTextures = new Atlas[texturePaths.Count-1];
						for(int i=1;i<texturePaths.Count;++i){
							Atlas atlas = new Atlas();
							atlas.atlasText = editor.altasTextAsset;
							atlas.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[i]);
							editor.otherTextures[i-1] = atlas;
						}
					}
				}
			}
		}

		static string LoadAtlas(string filePath) {
			if (File.Exists(filePath))     {
				return File.ReadAllText(filePath);
			}
			return null;
		}

		[MenuItem("Assets/Bone2D/Spine Default (SpriteFrame-SpriteMesh)",false,65)]
		[MenuItem("Bone2D/Spine Default (SpriteFrame-SpriteMesh)",false,65)]
		static void CreateSpineByDir_SpriteFrame()
		{
			SpineArmatureEditor editor = CreateSpineByDir(Bone2DSetupEditor.DisplayType.Default);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}
		[MenuItem("Assets/Bone2D/Spine Default (Sprite-SpriteMesh)",false,66)]
		[MenuItem("Bone2D/Spine Default (Sprite-SpriteMesh)",false,66)]
		static void CreateSpineByDir_UnitySprite()
		{
			SpineArmatureEditor editor = CreateSpineByDir(Bone2DSetupEditor.DisplayType.SpriteRender);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}

		[MenuItem("Assets/Bone2D/Spine UGUI (UIFrame-UIMesh)",false,67)]
		[MenuItem("Bone2D/Spine UGUI (UIFrame-UIMesh)",false,67)]
		static void CreateSpineByDir_UIFrame()
		{
			SpineArmatureEditor editor = CreateSpineByDir(Bone2DSetupEditor.DisplayType.UGUIDefault);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}
		[MenuItem("Assets/Bone2D/Spine UGUI (Image-UIMesh)",false,68)]
		[MenuItem("Bone2D/Spine UGUI (Image-UIMesh)",false,68)]
		static void CreateSpineByDir_UnityImage()
		{
			SpineArmatureEditor editor = CreateSpineByDir(Bone2DSetupEditor.DisplayType.UGUIImage);
			if(editor){
				editor.OnWizardCreate();
				DestroyImmediate(editor);
			}
		}

		static SpineArmatureEditor CreateSpineByDir(Bone2DSetupEditor.DisplayType displayType){
			if(Selection.activeObject != null)
			{
				string dirPath = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
				if(File.Exists(dirPath)){
					dirPath = dirPath.Substring(0,dirPath.LastIndexOf("/"));
				}
				if(Directory.Exists(dirPath)){
					string animJsonPath=null,textureFilePath=null;
					List<string> texturePaths = new List<string>();
					foreach (string path in Directory.GetFiles(dirPath))  
					{  
						if(path.IndexOf(".atlas")>-1 && path.LastIndexOf(".meta")==-1 ){
							textureFilePath = path;
							continue;
						}
						if(path.IndexOf(".png")>-1 && path.LastIndexOf(".meta")==-1){
							texturePaths.Add(path);
							continue;
						}
						if (path.IndexOf(".json")>-1 && path.LastIndexOf(".meta")==-1)  
						{  
							animJsonPath = path;
						}
					} 
					string texturePath = null;
					if(texturePaths.Count>0) texturePath = texturePaths[0];
					if(!string.IsNullOrEmpty(animJsonPath) && !string.IsNullOrEmpty(texturePath) && !string.IsNullOrEmpty(textureFilePath)){
						SpineArmatureEditor instance  = ScriptableObject.CreateInstance<SpineArmatureEditor>();
						instance.displayType = displayType;
						instance.altasTextAsset = LoadAtlas( Application.dataPath + "/" + textureFilePath.Substring(6) );
						instance.altasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
						instance.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);

						if(texturePaths.Count>1) instance.otherTextures = new Atlas[texturePaths.Count-1];
						for(int i=1;i<texturePaths.Count;++i){
							Atlas atlas = new Atlas();
							atlas.atlasText = instance.altasTextAsset;
							atlas.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[i]);
							instance.otherTextures[i-1] = atlas;
						}
						if(instance.altasTexture && !string.IsNullOrEmpty(instance.altasTextAsset) && instance.animTextAsset){
							return instance;
						}
					}
					else if(displayType == Bone2DSetupEditor.DisplayType.SpriteRender && !string.IsNullOrEmpty(animJsonPath))
					{
						string spritesPath = null;
						foreach (string path in Directory.GetDirectories(dirPath))  
						{  
							if(path.LastIndexOf("texture")>-1){
								spritesPath = path;
								break;
							}
						}
						if(!string.IsNullOrEmpty(spritesPath)){

							Dictionary<string,Sprite> spriteKV = new Dictionary<string, Sprite>();
							foreach (string path in Directory.GetFiles(spritesPath))  
							{  
								if(path.LastIndexOf(".png")>-1 && path.LastIndexOf(".meta")==-1 ){
									Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
									spriteKV[sprite.name]=sprite;
								}
							}
							if(spriteKV.Count>0){
								SpineArmatureEditor instance  = ScriptableObject.CreateInstance<SpineArmatureEditor>();
								instance.displayType = displayType;
								instance.spriteKV = spriteKV;
								instance.animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(animJsonPath);
								return instance;
							}
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
			try{
				Debug.ClearDeveloperConsole();
				if(animTextAsset==null || altasTexture==null || altasTextAsset==null){
					return;
				}
				if(altasTexture && !string.IsNullOrEmpty(altasTextAsset)){
					SetAtlasTextureImporter(AssetDatabase.GetAssetPath(altasTexture));
					SpineJsonParse.ParseTextureAtlas(this,altasTexture,altasTextAsset);
				}
				if(otherTextures!=null){
					foreach(Atlas atlas in otherTextures){
						SetAtlasTextureImporter(AssetDatabase.GetAssetPath(atlas.texture));
						SpineJsonParse.ParseTextureAtlas(this,atlas.texture,atlas.atlasText);
					}
				}
				CreateTextureFramesAndMaterails();
				SpineJsonParse.ParseAnimJsonData(this);
			}finally{
				SpineAnimFile.Dispose();
				SpineShowArmature.Dispose();

				bonesKV = null;
				slotsKV = null;
				slotsDataKV = null;
				spriteKV = null;


				atlasKV = null;
				ffdKV = null;

				spriteMeshUsedMatKV = null;
				animList = null;
				bonePoseKV = null;

				bones = null;
				slots = null;
				m_TextureFrames = null;

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
				List<TextureFrame> frames = TextureFrames.ParseSpineAtlasText(altasTextAsset.ToString().Replace("/","_"),mat);
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

					List<TextureFrame> frames = TextureFrames.ParseSpineAtlasText(atlas.atlasText.ToString().Replace("/","_"),mat);
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

		//init
		public void InitShow(){
			SpineShowArmature.AddBones(this);
			SpineShowArmature.AddSlot(this);
			SpineShowArmature.ShowBones(this);
			SpineShowArmature.ShowSlots(this);
			SpineShowArmature.ShowSkin(this);
			SpineShowArmature.SetIKs(this);
			SpineAnimFile.CreateAnimFile(this);

			Armature armature = _armature.GetComponent<Armature>();
			armature.textureFrames = m_TextureFrames;
			//update slot display
			for(int s=0;s<slots.Count;++s){
				slots[s].displayIndex = slots[s].displayIndex;
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
			armature.slots = slots.ToArray();
			armature.bones = bones.ToArray();
			armature.anims = animList.ToArray();
			armature.zSpace = zoffset;
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

		public static Color HexToColor(string hexColor)
		{
			hexColor = hexColor.Replace("#","");
			int red = System.Convert.ToInt32(hexColor.Substring(0,2), 16);
			int green = System.Convert.ToInt32(hexColor.Substring(2,2), 16);
			int blue = System.Convert.ToInt32(hexColor.Substring(4,2), 16);
			int alpha = System.Convert.ToInt32(hexColor.Substring(6,2), 16);
			return new Color(red/255f,green/255f,blue/255f,alpha/255f);
		}
	}
}