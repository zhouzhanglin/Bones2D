using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bones2D
{
	[CustomEditor(typeof(SpriteMesh))]
	public class SpriteMeshEditor : Editor {

		SpriteMesh sm = null;

		void OnEnable(){
			sm = target as SpriteMesh;
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();
			if (sm.vertControlTrans != null && sm.vertControlTrans.Length > 0) {
				foreach (Transform child in sm.vertControlTrans) {
					if (sm.isEdit) {
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
			if(sm.bindposes!=null && sm.bindposes.Length>0)
			{
				GUILayout.Space(20f);
				GUILayout.Label("Press shift to speed up");
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Left Move")){
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						m.m03 -= Event.current.shift ? 0.1f:0.01f;
						sm.bindposes[i] = m;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				if(GUILayout.Button("BindPose Right Move")){
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						m.m03 += Event.current.shift ? 0.1f:0.01f;
						sm.bindposes[i] = m;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Up Move")){
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						m.m13 += Event.current.shift ? 0.1f:0.01f;
						sm.bindposes[i] = m;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				if(GUILayout.Button("BindPose Down Move")){
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						m.m13 -= Event.current.shift ? 0.1f:0.01f;
						sm.bindposes[i] = m;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				GUILayout.EndHorizontal();


				GUILayout.BeginHorizontal();
				if(GUILayout.Button("BindPose Rotate +")){
					Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,Event.current.shift ? 1f:0.1f),Vector3.one);
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						sm.bindposes[i] = m*rotate;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				if(GUILayout.Button("BindPose Rotate -")){
					Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,-(Event.current.shift ? 1f:0.1f)),Vector3.one);
					for(int i = 0 ;i<sm.bindposes.Length;++i){
						Matrix4x4 m = sm.bindposes[i];
						sm.bindposes[i] = m*rotate;
					}
					sm.UpdateSkinnedMesh();
					if(!Application.isPlaying){
						UnityEditor.EditorUtility.SetDirty(sm);
					}
				}
				GUILayout.EndHorizontal();
			}

			if (!Application.isPlaying && sm.isEdit) {
				GUILayout.Space (5);
				if (GUILayout.Button ("Save To Pose")) {
					Armature armature = sm.GetComponentInParent<Armature> ();
					if (armature && armature.poseData) {
						for (int i = 0 ;i<armature.attachments.Length;++i) {
							Renderer render = armature.attachments [i];
							if (sm.gameObject == render.gameObject) {

								PoseData.DisplayData dd = armature.poseData.displayDatas [i];
								dd.color = sm.color;
								dd.vertex = (Vector3[])sm.vertices.Clone();
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

		Matrix4x4[] m_BoneMatrices = null;
		void OnSceneGUI(){
			if (Application.isPlaying || !sm.isEdit)
				return;
			Tools.current = Tool.None;
			Vector3[] vs = (sm.weightVertices==null || sm.weightVertices.Length==0) ? sm.vertices : sm.weightVertices ;
			if(vs!=null)
			{
				if(sm.weights!=null && sm.weights.Length>0){
					if(m_BoneMatrices==null || m_BoneMatrices.Length!=sm.bones.Length){
						m_BoneMatrices = new Matrix4x4[sm.bones.Length];
					}
					for (int j= 0; j< sm.bones.Length; ++j){
						m_BoneMatrices[j] = sm.bones[j].localToWorldMatrix * sm.bindposes[j];
					}
				}
				Matrix4x4 vertexMatrix = new Matrix4x4();
				for(int i = 0;i<vs.Length;++i){
					Vector3 v = vs[i];

					Vector3 prevWorldPos = sm.transform.TransformPoint(v);
					Vector3 worldPos = Handles.DoPositionHandle(prevWorldPos,Quaternion.identity);
					Handles.Label(worldPos,"  v"+i);
					worldPos.z = 0;

					Vector3 localPos = sm.transform.InverseTransformPoint(worldPos);
					localPos.z = 0;
					if((localPos-v).magnitude>0.005f){
						if(m_BoneMatrices!=null && i<sm.weights.Length){
							Armature.BoneWeightClass bw = sm.weights[i];
							Matrix4x4 bm0 = m_BoneMatrices[bw.boneIndex0];
							Matrix4x4 bm1 = m_BoneMatrices[bw.boneIndex1];
							Matrix4x4 bm2 = m_BoneMatrices[bw.boneIndex2];
							Matrix4x4 bm3 = m_BoneMatrices[bw.boneIndex3];
							Matrix4x4 bm4 = m_BoneMatrices[bw.boneIndex4];

							for (int n= 0; n < 16; ++n){
								vertexMatrix[n] =
									bm0[n] * bw.weight0 +
									bm1[n] * bw.weight1 +
									bm2[n] * bw.weight2 +
									bm3[n] * bw.weight3 +
									bm4[n] * bw.weight4;
							}
							vertexMatrix = sm.transform.worldToLocalMatrix*vertexMatrix;
							localPos = vertexMatrix.inverse.MultiplyPoint3x4(localPos);
							localPos.z=0f;
						}
						sm.vertices[i] = localPos;

						if(sm.vertControlTrans!=null&& i<sm.vertControlTrans.Length){
							sm.vertControlTrans[i].localPosition = sm.vertices[i];
						}
						EditorUtility.SetDirty(sm);
					}
				}
				SceneView.RepaintAll();
			}
		}
	}

}