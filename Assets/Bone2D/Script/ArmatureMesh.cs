using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bones2D
{
	[DisallowMultipleComponent]
	[ExecuteInEditMode,RequireComponent(typeof(Armature))]
	public class ArmatureMesh : MonoBehaviour {

		private Armature _unityArmature;
		public Armature unityArmature{
			get{ return _unityArmature; }
		}

		private Mesh _mesh;
		private MeshRenderer _meshRenderer;
		private MeshRenderer meshRenderer{
			get{
				if(_meshRenderer==null) {
					_meshRenderer = gameObject.GetComponent<MeshRenderer>();
					if(_meshRenderer==null) {
						_meshRenderer = gameObject.AddComponent<MeshRenderer>();
						#if UNITY_EDITOR
						UnityEditorInternal.ComponentUtility.MoveComponentDown (this);
						#endif
					}
				}
				return _meshRenderer;
			}
		}
		private MeshFilter _meshFilter;
		private MeshFilter meshFilter{
			get{
				if(_meshFilter==null) {
					_meshFilter = gameObject.GetComponent<MeshFilter>();
					if(_meshFilter==null) {
						_meshFilter = gameObject.AddComponent<MeshFilter>();
						#if UNITY_EDITOR
						UnityEditorInternal.ComponentUtility.MoveComponentDown (this);
						#endif
					}
				}
				return _meshFilter;
			}
		}

		void Start () {
			_unityArmature = GetComponent<Armature>();
			if(_unityArmature.isUGUI){
				Destroy(gameObject);
				return;
			}
			_mesh = new Mesh();
			_mesh.MarkDynamic();
			Init();
		}

		void Init(){
			if(_unityArmature!=null){
				DisplayEnable(_unityArmature,false);
			}
		}

		void DisplayEnable(Armature armature, bool flag){
			foreach(Slot slot in armature.slots){
				if(slot.childArmature!=null){
					DisplayEnable(slot.childArmature,flag);
				}else if(slot.currentDisplay){
					MeshRenderer mr = slot.currentDisplay.GetComponent<MeshRenderer>();
					if(mr) mr.enabled=flag;
				}
			}
		}

		void CollectMesh(Armature armature,List<List<CombineInstance>> combines,List<Material> mats){
			List<Slot> slots = armature.sortedSlots;
			foreach(Slot slot in slots){
				if(slot.currentDisplay){
					if(slot.childArmature!=null){
						CollectMesh(slot.childArmature,combines,mats);
						continue;
					}

					var meshRenderer = slot.currentDisplay.GetComponent<MeshRenderer>();
					if(meshRenderer!=null && meshRenderer.sharedMaterial){
						if(mats.Count==0 || mats[mats.Count-1] != meshRenderer.sharedMaterial )
						{
							mats.Add(meshRenderer.sharedMaterial);
						}
						if(combines.Count<mats.Count) {
							combines.Add(new List<CombineInstance>());
						}
					}else{
						continue;
					}
					var meshFilter = slot.currentDisplay.GetComponent<MeshFilter>();
					if(meshFilter && meshFilter.sharedMesh){
						CombineInstance com = new CombineInstance();
						com.mesh = meshFilter.sharedMesh;
						com.transform = transform.worldToLocalMatrix * meshRenderer.transform.localToWorldMatrix;
						combines[mats.Count-1].Add(com);
					}
				}
			}
		}

	
		void LateUpdate () {
			if(_unityArmature ==null || _mesh==null) return;

			#if UNITY_EDITOR
			Init();
			#endif

			_mesh.Clear();

			List<Material> mats = new List<Material>();
			List<List<CombineInstance>> combines =  new List<List<CombineInstance>>();
			CollectMesh(_unityArmature,combines,mats);
			int len = mats.Count;
			if(len>1){
				CombineInstance[] newCombines = new CombineInstance[len];
				for(int i=0;i<len;++i){
					Mesh mesh = new Mesh();
					mesh.CombineMeshes(combines[i].ToArray(),true,true);

					CombineInstance combine = new CombineInstance();
					combine.mesh = mesh;
					newCombines[i] = combine;
				}
				_mesh.CombineMeshes(newCombines,false,false);
			}else if(len==1){
				_mesh.CombineMeshes(combines[0].ToArray());
			}else{
				meshFilter.sharedMesh = _mesh;
				return;
			}
			_mesh.RecalculateBounds();
			meshFilter.sharedMesh = _mesh;
			meshRenderer.sharedMaterials = mats.ToArray();
			if(!_unityArmature.isUGUI){
				meshRenderer.sortingLayerName = _unityArmature.sortingLayerName;
				meshRenderer.sortingOrder = _unityArmature.sortingOrder;
			}
		}

		#if UNITY_EDITOR
		internal void Remove(){
			if(_meshFilter) {
				DestroyImmediate(_meshFilter.sharedMesh);
				DestroyImmediate(_meshFilter);
			}
			if(_meshRenderer) {
				_meshRenderer.sharedMaterials=new Material[0];
				DestroyImmediate(_meshRenderer);
			}
			if(_unityArmature!=null)
				DisplayEnable(_unityArmature,true);
			DestroyImmediate(this);
		}
		#endif
	}

}



#if UNITY_EDITOR
namespace Bones2D
{
	[CustomEditor(typeof(ArmatureMesh))]
	class ArmatureMeshEditor:Editor
	{
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();
			if(!Application.isPlaying){
				ArmatureMesh am = target as ArmatureMesh;
				if (am.unityArmature!=null && am.unityArmature!=null &&
					am.unityArmature.parentArmature==null && GUILayout.Button("Remove Armature Mesh",GUILayout.Height(20)))
				{
					am.Remove();
					GUIUtility.ExitGUI();
				}
			}
		}
	}
}
#endif