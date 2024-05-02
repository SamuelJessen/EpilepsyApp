using System.Diagnostics;
using System.Text;
using System.Text.Json;
using EpilepsyApp.Constants;
using EpilepsyApp.Models;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace EpilepsyApp.Services
{

	public interface IMQTTService
	{
		void OpenConncetion();
		void CloseConncetion();
		void Publish_RawData(ECGBatchSeriesData data);
		//void PublishMetaData(UserDataDTO data);

		void StartSending(string userId);
		void StopSending();

	}

	class MqttServiceMock : IMQTTService
	{
		public MqttServiceMock()
		{
			Debug.WriteLine("MetodH: MqttServiceMock");

		}
		public void CloseConncetion()
		{
			Debug.WriteLine("MetodH: CloseConncetion");
		}

		public void OpenConncetion()
		{
			Debug.WriteLine("MetodH: OpenConncetion");
		}

		//public void PublishMetaData(UserDataDTO data)
		//{
		//	Debug.WriteLine("MetodH: PublishMetaData");
		//}

		public void Publish_RawData(ECGBatchSeriesData data)
		{
			Debug.WriteLine("MetodH: Publish_RawData");
		}

		public void StartSending(string userId)
		{
			Debug.WriteLine("MetodH: StartSending");
		}

		public void StopSending()
		{
			Debug.WriteLine("MetodH: StopSending");
		}
	}



	class MqttService : IMQTTService
	{

		private readonly MqttClient client;
		private readonly string clientId;
		public MqttClient Client => client;

		private bool started;

		public bool Started
		{
			get { return started; }
			set { started = value; }
		}


		public MqttService()
		{
			Started = false;
			client = new MqttClient("test.mosquitto.org");
			clientId = Guid.NewGuid().ToString();
			Debug.WriteLine("Clientversion: " + client.ProtocolVersion);
			OpenConncetion();
		}


		public void Publish(string topic, byte[] data)
		{
			if (Client.IsConnected)
			{
				client.Publish(topic, data, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
			}
		}

		//This code runs when a message is received
		void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			string topic = e.Topic;

			//Switch case to handle messages different topics
			switch (topic)
			{
				case Topics.TOPIC_measurements:
					//TODO Handle status from CSSURE
					break;
				default:
					Debug.WriteLine("Received message from unhandled topic: " + e.Topic + " Message: " + e.Message);
					break;
			}
		}

		//This code runs when the client has subscribed to a topic
		static void client_MqttMsqSubsribed(object senser, MqttMsgSubscribedEventArgs e)
		{
			Debug.WriteLine("Subscribed to topic: " + e.MessageId);
		}

		public void OpenConncetion()
		{
			try
			{

				if (!Client.IsConnected)
				{
					client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

					client.MqttMsgSubscribed += client_MqttMsqSubsribed;

					//client.Connect(clientId);
					client.Connect(
						clientId: clientId
						);

					client.Subscribe(new string[] { Topics.TOPIC_measurements }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
					//Publish(Topics.TOPIC_measurements, Encoding.UTF8.GetBytes("Online"));
				}
			}
			catch (Exception)
			{

			}
		}

		public void CloseConncetion()
		{
			if (Client.IsConnected)
			{
				Client.Disconnect();
			}
		}

		public void Publish_RawData(ECGBatchSeriesData data)
		{
			if (Started)
			{
				data.PatientID = UserId;
				var serialData = JsonSerializer.Serialize<ECGBatchSeriesData>(data);
				client.Publish(Topics.TOPIC_measurements, Encoding.UTF8.GetBytes(serialData));
			}
		}

		//public void PublishMetaData(UserDataDTO data)
		//{
		//	if (Client.IsConnected)
		//	{
		//		Debug.WriteLine("Sending MetaData");
		//		var options = new JsonSerializerOptions { WriteIndented = true };
		//		var serialData = JsonSerializer.Serialize<UserDataDTO>(data, options);
		//		client.Publish(Topics.Topic_UserData + "/" + data.UserId, Encoding.UTF8.GetBytes(serialData), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
		//	}
		//}

		private string UserId = "Unknown";
		public void StartSending(string userId)
		{
			UserId = userId;
			Started = true;
		}

		public void StopSending()
		{
			Started = false;
		}
	}
}
