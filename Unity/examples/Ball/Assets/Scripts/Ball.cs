using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UGI;

public class Ball : MonoBehaviour {

	public Color color;

	void Awake() {

	}

	// Use this for initialization
	void Start () {
		SubscriptionServer.Instance.RegisterCommand (typeof(Ball).GetMethod("SetBallColor"), this);
	}
	
	// Update is called once per frame
	void Update () {
		GetComponent<Renderer> ().sharedMaterial.color = color;
		float x = Input.GetAxis ("Horizontal");
		float y = Input.GetAxis ("Vertical");
		transform.position = new Vector3 (x, y, 0f);
	}

	public void SetBallColor(float r, float g, float b, float a) {
		color = new Color (r, g, b, a);
	}

}