using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace UGI
{
	public class StreamingComponent : MonoBehaviour
	{

		public string streamingName;
		public string[] streamingValues;
		[Tooltip ("Interval between sending streaming updates, in miliseconds")]
		public long interval;

		private List<KeyValuePair<object, MemberInfo>> streamingObjects = new List<KeyValuePair<object, MemberInfo>> ();

		public List<KeyValuePair<object, MemberInfo>> StreamingObjects {
			get {
				return streamingObjects;
			}
		}

		private bool fetching = false;

		public bool Fetching {
			get {
				return fetching;
			}
		}

		private bool serializing;

		public bool Serializing {
			get {
				return serializing;
			}
			set {
				serializing = value;
			}
		}

		private object[] valuesForStreaming;

		public object[] ValuesForStreaming {
			get {
				return valuesForStreaming;
			}
		}

		// Use this for initialization
		void Start ()
		{
			foreach (string s in streamingValues) {
				string[] parts = s.Split (new char[] { '.' });
				if (parts.Length < 2) {
					Debug.LogWarning ("Ignored '" + s + "', because it is invalid entry");
					continue;
				}
				object targetObject = GetComponent (parts [0]);
				object targetparentObject = null;
				for (int i = 1; i < parts.Length; i++) {
					object newTargetObject = targetObject.GetType ().GetField (parts [i]);
					if (newTargetObject == null) {
						newTargetObject = targetObject.GetType ().GetProperty (parts [i]);
					}
					if (newTargetObject == null) {
						newTargetObject = targetObject.GetType ().GetMethod (parts [i]);
					}
					targetparentObject = targetObject;
					targetObject = newTargetObject;
				}
				streamingObjects.Add (new KeyValuePair<object, MemberInfo> (targetparentObject, (MemberInfo)targetObject));
			}
			valuesForStreaming = new object[streamingObjects.Count];
			SubscriptionServer.Instance.RegisterStreaming (this);
		}

		void Update ()
		{
			if (!serializing) {
				fetching = true;
				int i = 0;
				foreach (KeyValuePair<object, MemberInfo> tuple in streamingObjects) {
					// Get value
					object value = null;
					if (tuple.Value is FieldInfo) {
						value = ((FieldInfo)tuple.Value).GetValue (tuple.Key);
					}
					if (tuple.Value is PropertyInfo) {
						value = ((PropertyInfo)tuple.Value).GetValue (tuple.Key, null);
					}
					if (tuple.Value is MethodInfo) {
						value = ((MethodInfo)tuple.Value).Invoke (tuple.Key, null);
					}
					valuesForStreaming [i] = value;
					++i;
				}
				fetching = false;
			}
		}
	}
}