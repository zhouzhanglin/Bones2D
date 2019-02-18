using UnityEngine;
using System.Collections;
using UnityEditor;

public class Bone2DSetupEditor : ScriptableWizard {
	public enum DisplayType{
		//default is SpriteFrame and SpriteMesh
		Default,SpriteRender,UGUIDefault,UGUIImage
	}

	[Header("Setting")]
	public float zoffset = 0.002f;
	public bool genericAnim = false; //UI and Sprite with the same animation file.

	[Header("Colliders")]
	public bool genMeshCollider = false; //Mesh Collider
	public bool genImgCollider = false;// BoxCollider2D , use image size
	public bool genCustomCollider = true;//custom collider

	[Header("Generate File")]
	public bool genPrefab = false;
	public bool genAnimations = true;
	public bool genAvatar = false;//generate Avatar and Avatar Mask

	void OnEnable(){
		zoffset = EditorPrefs.GetFloat("bone2d_zoffset",0.002f);
		genericAnim = EditorPrefs.GetBool("bone2d_genericAnim",true);
		genPrefab = EditorPrefs.GetBool("bone2d_genPrefab",false);
		genAnimations = EditorPrefs.GetBool("bone2d_genAnims",true);
		genAvatar = EditorPrefs.GetBool("bone2d_genAvatar",false);
		genMeshCollider = EditorPrefs.GetBool("bone2d_genMeshCollider",false);
		genImgCollider = EditorPrefs.GetBool("bone2d_genImgCollider",false);
		genCustomCollider = EditorPrefs.GetBool("bone2d_genCustomCollider",true);
	}

	[MenuItem("Bone2D/Setting",false,0)]
	static void CreateWizard () {
		Bone2DSetupEditor editor = ScriptableWizard.DisplayWizard<Bone2DSetupEditor>("Bone2D Setup", "Set");
		editor.minSize = new Vector2(200,400);
	}
	public void OnWizardCreate(){
		//save settting
		EditorPrefs.SetFloat("bone2d_zoffset",zoffset);
		EditorPrefs.SetBool("bone2d_genericAnim",genericAnim);
		EditorPrefs.SetBool("bone2d_genPrefab",genPrefab);
		EditorPrefs.SetBool("bone2d_genAnims",genAnimations);
		EditorPrefs.SetBool("bone2d_genAvatar",genAvatar);
		EditorPrefs.SetBool("bone2d_genMeshCollider",genMeshCollider);
		EditorPrefs.SetBool("bone2d_genImgCollider",genImgCollider);
		EditorPrefs.SetBool("bone2d_genCustomCollider",genCustomCollider);
	}
}
