using System.Diagnostics;
using System.Text;
using System.Text.Json;
using EpilepsyApp.Constants;
using EpilepsyApp.Models;
using HiveMQtt.Client;
using HiveMQtt.Client.Events;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.Types;


namespace EpilepsyApp.Services;

public interface IMQTTService
{
	Task OpenConnection(string userId);
	Task CloseConnection();
	void StartSending(string userId);
	void StopSending();
	Task PublishRawDataAsync(ECGBatchSeriesData data);
	event EventHandler<OnMessageReceivedEventArgs> OnCSIReceived;
}

class MqttService : IMQTTService
{
	private readonly IHiveMQClient _client;
	private bool started;
	public event EventHandler<OnMessageReceivedEventArgs> OnCSIReceived;

	public bool Started
	{
		get { return started; }
		set { started = value; }
	}

	public MqttService()
	{
		Started = false;
		var options = new HiveMQClientOptions
		{
			Host = "telemonmqtt-wxmbnq.a01.euc1.aws.hivemq.cloud",
			Port = 8883,
			UseTLS = true,
			UserName = "TelemonBroker",
			Password = "RememberTheStack123",
			CleanStart = true
		};
		var client = new HiveMQClient(options);

		client.OnMessageReceived += (sender, args) =>
		{
			var message = args.PublishMessage.PayloadAsString;
			Console.WriteLine($"Message Received: {args.PublishMessage.PayloadAsString}");
			OnCSIReceived?.Invoke(this, args);
		};
		_client = client;
	}

	public async Task OpenConnection(string userId)
	{
		UserId = userId;
		Started = true;
		await ConnectAndSubscribeAsync();
	}

	public async Task CloseConnection()
	{
		started = false;
		// await _client.DisconnectAsync().ConfigureAwait(false);
	}

	public async Task PublishRawDataAsync(ECGBatchSeriesData data)
	{
		if (started)
		{
			data.PatientID = UserId;
			var stringToSend = JsonSerializer.Serialize(data);

			var message = new MQTT5PublishMessage
			{
				Topic = Topics.TOPIC_measurements,
				Payload = Encoding.UTF8.GetBytes(stringToSend),
				QoS = QualityOfService.AtLeastOnceDelivery,
			};

			var resultPublish = _client.PublishAsync(message);
			Debug.WriteLine($"Published: {resultPublish.Result.QoS2ReasonCode}");
		}
	}

	private async Task ConnectAndSubscribeAsync()
	{
		try
		{
			var connectResult = await _client.ConnectAsync();
			Debug.WriteLine($"Connected: {connectResult.ReasonString}");

			var subscribeResult = await _client.SubscribeAsync(Topics.TOPIC_processed_measurements);
			Debug.WriteLine($"Subscribed: {subscribeResult.Subscriptions}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MQTT Client Error: {ex.Message}");
			// Handle connection error
		}
	}
	private string UserId = "Unknown";
	public async void StartSending(string userId)
	{
		await OpenConnection(userId);
		UserId = userId;
		Started = true;
	}

	public async void StopSending()
	{
		await CloseConnection().ConfigureAwait(false);
		Started = false;
	}
}
