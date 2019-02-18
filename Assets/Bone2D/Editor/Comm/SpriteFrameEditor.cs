using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;

namespace Bones2D
{
	/// <summary>
	/// Sprite frame editor.
	/// author:bingheliefeng
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SpriteFrame))]
	public class SpriteFrameEditor : Editor {
		string[] sortingLayerNames;
		int selectedOption;
		private int m_frameIndex;
		private int m_pivotIndex;
		private string[] posList;

		void OnEnable () {
			posList = new string[]{
				"_","CENTER","TOP","TOP_LEFT","TOP_RIGHT",
				"LEFT","RIGHT","BOTTOM","BOTTOM_LEFT","BOTTOM_RIGHT"
			};
			SpriteFrame sprite = target as SpriteFrame;
			sprite.meshFilter.hideFlags =  HideFlags.HideInInspector;

			sortingLayerNames = GetSortingLayerNames();
			selectedOption = GetSortingLayerIndex(sprite.sortingLayerName);

			if(sprite && sprite.textureFrames!=null && sprite.textureFrames.frames.Length>0){
				if(!string.IsNullOrEmpty(sprite.frameName)){
					for(int i=0;i<sprite.textureFrames.frames.Length;++i){
						if(sprite.frameName == sprite.textureFrames.frames[i].name){
							m_frameIndex = i;
							break;
						}
					}
				}
			}
		}

		public override void OnInspectorGUI(){
			SpriteFrame sprite = target as SpriteFrame;

			serializedObject.Update();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("textureFrames"), true);
			if(sprite.textureFrames!=null && sprite.textureFrames.frames.Length>0){
				//显示frameName列表
				string[] list = new string[sprite.textureFrames.frames.Length];
				for(int i=0;i<sprite.textureFrames.frames.Length;++i){
					list[i] = sprite.textureFrames.frames[i].name;
				}
				int selectIndex = EditorGUILayout.Popup("Frame",m_frameIndex,list);
				if(selectIndex!=m_frameIndex){
					m_frameIndex = selectIndex;
					sprite.CreateQuad();
					sprite.frameName = sprite.textureFrames.frames[m_frameIndex].name;
					UpdatePivot(sprite,m_pivotIndex);
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_skew"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_uvOffset"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_pivot"), true);
			if(sprite.textureFrames!=null && !string.IsNullOrEmpty(sprite.frameName)){
				//			"None","CENTER","TOP","TOP_LEFT","TOP_RIGHT",
				//			"LEFT","RIGHT","BOTTOM","BOTTOM_LEFT","BOTTOM_RIGHT"
				int selectPivot = EditorGUILayout.Popup("Auto Pivot",m_pivotIndex,posList);
				if(selectPivot!=m_pivotIndex){
					UpdatePivot(sprite,selectPivot);
					sprite.frameName = sprite.frameName;
				}
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_brightness"), true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PreMultiplyAlpha"), true);
			selectedOption = EditorGUILayout.Popup("Sorting Layer", selectedOption, sortingLayerNames);
			if (sortingLayerNames[selectedOption] != sprite.sortingLayerName)
			{
				Undo.RecordObject(sprite, "Sorting Layer");
				sprite.sortingLayerName = sortingLayerNames[selectedOption];
				EditorUtility.SetDirty(sprite);
				if (!Application.isPlaying &&!string.IsNullOrEmpty(sprite.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}
			int newSortingLayerOrder = EditorGUILayout.IntField("Order in Layer", sprite.sortingOrder);
			if (newSortingLayerOrder != sprite.sortingOrder)
			{
				Undo.RecordObject(sprite, "Edit Sorting Order");
				sprite.sortingOrder = newSortingLayerOrder;
				EditorUtility.SetDirty(sprite);
				if (!Application.isPlaying &&!string.IsNullOrEmpty(sprite.gameObject.scene.name)){
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}
			}

			serializedObject.ApplyModifiedProperties();
		}


		void UpdatePivot( SpriteFrame sprite, int selectPivot){
			m_pivotIndex = selectPivot;
			sprite.CreateQuad();
			switch(selectPivot){
			case 1:
				sprite.pivot = new Vector2(0.5f,0.5f);
				break;
			case 2:
				sprite.pivot = new Vector2(0.5f,1f);
				break;
			case 3:
				sprite.pivot = new Vector2(0f,1f);
				break;
			case 4:
				sprite.pivot = new Vector2(1f,1f);
				break;
			case 5:
				sprite.pivot = new Vector2(0f,0.5f);
				break;
			case 6:
				sprite.pivot = new Vector2(1f,0.5f);
				break;
			case 7:
				sprite.pivot = new Vector2(0.5f,0f);
				break;
			case 8:
				sprite.pivot = new Vector2(0f,0f);
				break;
			case 9:
				sprite.pivot = new Vector2(1f,0f);
				break;
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



		#region create sprite frame
		[MenuItem("Assets/Bone2D/Create SpriteFrame",true)]
		[MenuItem("Bone2D/Create SpriteFrame",true)]
		static bool ValidateCreateSpriteFrame(){
			if(Selection.activeObject is TextureFrames){
				return true;
			}
			return false;
		}

		[MenuItem("Assets/Bone2D/Create SpriteFrame")]
		[MenuItem("Bone2D/Create SpriteFrame")]
		static void CreateSpriteFrame(){
			if(Selection.activeObject is TextureFrames){
				TextureFrames tfs = AssetDatabase.LoadAssetAtPath<TextureFrames>(AssetDatabase.GetAssetPath(Selection.activeObject));
				if(tfs&&tfs.frames!=null && tfs.frames.Length>0){
					GameObject go = new GameObject();
					SpriteFrame sf = go.AddComponent<SpriteFrame>();
					sf.CreateQuad();
					sf.textureFrames = tfs;
					foreach(Material mat in tfs.materials){
						if(mat.name.LastIndexOf("_UI_Mat")==-1){
							sf.material = tfs.materials[0];
							break;
						}
					}
				}
			}
		}
		#endregion
	}

}