using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;
#endif

namespace Bones2D
{
	/// <summary>
	/// Sprite frame.
	/// author:bingheliefeng
	/// </summary>
	[ExecuteInEditMode,DisallowMultipleComponent,RequireComponent(typeof(MeshFilter)),RequireComponent(typeof(MeshRenderer))]
	public class SpriteFrame : MonoBehaviour {
		
		public Material material{
			get{ 
				return meshRenderer.sharedMaterial;
			}
			set{
				meshRenderer.sharedMaterial = value;
			}
		}

		protected MeshFilter m_MeshFilter;
		public MeshFilter meshFilter {
			get {
				if(m_MeshFilter == null) m_MeshFilter = GetComponent<MeshFilter>();
				if(m_MeshFilter == null) m_MeshFilter = gameObject.AddComponent<MeshFilter>();
				return m_MeshFilter;
			}
		}

		protected MeshRenderer m_MeshRenderer;
		public MeshRenderer meshRenderer {
			get {
				if(m_MeshRenderer == null) m_MeshRenderer = GetComponent<MeshRenderer>();
				if(m_MeshRenderer == null) m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
				return m_MeshRenderer;
			}
		}

		[HideInInspector]
		[SerializeField]
		private string m_FrameName;
		public string frameName{
			get { 
				if(string.IsNullOrEmpty(m_FrameName)){
					if(m_Frame!=null) m_FrameName = m_Frame.name;
				}
				return m_FrameName;
			}
			set {
				if(!m_FrameName.Equals(value)){
					m_FrameName = value;
					if(m_createdMesh && !string.IsNullOrEmpty(m_FrameName)){
						frame = textureFrames.GetTextureFrame(m_FrameName);
					}
				}
			}
		}

		[SerializeField]
		private Vector2 m_uvOffset;
		public Vector2 uvOffset{
			get { return m_uvOffset; }
			set { 
				if(m_uvOffset.x!=value.x || m_uvOffset.y!=value.y ){
					m_uvOffset = value;
					if(m_createdMesh)	UpdateUV();
				}
			}
		}

		[SerializeField]
		private Vector2 m_pivot;
		public Vector2 pivot{
			get { return m_pivot; }
			set { 
				if(m_pivot.x!=value.x || m_pivot.y!=value.y){
					m_pivot=value;
					if(m_createdMesh)	UpdateVertex();
				}
			}
		}

        [SerializeField]
        private float m_skew = 0f;
        public float skew{
            get{ 
                return m_skew;
            }
            set{ 
                if(m_skew!=value){
                    m_skew = value;
                    if(m_createdMesh)   UpdateVertex();
                }
            }
        }

		private bool m_ColorIsDirty = false;
		[SerializeField]
		private Color m_Color = Color.white;//For Animation
		public Color color{
			get { return m_Color;}
			set { 
				if(m_Color.a!=value.a || m_Color.r != value.r || m_Color.g != value.g || m_Color.b != value.b){
					m_Color = value; 
					m_ColorIsDirty = true;
				}
			}
		}

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

		[SerializeField]
		private string m_sortingLayerName="Default";
		public string sortingLayerName{
			get { return m_sortingLayerName;}
			set {	
				if(!m_sortingLayerName.Equals(value)){
					m_sortingLayerName=value;
					if(m_createdMesh)	UpdateSorting();
				}
			}
		}

		[SerializeField]
		private int m_sortingOrder = 0;
		public int sortingOrder{
			get { return m_sortingOrder;}
			set {	
				if(m_sortingOrder!=value){
					m_sortingOrder=value;
					if(m_createdMesh)	UpdateSorting();
				}
			}
		}

		public TextureFrames textureFrames;

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
								material = m_Frame.material;
							}
						}else{
							material = m_Frame.material;
						}
					}
				}
			}
		}

		[HideInInspector]
		[SerializeField]
		private bool m_createdMesh = false;
		public bool isCreatedMesh{
			get{ return m_createdMesh && m_mesh!=null ; }
		}

		[HideInInspector]
		[SerializeField]
		private Mesh m_mesh ;

		private Color[] m_Colors;

		void OnEnable(){
			if(m_createdMesh && m_mesh==null){
				this.CreateQuad();
				this.frameName = m_FrameName;
			}
			UpdateVertexColor();
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

			meshFilter.mesh = m_mesh;

			meshRenderer.receiveShadows = false;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.material = material;

			UpdateVertex();
			UpdateUV();
			UpdateVertexColor();
			UpdateSorting();
			m_mesh.RecalculateBounds();
			m_createdMesh = true;
		}

		#if UNITY_EDITOR
		void LateUpdate(){
            if( m_createdMesh && (!Application.isPlaying||Selection.activeGameObject==gameObject)) {
				UpdateSorting();
				UpdateVertex();
				UpdateUV();
				UpdateVertexColor();
				m_mesh.RecalculateBounds();
			}
		}
		#endif

		public void UpdateFrame(){
			if( m_createdMesh){
				if(m_ColorIsDirty){
					UpdateVertexColor();
					m_ColorIsDirty = false;
				}
			}
		}


		public void UpdateSorting(){
			meshRenderer.sortingLayerName = m_sortingLayerName;
			meshRenderer.sortingOrder = m_sortingOrder;
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
					uvs[i] =new Vector2(rectUV[i].x/m_Frame.atlasTextureSize.x,rectUV[i].y/m_Frame.atlasTextureSize.y)-m_uvOffset*0.01f;
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
			if(m_Frame!=null && m_mesh){
				float x = 0f;
				float y = 0f;
				float w = m_Frame.rect.width;
				float h = m_Frame.rect.height;
				if(m_Frame.isRotated){
					w = m_Frame.rect.height;
					h = m_Frame.rect.width;
				}
				Vector3 offset = m_Frame.frameOffset*0.01f;
				x = offset.x;
				y = offset.y;

				Vector3[] verts = m_mesh.vertices;
				verts[0].x = x;
				verts[0].y = y;

				verts[1].x = x;
				verts[1].y = y+h*0.01f ;

				verts[2].x = x+w*0.01f;
				verts[2].y = y+h*0.01f;

				verts[3].x = x+w*0.01f;
				verts[3].y = y;

				Vector3 pivot = new Vector3(m_pivot.x*w*0.01f ,m_pivot.y*h*0.01f,0f);
                var skewed = m_skew < -0.01f || 0.01f < m_skew;
                if (skewed)
                {
                    var isPositive = true; //global.scaleX >= 0.0f;
                    var cos = Mathf.Cos(m_skew);
                    var sin = Mathf.Sin(m_skew);

                    for (int i = 0, l = verts.Length; i < l; ++i)
                    {
                        verts[i] -= pivot;

                        var px = verts[i].x;
                        var py = verts[i].y;
                        if (isPositive)
                            verts[i].x = px + py * sin;
                        else
                            verts[i].x = -px + py * sin;
                        verts[i].y = py * cos;
                    }
                }
                else
                {
                    for(int i=0;i<4;++i){
                        verts[i]-=pivot;
                    }
                }

				m_mesh.vertices = verts;
			}
		}

		public void UpdateVertexColor(){
			if(m_mesh){
				if(m_Colors==null || m_Colors.Length!= m_mesh.vertexCount){
					m_Colors = new Color[m_mesh.vertexCount];
				}

				float brightness1 = Mathf.Clamp(m_brightness,0f,1f)*2f+1f;
				Color c = m_Color;
				c *= brightness1;
				if(m_PreMultiplyAlpha){
					c.r*=c.a;
					c.g*=c.a;
					c.b*=c.a;
				}
				for(int i=0;i<4;++i){
					m_Colors[i]=c;
				}
				m_mesh.colors=m_Colors;
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmos(){
			if(!Application.isPlaying && m_Frame!=null && Selection.activeTransform){
				if(Selection.activeTransform==this.transform||Selection.activeTransform.parent==this.transform){
					Gizmos.color = Color.red;
					Matrix4x4 cubeTransform = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
					Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
					Gizmos.matrix *= cubeTransform;
					if(frame.isRotated){
						Gizmos.DrawWireCube(new Vector3(
							-m_pivot.x*m_Frame.frameSize.height*0.01f+m_Frame.frameSize.height*0.005f,
							-m_pivot.y*m_Frame.frameSize.width*0.01f+m_Frame.frameSize.width*0.005f,
							0
						),new Vector3(m_Frame.frameSize.height*0.01f,m_Frame.frameSize.width*0.01f,0.1f));
					}
					else
					{
						Gizmos.DrawWireCube(new Vector3(
							-m_pivot.x*m_Frame.frameSize.width*0.01f+m_Frame.frameSize.width*0.005f,
							-m_pivot.y*m_Frame.frameSize.height*0.01f+m_Frame.frameSize.height*0.005f,
							0
						),new Vector3(m_Frame.frameSize.width*0.01f,m_Frame.frameSize.height*0.01f,0.1f));
					}
					Gizmos.matrix = oldGizmosMatrix;
				}
			}
		}
		#endif
	}
}