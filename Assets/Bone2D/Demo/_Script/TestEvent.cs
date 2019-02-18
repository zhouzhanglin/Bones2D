using UnityEngine;
using System.Collections;

public class TestEvent : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GetComponent<Bones2D.AnimEvent>().onDragonBoneEvent += (Bones2D.EventData obj) => {
			print(obj.eventName+":"+ obj.stringParam +"  "+obj.intParam+"   "+obj.floatParam);
		};
	}
	

}
