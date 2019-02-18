using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Bones2D
{
	[ExecuteInEditMode,DisallowMultipleComponent]
	public class Slot : MonoBehaviour {

		public enum BlendMode{
			Normal,
			Add,
			Erase,
			Multiply,
			Screen,
			Mask,
			Below,
			None,
		}

		[Header("[Override Slot]")]
		/**
		 displayIndex = ?;
		 UpdateCurrentDisplay();
		 * */
		[Tooltip("设置了displayIndex后，需要调用UpdateCurrentDisplay方法来更新")]
		public bool manualDisplayIndex = false;
		/**
		UpdateDisplayColor(?);
		 * */
		[Tooltip("调用UpdateDisplayColor来更新颜色")]
		public bool manualColor = false;

		public bool manualZ = false;

		[HideInInspector]
		public Armature armature;

		[HideInInspector]
		public int zOrder=0;//default z order

		[SerializeField]
		[HideInInspector]
		private int m_ZOrder = 0;//current z order

		[Header("[Default]")]
		public Color color=Color.white;
		private SpriteFrame m_SpriteFrame = null;
		private SpriteMesh m_SpriteMesh = null;
		private SpriteRenderer m_SpriteRenderer = null;
		private UIFrame m_UIFrame;
		private UIMesh m_UIMesh;
		private Image m_Image;
		private Armature m_Armature = null;

		[SerializeField]
		[HideInInspector]
		private GameObject m_CurrentDisplay = null;
		public GameObject currentDisplay{
			get{ return m_CurrentDisplay;}
		}
		public Armature childArmature{
			get{ return m_Armature; }
		}

		[HideInInspector]
		[SerializeField]
		protected int __z=0;
		[HideInInspector]
		[SerializeField]
		private float m_z=0f;
		public int z{
			get {
				return __z;
			}
			set{
				__z = value;
				m_z = value;
			}
		}


		[HideInInspector]
		[SerializeField]
		protected int __displayIndex;
		[SerializeField]
		private float m_DisplayIndex;
		public int displayIndex{
			get {
				return __displayIndex;
			}
			set
			{
				m_DisplayIndex = value;
				__displayIndex = value;
				m_CurrentDisplay = null;
				Transform skin = (armature.skins==null || armature.skins.Length<=1) ?transform :transform.Find(armature.skinName);
				if(skin){
					for(int i=0;i<skin.childCount;++i){
						if(value>-1 && i==value) {
							m_CurrentDisplay = skin.GetChild(i).gameObject;
							m_CurrentDisplay.SetActive(true);
						}
						else {
							skin.GetChild(i).gameObject.SetActive(false);
						}
					}
				}
			}
		}

		[SerializeField]
		private BlendMode m_blendMode = BlendMode.Normal;
		public BlendMode blendMode{
			get { return m_blendMode; }
			set {
				m_blendMode = value;
				if(!Application.isPlaying || armature==null) return;

				if(armature.isUGUI)
				{
					MaskableGraphic[] renders = GetComponentsInChildren<MaskableGraphic>(true);
					for(int i=0;i<renders.Length;++i){
						MaskableGraphic r = renders[i];
						if(r && r.material){
							int last = r.material.name.LastIndexOf(" (Instance)");
							if(m_blendMode!=BlendMode.Normal){
								if(last==-1){
									r.material = Instantiate(r.material);
								}
								r.material.SetFloat("_BlendSrc",GetSrcFactor());
								r.material.SetFloat("_BlendDst",GetDstFactor());
							}
							else if(last>0){
								string matName = r.material.name.Substring(0,last);
								foreach(Material mat in armature.textureFrames.materials){
									if(matName.Equals(mat)){
										r.material = mat;
										break;
									}
								}
							}
						}
					}
				}
				else
				{
					Renderer[] renders = GetComponentsInChildren<Renderer>(true);
					for(int i=0;i<renders.Length;++i){
						Renderer r = renders[i];
						if(r && r.sharedMaterial){
							if(m_blendMode!=BlendMode.Normal){
								r.material.SetFloat("_BlendSrc",GetSrcFactor());
								r.material.SetFloat("_BlendDst",GetDstFactor());
							}
							else
							{
								int last = r.sharedMaterial.name.LastIndexOf(" (Instance)");
								if(last>0){
									string matName = r.sharedMaterial.name.Substring(0,last);
									foreach(Material mat in armature.textureFrames.materials){
										if(matName.Equals(mat)){
											r.sharedMaterial = mat;
											break;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public void UpdateZOrder(int zOrder)
		{
			if(m_ZOrder!=zOrder)
			{
				m_ZOrder = zOrder;
				#if UNITY_EDITOR
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
				#endif
				UpdateZOrder();
			}
		}

		void UpdateZOrder(){
			if(m_SpriteFrame){
				m_SpriteFrame.sortingOrder = m_ZOrder;
				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(m_SpriteFrame);
				#endif
			}else if(m_SpriteMesh){
				m_SpriteMesh.sortingOrder = m_ZOrder;
				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(m_SpriteMesh);
				#endif
			}else if(m_SpriteRenderer){
				m_SpriteRenderer.sortingOrder = m_ZOrder;
				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(m_SpriteRenderer);
				#endif
			}else if(m_Armature){
				m_Armature.sortingOrder = m_ZOrder;
				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(m_Armature);
				#endif
			}
		}

		public void UpdateSlot(){
			UpdateSlotByInheritSlot();

			if(manualDisplayIndex)
			{
				if(!Application.isPlaying){
					int tempIndex = Mathf.RoundToInt(m_DisplayIndex);
					if(transform.childCount>0 && Mathf.Abs(m_DisplayIndex-tempIndex)<0.0001f){
						UpdateSkin(tempIndex);
					}
				}else{
					int tempIndex = Mathf.RoundToInt(__displayIndex);
					if(transform.childCount>0 && Mathf.Abs(__displayIndex-tempIndex)<0.0001f){
						UpdateSkin(tempIndex);
					}
				}
			}
			else
			{
				int tempIndex = Mathf.RoundToInt(m_DisplayIndex);
				if(transform.childCount>0 && Mathf.Abs(m_DisplayIndex-tempIndex)<0.0001f){
					UpdateSkin(tempIndex);
				}
			}
			#if UNITY_EDITOR
			if(!Application.isPlaying) UpdateCurrentDisplay();
			#endif
			if(!manualColor || !Application.isPlaying){
				UpdateDisplayColor(color);
			}
		}

		void UpdateSkin(int tempIndex){
			if(tempIndex!=__displayIndex){
				Transform skin = (armature.skins==null || armature.skins.Length<=1) ?transform :transform.Find(armature.skinName);
				if(skin && skin.childCount>0){
					if(__displayIndex>-1) skin.GetChild(__displayIndex).gameObject.SetActive(false);
					if(tempIndex>-1) skin.GetChild(tempIndex).gameObject.SetActive(true);
					__displayIndex = tempIndex;
					UpdateCurrentDisplay();
				}
			}
		}

		public void CheckZorderChange(){
			int temp= Mathf.RoundToInt(m_z);
			if(Mathf.Abs(m_z-temp)<0.0001f){
				if(temp!=__z){
					__z = temp;
					armature.UpdateSlotZOrder(this);
				}
			}
		}

		void UpdateSlotByInheritSlot(){
			if(inheritSlot){
				#if UNITY_EDITOR
				if(!Application.isPlaying){
					Transform temp = inheritSlot.parent;
					Vector3 sc = inheritSlot.localScale;
					Vector3 pos = inheritSlot.localPosition;
					Quaternion rotate = inheritSlot.localRotation;
					inheritSlot.parent = transform.parent;
					transform.localScale =  inheritSlot.localScale;
					transform.localPosition = new Vector3(inheritSlot.localPosition.x,inheritSlot.localPosition.y,transform.localPosition.z);
					transform.localRotation = inheritSlot.localRotation;
					inheritSlot.parent = temp;
					inheritSlot.localScale = sc;
					inheritSlot.localPosition = pos;
					inheritSlot.localRotation = rotate;
				}
				#endif

				if (Application.isPlaying) {
					Vector3 p = inheritSlot.position;
					p.z = transform.position.z;
					transform.position = p;
					transform.rotation = inheritSlot.rotation;
					transform.localScale = Vector3.one;
					transform.localScale = transform.InverseTransformVector (inheritSlot.TransformVector(inheritSlot.localScale));
				}
			}
		}

		private int GetSrcFactor(){
			if(armature.preMultiplyAlpha){
				if(m_blendMode== BlendMode.Normal){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
				if(m_blendMode== BlendMode.Add){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
				if(m_blendMode== BlendMode.Erase){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
				if(m_blendMode== BlendMode.Multiply){
					return (int)UnityEngine.Rendering.BlendMode.DstColor;
				}
				if(m_blendMode== BlendMode.Screen){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
				if(m_blendMode== BlendMode.Mask){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
				if(m_blendMode== BlendMode.Below){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusDstAlpha;
				}
				if(m_blendMode== BlendMode.None){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
			}else{
				if(m_blendMode== BlendMode.Normal){
					return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				}
				if(m_blendMode== BlendMode.Add){
					return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				}
				if(m_blendMode== BlendMode.Erase){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
				if(m_blendMode== BlendMode.Multiply){
					return (int)UnityEngine.Rendering.BlendMode.DstColor;
				}
				if(m_blendMode== BlendMode.Screen){
					return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				}
				if(m_blendMode== BlendMode.Mask){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
				if(m_blendMode== BlendMode.Below){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusDstAlpha;
				}
				if(m_blendMode== BlendMode.None){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
			}


			return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
		}

		private int GetDstFactor(){
			if(armature.preMultiplyAlpha){
				if(m_blendMode== BlendMode.Normal){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Add){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
				if(m_blendMode== BlendMode.Erase){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Multiply){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Screen){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
				}
				if(m_blendMode== BlendMode.Mask){
					return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				}
				if(m_blendMode== BlendMode.Below){
					return (int)UnityEngine.Rendering.BlendMode.DstAlpha;
				}
				if(m_blendMode== BlendMode.None){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
			}else{
				if(m_blendMode== BlendMode.Normal){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Add){
					return (int)UnityEngine.Rendering.BlendMode.DstAlpha;
				}
				if(m_blendMode== BlendMode.Erase){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Multiply){
					return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
				}
				if(m_blendMode== BlendMode.Screen){
					return (int)UnityEngine.Rendering.BlendMode.One;
				}
				if(m_blendMode== BlendMode.Mask){
					return (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
				}
				if(m_blendMode== BlendMode.Below){
					return (int)UnityEngine.Rendering.BlendMode.DstAlpha;
				}
				if(m_blendMode== BlendMode.None){
					return (int)UnityEngine.Rendering.BlendMode.Zero;
				}
			}
			return (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
		}

		public BlendMode ConvertBlendMode(string blendMode){
			if(blendMode.Equals("add")||blendMode.Equals("additive")){
				return BlendMode.Add;
			}
			if(blendMode.Equals("erase")){
				return BlendMode.Erase;
			}
			if(blendMode.Equals("screen")){
				return BlendMode.Screen;
			}
			if(blendMode.Equals("mask")){
				return BlendMode.Mask;
			}
			if(blendMode.Equals("multiply")){
				return BlendMode.Multiply;
			}
			if(blendMode.Equals("below")){
				return BlendMode.Below;
			}
			if(blendMode.Equals("none")){
				return BlendMode.None;
			}
			return BlendMode.Normal;
		}

		public Transform inheritSlot = null;

		void Start(){
			blendMode = m_blendMode;
			__displayIndex = (int)m_DisplayIndex;
			__z = 0;
			UpdateCurrentDisplay();
			if(!manualColor || !Application.isPlaying){
				UpdateDisplayColor(color);
			}
		}

		protected internal void UpdateCurrentDisplay(){
			m_SpriteFrame = null;
			m_SpriteMesh = null;
			m_SpriteRenderer = null;
			m_Armature = null;
			m_Image = null;
			m_UIFrame = null;
			m_UIMesh = null;
			m_CurrentDisplay = null;
			if(armature==null) return;

			if(__displayIndex>-1 && transform.childCount>0){
				Transform skin = (armature.skins==null || armature.skins.Length<=1) ?transform :transform.Find(armature.skinName);
				if(skin && skin.childCount>0){
					Transform child = skin.GetChild(__displayIndex);
					m_CurrentDisplay = child.gameObject;
					if(armature.isUGUI)
					{
						m_UIFrame = child.GetComponent<UIFrame>();
						if(!m_UIFrame){
							m_UIMesh = child.GetComponent<UIMesh>();
							if(!m_UIMesh){
								m_Image = child.GetComponent<Image>();
								if(!m_Image){
									m_Armature = child.GetComponent<Armature>();
								}
							}
						}
					}
					else
					{
						m_SpriteFrame = child.GetComponent<SpriteFrame>();
						if(!m_SpriteFrame){
							m_SpriteMesh = child.GetComponent<SpriteMesh>();
							if(!m_SpriteMesh){
								m_SpriteRenderer = child.GetComponent<SpriteRenderer>();
								if(!m_SpriteRenderer){
									m_Armature = child.GetComponent<Armature>();
								}
							}
						}
						UpdateZOrder();
					}
				}

			}
			else if(armature.skins!=null && armature.skins.Length>1)
			{
				for(int i=0;i<transform.childCount;++i){
					transform.GetChild(i).gameObject.SetActive(false);
				}
			}
		}

		public void UpdateDisplayColor(Color col){
			if(armature==null) return;

			Color rc = col;
			rc.r *= armature.color.r;
			rc.g *= armature.color.g;
			rc.b *= armature.color.b;
			rc.a *= armature.color.a;

			if(armature.isUGUI)
			{
				if(m_UIFrame){
					if(m_UIFrame.gameObject.activeSelf){
						m_UIFrame.m_PreMultiplyAlpha = armature.m_PreMultiplyAlpha;
						if(!m_UIFrame.color.Equals(rc)){
							m_UIFrame.m_ColorIsDirty = true;
							m_UIFrame.color = rc;
						}
						m_UIFrame.UpdateFrame();
					}
				}else if(m_UIMesh){
					if(m_UIMesh.gameObject.activeSelf){
						m_UIMesh.m_PreMultiplyAlpha = armature.m_PreMultiplyAlpha;
						if(!m_UIMesh.color.Equals(rc)){
							m_UIMesh.m_ColorIsDirty = true;
							m_UIMesh.color = rc;
						}
						m_UIMesh.UpdateMesh();
						if(m_UIMesh.mesh){
							m_UIMesh.rectTransform.sizeDelta = (Vector2)m_UIMesh.mesh.bounds.size;
						}
					}
				}else if(m_Image){
					if(m_Image.gameObject.activeSelf){
						Color c = rc;
						if(armature.m_PreMultiplyAlpha){
							c.r *= c.a;
							c.g *= c.a;
							c.b *= c.a;
						}
						m_Image.color = c ;
					}
				}else if(m_Armature){
					Color c = color;
					Armature parentArmature = m_Armature.parentArmature;
					while(parentArmature){
						c.r *= parentArmature.color.r;
						c.g *= parentArmature.color.g;
						c.b *= parentArmature.color.b;
						c.a *= parentArmature.color.a;
						parentArmature = parentArmature.parentArmature;
					}
					m_Armature.color = c;
				}
			}
			else
			{
				if(m_SpriteFrame){
					if(m_SpriteFrame.gameObject.activeSelf){
						m_SpriteFrame.m_PreMultiplyAlpha = armature.m_PreMultiplyAlpha;
						m_SpriteFrame.color = rc;
						m_SpriteFrame.UpdateFrame();
					}
				}else if(m_SpriteMesh){
					if(m_SpriteMesh.gameObject.activeSelf){
						m_SpriteMesh.m_PreMultiplyAlpha = armature.m_PreMultiplyAlpha;
						m_SpriteMesh.color = rc;
						m_SpriteMesh.UpdateMesh();
					}
				}else if(m_SpriteRenderer){
					if(m_SpriteRenderer.gameObject.activeSelf){
						Color c = rc;
						if(armature.m_PreMultiplyAlpha){
							c.r *= c.a;
							c.g *= c.a;
							c.b *= c.a;
						}
						m_SpriteRenderer.color = c ;
					}
				}else if(m_Armature){
					Color c = color;
					Armature parentArmature = m_Armature.parentArmature;
					while(parentArmature){
						c.r *= parentArmature.color.r;
						c.g *= parentArmature.color.g;
						c.b *= parentArmature.color.b;
						c.a *= parentArmature.color.a;
						parentArmature = parentArmature.parentArmature;
					}
					m_Armature.color = c;
				}
			}
		}

	}

}