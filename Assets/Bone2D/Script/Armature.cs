using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Armature.
/// author:bingheliefeng
/// </summary>
namespace Bones2D
{
	[ExecuteInEditMode,DisallowMultipleComponent]
	public class Armature : MonoBehaviour {

		public enum SortType{
			ZSpace,ZOrder
		}

		[System.Serializable]
		public class BoneWeightClass{
			public int boneIndex0;
			public int boneIndex1;
			public int boneIndex2;
			public int boneIndex3;
			public int boneIndex4;
			public float weight0;
			public float weight1;
			public float weight2;
			public float weight3;
			public float weight4;
		}

		[SerializeField]
		public SortType sortType = SortType.ZSpace;
		[Range(0.001f,1f)]
		public float zSpace = 0.002f;
		[SerializeField]
		private bool m_FlipX;
		[SerializeField]
		private bool m_FlipY;

		[SerializeField]
		private float m_AnimIndex =  -1f;
		[SerializeField]
		private float __AnimIndex =  -1f;
		public float animIndex{
			get{ return __AnimIndex;}
			set{
				if(m_AnimIndex!=value || __AnimIndex!=value){
					m_AnimIndex = value;
					__AnimIndex = value;
					if(Application.isPlaying){
						PlayAnimByIndex();
					}
				}
			}
		}

		[SerializeField]
		private int m_SkinIndex=0;
		public int skinIndex{
			get { return m_SkinIndex; }
			set {
				if(m_SkinIndex!=value && value>-1 && value<skins.Length){
					m_SkinIndex = value;
					//switch skin
					if(slots!=null){
						int len = slots.Length;
						for(int i=0;i<len;++i){
							Slot slot = slots[i];
							if(slot){
								for(int j=0;j<slot.transform.childCount;++j){
									Transform skinTran= slot.transform.GetChild(j);
                                    if (!skinTran.name.ToLower().Equals("default"))
                                    {
                                        skinTran.gameObject.SetActive(skinTran.name.Equals(skinName));
                                    }
                                }
                                slot.displayIndex = slot.displayIndex;
								slot.UpdateCurrentDisplay();
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the name of the skin.
		/// </summary>
		/// <value>The name of the skin.</value>
		public string skinName{
			get{
				return skins[m_SkinIndex];
			}
			set{
				if(skins!=null){
					for(int i=0;i<skins.Length;++i){
						if(skins[i].Equals(value)){
							skinIndex = i;
							break;
						}
					}
				}
			}
		}
		public PoseData poseData;
		public string[] skins;
		public string[] anims;
		public Slot[] slots;
		public Transform[] bones;
		public Renderer[] attachments;
		public MaskableGraphic[] uiAttachments;
		public TextureFrames textureFrames;
		public Armature[] sonArmatures;
		public Armature parentArmature;

		private Animator m_animator;
		public Animator animator{
			get { 
				if(m_animator==null){
					m_animator = gameObject.GetComponent<Animator>();
				}
				return m_animator;
			} 
		}

		[SerializeField]
		protected internal bool m_PreMultiplyAlpha = false;
		public bool preMultiplyAlpha{
			get{ return m_PreMultiplyAlpha; }
			set{
				m_PreMultiplyAlpha = value;
				if(textureFrames!=null){
					foreach(Material mat in textureFrames.materials){
						if(mat){
							mat.SetInt("_BlendSrc",m_PreMultiplyAlpha? (int)UnityEngine.Rendering.BlendMode.One:(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
						}
					}
					#if UNITY_EDITOR 
					if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
					#endif
				}
			}
		}

		public Color color = Color.white;

		[SerializeField]
		protected string m_SortingLayerName = "Default";
		/// <summary>
		/// Name of the Renderer's sorting layer.
		/// </summary>
		public string sortingLayerName
		{
			get {
				return m_SortingLayerName;
			}
			set {
				m_SortingLayerName = value;
				if(!isUGUI)
				{
					foreach(Renderer r in attachments){
						if(r) {
							r.sortingLayerName = value;
							#if UNITY_EDITOR 
							if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(r);
							#endif
							SpriteFrame sf = r.GetComponent<SpriteFrame>();
							if(sf) {
								sf.sortingLayerName = value;
								#if UNITY_EDITOR 
								if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sf);
								#endif
							}
							else {
								SpriteMesh sm = r.GetComponent<SpriteMesh>();
								if(sm) {
									sm.sortingLayerName = value;
									#if UNITY_EDITOR 
									if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sm);
									#endif
								}
							}
						}
					}
					if(sonArmatures!=null){
						foreach(Armature armature in sonArmatures){
							if(armature) {
								armature.sortingLayerName = value;
								#if UNITY_EDITOR 
								if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(armature);
								#endif
							}
						}
					}
				}
			}
		}

		[SerializeField]
		protected int m_SortingOrder = 0;
		/// <summary>
		/// Renderer's order within a sorting layer.
		/// </summary>
		public int sortingOrder
		{
			get {
				return m_SortingOrder;
			}
			set {
				m_SortingOrder = value;
				if(!isUGUI)
				{
					#if UNITY_5_6_OR_NEWER
					if(_sortingGroup){
						_sortingGroup.sortingOrder = value;
					#if UNITY_EDITOR
						if(!Application.isPlaying){
							EditorUtility.SetDirty(_sortingGroup);
						}
					#endif
					}
					#endif
					for(int i=0;i<sortedSlots.Count;++i){
						Slot slot = sortedSlots[i];
						if(slot) {
							if(sortType== SortType.ZSpace){
								slot.UpdateZOrder(m_SortingOrder);
							}else if(sortType== SortType.ZOrder){
								slot.UpdateZOrder(i);
							}
						}
					}
				}
			}
		}


		public bool flipX{
			get { return m_FlipX; }
			set {
				#if !UNITY_EDITOR
				if(m_FlipX == value) return;
				#endif
				m_FlipX =  value;

				transform.Rotate(0f,180f,0f);
			
				Vector3 v = transform.localEulerAngles;
				v.x = ClampAngle(v.x,-360f,360f);
				v.y = ClampAngle(v.y,-360f,360f);
				v.z = ClampAngle(v.z,-720f,720f);
				transform.localEulerAngles=v;

				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(transform);
				#endif
				ResetSlotZOrder();
			}
		}

		public bool flipY{
			get { return m_FlipY; }
			set {
				#if !UNITY_EDITOR
				if(m_FlipY == value) return;
				#endif
				m_FlipY =  value;
				transform.Rotate(180f,0f,0f);

				Vector3 v = transform.localEulerAngles;
				v.x = ClampAngle(v.x,-360f,360f);
				v.y = ClampAngle(v.y,-360f,360f);
				v.z = ClampAngle(v.z,-720f,720f);
				transform.localEulerAngles=v;

				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(transform);
				#endif
				ResetSlotZOrder();
			}
		}

		float ClampAngle(float angle,float min ,float max){
			if (angle<90 || angle>270){       // if angle in the critic region...
				if (angle>180) angle -= 360;  // convert all angles to -180..+180
				if (max>180) max -= 360;
				if (min>180) min -= 360;
			}
			angle = Mathf.Clamp(angle, min, max);
			return angle;
		}

		private List<Slot> m_OrderSlots = new List<Slot>();
		private int[] m_NewSlotOrders = null ;
		private bool m_CanSortAllSlot = false;
		private List<Slot> m_SortedSlots = null;
		public List<Slot> sortedSlots{
			get{
				if(m_SortedSlots==null || m_SortedSlots.Count==0){
					m_SortedSlots = new List<Slot>(slots);
 				}
				return m_SortedSlots;
			}
		}
	
		//whether set to pose order
		protected int __ZOrderValid = 0;
		[HideInInspector]
		[SerializeField]
		private float m_ZOrderValid = 0f;

		private static Texture2D m_AlphaTex = null;

		[HideInInspector]
		[SerializeField]
		private bool m_isUGUI = false;
		public bool isUGUI{
			get { return m_isUGUI; }
			#if UNITY_EDITOR
			set { m_isUGUI = value ; }
			#endif
		}

		#if UNITY_5_6_OR_NEWER
		internal UnityEngine.Rendering.SortingGroup _sortingGroup;
		public UnityEngine.Rendering.SortingGroup sortingGroup{
			get { return _sortingGroup; }
		}
		#endif

		void OnEnable(){
			if(Application.isPlaying){
				PlayAnimByIndex();
			}
		}
		void OnDisable(){
			if(Application.isPlaying){
				SetToPose();
			}
		#if UNITY_EDITOR
			SetAlphaTex();
		#endif
		}

		void Awake(){
			SetAlphaTex();
			if(textureFrames!=null && textureFrames.materials.Length>0 && textureFrames.materials[0]!=null){
				m_PreMultiplyAlpha = textureFrames.materials[0].GetFloat("_BlendSrc")==(int)UnityEngine.Rendering.BlendMode.One;
			}
			#if UNITY_5_6_OR_NEWER
			if(!isUGUI) {
				_sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
				if(_sortingGroup){
					sortType = SortType.ZOrder;
					m_SortingLayerName = _sortingGroup.sortingLayerName;
					m_SortingOrder = _sortingGroup.sortingOrder;
				}
			}
			#endif
		}

		void SetAlphaTex(){
			if(m_AlphaTex==null){
				m_AlphaTex = new Texture2D(2,2,TextureFormat.RGBA32,false,false);
				Color32 c = new Color32(255,255,255,0);
				m_AlphaTex.SetPixels32(new Color32[]{
					c,c,c,c
				});
				m_AlphaTex.Apply();
			}
			if(textureFrames!=null){
				foreach(Material m in textureFrames.materials){
					if(m!=null && m.mainTexture==null){
						m.mainTexture = m_AlphaTex;
					}
				}
			}
		}

		void PlayAnimByIndex(){
			if(this.isActiveAndEnabled){
				if(anims!=null && m_AnimIndex>=0){
					if(m_AnimIndex<anims.Length){
						string name = anims[Mathf.FloorToInt(m_AnimIndex)];
						if(!string.IsNullOrEmpty(name)){
							animator.Play(name);
						}
					}
				}
				else if(m_AnimIndex==-1){
					animator.Play("None");
				}
			}
		}

		void Update(){
			if(Application.isPlaying){
				if(animator!=null && animator.enabled)
				{
					UpdateArmature();
				}
			}
		}

		/// <summary>
		/// Lates the update. Sort slot
		/// </summary>
		void LateUpdate(){
			#if UNITY_EDITOR
			if(!Application.isPlaying){
				if(animator!=null)
				{
					UpdateArmature();
				}

				if(parentArmature){
					sortType = parentArmature.sortType;
				}
			#if UNITY_5_6_OR_NEWER
				if(!isUGUI){
					_sortingGroup = gameObject.GetComponent<UnityEngine.Rendering.SortingGroup>();
					if(parentArmature){
						if(parentArmature.sortingGroup!=null){
							if(_sortingGroup==null){
								_sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
								_sortingGroup.sortingLayerName = sortingLayerName;
								_sortingGroup.sortingOrder = sortingOrder;
							}
						}
						else if(_sortingGroup){
							DestroyImmediate(_sortingGroup);
						}
					}
				}
			#endif
			}

			SetAlphaTex();
			#endif

			//caculate sort
			if(slots!=null){
				int len = slots.Length;
				for(int i=0;i<len;++i){
					Slot slot = slots[i];
					if(slot && slot.isActiveAndEnabled){
						slot.CheckZorderChange();
					}
				}
			}

			if(m_CanSortAllSlot)
			{
				ForceSortAll();
				m_CanSortAllSlot = false;
				int temp=(int) m_ZOrderValid;
				if(Mathf.Abs(m_ZOrderValid-temp)>0.0001f) return;
				if(temp!=__ZOrderValid){
					__ZOrderValid = temp;
				}
			}
			else
			{
				int temp=(int) m_ZOrderValid;
				if(Mathf.Abs(m_ZOrderValid-temp)>0.0001f) return;
				if(temp!=__ZOrderValid){
					__ZOrderValid = temp;
					ResetSlotZOrder();
				}
			}
		}

		void CalculatZOrder(){
			int orderCount = m_OrderSlots.Count;
			int slotCount = slots.Length;
			int[] unchanged = new int[slotCount - orderCount];

			if(m_NewSlotOrders==null){
				m_NewSlotOrders = new int[slotCount];
			}
			for (int i = 0; i < slotCount; ++i){
				m_NewSlotOrders[i] = -1;
			}

			int originalIndex = 0;
			int unchangedIndex = 0;
			for (int i = 0; i<orderCount ; ++i)
			{
				Slot slot = m_OrderSlots[i];
				int slotIndex = slot.zOrder;
				int offset = slot.z;
				while (originalIndex != slotIndex)
				{
					unchanged[unchangedIndex++] = originalIndex++;
				}
				m_NewSlotOrders[originalIndex + offset] = originalIndex++;
			}

			while (originalIndex < slotCount)
			{
				unchanged[unchangedIndex++] = originalIndex++;
			}

			int iC = slotCount;
			while (iC-- != 0)
			{
				if (m_NewSlotOrders[iC] == -1 && unchangedIndex>0)
				{
					m_NewSlotOrders[iC] = unchanged[--unchangedIndex];
				}
			}

			if(isUGUI)
			{
				for(int i=0;i<slotCount;++i){
					Slot slot = slots[m_NewSlotOrders[i]];
					if(slot){
						slot.transform.SetSiblingIndex(i);
					}
				}
			}
			else
			{
				//set order
				float zoff = m_FlipX || m_FlipY ? 1f : -1f;
				if(m_FlipX && m_FlipY) zoff = -1f;
				zoff*=zSpace;
				for(int i=0;i<slotCount;++i){
					if(m_NewSlotOrders[i]<0) continue;
					Slot slot = slots[m_NewSlotOrders[i]];
					if(slot && !slot.manualZ){
						Vector3 v = slot.transform.localPosition;
						v.z = zoff*i;
						slot.transform.localPosition = v;
					}
				}
				m_SortedSlots = new List<Slot>(slots);
				m_SortedSlots.Sort(delegate(Slot x, Slot y) {
					if( x.transform.localPosition.z<y.transform.localPosition.z) return 1;
					else if( x.transform.localPosition.z>y.transform.localPosition.z) return -1;
					return 0;
				});
				if(sortType== SortType.ZOrder){
					sortingOrder = sortingOrder;
				}
			}
			m_OrderSlots.Clear();
		}

		/// <summary>
		/// Sets to pose.
		/// </summary>
		public void SetToPose(){
			if(poseData){
				for(int i=0;i<poseData.boneDatas.Length && i<bones.Length;++i){
					Transform bone = bones[i];
					if(bone){
						PoseData.TransformData transData = poseData.boneDatas[i];
						bone.localPosition = new Vector3(transData.x,transData.y,bone.localPosition.z);
						bone.localScale = new Vector3(transData.sx,transData.sy,bone.localScale.z);
						bone.localEulerAngles = new Vector3(bone.localEulerAngles.x,bone.localEulerAngles.y,transData.rotation);
					}
				}
				for(int i=0;i<poseData.slotDatas.Length && i<slots.Length;++i){
					Slot slot = slots[i];
					if(slot){
						slot.color = poseData.slotDatas[i].color;
						slot.displayIndex = poseData.slotDatas[i].displayIndex;
						slot.z = poseData.slotDatas[i].zorder;
					}
					m_SortedSlots = null;
				}
				if(isUGUI)
				{
					for(int i=0;i<poseData.displayDatas.Length && i<uiAttachments.Length;++i){
						MaskableGraphic mg = uiAttachments[i];
						if(mg){
							PoseData.DisplayData displayData = poseData.displayDatas[i];
							switch(displayData.type)
							{
							case PoseData.AttachmentType.IMG:
								UIFrame uf = mg.GetComponent<UIFrame>();
								if(uf){
									uf.color = displayData.color;
								}else{
									Image img = mg.GetComponent<Image>();
									if(img){
										img.color = displayData.color;
									}
								}
								break;
							case PoseData.AttachmentType.MESH:
								UIMesh um = mg.GetComponent<UIMesh> ();
								um.vertices = (Vector3[])displayData.vertex.Clone();
								if(um.vertControlTrans!=null&&um.vertControlTrans.Length>0){
									for (int j = 0; j < um.vertControlTrans.Length && j < um.vertices.Length; ++j) {
										Transform vctr = um.vertControlTrans [j];
										if (vctr) {
											vctr.localPosition = um.vertices [j];
										}
									}
								}
								break;
							}
						}
					}
				}
				else
				{
					for(int i=0;i<poseData.displayDatas.Length && i<attachments.Length;++i){
						Renderer r = attachments[i];
						if(r){
							PoseData.DisplayData displayData = poseData.displayDatas[i];
							switch(displayData.type)
							{
							case PoseData.AttachmentType.IMG:
								SpriteFrame sf = r.GetComponent<SpriteFrame>();
								if(sf){
									sf.color = displayData.color;
								}else{
									SpriteRenderer sr = r.GetComponent<SpriteRenderer>();
									if(sr){
										sr.color = displayData.color;
									}
								}
								break;
							case PoseData.AttachmentType.MESH:
								SpriteMesh sm = r.GetComponent<SpriteMesh>();
								sm.vertices = (Vector3[])displayData.vertex.Clone();
								if(sm.vertControlTrans!=null){
									for(int j=0;j<sm.vertControlTrans.Length && j<sm.vertices.Length;++j){
										Transform vctr = sm.vertControlTrans[j];
										if(vctr){
											vctr.localPosition = sm.vertices[j];
										}
									}
								}
								break;
							}
						}
					}
				}
			}
			ResetSlotZOrder();
		}

		/// <summary>
		/// update
		/// </summary>
		void UpdateArmature(){
			this.animIndex = this.m_AnimIndex;
			if(slots!=null){
				int len = slots.Length;
				for(int i=0;i<len;++i){
					Slot slot = slots[i];
					if(slot && slot.isActiveAndEnabled){
						slot.UpdateSlot();
					}
				}
			}
		}

		/// <summary>
		/// Resets the slot Z order to pose order.
		/// </summary>
		public void ResetSlotZOrder(){
			if(slots==null || slots.Length==0) return;

			if(isUGUI)
			{
				int count = slots.Length;
				for(int i=0;i<count;++i){
					Slot slot = slots[i];
					if(slot){
						slot.transform.SetSiblingIndex(slot.zOrder);
						#if UNITY_EDITOR 
						if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(slot.transform);
						#endif
					}
				}
			}
			else
			{
				float tempZ = m_FlipX || m_FlipY ? 1f : -1f;
				if(m_FlipX && m_FlipY) tempZ = -1f;

				tempZ*=zSpace;
				int len = slots.Length;
				for(int i=0;i<len;++i){
					Slot slot = slots[i];
					if(slot && !slot.manualZ){
						Vector3 v = slot.transform.localPosition;
						v.z = tempZ*slot.zOrder+tempZ;
						slot.transform.localPosition = v;
						slot.z = 0;
						if(sortType== SortType.ZOrder){
							slot.UpdateZOrder(slot.zOrder);
						}
						#if UNITY_EDITOR 
						if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(slot.transform);
						#endif
					}
				}
				m_SortedSlots = null;
			}

			m_OrderSlots.Clear();
			m_CanSortAllSlot = false;
		}

		/// <summary>
		/// Forces the sort all.
		/// </summary>
		public void ForceSortAll(){
			m_OrderSlots.Clear();
			int len = slots.Length;
			for(int i=0;i<len;++i){
				Slot slot = slots[i];
				if(slot){
					if(slot.z!=0) m_OrderSlots.Add(slot);
				}
			}
			if(m_OrderSlots.Count>0){
				CalculatZOrder();
			}
		}

		/// <summary>
		/// slot call this function
		/// </summary>
		/// <param name="slot">Slot.</param>
		protected internal void UpdateSlotZOrder(Slot slot){
			if(slot.z!=0) m_CanSortAllSlot = true;
		}

		#region Change Skin

		MaterialPropertyBlock m_MatBlock = null;
		MaterialPropertyBlock materialPropertyBlock{
			get { 
				if(m_MatBlock==null){
					return m_MatBlock = new MaterialPropertyBlock();
				}
				return m_MatBlock; 
			}
		}

		/// <summary>
		/// Gets attachment by name.
		/// </summary>
		/// <returns>The attachment.</returns>
		/// <param name="attachmentName">Attachment name.</param>
		public Renderer GetAttachmentByName( string attachmentName){
			foreach(Renderer r in attachments){
				if(r && r.name.Equals(attachmentName)) {
					return r;
				}
			}
			return null;
		}
		public MaskableGraphic GetUIAttachmentByName( string attachmentName){
			foreach(MaskableGraphic r in uiAttachments){
				if(r && r.name.Equals(attachmentName)) {
					return r;
				}
			}
			return null;
		}

		/// <summary>
		/// Changes the sprite frame.
		/// </summary>
		/// <param name="spriteFrameName">Will replace SpriteFrame's name.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteFrame(string spriteFrameName,Texture texture,Material mat=null,bool useMaterialBlock=true){
			if(string.IsNullOrEmpty(spriteFrameName) || !texture) return;

			Renderer attachment = GetAttachmentByName(spriteFrameName);
			if(!attachment) return;
			SpriteFrame sf = attachment.GetComponent<SpriteFrame>();
			ChangeSpriteFrame(sf,texture,mat,useMaterialBlock);
		}

		/// <summary>
		/// Changes the sprite frame.
		/// </summary>
		/// <param name="sf">Will replace SpriteFrame</param>
		/// <param name="texture">new Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteFrame(SpriteFrame sf,Texture texture,Material mat=null,bool useMaterialBlock=true){
			if(!sf || !texture) return;
		
			//new textureframe
			TextureFrame frame = new TextureFrame();
			frame.atlasTextureSize=new Vector2(texture.width,texture.height);
			frame.material = mat==null ? sf.material : mat;
			frame.rect.x = 0f;
			frame.rect.y = 0f;
			frame.rect.width = texture.width;
			frame.rect.height = texture.height;
			frame.frameSize = new Rect(0,0,texture.width,texture.height);
			frame.isRotated =false;
			if(!sf.isCreatedMesh){
				sf.CreateQuad();
			}
			sf.frame = frame;
			if(mat){
				sf.material = mat;
			}

			//change texture
			if(useMaterialBlock){
				sf.meshRenderer.GetPropertyBlock(materialPropertyBlock);
				m_MatBlock.SetTexture("_MainTex",texture);
				sf.meshRenderer.SetPropertyBlock(materialPropertyBlock);
			}else{
				sf.material.mainTexture = texture;
			}
		}

		/// <summary>
		/// Changes the sprite frame.
		/// </summary>
		/// <param name="spriteFrameName">Sprite frame name.</param>
		/// <param name="newFrameName">New frame name.</param>
		public void ChangeSpriteFrame(string spriteFrameName, string newFrameName){
			Renderer attachment = GetAttachmentByName(spriteFrameName);
			if(!attachment) return;

			SpriteFrame sf = attachment.GetComponent<SpriteFrame>();
			TextureFrame frame = textureFrames.GetTextureFrame(newFrameName);
			if(sf!=null && frame!=null){
				sf.frame = frame;
			}
		}

		/// <summary>
		/// Changes the sprite mesh.
		/// </summary>
		/// <param name="spriteMeshName">Will replace SpriteMesh'name.</param>
		/// <param name="texture">new Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteMesh(string spriteMeshName,Texture texture,Material mat=null,bool useMaterialBlock=true){
			Renderer attachment = GetAttachmentByName(spriteMeshName);
			if(!attachment) return;
			SpriteMesh sm = attachment.GetComponent<SpriteMesh>();
			ChangeSpriteMesh(sm,texture,mat,useMaterialBlock);
		}
		/// <summary>
		/// Changes the sprite mesh.
		/// </summary>
		/// <param name="sm">Will replace SpriteMesh</param>
		/// <param name="texture">new Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteMesh(SpriteMesh sm,Texture texture,Material mat=null,bool useMaterialBlock=true){
			if(!sm || !texture) return;

			TextureFrame frame = new TextureFrame();
			frame.material = mat==null ? sm.material : mat;
			frame.texture = texture;
			frame.isRotated = false;

			frame.rect.x = 0;
			frame.rect.y = 0;
			frame.rect.width = texture.width;
			frame.rect.height = texture.height;
			frame.frameSize = new Rect(0,0,texture.width,texture.height);
			frame.atlasTextureSize.x = texture.width;
			frame.atlasTextureSize.y = texture.height;
			if(!sm.isCreatedMesh){
				sm.CreateMesh();
			}
			sm.frame = frame;
			if(mat){
				sm.material = mat;
			}

			if(useMaterialBlock){
				sm.render.GetPropertyBlock(materialPropertyBlock);
				m_MatBlock.SetTexture("_MainTex",texture);
				sm.render.SetPropertyBlock(materialPropertyBlock);
			}else{
				sm.material.mainTexture = texture;
			}
		}


		/// <summary>
		/// Changes the sprite mesh.
		/// </summary>
		/// <param name="spriteMeshName">Sprite mesh name.</param>
		/// <param name="newTextureFrameName">New texture frame name.</param>
		public void ChangeSpriteMesh(string spriteMeshName, string newTextureFrameName){
			Renderer attachment = GetAttachmentByName(spriteMeshName);
			if(!attachment) return;

			SpriteMesh sm = attachment.GetComponent<SpriteMesh>();
			TextureFrame frame = textureFrames.GetTextureFrame(newTextureFrameName);
			if(sm!=null && frame!=null){
				sm.frame = frame;
			}
		}

		/// <summary>
		/// Changes the sprite renderer.
		/// </summary>
		/// <param name="spriteRendererName">Sprite renderer name.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="pivot">Pivot.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteRenderer(string spriteRendererName,Texture2D texture,Material mat=null){
			SpriteRenderer attachment = GetAttachmentByName(spriteRendererName) as SpriteRenderer;
			if(!attachment) return;

			if(mat!=null) attachment.material = mat;

			Sprite sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*0.5f,100f,1,SpriteMeshType.FullRect);
			attachment.sprite=sprite;
		}

		/// <summary>
		/// Changes the sprite renderer.
		/// </summary>
		/// <param name="spriteRendererName">Sprite renderer name.</param>
		/// <param name="sprite">Sprite.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteRenderer(string spriteRendererName,Sprite sprite,Material mat= null){
			SpriteRenderer attachment = GetAttachmentByName(spriteRendererName) as SpriteRenderer;
			if(!attachment) return;
			if(mat!=null) attachment.material = mat;
			attachment.sprite = sprite;
		}

		/// <summary>
		/// Changes the sprite of SpriteRenderer.
		/// </summary>
		/// <param name="sr">Will replace SpriteRenderer.</param>
		/// <param name="sprite">new Sprite.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeSpriteRenderer(SpriteRenderer sr,Sprite sprite,Material mat= null){
			if(mat!=null) sr.material = mat;
			sr.sprite = sprite;
		}







		//  change UI===================

		/// <summary>
		/// Changes the user interface frame.
		/// </summary>
		/// <param name="uiFrameName">User interface frame name.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeUIFrame(string uiFrameName,Texture texture,Material mat=null){
			if(string.IsNullOrEmpty(uiFrameName) || !texture) return;

			MaskableGraphic uiAttachment = GetUIAttachmentByName(uiFrameName);
			if(!uiAttachment) return;
			UIFrame sf = uiAttachment.GetComponent<UIFrame>();
			ChangeUIFrame(sf,texture,mat);
		}

		/// <summary>
		/// Changes the user interface frame.
		/// </summary>
		/// <param name="uf">Uf.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeUIFrame(UIFrame uf,Texture texture,Material mat=null){
			if(!uf || !texture) return;

			//new textureframe
			TextureFrame frame = new TextureFrame();
			frame.atlasTextureSize=new Vector2(texture.width,texture.height);
			frame.uiMaterial = mat==null ? uf.material : mat;
			frame.rect.x = 0f;
			frame.rect.y = 0f;
			frame.rect.width = texture.width;
			frame.rect.height = texture.height;
			frame.frameSize = new Rect(0,0,texture.width,texture.height);
			frame.isRotated =false;
			frame.rect = uf.frame.frameSize;
			if(!uf.isCreatedMesh){
				uf.CreateQuad();
			}
			uf.frame = frame;
			if(mat){
				uf.material = mat;
			}
			//change texture
			uf.texture = texture;
		}

		/// <summary>
		/// Changes the user interface frame.
		/// </summary>
		/// <param name="uiFrameName">User interface frame name.</param>
		/// <param name="newFrameName">New frame name.</param>
		public void ChangeUIFrame(string uiFrameName, string newFrameName){
			MaskableGraphic attachment = GetUIAttachmentByName(uiFrameName);
			if(!attachment) return;

			UIFrame uf = attachment.GetComponent<UIFrame>();
			TextureFrame frame = textureFrames.GetTextureFrame(newFrameName);
			if(uf!=null && frame!=null){
				uf.frame = frame;
			}
		}

		/// <summary>
		/// Changes the user interface mesh.
		/// </summary>
		/// <param name="uiMeshName">User interface mesh name.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeUIMesh(string uiMeshName,Texture texture,Material mat=null){
			MaskableGraphic attachment = GetUIAttachmentByName(uiMeshName);
			if(!attachment) return;
			UIMesh um = attachment.GetComponent<UIMesh>();
			ChangeUIMesh(um,texture,mat);
		}
		/// <summary>
		/// Changes the user interface mesh.
		/// </summary>
		/// <param name="um">Um.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeUIMesh(UIMesh um,Texture texture,Material mat=null){
			if(!um || !texture) return;

			TextureFrame frame = new TextureFrame();
			frame.uiMaterial = mat==null ? um.material : mat;
			frame.texture = texture;
			frame.isRotated = false;

			frame.rect.x = 0;
			frame.rect.y = 0;
			frame.rect.width = texture.width;
			frame.rect.height = texture.height;
			frame.frameSize = new Rect(0,0,texture.width,texture.height);
			frame.atlasTextureSize.x = texture.width;
			frame.atlasTextureSize.y = texture.height;
			if(!um.isCreatedMesh){
				um.CreateMesh();
			}
			um.frame = frame;
			if(mat){
				um.material = mat;
			}
			//change texture
			um.texture = texture;
		}


		/// <summary>
		/// Changes the user interface mesh.
		/// </summary>
		/// <param name="uiMeshName">User interface mesh name.</param>
		/// <param name="newTextureFrameName">New texture frame name.</param>
		public void ChangeUIMesh(string uiMeshName, string newTextureFrameName){
			MaskableGraphic attachment = GetUIAttachmentByName(uiMeshName);
			if(!attachment) return;

			UIMesh um = attachment.GetComponent<UIMesh>();
			TextureFrame frame = textureFrames.GetTextureFrame(newTextureFrameName);
			if(um!=null && frame!=null){
				um.frame = frame;
			}
		}

		/// <summary>
		/// Changes the image.
		/// </summary>
		/// <param name="imgName">Image name.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeImage(string imgName,Texture2D texture,Material mat=null){
			Image attachment = GetUIAttachmentByName(imgName) as Image;
			if(!attachment) return;

			if(mat!=null) attachment.material = mat;

			Sprite sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*0.5f,100f,1,SpriteMeshType.FullRect);
			attachment.sprite=sprite;
		}

		/// <summary>
		/// Changes the image.
		/// </summary>
		/// <param name="imgName">Image name.</param>
		/// <param name="sprite">Sprite.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeImage(string imgName,Sprite sprite,Material mat= null){
			Image attachment = GetUIAttachmentByName(imgName) as Image;
			if(!attachment) return;
			if(mat!=null) attachment.material = mat;
			attachment.sprite = sprite;
		}

		/// <summary>
		/// Changes the imge.
		/// </summary>
		/// <param name="img">Image.</param>
		/// <param name="sprite">Sprite.</param>
		/// <param name="mat">Mat.</param>
		public void ChangeImage(Image img,Sprite sprite,Material mat= null){
			if(mat!=null) img.material = mat;
			img.sprite = sprite;
		}
		#endregion
	}
}