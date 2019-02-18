using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bones2D
{
	[CustomEditor(typeof(UIMesh))]
	public class UIMeshEditor : Editor {

		UIMesh um = null;

		void OnEnable(){
			um = target as UIMesh;
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();
			if (um.vertControlTrans != null && um.vertControlTrans.Length > 0) {
				foreach (Transform child in um.vertControlTrans) {
					if (um.isEdit) {
						if (child.gameObject.activeSelf) {
							break;
						} else {
							child.gameObject.SetActive (true);
						}
					} else {
						if (!child.gameObject.activeSelf) {
							break;
						} else {
							child.gameObject.SetActive (false);
						}
					}
				}
			}
			//Modify bindPose by hand
			if(um.bindposes!=null && um.bindposes.Length>0)
			{
				GUILayout.Space(20f);
				GUILayout.Label("Press shift to speed up");
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Left Move")){
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						m.m03 -= Event.current.shift ? 0.1f:0.01f;
						um.bindposes[i] = m;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				if(GUILayout.Button("BindPose Right Move")){
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						m.m03 += Event.current.shift ? 0.1f:0.01f;
						um.bindposes[i] = m;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Up Move")){
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						m.m13 += Event.current.shift ? 0.1f:0.01f;
						um.bindposes[i] = m;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				if(GUILayout.Button("BindPose Down Move")){
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						m.m13 -= Event.current.shift ? 0.1f:0.01f;
						um.bindposes[i] = m;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				GUILayout.EndHorizontal();


				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Rotate +")){
					
					Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0, Event.current.shift ? 1f:0.1f ),Vector3.one);
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						um.bindposes[i] = m*rotate;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				if(GUILayout.Button("BindPose Rotate -")){
					Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,-(Event.current.shift ? 1f:0.1f)),Vector3.one);
					for(int i = 0 ;i<um.bindposes.Length;++i){
						Matrix4x4 m = um.bindposes[i];
						um.bindposes[i] = m*rotate;
					}
					um.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(um);
					}
				}
				GUILayout.EndHorizontal();
			}
			if (!Application.isPlaying && um.isEdit) {
				GUILayout.Space (5);
				if (GUILayout.Button ("Save To Pose")) {
					Armature armature = um.GetComponentInParent<Armature> ();
					if (armature && armature.poseData) {
						for (int i = 0 ;i<armature.uiAttachments.Length;++i) {
							UnityEngine.UI.MaskableGraphic render = armature.uiAttachments [i];
							if (um.gameObject == render.gameObject) {

								PoseData.DisplayData dd = armature.poseData.displayDatas [i];
								dd.color = um.color;
								dd.vertex = (Vector3[])um.vertices.Clone();
								armature.poseData.displayDatas [i] = dd;

								AssetDatabase.Refresh();
								EditorUtility.SetDirty(armature.poseData);
								AssetDatabase.SaveAssets();
								break;
							}
						}
					}
				}
			}
		}

		void  OnSceneGUI(){
			if (Application.isPlaying || !um.isEdit)
				return;
			Tools.current = Tool.None;
			Vector3[] vs = um.vertices;//sm.weightVertices==null ? sm.vertices : sm.weightVertices ;
			float unit = (um.weights!=null && um.weights.Length>0 )?1f:100f;
			for (int i = 0; i < vs.Length; ++i) {
				Vector3 v = vs [i];
				if (um.weights != null && um.weights.Length > 0) {
					v *= 100f;
				}

				Vector3 worldPos = Handles.PositionHandle (um.transform.TransformPoint(v), Quaternion.identity);
				Handles.Label(worldPos,"  v"+i);
				worldPos.z = 0;

				if (um.vertControlTrans != null && i < um.vertControlTrans.Length) {
					Transform con = um.vertControlTrans [i];
					worldPos = um.transform.InverseTransformPoint (worldPos);
					if (um.weights != null && um.weights.Length > 0) {
						worldPos /= 100f;
					}
					worldPos.z = 0;
					con.localPosition = worldPos/unit;
				} else {
					v = um.transform.InverseTransformPoint (worldPos);
					if (um.weights != null && um.weights.Length > 0) {
						v /= 100f;
					}
					v.z = 0;
					um.vertices [i] = v;
				}
			}
			SceneView.RepaintAll ();
		}
	}

}