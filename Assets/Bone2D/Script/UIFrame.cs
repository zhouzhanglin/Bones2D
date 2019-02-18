using UnityEngine;
using System.Collections;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;
#endif

namespace Bones2D
{
	/// <summary>
	/// UI frame.
	/// author:bingheliefeng
	/// </summary>
	[ExecuteInEditMode,DisallowMultipleComponent,RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
	public class UIFrame : MaskableGraphic {

		public TextAsset atlasText;
	
		private Texture m_Texture;
		public override Texture mainTexture {
			get { 
				return m_Texture == null ? material.mainTexture : m_Texture;
			}
		}
		/// <summary>
		/// Texture to be used.
		/// </summary>
		public Texture texture
		{
			get
			{
				return m_Texture;
			}
			set
			{
				if (m_Texture == value)
					return;
				m_Texture = value;
				SetMaterialDirty();
			}
		}

		protected internal bool m_ColorIsDirty = false;

		[SerializeField]
		protected internal bool m_PreMultiplyAlpha = false;

		[Range(0f,1f)]
		[SerializeField]
		private float m_brightness = 0f;
		public float brightness{
			get { return m_brightness;}
			set {	
				if(m_brightness!=value){
					m_brightness=value;
					if(m_createdMesh)	UpdateVertexColor();
				}
			}
		}


		[HideInInspector]
		[SerializeField]
		private TextureFrame m_Frame;
		public TextureFrame frame{
			get { return m_Frame; }
			set {
				if(m_Frame!=value){
					m_Frame = value;
					if(m_Frame!=null){
						UpdateVertex();
						UpdateUV();
						if(Application.isPlaying){
							if(material==null){
								material = m_Frame.uiMaterial;
							}
						}else{
							material = m_Frame.uiMaterial;
						}
					}
				}
			}
		}

		[HideInInspector]
		[SerializeField]
		private bool m_createdMesh = false;
		public bool isCreatedMesh{
			get{
				return m_createdMesh && m_mesh!=null;
			}
		}

		[HideInInspector]
		[SerializeField]
		private Mesh m_mesh ;

		private Color32[] m_Colors;

		protected override void OnEnable ()
		{
			base.OnEnable ();
			if(m_createdMesh && m_mesh==null){
				this.CreateQuad();
			}
			UpdateVertexColor();
		}

		public override void Rebuild (CanvasUpdate update) {
			base.Rebuild(update);
			if (canvasRenderer.cull) return;
			if (update == CanvasUpdate.PreRender){
				UpdateAll();
			}
		}

		protected override void OnPopulateMesh (VertexHelper vh)
		{
			vh.Clear();
		}

		public void CreateQuad(){
			if(m_mesh==null) {
				m_mesh = new Mesh();
				m_mesh.hideFlags = HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;
				m_mesh.MarkDynamic();
			}
			m_mesh.vertices = new Vector3[4];
			m_mesh.uv = new Vector2[4];
			m_mesh.colors = new Color[4];
			m_mesh.triangles = new int[]{0,1,2,2,3,0};

			UpdateVertex();
			UpdateUV();
			UpdateVertexColor();
			m_mesh.RecalculateBounds();
			rectTransform.sizeDelta = (Vector2)m_mesh.bounds.size;
			m_createdMesh = true;
			canvasRenderer.SetMesh(m_mesh);
		}

		#if UNITY_EDITOR
		void LateUpdate(){
			if(!Application.isPlaying){
				UpdateFrame();
			}
		}
		#endif

		public void UpdateAll(){
			if( m_createdMesh) {
				UpdateVertex();
				UpdateUV();
				UpdateVertexColor();
				m_mesh.RecalculateBounds();
				rectTransform.sizeDelta = (Vector2)m_mesh.bounds.size;
				canvasRenderer.SetMesh(m_mesh);
			}
		}

		public void UpdateFrame(){
			if( m_createdMesh){
				if(m_ColorIsDirty){
					UpdateVertexColor();
					m_ColorIsDirty = false;
					canvasRenderer.SetMesh(m_mesh);
				}
			}
		}

		public void UpdateUV(){
			if(m_mesh!=null && m_Frame!=null && m_Frame.atlasTextureSize.x>1 && m_Frame.atlasTextureSize.y>1){
				Vector2[] rectUV = new Vector2[]{
					new Vector2(m_Frame.rect.x,  m_Frame.atlasTextureSize.y-m_Frame.rect.y-m_Frame.rect.height),
					new Vector2(m_Frame.rect.x,  m_Frame.atlasTextureSize.y-m_Frame.rect.y),
					new Vector2(m_Frame.rect.x+m_Frame.rect.width,  m_Frame.atlasTextureSize.y-m_Frame.rect.y),
					new Vector2(m_Frame.rect.x+m_Frame.rect.width, m_Frame.atlasTextureSize.y-m_Frame.rect.y-m_Frame.rect.height),
				};
				Vector2[] uvs= m_mesh.uv;
				for(int i=0;i<4;++i){
					uvs[i] =new Vector2(rectUV[i].x/m_Frame.atlasTextureSize.x,rectUV[i].y/m_Frame.atlasTextureSize.y);
				}
				if(m_Frame!=null && m_Frame.isRotated){
					rectUV[0] = uvs[3];
					rectUV[1] = uvs[0];
					rectUV[2] = uvs[1];
					rectUV[3] = uvs[2];
					uvs = rectUV;
				}

				m_mesh.uv = uvs;
			}
		}

		public void UpdateVertex(){
			if(m_mesh!=null && m_Frame!=null){
				float x = 0f;
				float y = 0f;
				float w = m_Frame.rect.width;
				float h = m_Frame.rect.height;
				if(m_Frame.isRotated){
					w = m_Frame.rect.height;
					h = m_Frame.rect.width;
				}
				Vector3 offset = m_Frame.frameOffset;
				x = offset.x;
				y = offset.y;

				Vector3[] verts = m_mesh.vertices;
				verts[0].x = x;
				verts[0].y = y;

				verts[1].x = x;
				verts[1].y = y+h ;

				verts[2].x = x+w;
				verts[2].y = y+h;

				verts[3].x = x+w;
				verts[3].y = y;

				Vector3 pivot = new Vector3(0.5f*w ,0.5f*h,0f);
				for(int i=0;i<4;++i){
					verts[i]-=pivot;
				}
				m_mesh.vertices = verts;
			}
		}

		public void UpdateVertexColor(){
			if(m_mesh){
				if(m_Colors==null || m_Colors.Length!= m_mesh.vertexCount){
					m_Colors = new Color32[m_mesh.vertexCount];
				}
				Color col = color;
				col *= (Mathf.Clamp(m_brightness,0f,1f)*2f+1f);
				if(m_PreMultiplyAlpha){
					col.r*=col.a;
					col.g*=col.a;
					col.b*=col.a;
				}
				for(int i=0;i<m_Colors.Length;++i){
					m_Colors[i]=col;
				}
				m_mesh.colors32=m_Colors;
			}
		}

	}
}