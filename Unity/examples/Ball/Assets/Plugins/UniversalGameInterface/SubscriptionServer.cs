using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System;
using System.Text;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Linq;

namespace UGI
{
	public class SubscriptionServer : MonoBehaviour
	{

		#region common

		private static SubscriptionServer instance;

		public static SubscriptionServer Instance {
			get {
				return instance;
			}
		}

		void Awake ()
		{
			if (instance != null) {
				Debug.LogWarning ("Found duplicated server script! Original server: " + instance.gameObject.name + ", this server: " + gameObject.name);
				Destroy (this);
				return;
			}
			instance = this;
			incomingConnectionHandlerCallback = new AsyncCallback (HandleIncomingConnections);
			connectionListener = new TcpListener (port);
			serverThread = new Thread (new ThreadStart (ServerThreadMainLoop));
		}

		Thread serverThread;

		public string secret;
		public int port;
		public int maxConnections;

		private bool started;

		private List<KeyValuePair<MethodInfo, object>> commands = new List<KeyValuePair<MethodInfo, object>> ();

		private List<TcpClient> connections = new List<TcpClient> ();
		private TcpListener connectionListener;

		private AsyncCallback incomingConnectionHandlerCallback;

		// Use this for initialization
		void Start ()
		{
			MethodInfo info = typeof(SubscriptionServer).GetMethod ("TestCommand", BindingFlags.Static | BindingFlags.NonPublic);
			RegisterCommand (info);
			StartServer ();
		}

		// Update is called once per frame
		void Update ()
		{
		
		}

		public void StartServer ()
		{
			started = true;
			try {
				connectionListener.Start ();
				connectionListener.BeginAcceptTcpClient (incomingConnectionHandlerCallback, connectionListener);
				stopwatch = System.Diagnostics.Stopwatch.StartNew ();
				serverThread.Start ();
				Debug.Log ("Server started successfully");
			} catch (Exception e) {
				Debug.Log (e + e.Message + e.Data + e.GetBaseException ());
			}
		}

		public void StopServer ()
		{
			started = false;
		}

		private void HandleIncomingConnections (IAsyncResult result)
		{
			// Get the listener that handles the client request.
			TcpListener listener = (TcpListener)result.AsyncState;

			// End the operation and display the received data on 
			// the console.
			TcpClient client = listener.EndAcceptTcpClient (result);

			// Process the connection here. (Add the client to a
			// server table, read data, etc.)
			Debug.Log ("Client connected completed");

			if (connections.Count < maxConnections) {
				NetworkStream clientStream = client.GetStream ();
				if (clientStream.CanRead) {
					byte[] inBuffer = new byte[secret.Length];
					//Debug.Log ("Here");
					//Debug.Log ("Position before: " + clientStream.Position);
					clientStream.Read (inBuffer, 0, secret.Length);
					//Debug.Log ("Position after: " + clientStream.Position);
					String inputString = Encoding.ASCII.GetString (inBuffer);
					Debug.Log ("Received message over TCP: " + inputString);
					if (String.Equals (inputString, secret)) {
						connections.Add (client);
						Debug.Log ("Connection added successfully! Client address: " + client.Client.RemoteEndPoint);
						//Add return message
						clientStream.WriteByte ((byte)1);
						clientStream.Flush ();
					} else {
						Debug.Log ("Error - connection secret wrong, closing connection, client address: "
						+ client.Client.RemoteEndPoint);
						byte[] response = Encoding.ASCII.GetBytes ("Error - Application secret wrong");
						clientStream.Write (response, 0, response.Length);
						clientStream.Flush ();
						client.Close ();
					}
				}
			} else {
				Debug.Log ("Client refused - maxConnecitons limit of " + maxConnections + " reached. Client address: " + client.Client.RemoteEndPoint);
				string errorMsg = "Connection refused - too many connections.";
				byte[] outBuff = Encoding.ASCII.GetBytes (errorMsg);
				client.GetStream ().Write (outBuff, 0, outBuff.Length);
				client.GetStream ().Flush ();
				client.Close ();
			}
		}

		private void ServerThreadMainLoop ()
		{
			while (true) {
				ListenClients ();
				StreamToClients ();
				Thread.Sleep (10);
			}
		}

		#endregion

		#region operations

		private void ListenClients ()
		{
			if (started) {
				//Debug.Log ("Listening");
				//String message = "This is sample data received from Unity";
				foreach (TcpClient client in connections) {
					//Debug.Log ("Client: " + client.Client.RemoteEndPoint + ", connected: " + client.Connected);
					if (client.Connected) {
						//client.GetStream ().BeginWrite (Encoding.ASCII.GetBytes (message), 0, message.Length, NetworkWritingCallback, client);
						Listen (client);
					}
				}
			}
		}

		private void Listen (TcpClient client)
		{
			//Debug.Log (client.Connected);
			if (client.GetStream ().DataAvailable) {
				Debug.Log ("Has data to read");
				NetworkStream stream = client.GetStream ();
				int operation = stream.ReadByte ();
				Debug.Log ("Operation: " + operation);
				switch (operation) {
				case(1):
				//Subscribe to a stream
					SubscribeToAStream (client);
					break;
				case(2):
				//Unsubscribe from a stream
					UnsubscribeFromAStream (client);
					break;
				case(3):
				//Invoke a command
					HandleCommand (client);
					break;
				case(4):
				//Get available commands
					ReturnAvailableCommands (client);
					break;
				}
			}
		}

		#endregion

		#region deserialization
		private String ReadString(NetworkStream stream)
		{
			int streamNameLength = (int)stream.ReadByte ();
			byte[] rawStreamName = new byte[streamNameLength];
			stream.Read (rawStreamName, 0, streamNameLength);
			return Encoding.ASCII.GetString (rawStreamName);
		}
		private float ReadFloat(NetworkStream stream)
		{
			int sizeOfFloat = sizeof(float);
			byte[] rawFloat = new byte[sizeOfFloat];
			stream.Read (rawFloat, 0, sizeOfFloat);
			return System.BitConverter.ToSingle (rawFloat, 0);
		}
		private bool ReadBool(NetworkStream stream)
		{
			int val = stream.ReadByte ();
			if (val == 0) {
				return false;
			} else {
				return true;
			}
		}
		private int ReadInt(NetworkStream stream)
		{
			int sizeOfInt = sizeof(int);
			byte[] rawInt = new byte[sizeOfInt];
			stream.Read (rawInt, 0, sizeOfInt);
			return System.BitConverter.ToInt32 (rawInt, 0);
		}
		private Vector3 ReadVector3(NetworkStream stream)
		{
			int sizeOfFloat = sizeof(float);
			Vector3 parameter = new Vector3 ();
			byte[] rawFloat = new byte[sizeOfFloat];
			stream.Read (rawFloat, 0, sizeOfFloat);
			parameter.x = System.BitConverter.ToSingle (rawFloat, 0);
			stream.Read (rawFloat, 0, sizeOfFloat);
			parameter.y = System.BitConverter.ToSingle (rawFloat, 0);
			stream.Read (rawFloat, 0, sizeOfFloat);
			parameter.z = System.BitConverter.ToSingle (rawFloat, 0);
			return parameter;
		}
		#endregion

		#region Commands

		public void RegisterCommand (MethodInfo methodInfo, object targetObject = null)
		{
			if (methodInfo == null) {
				Debug.LogWarning ("Register Command: Command cannot be null!");
				return;
			}
			commands.Add (new KeyValuePair<MethodInfo, object> (methodInfo, targetObject));
			Debug.Log (commands.Count);
		}


		private void HandleCommand (TcpClient client)
		{
			NetworkStream stream = client.GetStream ();
			int commandId = stream.ReadByte ();
			Debug.Log ("Command id: " + commandId);
			KeyValuePair<MethodInfo, object> commandPair = commands [commandId];
			MethodInfo requestedCommand = commandPair.Key;
			ParameterInfo[] parametersInfo = requestedCommand.GetParameters ();
			object[] receivedParameters = new object[parametersInfo.Length];
			int paramId = 0;
			foreach (ParameterInfo parameterInfo in parametersInfo) {
				Type paramType = parameterInfo.ParameterType;
				Debug.Log ("Param type: " + paramType);
				if (paramType.Equals (typeof(bool))) {
					receivedParameters [paramId] = ReadBool (stream);
					++paramId;
					continue;
				}
				if (paramType.Equals (typeof(float))) {
					receivedParameters [paramId] = ReadFloat (stream);
					++paramId;
					continue;
				}
				if (paramType.Equals (typeof(int))) {
					receivedParameters [paramId] = ReadInt(stream);
					++paramId;
					continue;
				}
				if (paramType.Equals (typeof(string))) {
					receivedParameters [paramId] = ReadString(stream);
					++paramId;
					continue;
				}
				if (paramType.Equals (typeof(Vector3))) {
					receivedParameters [paramId] = ReadVector3(stream);
					++paramId;
					continue;
				}
			}
			requestedCommand.Invoke (commandPair.Value, receivedParameters);
		}

		byte[] commandsBuffer = new byte[2048];

		private void ReturnAvailableCommands (TcpClient client)
		{
			Debug.Log ("Returning available commands to " + client.Client.RemoteEndPoint);
			NetworkStream clientStream = client.GetStream ();
			int size = 0;
			commandsBuffer [size] = ((byte)0);
			++size;
			commandsBuffer [size] = ((byte)commands.Count);
			++size;
			foreach (KeyValuePair<MethodInfo, object> commandPair in commands) {
				MethodInfo methodInfo = commandPair.Key;
				byte[] commandName = Encoding.ASCII.GetBytes (methodInfo.Name);
				byte commandNameLength = (byte)commandName.Length;
				commandsBuffer [size] = (commandNameLength);
				++size;
				Array.Copy (commandName, 0, commandsBuffer, size, commandNameLength);
				size += commandNameLength;
			}
			clientStream.Write (commandsBuffer, 0, size);
			clientStream.Flush ();
		}

		#endregion

		#region Test

		private static void TestCommand (int a, bool b, float c, string d)
		{
			Debug.Log ("Command invoked successfully, receivedValues: " + a + " " + b + " " + c + " " + d);
		}

		#endregion

		#region Data Streaming

		private Dictionary<StreamingComponent, List<IPEndPoint>> subscriptions = new Dictionary<StreamingComponent, List<IPEndPoint>> ();
		private Dictionary<StreamingComponent, long> countdown = new Dictionary<StreamingComponent, long> ();
		private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch ();
		private UdpClient udpSender = new UdpClient ();

		public void RegisterStreaming (StreamingComponent streaming)
		{
			subscriptions.Add (streaming, new List<IPEndPoint> ());
			countdown.Add (streaming, stopwatch.ElapsedMilliseconds);
		}

		public void StreamToClients ()
		{
			foreach (KeyValuePair<StreamingComponent, List<IPEndPoint>> streamingWitSubscribers in subscriptions) {
				if (streamingWitSubscribers.Value.Count <= 0)
					continue;
				StreamingComponent streaming = streamingWitSubscribers.Key;
				long timeNow = stopwatch.ElapsedMilliseconds;
				long deltaTime = timeNow - countdown [streaming];
				if (deltaTime >= streaming.interval && !streaming.Fetching) {
					countdown [streaming] = timeNow;
					//Stream to all subscribers
					int streamLength = SerializeStream (streaming);
					foreach (IPEndPoint endpoint in streamingWitSubscribers.Value) {
						udpSender.Connect (endpoint);
						udpSender.Send (serializeBuff, streamLength);
					}
				}
			}
		}

		private byte[] serializeBuff = new byte[2048];

		private int SerializeStream (StreamingComponent streaming)
		{
			streaming.Serializing = true;
			byte[] rawStreamName = Encoding.ASCII.GetBytes (streaming.streamingName);
			byte rawStreamNameLength = (byte)rawStreamName.Length;
			serializeBuff [0] = rawStreamNameLength;
			Array.Copy (rawStreamName, 0, serializeBuff, 1, (int)rawStreamNameLength);
			int size = (int)rawStreamNameLength + 1;
			foreach (object value in streaming.ValuesForStreaming) {

				//Serialize value
				if (value is string) {
					string str = (string)value;
					byte[] rawString = Encoding.ASCII.GetBytes (str);
					int length = rawString.Length;
					byte rawLength = (byte)length;
					serializeBuff [size] = rawLength;
					++size;
					Array.Copy (rawString, 0, serializeBuff, size, rawString.Length);
					size += rawString.Length;
				}
				if (value is int) {
					byte[] rawInt = BitConverter.GetBytes ((int)value);
					Array.Copy (rawInt, 0, serializeBuff, size, rawInt.Length);
					size += rawInt.Length;
				}
				if (value is float) {
					byte[] rawFloat = BitConverter.GetBytes ((float)value);
					Array.Copy (rawFloat, 0, serializeBuff, size, rawFloat.Length);
					size += rawFloat.Length;
				}
				if (value is Vector3) {
					byte[] rawFloat = BitConverter.GetBytes (((Vector3)value).x);
					Array.Copy (rawFloat, 0, serializeBuff, size, rawFloat.Length);
					size += rawFloat.Length;
					rawFloat = BitConverter.GetBytes (((Vector3)value).y);
					Array.Copy (rawFloat, 0, serializeBuff, size, rawFloat.Length);
					size += rawFloat.Length;
					rawFloat = BitConverter.GetBytes (((Vector3)value).z);
					Array.Copy (rawFloat, 0, serializeBuff, size, rawFloat.Length);
					size += rawFloat.Length;
				}
			}
			streaming.Serializing = false;
			return size;
		}

		private void SubscribeToAStream (TcpClient client)
		{
			string streamName = ReadString (client.GetStream ());
			StreamingComponent streamingComponent = subscriptions.Keys.FirstOrDefault (x => x.streamingName.Equals (streamName));
			if (streamingComponent) {
				List<IPEndPoint> endpoints = subscriptions [streamingComponent];
				EndPoint endPoint = client.Client.RemoteEndPoint;
				if (endPoint is IPEndPoint) {
					endpoints.Add ((IPEndPoint)endPoint);
				} else {
					Debug.LogError ("EndPoint is not of type IPEndPoint");
				}
			} else {
				Debug.LogWarning ("Desired stream to subscribe to does not exist: " + streamName);
			}
		}

		private void UnsubscribeFromAStream (TcpClient client)
		{
			string streamName = ReadString (client.GetStream ());
			StreamingComponent streamingComponent = subscriptions.Keys.FirstOrDefault (x => x.streamingName.Equals (streamName));
			if (streamingComponent) {
				List<IPEndPoint> endpoints = subscriptions [streamingComponent];
				EndPoint endPoint = client.Client.RemoteEndPoint;
				if (endPoint is IPEndPoint) {
					endpoints.Remove ((IPEndPoint)endPoint);
				} else {
					Debug.LogError ("EndPoint is not of type IPEndPoint");
				}
			} else {
				Debug.LogWarning ("Desired stream to unsubscribe from does not exist: " + streamName);
			}
		}

		#endregion

	}
}