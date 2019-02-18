using UnityEngine;
using System.Collections;
/// <summary>
/// Event data.
/// author:bingheliefeng
/// </summary>
namespace Bones2D
{
	[System.Serializable]
	public class EventData
	{
		public string eventName;
		public string stringParam;
		public int intParam=0;
		public float floatParam=0f;
		public string sound; //for dragonBone
	}

	[DisallowMultipleComponent]
	public class AnimEvent : MonoBehaviour {

		public event System.Action<EventData> onDragonBoneEvent;

		/// <summary>
		/// Animation Event
		/// </summary>
		/// <param name="stringParam">String parameter.</param>
		/// <param name="intParam">Int parameter.</param>
		/// <param name="floatParam">Float parameter.</param>
		public void OnAnimEvent(AnimationEvent evt){
			if(onDragonBoneEvent!=null)
			{
				EventData evtData = new EventData();
				if(!string.IsNullOrEmpty(evt.stringParameter) && evt.stringParameter.Length>0){
					string[] param =  evt.stringParameter.Split('$');
					evtData.eventName = param[0];
					if(param.Length>1 && !string.IsNullOrEmpty(param[1])){
						evtData.stringParam = param[1];
					}
					if(param.Length>2 && !string.IsNullOrEmpty(param[2])){
						evtData.sound = param[2];
					}
				}
				evtData.intParam = evt.intParameter;
				evtData.floatParam = evt.floatParameter;
				onDragonBoneEvent(evtData);
			}
		}

	}

}