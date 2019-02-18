using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Bones2D{
	
	public class TextureFrames : ScriptableObject{
		public TextureFrame[] frames;
		public Material[] materials;

		/// <summary>
		/// Get Frame by Name
		/// </summary>
		/// <returns>The texture frame.</returns>
		/// <param name="name">Name.</param>
		public TextureFrame GetTextureFrame(string name){
			if(frames!=null){
				int len = frames.Length;
				for(int i=0;i<len;++i){
					TextureFrame frame = frames[i];
					if(frame.name.Equals(name)){
						return frame;
					}
				}
			}
			return null;
		}

		#if UNITY_EDITOR

		public void UpdateFrames(){
			if(frames!=null){
				int len = frames.Length;
				bool temp = false;
				for(int i=0;i<len;++i){
					TextureFrame frame = frames[i];
					if(frame.texture==null){
						if(frame.material && frame.material.mainTexture){
							frame.texture = frame.material.mainTexture;
							temp = true;
						}
						if(frame.uiMaterial && frame.uiMaterial.mainTexture){
							frame.texture = frame.uiMaterial.mainTexture;
							temp = true;
						}
					}
				}
				if(temp){
					AssetDatabase.Refresh();
					EditorUtility.SetDirty(this);
					AssetDatabase.SaveAssets();
				}
			}

		}

		public static List<TextureFrame> ParseSpineAtlasText(string atlasText,Material material){
			if(atlasText!=null && material!=null && material.mainTexture!=null)
			{
				Vector2 textureSize = GetTextureSize(material.mainTexture);
				List<TextureFrame> framesList = new List<TextureFrame>();
				using(TextReader reader = new StringReader(atlasText))
				{
					string pngName = null;
					string frameName="";
					TextureFrame frame = null;
					while (reader.Peek() != -1)
					{
						string line = reader.ReadLine().Trim();
						if(line.Length>0){
							if(line.LastIndexOf(".png")>-1||line.LastIndexOf(".PNG")>-1)
							{
								if(line.Contains(material.mainTexture.name)){
									if(pngName!=null) break;//have a png name , over

									pngName = line;
								}else{
									pngName = null;
								}
							}
							if(pngName==null) continue;

							if(line.IndexOf(":")==-1) frameName = line;
							else{
								string[] lineArray = line.Split(':');
								string key = lineArray[0].Trim();
								string value = lineArray[1].Trim();
								if(key.ToLower()=="rotate"){
									frame =new TextureFrame();
									frame.texture = material.mainTexture;
									frame.atlasTextureSize = textureSize;
									if(material.name.LastIndexOf("_UI_Mat")>-1){
										frame.uiMaterial = material;
									}else{
										frame.material = material;
									}
									framesList.Add(frame);

									frame.isRotated = value.ToLower()=="true";
									frame.name = frameName;
								}
								else if(key=="xy"){
									string[] xy = value.Split(',');
									frame.rect.x = float.Parse(xy[0].Trim());
									frame.rect.y = float.Parse(xy[1].Trim());
								}else if(key=="size"){
									if(frame!=null){
										string[] size = value.Split(',');
										if(frame.isRotated){
											frame.rect.height = float.Parse(size[0].Trim());
											frame.rect.width = float.Parse(size[1].Trim());
										}else{
											frame.rect.width = float.Parse(size[0].Trim());
											frame.rect.height = float.Parse(size[1].Trim());
										}
									}
								}else if(key=="orig"){
									string[] orig = value.Split(',');
									if(frame.isRotated){
										frame.frameSize.height = float.Parse(orig[0].Trim());
										frame.frameSize.width = float.Parse(orig[1].Trim());
									}else{
										frame.frameSize.width = float.Parse(orig[0].Trim());
										frame.frameSize.height = float.Parse(orig[1].Trim());
									}
								}else if(key=="offset"){
									string[] xy = value.Split(',');
									frame.frameSize.x = float.Parse(xy[0].Trim());
									frame.frameSize.y = float.Parse(xy[1].Trim());
								}else if(key=="index"){

								}
							}
						}
					}
					reader.Close();
				}
				return framesList;
			}
			return null;
		}

		public  static List<TextureFrame> ParseDragonBoneAtlasText(string atlasText,Material material){
			if(atlasText!=null && material!=null && material.mainTexture!=null)
			{
				Bones2D.JSONClass obj = Bones2D.JSON.Parse(atlasText).AsObject ;
				Bones2D.JSONArray arr = obj["SubTexture"].AsArray;
				List<TextureFrame> framesList = new List<TextureFrame>();
				Vector2 textureSize = GetTextureSize(material.mainTexture);
				for(int i=0;i<arr.Count;++i){
					Bones2D.JSONClass frameObj = arr[i].AsObject;
					TextureFrame frame = new TextureFrame();
					frame.atlasTextureSize = textureSize;
					frame.name = frameObj["name"];
					frame.name = frame.name.Replace('/','_');
					frame.texture = material.mainTexture;
					if(material.name.LastIndexOf("_UI_Mat")>-1){
						frame.uiMaterial = material;
					}else{
						frame.material = material;
					}
					Rect rect = new Rect();
					rect.x = frameObj["x"].AsFloat;
					rect.y = frameObj["y"].AsFloat;
					rect.width = frameObj["width"].AsFloat;
					rect.height = frameObj["height"].AsFloat;
					Rect frameSize=new Rect(0,0,rect.width,rect.height);
					if(frameObj.ContainKey("frameX")){
						frameSize.x = frameObj["frameX"].AsFloat;
					}
					if(frameObj.ContainKey("frameY")){
						frameSize.y = frameObj["frameY"].AsFloat;
					}
					if(frameObj.ContainKey("frameWidth")){
						frameSize.width = frameObj["frameWidth"].AsFloat;
					}
					if(frameObj.ContainKey("frameHeight")){
						frameSize.height = frameObj["frameHeight"].AsFloat;
					}
					frame.rect = rect;
					frame.frameSize = frameSize;
					framesList.Add(frame);
				}
				return framesList;
			}
			return null;
		}

		static Texture2D LoadPNG(string filePath) {
			Texture2D tex = null;
			byte[] fileData;

			if (File.Exists(filePath))     {
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData);
			}
			return tex;
		}

		static Vector2 GetTextureSize(Texture texture){
			Vector2 v = new Vector2();
			Texture t = LoadPNG(Application.dataPath+"/"+AssetDatabase.GetAssetPath(texture).Substring(6));
			v.x = t.width;
			v.y = t.height;
			DestroyImmediate(t);
			return v;
		}
		#endif
	}
}