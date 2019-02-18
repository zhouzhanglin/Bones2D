using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
#endif

namespace Bones2D{
	[System.Serializable]
	public class TextureFrame{
		public string name;
		public Texture texture;
		/// <summary>
		/// Texture Size
		/// </summary>
		public Rect rect;
		/// <summary>
		/// Real Size
		/// </summary>
		public Rect frameSize;
		public Material material;
		public Material uiMaterial;
		public Vector2 atlasTextureSize;
		public bool isRotated = false;

		//偏移
		public Vector3 frameOffset{
			get{
				Vector3 v = Vector3.zero;
				v.x = (rect.width-frameSize.width)*0.5f-frameSize.x;
				v.y = (frameSize.height-rect.height)*0.5f+frameSize.y;
				return v;
			}
		}
	}
}