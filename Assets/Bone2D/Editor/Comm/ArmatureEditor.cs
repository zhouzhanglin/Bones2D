using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.IO;

namespace Bones2D
{
	[InitializeOnLoad]
	[CustomEditor(typeof(Armature))]
	public class ArmatureEditor : Editor {
		
		#region Show Hierarchy Icons
		static Texture2D textureBone,textureSlot,textureArmature,textureImg,textureMesh;
		static string editorPath="";
		static string editorGUIPath=  "";
		static bool isInited = false;
		static ArmatureEditor(){
			#if !UNITY_5_5_OR_NEWER
			Initialize();
			#endif
		}
		static void Initialize(){
			if(isInited) return;

			DirectoryInfo rootDir = new DirectoryInfo(Application.dataPath);
			FileInfo[] files = rootDir.GetFiles("ArmatureEditor.cs", SearchOption.AllDirectories);
			editorPath = Path.GetDirectoryName(files[0].FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets"));
			editorGUIPath = editorPath.Substring(0,editorPath.LastIndexOf("Comm")) + "GUI";

			textureBone = AssetDatabase.LoadAssetAtPath<Texture2D>(editorGUIPath+"/icon-bone.png");
			textureSlot = AssetDatabase.LoadAssetAtPath<Texture2D>(editorGUIPath+"/icon-slot.png");
			textureArmature = AssetDatabase.LoadAssetAtPath<Texture2D>(editorGUIPath+"/icon-skeleton.png");
			textureImg = AssetDatabase.LoadAssetAtPath<Texture2D>(editorGUIPath+"/icon-image.png");
			textureMesh = AssetDatabase.LoadAssetAtPath<Texture2D>(editorGUIPath+"/icon-mesh.png");

			EditorApplication.hierarchyWindowItemOnGUI -= HierarchyIconsOnGUI;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyIconsOnGUI;
			isInited = true;
		}

		static Armature _armature;
		static void HierarchyIconsOnGUI (int instanceId, Rect selectionRect) {
			Rect rect = new Rect(selectionRect.x-25f, selectionRect.y, 16f, 16f);
			GameObject armatureGo = (GameObject)EditorUtility.InstanceIDToObject(instanceId);
			if(armatureGo && armatureGo.GetComponent<Armature>()!=null && textureArmature ){
				rect.x =  selectionRect.x+ selectionRect.width - 16f;
				GUI.Label(rect,textureArmature);
			}


			GameObject go = Selection.activeGameObject;
			if(go){
				_armature = go.GetComponentInParent<Armature>();
				if(_armature==null) return;

			}else if(_armature==null){
				return;
			}
			if(_armature.bones!=null && textureBone!=null){
				foreach(Transform bone in _armature.bones){
					if(bone && EditorUtility.InstanceIDToObject(instanceId)==bone.gameObject){
						GUI.Label(rect,textureBone);
						break;
					}
				}
			}
			if(_armature.slots!=null && textureSlot!=null){
				foreach(Slot slot in _armature.slots){
					GameObject slotGo = (GameObject)EditorUtility.InstanceIDToObject(instanceId);
					if(slot && slot.GetComponent<Armature>()==null){
						if(slotGo==slot.gameObject){
							GUI.Label(rect,textureSlot);
							break;
						}else if(slot.inheritSlot && slot.inheritSlot.gameObject==slotGo){
							rect.x+=10f;
							rect.y+=2f;
							GUI.Label(rect,textureSlot);
							break;
						}
					}
				}
			}
			if(_armature.uiAttachments!=null && textureImg && textureMesh){
				foreach(UnityEngine.UI.MaskableGraphic g in _armature.uiAttachments){
					if(g && EditorUtility.InstanceIDToObject(instanceId)==g.gameObject){
						rect.x += 8f;
						if(g.transform.childCount>0) rect.x -= 8;
						if(g.GetComponent<UIFrame>() || g.GetComponent<UnityEngine.UI.Image>()){
							GUI.Label(rect,textureImg);
						}else{
							GUI.Label(rect,textureMesh);
						}
						break;
					}
				}
			}

			if(_armature.attachments!=null && textureImg && textureMesh){
				foreach(Renderer r in _armature.attachments){
					if(r && EditorUtility.InstanceIDToObject(instanceId)==r.gameObject){
						rect.x += 8f;
						if(r.transform.childCount>0) rect.x -= 8;
						if(r.GetComponent<SpriteFrame>() || r.GetComponent<SpriteRenderer>()){
							GUI.Label(rect,textureImg);
						}else{
							GUI.Label(rect,textureMesh);
						}
						break;
					}
				}
			}
		}
		#endregion



		string[] sortingLayerNames;
		int selectedOption;
		bool flipX,flipY;
		bool preMultiplyAlpha ;
		float zspace;
		Armature armature ;
		Armature.SortType sortType;

		void OnEnable(){
			armature = target as Armature;
			if(armature==null) return;
			sortingLayerNames = GetSortingLayerNames();
			selectedOption = GetSortingLayerIndex(armature.sortingLayerName);
			flipX = armature.flipX;
			flipY = armature.flipY;
			zspace = armature.zSpace;
			sortType = armature.sortType;

			if(armature.textureFrames && armature.textureFrames.materials!=null && armature.textureFrames.materials.Length>0 && armature.textureFrames.materials[0]!=null){
				preMultiplyAlpha = armature.textureFrames.materials[0].GetFloat("_BlendSrc")==(int)UnityEngine.Rendering.BlendMode.One;
			}
			armature.preMultiplyAlpha = preMultiplyAlpha;

			Initialize();
		}

		public override void OnInspectorGUI(){
			Armature armature = target as Armature;
			if(armature==null) return;

			bool haveGroup = false;
			#if UNITY_5_6_OR_NEWER
			haveGroup = armature.sortingGroup!=null;
			#endif

			if(!armature.isUGUI && !haveGroup){
				selectedOption = EditorGUILayout.Popup("Sorting Layer", selectedOption, sortingLayerNames);
				if (sortingLayerNames[selectedOption] != armature.sortingLayerName)
				{
					Undo.RecordObject(armature, "Sorting Layer");
					armature.sortingLayerName = sortingLayerNames[selectedOption];
					EditorUtility.SetDirty(armature);
				}

				int newSortingLayerOrder = EditorGUILayout.IntField("Order in Layer", armature.sortingOrder);
				if (newSortingLayerOrder != armature.sortingOrder)
				{
					Undo.RecordObject(armature, "Edit Sorting Order");
					armature.sortingOrder = newSortingLayerOrder;
					EditorUtility.SetDirty(armature);
				}

				if(GUILayout.Button("Update All Sorting",GUILayout.Height(20))){
					armature.sortingLayerName = armature.sortingLayerName;
					armature.sortingOrder = armature.sortingOrder;
					EditorUtility.SetDirty(armature);

					foreach(Renderer render in armature.GetComponentsInChildren<Renderer>(true)){
						render.sortingLayerName = armature.sortingLayerName;
						render.sortingOrder = armature.sortingOrder;
						EditorUtility.SetDirty(render);

						SpriteFrame sf = render.GetComponent<SpriteFrame>();
						if(sf) {
							sf.sortingLayerName = armature.sortingLayerName;
							sf.sortingOrder = armature.sortingOrder;
							UnityEditor.EditorUtility.SetDirty(sf);
						}
						else {
							SpriteMesh sm = render.GetComponent<SpriteMesh>();
							if(sm) {
								sm.sortingLayerName = armature.sortingLayerName;
								sm.sortingOrder = armature.sortingOrder;
								UnityEditor.EditorUtility.SetDirty(sm);
							}
						}
					}

					foreach(Armature sonArmature in armature.GetComponentsInChildren<Armature>(true)){
						sonArmature.sortingLayerName = sonArmature.sortingLayerName;
						sonArmature.sortingOrder = sonArmature.sortingOrder;
						EditorUtility.SetDirty(sonArmature);
					}

					if (!string.IsNullOrEmpty(armature.gameObject.scene.name)){
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
					}
				}
				EditorGUILayout.Space();
			}

			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("color"), true);
			if (!Application.isPlaying) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipX"), true);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipY"), true);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PreMultiplyAlpha"), true);

			if(!Application.isPlaying){
				if(!armature.isUGUI){
					#if UNITY_5_6_OR_NEWER
					EditorGUILayout.PropertyField(serializedObject.FindProperty("sortType"), true);
					#else
					armature.sortType = Armature.SortType.ZSpace;
					#endif
					EditorGUILayout.PropertyField(serializedObject.FindProperty("zSpace"), true);

					if(sortType!=armature.sortType){
						sortType = armature.sortType;
						armature.sortingOrder = armature.sortingOrder;
					}
				}
			}
			if(armature.anims!=null && armature.anims.Length>0){
				int temp = (int)armature.animIndex;
				System.Collections.Generic.List<string> animsList =new System.Collections.Generic.List<string>(armature.anims);
				animsList.Insert(0,"<None>");
				armature.animIndex = EditorGUILayout.Popup("Current Animation", temp+1, animsList.ToArray())-1;
				if(armature.animIndex!=temp && !Application.isPlaying){
					UnityEditor.EditorUtility.SetDirty(armature);
					if (!Application.isPlaying && !string.IsNullOrEmpty(armature.gameObject.scene.name)){
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
					}
				}
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("anims"), true);
			if(armature.skins!=null && armature.skins.Length>1){
				int temp = armature.skinIndex;
				armature.skinIndex = EditorGUILayout.Popup("Skins", armature.skinIndex, armature.skins);
				if(temp!=armature.skinIndex  && !Application.isPlaying){
					UnityEditor.EditorUtility.SetDirty(armature);
					if (!string.IsNullOrEmpty(armature.gameObject.scene.name)){
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
					}
				}
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("slots"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("bones"), true);
			if(armature.isUGUI){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("uiAttachments"), true);
			}else{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("attachments"), true);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("sonArmatures"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("parentArmature"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("textureFrames"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("poseData"), true);
			serializedObject.ApplyModifiedProperties();

			if(!Application.isPlaying && armature.flipX!=flipX){
				armature.flipX = armature.flipX;
				flipX = armature.flipX;
				if (!string.IsNullOrEmpty(armature.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}
			if(!Application.isPlaying && armature.flipY!=flipY){
				armature.flipY = armature.flipY;
				flipY = armature.flipY;
				if (!string.IsNullOrEmpty(armature.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}
			if(!Application.isPlaying && armature.zSpace!=zspace){
				zspace = armature.zSpace;
				armature.ResetSlotZOrder();
				if (!string.IsNullOrEmpty(armature.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}

			if(armature.preMultiplyAlpha!=preMultiplyAlpha){
				preMultiplyAlpha = armature.preMultiplyAlpha;
				armature.preMultiplyAlpha = preMultiplyAlpha;
				if (!Application.isPlaying &&!string.IsNullOrEmpty(armature.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}
			if(!Application.isPlaying && !armature.isUGUI && armature.parentArmature==null){
				ArmatureMesh am = armature.gameObject.GetComponent<ArmatureMesh>();
				if(!am) {
					if(GUILayout.Button("Add Armature Mesh",GUILayout.Height(20))){
						am = armature.gameObject.AddComponent<ArmatureMesh>();
					}
				}
			}
			GUILayout.Space (5);
			if(GUILayout.Button("Set To Pose",GUILayout.Height(20))){
				armature.SetToPose ();
			}
		}

		public string[] GetSortingLayerNames() {
			System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			return (string[])sortingLayersProperty.GetValue(null, new object[0]);
		}
		public int[] GetSortingLayerUniqueIDs()
		{
			System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
			return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
		}
		int GetSortingLayerIndex(string layerName){
			for(int i = 0; i < sortingLayerNames.Length; ++i){  
				if(sortingLayerNames[i] == layerName) return i;  
			}  
			return 0;  
		}

		#region Editor Util

		[MenuItem("Assets/Bone2D/AnimatorController No Transition",true)]
		[MenuItem("Bone2D/AnimatorController No Transition",true)]
		static bool ValidateSetTransition(){
			if(Selection.activeObject is UnityEditor.Animations.AnimatorController){
				return true;
			}
			return false;
		}
		[MenuItem("Assets/Bone2D/AnimatorController No Transition",false,128)]
		[MenuItem("Bone2D/AnimatorController No Transition",false,128)]
		static void SetTransition(){
			if(Selection.activeObject is UnityEditor.Animations.AnimatorController){
				UnityEditor.Animations.AnimatorController ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(AssetDatabase.GetAssetPath(Selection.activeObject));
				foreach(UnityEditor.Animations.ChildAnimatorState cas in ac.layers[0].stateMachine.states){
					foreach(UnityEditor.Animations.AnimatorStateTransition ast in cas.state.transitions){
						ast.exitTime = 0.99f;
						ast.duration = 0f;
					}
				}
			}
		}

		[MenuItem("Bone2D/Update Sprites",true)]
		static bool ValidateUpdateSprites(){
			if(Selection.activeObject is GameObject){
				return (Selection.activeObject as GameObject).GetComponent<Armature>()!=null;
			}
			return false;
		}
		[MenuItem("Bone2D/Update Sprites",false,129)]
		static void UpdateSprites(){
			Armature armature = (Selection.activeObject as GameObject).GetComponent<Armature>();
			bool isSpriteRenderer = false;
			foreach(Renderer render in armature.attachments){
				if(render is SpriteRenderer){
					isSpriteRenderer=true;
					break;
				}
			}
			if(isSpriteRenderer){
				Texture t = null;
				Object[] objs = null;
				foreach(Renderer render in armature.attachments){
					if(render is SpriteRenderer){
						SpriteRenderer sr = render as SpriteRenderer;
						if(sr.sprite){
							if(t!=sr.sharedMaterial.mainTexture || objs==null){
								objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(sr.sharedMaterial.mainTexture));
								t = sr.sharedMaterial.mainTexture;
							}
							if(objs!=null){
								foreach(Object obj in objs){
									if(obj is Sprite && obj.name.Equals(sr.sprite.name)){
										sr.sprite = obj as Sprite;
										UnityEditor.EditorUtility.SetDirty(sr);
									}
								}
							}
						}
					}
				}
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				return;
			}

			bool isImage = false;
			foreach(UnityEngine.UI.MaskableGraphic render in armature.uiAttachments){
				if(render is UnityEngine.UI.Image){
					isImage=true;
					break;
				}
			}
			if(isImage){
				Material m = null;
				Object[] objs = null;
				foreach(UnityEngine.UI.MaskableGraphic render in armature.uiAttachments){
					if(render is UnityEngine.UI.Image){
						UnityEngine.UI.Image sr = render as UnityEngine.UI.Image;
						if(sr.sprite){
							if(m!=sr.material || objs==null){
								objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(sr.material.mainTexture));
								m = sr.material;
							}
							if(objs!=null){
								foreach(Object obj in objs){
									if(obj is Sprite && obj.name.Equals(sr.sprite.name)){
										sr.sprite = obj as Sprite;
									}
								}
							}
						}
					}
				}
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				return;
			}
		}


		[MenuItem("Assets/Bone2D/Correct Triangles",false,129)]
		[MenuItem("Bone2D/Correct Triangles",false,129)]
		static void UpdateTriangles(){
			Armature armature = (Selection.activeObject as GameObject).GetComponent<Armature>();

			foreach(SpriteMesh sm in armature.GetComponentsInChildren<SpriteMesh>(true)){
				if(ModifyTriangles(sm.triangles,sm.vertices)){
					sm.mesh.triangles = sm.triangles;
					if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sm);
				}
			}
			foreach(UIMesh um in armature.GetComponentsInChildren<UIMesh>(true)){
				if(ModifyTriangles(um.triangles,um.vertices)){
					um.mesh.triangles = um.triangles;
					if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(um);
				}
			}
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
		}

		[MenuItem("Bone2D/Correct Triangles",true)]
		static bool ValidateUpdateTriangles(){
			if(Selection.activeObject is GameObject){
				return (Selection.activeObject as GameObject).GetComponent<Armature>()!=null;
			}
			return false;
		}

		public static bool ModifyTriangles(int[] triangles,Vector3[] vertices)
		{
			bool flag = false;
			int len = triangles.Length;
			for(int i = 0;i<len ; i+=3){
				Vector3 v1 = vertices[triangles[i]];
				Vector3 v2 = vertices[triangles[i+1]];
				Vector3 v3 = vertices[triangles[i+2]];

				float dot = (v2.x-v1.x)*(v3.y-v2.y)-(v2.y-v1.y)*(v3.x-v2.x);
				if(dot>0){
					//逆时针
					var temp = triangles[i];
					triangles[i] = triangles[i+2];
					triangles[i+2] = temp;
					flag = true;
				}
			}
			return flag;
		}

		#endregion
	}
}