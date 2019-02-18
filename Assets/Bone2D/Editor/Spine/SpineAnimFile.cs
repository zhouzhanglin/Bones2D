using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using CurveExtended;

/// <summary>
/// Spine animation file.
/// author:bingheliefeng
/// </summary>
namespace Bones2D
{
	public class SpineAnimFile {
		private static Dictionary<string,string> slotPathKV = null;
		private static AnimationClip poseClip;
		private static int tempZNumber = 0;

		public static void Dispose(){
			slotPathKV = null;
			poseClip = null;
		}

		public static void CreateAnimFile(SpineArmatureEditor armatureEditor)
		{
			armatureEditor.animList.Clear();
			slotPathKV = new Dictionary<string, string>();
			tempZNumber = 0;

			string path = AssetDatabase.GetAssetPath(armatureEditor.animTextAsset);
			path = path.Substring(0,path.LastIndexOf('/'))+"/"+armatureEditor.armature.name +"_Anims";
			if(!AssetDatabase.IsValidFolder(path)){
				Directory.CreateDirectory(path);
			}
			path+="/";

			Animator animator= armatureEditor.armature.gameObject.AddComponent<Animator>();
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path+armatureEditor.armature.name+".controller");
			if(armatureEditor.genAnimations)
			{
				AnimatorStateMachine rootStateMachine = null;
				if(controller==null){
					controller = AnimatorController.CreateAnimatorControllerAtPath(path+armatureEditor.armature.name+".controller");
					rootStateMachine = controller.layers[0].stateMachine;
				}
				animator.runtimeAnimatorController = controller;
				if(armatureEditor.armatureData.animations!=null)
				{
					//add empty state
					string clipPath = path+"pose.anim";
					poseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
					if(poseClip==null){
						poseClip = new AnimationClip();
						AssetDatabase.CreateAsset(poseClip,clipPath);
					}else{
						poseClip.ClearCurves();
					}
					if(rootStateMachine!=null){
						rootStateMachine.AddState("None");
	//					state.motion = poseClip;
					}
					//save
					SerializedObject serializedClip = new SerializedObject(poseClip);
					Bones2D.AnimationClipSettings clipSettings = new Bones2D.AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));
					clipSettings.loopTime = false;
					serializedClip.ApplyModifiedProperties();

					for(int i=0;i<armatureEditor.armatureData.animations.Length ;++i)
					{
						SpineData.AnimationData animationData = armatureEditor.armatureData.animations[i];
						armatureEditor.animList.Add(animationData.name);
						clipPath = path+animationData.name+".anim";
						AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
						bool loopTime = true;
						if(clip==null){
							clip = new AnimationClip();
							AssetDatabase.CreateAsset(clip,clipPath);
						}else{
							loopTime = clip.isLooping;
							clip.ClearCurves();
						}
						clip.name = animationData.name;
						clip.frameRate = armatureEditor.armatureData.frameRate;

						if(animationData.slotAnims!=null)
							CreateAnimSlot(armatureEditor ,clip,animationData.slotAnims);
						if(animationData.boneAnims!=null)
						{
							bool sucess = CreateAnimBone(armatureEditor ,clip,animationData.boneAnims);
							if(!sucess){
								return;
							}
						}
						if(animationData.deforms!=null)
							CreateAnimDeform(armatureEditor ,clip,animationData.deforms);
						if(animationData.events!=null)
							CreateAnimEvent( armatureEditor ,clip,animationData.events);
						if(animationData.drawOrders!=null)
							CreateAnimDrawOrder( armatureEditor ,clip,animationData.drawOrders);

						serializedClip = new SerializedObject(clip);
						clipSettings = new Bones2D.AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));
						clipSettings.loopTime = loopTime;
						serializedClip.ApplyModifiedProperties();

						if(rootStateMachine!=null){
							AnimatorState state = rootStateMachine.AddState(clip.name);
							state.motion = clip;
						}
					}
				}
				if(rootStateMachine!=null && rootStateMachine.states!=null && rootStateMachine.states.Length>0){
					rootStateMachine.defaultState= rootStateMachine.states[0].state;
				}
			}
			else
			{
				animator.runtimeAnimatorController = controller;
				for(int i=0;i<armatureEditor.armatureData.animations.Length ;++i)
				{
					SpineData.AnimationData animationData = armatureEditor.armatureData.animations[i];
					armatureEditor.animList.Add(animationData.name);
				}
			}
			//createAvatarMask
			if(armatureEditor.genAvatar)
				CreateAvatar(armatureEditor,animator,path);
			
			AssetDatabase.SaveAssets();
		}

		static void CreateAvatar( SpineArmatureEditor armatureEditor,Animator animator,string path){
			Avatar avatar = AvatarBuilder.BuildGenericAvatar(armatureEditor.armature.gameObject,"");
			animator.avatar = avatar;
			AvatarMask avatarMask = new AvatarMask();
			string[] transofrmPaths = GetTransformPaths(armatureEditor);
			avatarMask.transformCount = transofrmPaths.Length;
			for (int i=0; i< transofrmPaths.Length; i++){
				avatarMask.SetTransformPath(i, transofrmPaths[i]);
				avatarMask.SetTransformActive(i, true);
			}
			AssetDatabase.CreateAsset(avatar    , path + "/" + armatureEditor.armature.name + "Avatar.asset");
			AssetDatabase.CreateAsset(avatarMask, path + "/" + armatureEditor.armature.name + "Mask.asset");
		}

		static string[] GetTransformPaths(SpineArmatureEditor armatureEditor ){
			List<string> result = new List<string>();
			result.Add("");
			foreach(Transform t in armatureEditor.bones){
				string path = AnimationUtility.CalculateTransformPath(t,armatureEditor.armature);
				result.Add(path);
			}
			return result.ToArray();
		}

		/// <summary>
		/// set draw order
		/// </summary>
		static void CreateAnimDrawOrder(SpineArmatureEditor armatureEditor,AnimationClip clip,SpineData.AnimationDrawOrderData[] orderDatas)
		{
			int len = orderDatas.Length;
			AnimationCurve armatureCurve = new AnimationCurve();
			Dictionary<string ,AnimationCurve> slotZOrderKV = new Dictionary<string, AnimationCurve>();
			for(int i=0;i<len;++i){
				SpineData.AnimationDrawOrderData frameData = orderDatas[i];
				if(frameData.offsets!=null && frameData.offsets.Length>0){
					for(int z=0;z<frameData.offsets.Length;++z){
						SpineData.DrawOrderOffset offset = frameData.offsets[z];
						Slot slot = armatureEditor.slotsKV[offset.slotName].GetComponent<Slot>();

						string path = "";
						if(slotPathKV.ContainsKey(offset.slotName)){
							path = slotPathKV[offset.slotName];
						}else{
							path = GetNodeRelativePath(armatureEditor,slot.transform) ;
							slotPathKV[offset.slotName] = path;
						}

						AnimationCurve curve = null;
						if(slotZOrderKV.ContainsKey(offset.slotName)){
							curve = slotZOrderKV[offset.slotName];
						}else{
							curve = new AnimationCurve();
							slotZOrderKV[offset.slotName] = curve;
						}
						if(curve.length==0 && frameData.time>0){
							//first key
							curve.AddKey( new Keyframe(0,0,float.PositiveInfinity,float.PositiveInfinity));
						}
						if(orderDatas.Length==i+1){
							//last
							curve.AddKey( new Keyframe(orderDatas[orderDatas.Length-1].time,offset.offset,float.PositiveInfinity,float.PositiveInfinity));
						}
						curve.AddKey( new Keyframe(frameData.time,offset.offset,float.PositiveInfinity,float.PositiveInfinity));

						//set Armature zorder invalid
						++tempZNumber;
						armatureCurve.AddKey(new Keyframe(frameData.time,tempZNumber,float.PositiveInfinity,float.PositiveInfinity));
					}

					for(int z=0;z<armatureEditor.slots.Count;++z){
						Slot slot = armatureEditor.slots[z];
						bool flag = true;
						for(int t=0;t<frameData.offsets.Length;++t){
							string slotname = frameData.offsets[t].slotName;
							if(slot.name.Equals(slotname)){
								flag = false;
								break;
							}
						}
						if(flag)
						{
							string path = "";
							if(slotPathKV.ContainsKey(slot.name)){
								path = slotPathKV[slot.name];
							}else{
								path = GetNodeRelativePath(armatureEditor,slot.transform) ;
								slotPathKV[slot.name] = path;
							}

							AnimationCurve curve = null;
							if(slotZOrderKV.ContainsKey(slot.name)){
								curve = slotZOrderKV[slot.name];
								curve.AddKey( new Keyframe(frameData.time,0,float.PositiveInfinity,float.PositiveInfinity));
							}
						}
					}
				}
				else
				{
					for(int z=0;z<armatureEditor.slots.Count;++z){
						Slot slot = armatureEditor.slots[z];
						string path = "";
						if(slotPathKV.ContainsKey(slot.name)){
							path = slotPathKV[slot.name];
						}else{
							path = GetNodeRelativePath(armatureEditor,slot.transform) ;
							slotPathKV[slot.name] = path;
						}

						AnimationCurve curve = null;
						if(slotZOrderKV.ContainsKey(slot.name)){
							curve = slotZOrderKV[slot.name];
						}else{
							curve = new AnimationCurve();
							slotZOrderKV[slot.name] = curve;
						}
						curve.AddKey( new Keyframe(frameData.time,0,float.PositiveInfinity,float.PositiveInfinity));
					}

					//set Armature zorder invalid
					++tempZNumber;
					armatureCurve.AddKey(new Keyframe(frameData.time,tempZNumber,float.PositiveInfinity,float.PositiveInfinity));
				}
			}
			foreach(string name in slotZOrderKV.Keys)
			{
				AnimationCurve zOrderCurve = slotZOrderKV[name];
				CurveExtension.OptimizesCurve(zOrderCurve);
				if(zOrderCurve!=null && zOrderCurve.keys!=null && zOrderCurve.keys.Length>0 && CheckCurveValid(zOrderCurve,0)){
					clip.SetCurve(slotPathKV[name],typeof(Slot),"m_z",zOrderCurve);
				}
			}

			if(armatureCurve.keys.Length>0){
				clip.SetCurve("",typeof(Armature),"m_ZOrderValid",armatureCurve);

				//add pose
				++tempZNumber;
				AnimationCurve posezordercurve = new AnimationCurve();
				posezordercurve.AddKey(new Keyframe(0f,tempZNumber));
				AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve("",typeof( Armature ), "m_ZOrderValid" ),posezordercurve);
			}
		}



		/// <summary>
		/// set events
		/// </summary>
		static void CreateAnimEvent( SpineArmatureEditor armatureEditor,AnimationClip clip,SpineData.AnimationEventsData[] eventDatas)
		{
			if(eventDatas==null || eventDatas.Length==0) return;

			if(armatureEditor.armature.gameObject.GetComponent<AnimEvent>()==null)
				armatureEditor.armature.gameObject.AddComponent<AnimEvent>();

			List<AnimationEvent> evts=new List<AnimationEvent>();
			foreach(SpineData.AnimationEventsData animEvtData in eventDatas)
			{
				AnimationEvent ae = new AnimationEvent();
				ae.messageOptions = SendMessageOptions.DontRequireReceiver;

				string param = animEvtData.name+"$";
				if(!string.IsNullOrEmpty(animEvtData.stringParam))
				{
					param+=animEvtData.stringParam;
				}

				ae.functionName = "OnAnimEvent";
				ae.time = animEvtData.time;
				ae.stringParameter = param;
				ae.intParameter = animEvtData.intParam;
				ae.floatParameter = animEvtData.floatParam;
				evts.Add(ae);
			}
			if(evts.Count>0){
				AnimationUtility.SetAnimationEvents(clip,evts.ToArray());
			}

		}


		static bool CreateAnimBone(SpineArmatureEditor armatureEditor , AnimationClip clip, SpineData.AnimationBoneData[] animBoneDatas){
			Dictionary<string,string> bonePathKV= new Dictionary<string, string>();
			for(int i=0;i<animBoneDatas.Length;++i){
				SpineData.AnimationBoneData animBoneData = animBoneDatas[i];
				Transform bone = armatureEditor.bonesKV[animBoneData.name];

				AnimationCurve xcurve = new AnimationCurve();
				AnimationCurve ycurve = new AnimationCurve();
				AnimationCurve sxcurve = new AnimationCurve();
				AnimationCurve sycurve = new AnimationCurve();
				AnimationCurve rotatecurve = new AnimationCurve();

				bool isHaveCurve = false;
				for(int j=0;j<animBoneData.timelines.Length;++j){
					SpineData.BoneTimeline timeline = animBoneData.timelines[j];
					string prevTweeneasing = "linear";//前一帧的tweenEasing
					float[] prevCurves = null;
					if(j>0) {
						prevTweeneasing = animBoneData.timelines[j-1].tweenEasing;
						prevCurves = animBoneData.timelines[j-1].curve;
					}
					TangentMode tanModeL = GetPrevFrameTangentMode(prevTweeneasing,prevCurves);
					TangentMode tanModeR = TangentMode.Linear;

					if(timeline.curve!=null && timeline.curve.Length>0){
						tanModeR = TangentMode.Editable;
						isHaveCurve = true;
					}else{
						if(timeline.tweenEasing=="stepped"){
							tanModeR = TangentMode.Stepped;
						}else{
							tanModeR = TangentMode.Linear;
						}
					}
					if(timeline.type=="rotate")//rotate,scale,translate,shear
					{
						if(!float.IsNaN(timeline.angle)) {
							float rotate = timeline.angle+bone.localEulerAngles.z;
							rotatecurve.AddKey(KeyframeUtil.GetNew(timeline.time,rotate,tanModeL,tanModeR));
						}
					}
					else if(timeline.type=="translate")
					{
						if(!float.IsNaN(timeline.x)) {
							xcurve.AddKey(KeyframeUtil.GetNew(timeline.time,timeline.x+bone.localPosition.x,tanModeL,tanModeR));
						}
						if(!float.IsNaN(timeline.y)) {
							ycurve.AddKey(KeyframeUtil.GetNew(timeline.time,timeline.y+bone.localPosition.y,tanModeL,tanModeR));
						}
					}
					else if(timeline.type=="scale")
					{
						if(!float.IsNaN(timeline.x)){
							sxcurve.AddKey(KeyframeUtil.GetNew(timeline.time,timeline.x*bone.localScale.x,tanModeL,tanModeR));
						}
						if(!float.IsNaN(timeline.y)){
							sycurve.AddKey(KeyframeUtil.GetNew(timeline.time,timeline.y*bone.localScale.y,tanModeL,tanModeR));
						}
					}
				}
				CurveExtension.OptimizesCurve(xcurve);
				CurveExtension.OptimizesCurve(ycurve);
				CurveExtension.OptimizesCurve(sxcurve);
				CurveExtension.OptimizesCurve(sycurve);
				CurveExtension.OptimizesCurve(rotatecurve);

				string path = "";
				if(bonePathKV.ContainsKey(bone.name)){
					path = bonePathKV[bone.name];
				}else{
					path = GetNodeRelativePath(armatureEditor,bone) ;
					bonePathKV[bone.name] = path;

					if(slotPathKV.ContainsKey(bone.name) && slotPathKV[bone.name].Equals(path)){
						Debug.LogError("Bone2D Error: Name conflict ->"+path);
						return false;
					}
				}

				bool localPosFlag = false;
				if(xcurve.keys !=null && xcurve.keys.Length>0 && CheckCurveValid(xcurve,bone.localPosition.x)) localPosFlag = true;
				if(ycurve.keys !=null && ycurve.keys.Length>0 && CheckCurveValid(ycurve,bone.localPosition.y))  localPosFlag = true;
				if(localPosFlag){
					if(isHaveCurve) SetCustomCurveTangents(xcurve,animBoneData.timelines);
					CurveExtension.UpdateAllLinearTangents(xcurve);
					AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalPosition.x" ), xcurve );
					if(isHaveCurve) SetCustomCurveTangents(ycurve,animBoneData.timelines);
					CurveExtension.UpdateAllLinearTangents(ycurve);
					AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalPosition.y" ), ycurve );

					//add pose
					AnimationCurve posexcurve = new AnimationCurve();
					AnimationCurve poseycurve = new AnimationCurve();
					posexcurve.AddKey(new Keyframe(0f,bone.localPosition.x));
					poseycurve.AddKey(new Keyframe(0f,bone.localPosition.y));
					AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalPosition.x" ),posexcurve);
					AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalPosition.y" ),poseycurve);
				}

				Bone myBone = bone.GetComponent<Bone>();
				string scPath = path;
				if(myBone && myBone.inheritScale )
				{
					scPath = myBone.inheritScale.name ;
				}
				bool localSc = false;
				if(sxcurve.keys !=null && sxcurve.keys.Length>0 && CheckCurveValid(sxcurve,bone.localScale.x)) localSc=true;
				if(sycurve.keys !=null && sycurve.keys.Length>0 && CheckCurveValid(sycurve,bone.localScale.y)) localSc=true;
				if(localSc){
					if(isHaveCurve) SetCustomCurveTangents(sxcurve,animBoneData.timelines);
					CurveExtension.UpdateAllLinearTangents(sxcurve);
					AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( scPath, typeof( Transform ), "m_LocalScale.x" ), sxcurve );
					if(isHaveCurve) SetCustomCurveTangents(sycurve,animBoneData.timelines);
					CurveExtension.UpdateAllLinearTangents(sycurve);
					AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( scPath, typeof( Transform ), "m_LocalScale.y" ), sycurve );

					//add pose
					AnimationCurve posesxcurve = new AnimationCurve();
					AnimationCurve posesycurve = new AnimationCurve();
					posesxcurve.AddKey(new Keyframe(0f,bone.localScale.x));
					posesycurve.AddKey(new Keyframe(0f,bone.localScale.y));
					AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalScale.x" ),posesxcurve);
					AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "m_LocalScale.y" ),posesycurve);
				}

				string rotatePath = path;
				if(myBone && myBone.inheritRotation )
				{
					rotatePath = myBone.inheritRotation.name ;
				}
				if(rotatecurve.keys !=null && rotatecurve.keys.Length>0 && CheckCurveValid(rotatecurve,bone.localEulerAngles.z)){
					CurveExtension.ClampCurveRotate360(rotatecurve,false);
					if(isHaveCurve) SetCustomCurveTangents(rotatecurve,animBoneData.timelines);
					CurveExtension.UpdateAllLinearTangents(rotatecurve);
					clip.SetCurve(rotatePath,typeof(Transform),"localEulerAngles.z",rotatecurve);

					//add pose
					AnimationCurve posesrotatecurve = new AnimationCurve();
					posesrotatecurve.AddKey(new Keyframe(0f,bone.localEulerAngles.z));
					AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve( path, typeof( Transform ), "localEulerAngles.z" ),posesrotatecurve);
				}
			}
			return true;
		}


		static void CreateAnimDeform(SpineArmatureEditor armatureEditor , AnimationClip clip, SpineData.AnimationDeformData[] animDeformDatas){
			string[] skins= armatureEditor.armature.GetComponent<Armature>().skins;
			bool multiSkin = (skins==null || skins.Length<=1 )? false : true ;
			for(int i=0;i<animDeformDatas.Length;++i){
				SpineData.AnimationDeformData animDeformData = animDeformDatas[i];
				if(animDeformData.timelines==null || animDeformData.timelines.Length==0) continue;

				Dictionary<string,AnimationCurve[]> xCurveKV = new Dictionary<string, AnimationCurve[]>();//key is attachment name
				Dictionary<string,AnimationCurve[]> yCurveKV = new Dictionary<string, AnimationCurve[]>();

				Transform slot = armatureEditor.slotsKV[animDeformData.slotName];

				bool isHaveCurve = false;
				for(int j=0;j<animDeformData.timelines.Length;++j){
					SpineData.DeformTimeline timeline = animDeformData.timelines[j];
					Transform attachment = multiSkin? slot.Find(animDeformData.skinName+"/"+timeline.attachment) : slot.Find(timeline.attachment);
					string prevTweeneasing = "linear";//前一帧的tweenEasing
					float[] prevCurves = null;
					if(j>0) {
						prevTweeneasing = animDeformData.timelines[j-1].tweenEasing;
						prevCurves = animDeformData.timelines[j-1].curve;
					}
					TangentMode tanModeL = GetPrevFrameTangentMode(prevTweeneasing,prevCurves);
					TangentMode tanModeR = TangentMode.Linear;

					if(timeline.curve!=null && timeline.curve.Length>0){
						tanModeR = TangentMode.Editable;
						isHaveCurve = true;
					}else{
						if(timeline.tweenEasing=="stepped"){
							tanModeR = TangentMode.Stepped;
						}else{
							tanModeR = TangentMode.Linear;
						}
					}
					if(!xCurveKV.ContainsKey(timeline.attachment)) {
						xCurveKV[timeline.attachment] = new AnimationCurve[attachment.childCount];
						yCurveKV[timeline.attachment] = new AnimationCurve[attachment.childCount];
					}
					AnimationCurve[] xCurveArray = xCurveKV[timeline.attachment];
					AnimationCurve[] yCurveArray = yCurveKV[timeline.attachment];

					int len = attachment.childCount;
					if(timeline.vertices!=null && timeline.vertices.Length>0)
					{ 
						for(int r =0;r<len;++r){
							if(xCurveArray[r]==null) {
								xCurveArray[r] = new AnimationCurve();
								yCurveArray[r] = new AnimationCurve();
							}
							AnimationCurve xCurve = xCurveArray[r];
							AnimationCurve yCurve = yCurveArray[r];

							Transform vCtr = attachment.GetChild(r);//vertex control
							if(r>=timeline.offset && r-timeline.offset<timeline.vertices.Length){
								Vector3 v = timeline.vertices[r-timeline.offset];
								v += vCtr.localPosition;
								Keyframe kfx = KeyframeUtil.GetNew(timeline.time,v.x,tanModeL,tanModeR);
								xCurve.AddKey(kfx);
								Keyframe kfy = KeyframeUtil.GetNew(timeline.time,v.y,tanModeL,tanModeR);
								yCurve.AddKey(kfy);
							}
							else
							{
								Keyframe kfx = KeyframeUtil.GetNew(timeline.time,vCtr.localPosition.x,tanModeL,tanModeR);
								xCurve.AddKey(kfx);
								Keyframe kfy = KeyframeUtil.GetNew(timeline.time,vCtr.localPosition.y,tanModeL,tanModeR);
								yCurve.AddKey(kfy);
							}
						}
					}
					else
					{
						//add default vertex position
						for(int r =0;r<len;++r){
							if(xCurveArray[r]==null) {
								xCurveArray[r] = new AnimationCurve();
								yCurveArray[r] = new AnimationCurve();
							}
							AnimationCurve xCurve = xCurveArray[r];
							AnimationCurve yCurve = yCurveArray[r];

							Transform vCtr = attachment.GetChild(r);//vertex control
							Keyframe kfx = KeyframeUtil.GetNew(timeline.time,vCtr.localPosition.x,tanModeL,tanModeR);
							xCurve.AddKey(kfx);
							Keyframe kfy = KeyframeUtil.GetNew(timeline.time,vCtr.localPosition.y,tanModeL,tanModeR);
							yCurve.AddKey(kfy);
						}
					}
				}
				string path = "";
				if(slotPathKV.ContainsKey(slot.name)){
					path = slotPathKV[slot.name];
				}else{
					path = GetNodeRelativePath(armatureEditor,slot) ;
					slotPathKV[slot.name] = path;
				}
				if(multiSkin){
					path+="/"+animDeformData.skinName;
				}

				foreach(string attachmentName in xCurveKV.Keys){
					AnimationCurve[] vertex_xcurves= xCurveKV[attachmentName];
					AnimationCurve[] vertex_ycurves= yCurveKV[attachmentName];

					Transform attachment =  multiSkin? slot.Find(animDeformData.skinName+"/"+attachmentName) : slot.Find(attachmentName);
					int len = attachment.childCount;
					for(int r=0;r<len;++r){
						AnimationCurve vertex_xcurve = vertex_xcurves[r];
						AnimationCurve vertex_ycurve = vertex_ycurves[r];
						Transform v = attachment.GetChild(r);
						string ctrlPath = path+"/"+attachment.name+"/"+v.name;

						CurveExtension.OptimizesCurve(vertex_xcurve);
						CurveExtension.OptimizesCurve(vertex_ycurve);

						bool vcurveFlag = false;
						if(vertex_xcurve.keys !=null&& vertex_xcurve.keys.Length>0&& CheckCurveValid(vertex_xcurve,v.localPosition.x)) vcurveFlag = true;
						if(vertex_ycurve.keys !=null&& vertex_ycurve.keys.Length>0&& CheckCurveValid(vertex_ycurve,v.localPosition.y)) vcurveFlag=  true;
						if(vcurveFlag){
							if(isHaveCurve) SetCustomCurveTangents(vertex_xcurve,animDeformData.timelines);
							CurveExtension.UpdateAllLinearTangents(vertex_xcurve);
							AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( ctrlPath, typeof( Transform ), "m_LocalPosition.x" ), vertex_xcurve );
							if(isHaveCurve) SetCustomCurveTangents(vertex_ycurve,animDeformData.timelines);
							CurveExtension.UpdateAllLinearTangents(vertex_ycurve);
							AnimationUtility.SetEditorCurve( clip, EditorCurveBinding.FloatCurve( ctrlPath, typeof( Transform ), "m_LocalPosition.y" ), vertex_ycurve );

							//add pose
							AnimationCurve pose_vertex_xcurve = new AnimationCurve();
							AnimationCurve pose_vertex_ycurve = new AnimationCurve();
							pose_vertex_xcurve.AddKey(new Keyframe(0f,v.localPosition.x));
							pose_vertex_ycurve.AddKey(new Keyframe(0f,v.localPosition.y));
							AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve(ctrlPath,typeof( Transform ), "m_LocalPosition.x" ),pose_vertex_xcurve);
							AnimationUtility.SetEditorCurve(poseClip, EditorCurveBinding.FloatCurve(ctrlPath,typeof( Transform ), "m_LocalPosition.y" ),pose_vertex_ycurve);
						}
					}
				}
			}
		}




		static void CreateAnimSlot(SpineArmatureEditor armatureEditor , AnimationClip clip, SpineData.AnimationSlotData[] animSlotDatas){
			for(int i=0;i<animSlotDatas.Length;++i){
				SpineData.AnimationSlotData animSlotData = animSlotDatas[i];
				string slotName = animSlotData.name;
				Transform slot = armatureEditor.slotsKV[slotName];
				SpineData.SlotData defaultSlotData = armatureEditor.slotsDataKV[slotName];
				Color defaultColorData = defaultSlotData.color ;

				AnimationCurve color_rcurve = new AnimationCurve();
				AnimationCurve color_gcurve = new AnimationCurve();
				AnimationCurve color_bcurve = new AnimationCurve();
				AnimationCurve color_acurve = new AnimationCurve();

				AnimationCurve display_curve = new AnimationCurve();

				bool isHaveCurve = false;
				for(int j=0;j<animSlotData.timelines.Length;++j){
					SpineData.SlotTimeline timeline = animSlotData.timelines[j];
					string prevTweeneasing = "linear";//前一帧的tweenEasing
					float[] prevCurves = null;
					if(j>0) {
						prevTweeneasing = animSlotData.timelines[j-1].tweenEasing;
						prevCurves = animSlotData.timelines[j-1].curve;
					}
					TangentMode tanModeL = GetPrevFrameTangentMode(prevTweeneasing,prevCurves);
					TangentMode tanModeR = TangentMode.Linear;

					if(timeline.curve!=null && timeline.curve.Length>0){
						tanModeR = TangentMode.Editable;
						isHaveCurve = true;
					}else{
						if(timeline.tweenEasing=="stepped"){
							tanModeR = TangentMode.Stepped;
						}else{
							tanModeR = TangentMode.Linear;
						}
					}

					if(timeline.type=="color")
					{
						if(!string.IsNullOrEmpty(timeline.color)){
							Color c = SpineArmatureEditor.HexToColor(timeline.color);
							color_rcurve.AddKey(KeyframeUtil.GetNew(timeline.time,c.r,tanModeL,tanModeR));
							color_gcurve.AddKey(KeyframeUtil.GetNew(timeline.time,c.g,tanModeL,tanModeR));
							color_bcurve.AddKey(KeyframeUtil.GetNew(timeline.time,c.b,tanModeL,tanModeR));
							color_acurve.AddKey(KeyframeUtil.GetNew(timeline.time,c.a,tanModeL,tanModeR));//*defaultColorData.a
						}else if(color_acurve.length>0){
							color_rcurve.AddKey(KeyframeUtil.GetNew(timeline.time,defaultColorData.r,tanModeL,tanModeR));
							color_gcurve.AddKey(KeyframeUtil.GetNew(timeline.time,defaultColorData.g,tanModeL,tanModeR));
							color_bcurve.AddKey(KeyframeUtil.GetNew(timeline.time,defaultColorData.b,tanModeL,tanModeR));
							color_acurve.AddKey(KeyframeUtil.GetNew(timeline.time,defaultColorData.a,tanModeL,tanModeR));
						}
					}
					else if(timeline.type=="attachment")
					{
						if(string.IsNullOrEmpty(timeline.attachmentName)){
							display_curve.AddKey(new Keyframe(timeline.time,-1f,float.PositiveInfinity,float.PositiveInfinity));
						}else{
							for(int r=0;r<slot.childCount;++r){
								if(slot.GetChild(r).name.Equals(timeline.attachmentName)){
									display_curve.AddKey(new Keyframe(timeline.time,r,float.PositiveInfinity,float.PositiveInfinity));
									break;
								}
							}
						}
					}
				}
				CurveExtension.OptimizesCurve(color_rcurve);
				CurveExtension.OptimizesCurve(color_gcurve);
				CurveExtension.OptimizesCurve(color_bcurve);
				CurveExtension.OptimizesCurve(color_acurve);
				CurveExtension.OptimizesCurve(display_curve);

				string path = "";
				if(slotPathKV.ContainsKey(slot.name)){
					path = slotPathKV[slot.name];
				}else{
					path = GetNodeRelativePath(armatureEditor,slot) ;
					slotPathKV[slot.name] = path;
				}

				SetColorCurve<Slot>(path,clip,color_rcurve,"color.r",isHaveCurve,defaultColorData.r,animSlotData.timelines);
				SetColorCurve<Slot>(path,clip,color_gcurve,"color.g",isHaveCurve,defaultColorData.g,animSlotData.timelines);
				SetColorCurve<Slot>(path,clip,color_bcurve,"color.b",isHaveCurve,defaultColorData.b,animSlotData.timelines);
				SetColorCurve<Slot>(path,clip,color_acurve,"color.a",isHaveCurve,defaultColorData.a,animSlotData.timelines);

				//add pose
				AnimationCurve pose_color_rcurve = new AnimationCurve();
				AnimationCurve pose_color_gcurve = new AnimationCurve();
				AnimationCurve pose_color_bcurve = new AnimationCurve();
				AnimationCurve pose_color_acurve = new AnimationCurve();
				pose_color_rcurve.AddKey(new Keyframe(0f,defaultColorData.r));
				pose_color_gcurve.AddKey(new Keyframe(0f,defaultColorData.g));
				pose_color_bcurve.AddKey(new Keyframe(0f,defaultColorData.b));
				pose_color_acurve.AddKey(new Keyframe(0f,defaultColorData.a));
				AnimationUtility.SetEditorCurve(poseClip,EditorCurveBinding.FloatCurve(path,typeof(Slot),"color.r"),pose_color_rcurve);
				AnimationUtility.SetEditorCurve(poseClip,EditorCurveBinding.FloatCurve(path,typeof(Slot),"color.g"),pose_color_gcurve);
				AnimationUtility.SetEditorCurve(poseClip,EditorCurveBinding.FloatCurve(path,typeof(Slot),"color.b"),pose_color_bcurve);
				AnimationUtility.SetEditorCurve(poseClip,EditorCurveBinding.FloatCurve(path,typeof(Slot),"color.a"),pose_color_acurve);

				if(display_curve.keys!=null && display_curve.keys.Length>0 && 
					CheckCurveValid(display_curve,slot.GetComponent<Slot>().displayIndex))
				{
					clip.SetCurve(path,typeof(Slot),"m_DisplayIndex",display_curve);
					//add pose
					AnimationCurve pose_display_curve = new AnimationCurve();
					pose_display_curve.AddKey(new Keyframe(0f,slot.GetComponent<Slot>().displayIndex));
					AnimationUtility.SetEditorCurve(poseClip,EditorCurveBinding.FloatCurve(path,typeof(Slot),"m_DisplayIndex"),pose_display_curve);
				}

			}
		}

		static bool SetColorCurve<T>(string path,AnimationClip clip, AnimationCurve curve,string prop, bool isHaveCurve,float defaultVal,SpineData.SlotTimeline[] timelines){
			if(curve.keys !=null&& curve.keys.Length>0&& CheckCurveValid(curve,defaultVal)) 
			{
				if(isHaveCurve) SetCustomCurveTangents(curve,timelines);
				CurveExtension.UpdateAllLinearTangents(curve);
				AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(T),prop),curve);
				return true;
			}
			return false;
		}

		static TangentMode GetPrevFrameTangentMode(string easingTween,float[] curves){
			if(curves!=null && curves.Length>0) return TangentMode.Editable;

			if(easingTween=="stepped"){
				return TangentMode.Stepped;
			}
			return TangentMode.Linear;
		}
		static string GetNodeRelativePath(SpineArmatureEditor armatureEditor ,Transform node){
			List<string> path = new List<string>();
			while(node!=armatureEditor.armature)
			{
				path.Add(node.name);
				node = node.parent;
			}
			string result="";
			for(int i=path.Count-1;i>=0;i--){
				result+=path[i]+"/";
			}
			return result.Substring(0,result.Length-1);
		}
		//check invalid curve
		static bool CheckCurveValid(AnimationCurve curve , float defaultValue){
			Keyframe frame = curve.keys[0];
			if(curve.length==1) {
				if(frame.value==defaultValue) return false;
				return true;
			}
			for(int i=0;i<curve.keys.Length;++i){
				Keyframe frame2 = curve.keys[i];
				if(frame.value!=defaultValue || frame.value!=frame2.value) {
					return true;
				}
			}
			return false;
		}
		static void SetCustomCurveTangents(AnimationCurve curve, SpineData.BaseTimeline[] timelines){
			int len=curve.keys.Length;
			for (int i = 0; i < len; i++) {
				int nextI = i + 1;
				if (nextI < curve.keys.Length){
					if (timelines[i].curve != null ){ 
						CurveExtension.SetCustomTangents(curve, i, nextI, timelines[i].curve);
					}
				}
			}
		}
	}


}