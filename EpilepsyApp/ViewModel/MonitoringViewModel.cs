﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EpilepsyApp.Constants;
using EpilepsyApp.CortrumDevice;
using EpilepsyApp.DTO;
using EpilepsyApp.Events;
using EpilepsyApp.Models;
using EpilepsyApp.Services;
using HiveMQtt.Client.Events;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using SkiaSharp;

namespace EpilepsyApp.ViewModel
{
	[QueryProperty(nameof(PatientID), nameof(PatientID))]

	public partial class MonitoringViewModel : ObservableObject, IQueryAttributable
	{
		[ObservableProperty] string _patientID;

		[ObservableProperty] string ecgChannel;
		[ObservableProperty] bool _settingsIsOpen;

		private readonly Random _random = new();
		private readonly List<DateTimePoint> _values = new();
		private readonly DateTimeAxis _customAxis;
		public BLEservice BLEservice { get; set; }
		public BLEdevice BleDevice { get; set; } //This is the device that is selected in the UI
		public IService EKGservice { get; private set; }
		public ICharacteristic EKGCharacteristic { get; private set; }
		private ObservableCollection<EKGSampleDTO> _ekgSamples = new ObservableCollection<EKGSampleDTO>();
		public ObservableCollection<EKGSampleDTO> EKGSamples { get { return _ekgSamples; } set { _ekgSamples = value; } }
		public ObservableCollection<BLEdevice> ListOfDeviceCandidates
		{
			get { return _listOfDeviceCandidates; }
			set
			{
				_listOfDeviceCandidates = value;
			}
		} //This is the list of devices that is shown in the UI
		private ObservableCollection<BLEdevice> _listOfDeviceCandidates = new ObservableCollection<BLEdevice>();

		public IMQTTService _mqttService { get; set; }
		public IRawDataService _rawDataService { get; set; }
		public IAPIService _apiService { get; set; }
		private readonly IDecoder _decoder;
		public bool Started { get; set; }

		private readonly string StartText = "Start monitoring";
		private readonly string StopText = "Stop monitoring";

		[ObservableProperty] string startbtntext;
		[ObservableProperty] bool scanning;
		[ObservableProperty] string scanningtext;
		[ObservableProperty] bool scanningBtnVisble;
		[ObservableProperty] Patient _patient;
		[ObservableProperty] int _csi30Threshold;
		[ObservableProperty] int _csi50Threshold;
		[ObservableProperty] int _csi100Threshold;
		[ObservableProperty] int _modCSI100Threshold;

		public MonitoringViewModel(BLEservice ble, IDecoder decoder, IMQTTService mqttClient, IRawDataService rawDataService, IAPIService apiService)
		{
			BLEservice = ble;
			_decoder = decoder;
			_mqttService = mqttClient;
			_rawDataService = rawDataService;
			_apiService = apiService;

			ecgChannel = "ECG Channel 1";
			Startbtntext = StartText;
			Scanning = false;
			Scanningtext = "Connect to device";
			ScanningBtnVisble = true;

			Series = new ObservableCollection<ISeries>
			{
				new LineSeries<DateTimePoint>
				{
					Values = _values,
					Fill = null,
					GeometryFill = null,
					GeometryStroke = null
				}
			};

			_customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
			{
				CustomSeparators = GetSeparators(),
				AnimationsSpeed = TimeSpan.FromMilliseconds(0.5),
				SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
			};

			XAxes = new Axis[] { _customAxis };

			_decoder.ECGDataReceivedEvent += HandleECGDataReceivedEvent;
			_mqttService.OnCSIReceived += HandleCSIReceivedEvent;
		}

		private async Task Initialize()
		{
			var patientResponse = await _apiService.GetPatient(PatientID);
			if (patientResponse == null)
			{
				await Shell.Current.DisplayAlert("Error", "Patient not found", "OK");
			}
			Patient = patientResponse;
			Csi30Threshold = Patient.CSIThreshold30;
			Csi50Threshold = Patient.CSIThreshold50;
			Csi100Threshold = Patient.CSIThreshold100;
			ModCSI100Threshold = Patient.ModCSIThreshold100;
		}

		public void ApplyQueryAttributes(IDictionary<string, object> query)
		{
			if (!query.ContainsKey(nameof(PatientID))) return;
			PatientID = (string)query[nameof(PatientID)];
			Initialize().ConfigureAwait(false);
		}

		public ObservableCollection<ISeries> Series { get; set; }

		public Axis[] XAxes { get; set; }

		public object Sync { get; } = new object();

		public object LockECGSamples { get; } = new object();

		private double[] GetSeparators()
		{
			var now = DateTime.Now;

			return new double[]
			{
			now.AddSeconds(-25).Ticks,
			now.AddSeconds(-20).Ticks,
			now.AddSeconds(-15).Ticks,
			now.AddSeconds(-5).Ticks,
			now.AddSeconds(-2).Ticks,
			now.Ticks
			};
		}

		private static string Formatter(DateTime date)
		{
			var secsAgo = (DateTime.Now - date).TotalSeconds;

			return secsAgo < 1
			   ? "now"
			   : $"{secsAgo:N0}s ago";
		}

		[ICommand]
		Task OpenSettings()
		{
			SettingsIsOpen = true;
			return Task.CompletedTask;
		}

		[ICommand]
		Task CloseSetting()
		{
			SettingsIsOpen = false;
			return Task.CompletedTask;
		}

		[ICommand]
		async Task SaveThresholds()
		{
			var thresholdresponse = await _apiService.UpdateThresholds(Patient.Id, Csi30Threshold, Csi50Threshold, Csi100Threshold, ModCSI100Threshold);
			if (thresholdresponse)
			{
				SettingsIsOpen = false;
			}
			else
			{
				await Shell.Current.DisplayAlert("Threshold update failed", "Failed to update thresholds", "OK");
			}
		}

		[ICommand]
		async Task Logout()
		{
			await Shell.Current.GoToAsync("..");
		}

		[ICommand]
		async Task ScanDevicesAsync()
		{
			await Permissions.RequestAsync<Permissions.Bluetooth>();
			Scan();
		}

		public async void Scan()
		{
			Scanning = true;
			Scanningtext = "Connecting...";

			CheckBluetoothAvailabilityAsync();

			try
			{
				if (!BLEservice.bleInterface.IsAvailable)
				{
					Debug.WriteLine($"Bluetooth is missing.");
					await Shell.Current.DisplayAlert($"Bluetooth", $"Bluetooth is missing.", "OK");
					return;
				}

#if ANDROID
				PermissionStatus permissionStatus = await BLEservice.CheckBluetoothPermissions();
				if (permissionStatus != PermissionStatus.Granted)
				{
					permissionStatus = await BLEservice.RequestBluetoothPermissions();
					if (permissionStatus != PermissionStatus.Granted)
					{
						await Shell.Current.DisplayAlert($"Bluetooth LE permissions", $"Bluetooth LE permissions are not granted.", "OK");
						return;
					}
				}
#elif IOS
#elif WINDOWS
#endif

				try
				{
					if (!BLEservice.bleInterface.IsOn)
					{
						await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
						return;
					}



					List<BLEdevice> newlyFoundDevices = await BLEservice.ScanForDevicesAsync();

					if (newlyFoundDevices.Count == 0)
					{
						Scanning = false;
						Scanningtext = "Connect to device";
						ScanningBtnVisble = true;
						await BLEservice.ShowToastAsync("BLE Error", $"Unable to find nearby Bluetooth LE devices. Try again.");
					}

					if (ListOfDeviceCandidates.Count > 0) // clear the old global list
					{
						ListOfDeviceCandidates.Clear();
					}


					foreach (var deviceCandidate in newlyFoundDevices) //Fill the global list with newly found devices
					{
						ListOfDeviceCandidates.Add(deviceCandidate); //add the found devices to the global list for the viewmodel
					}


				}
				catch (Exception ex)
				{
					Scanning = false;
					Scanningtext = "Connect to device";
					ScanningBtnVisble = true;
					Debug.WriteLine($"Unable to get nearby Bluetooth LE devices: {ex.Message}");
					await Shell.Current.DisplayAlert($"Unable to get nearby Bluetooth LE devices", $"{ex.Message}.", "OK");
				}
			}
			catch (Exception ex2)
			{
				Debug.WriteLine($"Bssure Error while getting Bluetooth LE devices: {ex2.Message}");
			}

			SelectDevice(ListOfDeviceCandidates);
		}
		async Task CheckBluetoothAvailabilityAsync()
		{
			try
			{
				if (!BLEservice.bleInterface.IsAvailable)
				{
					Debug.WriteLine($"Error: Bluetooth is missing.");
					await Shell.Current.DisplayAlert($"Bluetooth", $"Bluetooth is missing.", "OK");
					return;
				}

				if (BLEservice.bleInterface.IsOn)
				{
					await Shell.Current.DisplayAlert($"Bluetooth is on", $"You are good to go.", "OK");
				}
				else
				{
					await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
				}
			}
			catch (Exception ex)
			{
				Scanning = false;
				Scanningtext = "Connect to device";
				ScanningBtnVisble = true;
				Debug.WriteLine($"Unable to check Bluetooth availability: {ex.Message}");
				await Shell.Current.DisplayAlert($"Unable to check Bluetooth availability", $"{ex.Message}.", "OK");
			}
		}

		async Task SelectDevice(ObservableCollection<BLEdevice> ListOfDeviceCandidates)
		{
			if (ListOfDeviceCandidates.Count == 0)
			{
				Scanning = false;
				Scanningtext = "Connect to device";
				ScanningBtnVisble = true;
				await BLEservice.ShowToastAsync("BLE Error", $"Unable to find nearby Bluetooth LE devices. Try again.");
			}

			else if (ListOfDeviceCandidates.Count == 1)
			{
				await ConnectToDeviceCandidateAsync(ListOfDeviceCandidates[0]);
			}

			else
			{
				string action = await Shell.Current.DisplayActionSheet("Select device:", ListOfDeviceCandidates[0].Name.ToString(), ListOfDeviceCandidates[1].Name.ToString());
				if (action == ListOfDeviceCandidates[0].Name.ToString())
				{
					ConnectToDeviceCandidateAsync(ListOfDeviceCandidates[0]);
				}
				else if (action == ListOfDeviceCandidates[1].Name.ToString())
				{
					ConnectToDeviceCandidateAsync(ListOfDeviceCandidates[1]);
				}
			}

			Scanning = false;
			Scanningtext = "Connect to device";
			ScanningBtnVisble = false;
		}

		private async Task ConnectToDeviceCandidateAsync(BLEdevice deviceCandidate)
		{
			try
			{
				BLEservice.BleDevice = deviceCandidate;


				if (!BLEservice.bleInterface.IsOn)
				{
					await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
					return;
				}

				if (BLEservice.AdapterInterface.IsScanning)
				{
					await BLEservice.ShowToastAsync("Bluetooth adapter is scanning.", $"Try again.");
					return;
				}

				try
				{

					if (BLEservice.DeviceInterface != null)
					{
						if (BLEservice.DeviceInterface.State == DeviceState.Connected)
						{
							if (BLEservice.DeviceInterface.Id.Equals(BLEservice.BleDevice.Id))
							{
								await BLEservice.ShowToastAsync("Connection error.", $"{BLEservice.DeviceInterface.Name} is already connected.");
								return;
							}

							if (BLEservice.BleDevice != null)
							{
								#region another device
								if (!BLEservice.DeviceInterface.Id.Equals(BLEservice.BleDevice.Id))
								{
									Debug.WriteLine($"Disconnected: {BLEservice.BleDevice.Name}");
									//await DisconnectFromDeviceAsync();
									await BLEservice.ShowToastAsync("Succes.", $"{BLEservice.DeviceInterface.Name} has been disconnected.");
								}
								#endregion another device
							}
						}
					}

					BLEservice.DeviceInterface = await BLEservice.AdapterInterface.ConnectToKnownDeviceAsync(BLEservice.BleDevice.Id);

					if (BLEservice.DeviceInterface.State == DeviceState.Connected)
					{
						EKGservice = await BLEservice.DeviceInterface.GetServiceAsync(CortriumUUIDs.BLE_SERVICE_UUID_C3TESTER[0]);
						if (EKGservice != null)
						{
							EKGCharacteristic = await EKGservice.GetCharacteristicAsync(CortriumUUIDs.BLE_CHARACTERISTIC_UUID_Rx[0]); //0 is because of array, want the first one eventhough there is only one
							if (EKGCharacteristic != null)
							{
								if (EKGCharacteristic.CanUpdate)
								{
									Debug.WriteLine($"Found service: {EKGservice.Device.Name}");

									#region save device id to storage
									await SecureStorage.Default.SetAsync("device_name", $"{BLEservice.DeviceInterface.Name}");
									await SecureStorage.Default.SetAsync("device_id", $"{BLEservice.DeviceInterface.Id}");
									#endregion save device id to storage

									EKGCharacteristic.ValueUpdated += ECGMeasurementCharacteristic_ValueUpdated;
									await EKGCharacteristic.StartUpdatesAsync();
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Scanning = false;
					Scanningtext = "Connect to device";
					ScanningBtnVisble = true;
					Debug.WriteLine($"Unable to connect to {BLEservice.BleDevice.Name} {BLEservice.BleDevice.Id}: {ex.Message}.");
					await Shell.Current.DisplayAlert($"{BLEservice.BleDevice.Name}", $"Unable to connect to {BLEservice.BleDevice.Name}.", "OK");
				}
			}
			catch (Exception ex2)
			{
				Scanning = false;
				Scanningtext = "Connect to device";
				ScanningBtnVisble = true;
				Debug.WriteLine($"Error during: ConnectToDeviceCandidateAsync: {ex2.Message}.");
				//await Shell.Current.DisplayAlert($"{BLEservice.BleDevice.Name}", $"Unable to connect to {BLEservice.BleDevice.Name}.", "OK");
			}

		}

		//This is the eventhandler that receives raw samples from the device
		private async void ECGMeasurementCharacteristic_ValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
		{
			try
			{
				var bytes = e.Characteristic.Value;//byte array, with raw data to be sent to CSSURE
				sbyte[] bytessigned = Array.ConvertAll(bytes, x => unchecked((sbyte)x));
				var time = DateTime.Now;

				var decoded_data = _decoder.DecodeBytes(bytessigned);
				var ecg_series = new ECGBatchSeriesData();

				EKGSampleDTO item = new EKGSampleDTO { PatientID = Patient.Id, RawBytes = bytessigned, TimeStamp = time };

				await Task.Run(async () =>
				{
					ecg_series = _rawDataService.ProcessData(item, Started);
				});

				if (ecg_series != null)
				{
					Debug.WriteLine("First digit: " + decoded_data.ECGChannel1[0].ToString());
					Debug.WriteLine("Second digit: " + decoded_data.ECGChannel1[1].ToString());
					Debug.WriteLine("Third digit: " + decoded_data.ECGChannel1[2].ToString());
					Debug.WriteLine("Fourth digit: " + decoded_data.ECGChannel1[3].ToString());
					Debug.WriteLine("Length: " + decoded_data.ECGChannel1.Length.ToString());
					DateTime now = DateTime.Now;
					Debug.WriteLine("First digit lastbytearray: " + bytessigned[0].ToString());
					Debug.WriteLine("Second digit bytearray: " + bytessigned[1].ToString());
					Debug.WriteLine("Third digit bytearray: " + bytessigned[2].ToString());
					Debug.WriteLine("Fourth digit bytearray: " + bytessigned[3].ToString());
					Debug.WriteLine("ECG data received: " + now.ToString("HH:mm:ss.fff"));
					Debug.WriteLine("Length bytearray: " + ecg_series.EcgRawBytes.Count.ToString());
					Debug.WriteLine("First digit bytearray: " + ecg_series.EcgRawBytes[0][0].ToString());
					Debug.WriteLine("Second digit bytearray: " + ecg_series.EcgRawBytes[0][1].ToString());
					Debug.WriteLine("Third digit bytearray: " + ecg_series.EcgRawBytes[0][2].ToString());
					Debug.WriteLine("Fourth digit bytearray: " + ecg_series.EcgRawBytes[0][3].ToString());
					sendData(ecg_series);
				}

			}
			catch (Exception ex)
			{
				Debug.WriteLine("heart rate measurement found " + ex);
			}
		}

		private void sendData(ECGBatchSeriesData item)
		{
			_mqttService.PublishRawDataAsync(item);
		}

		public int valueCounter = 0;
		public List<DateTimePoint> intermediateValues = new List<DateTimePoint>();
		private void HandleECGDataReceivedEvent(object sender, ECGDataReceivedEventArgs e)
		{
			lock (Sync)
			{
				if (_values.Count >= 21 * 10)
				{
					lock (LockECGSamples)
					{
						try
						{
							_values.Clear();

							int ecg1 = (e.ECGBatch.ECGChannel1[0] + e.ECGBatch.ECGChannel1[1] + e.ECGBatch.ECGChannel1[2] + e.ECGBatch.ECGChannel1[3] + e.ECGBatch.ECGChannel1[4] + e.ECGBatch.ECGChannel1[5]) / 6;

							_values.Add(new DateTimePoint(DateTime.Now, ecg1));

							_customAxis.CustomSeparators = GetSeparators();
						}
						catch (Exception exp)
						{
							Debug.WriteLine(exp.Message + ": removing from ECGSamples failed in MeasurementPageViewModel, ECGSamples count: " + _values.Count);
						}
					}
				}
				try
				{
					int ecg1 = (e.ECGBatch.ECGChannel1[0] + e.ECGBatch.ECGChannel1[1] + e.ECGBatch.ECGChannel1[2] + e.ECGBatch.ECGChannel1[3] + e.ECGBatch.ECGChannel1[4] + e.ECGBatch.ECGChannel1[5] + e.ECGBatch.ECGChannel1[6] + e.ECGBatch.ECGChannel1[7] + e.ECGBatch.ECGChannel1[8] + e.ECGBatch.ECGChannel1[9] + e.ECGBatch.ECGChannel1[10] + e.ECGBatch.ECGChannel1[11]) / 12;

					var numberOfSamples = 3;

					if (intermediateValues.Count < numberOfSamples)
					{
						intermediateValues.Add(new DateTimePoint(DateTime.Now, ecg1));
					}

					else
					{
						intermediateValues.Add(new DateTimePoint(DateTime.Now, ecg1));
						_values.AddRange(intermediateValues);
						_customAxis.CustomSeparators = GetSeparators();
						intermediateValues.Clear();
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message + ": Adding to ECGSamples failed in MeasurementPageViewModel, ECGSamples count: " + _values.Count);
				}
			}
		}

		[ICommand]
		async Task OnStartMeasurementClicked()
		{
			if (Startbtntext == StartText)
			{
				//Todo:Her startes målingen
				var ble = BLEservice;
				Started = true;
				if (ble.DeviceInterface == null)
				{
					await Application.Current.MainPage.DisplayAlert("No device connected", "Go back and connect to a device", "OK");

				}
				else
				{
					Startbtntext = StopText;
					_mqttService.StartSending(PatientID);
				}
			}

			else
			{
				Startbtntext = StartText;
				//Todo:Her stoppes målingen
				Started = false;
				_mqttService.StopSending();
			}
		}

		public DateTime timeForLatestAlarm = new DateTime();
		public int counter = 0;

		private void HandleCSIReceivedEvent(object sender, OnMessageReceivedEventArgs e)
		{
			Thread newThread = new Thread(HandleCSI);
			newThread.Start();

			void HandleCSI()
			{
				try
				{
					string message = e.PublishMessage.PayloadAsString;

					var CSINormMax = new int[] { 1, 1, 1, 1 };

					if (e.PublishMessage.Topic == Topics.TOPIC_processed_measurements)
					{
						//Debug.WriteLine($"Received message on topic '{e.Topic}': {message}");
						var decodedMessage = JsonSerializer.Deserialize<PythonEcgProcessedMeasurements>(message);

						var ecgAlarm = new EcgAlarm();

						if (decodedMessage.CSI30 / Csi30Threshold > 1.65)
						{
							ecgAlarm.CSI30Alarm = true;
						}

						else
						{
							ecgAlarm.CSI30Alarm = false;
						}

						if (decodedMessage.CSI50 / Csi50Threshold > 2.15)
						{
							ecgAlarm.CSI50Alarm = true;
						}

						else
						{
							ecgAlarm.CSI50Alarm = false;
						}

						if (decodedMessage.CSI100 / Csi100Threshold > 2.15)
						{
							ecgAlarm.CSI100Alarm = true;
						}

						else
						{
							ecgAlarm.CSI100Alarm = false;
						}

						if (decodedMessage.ModCSI100 / ModCSI100Threshold > 2.15)
						{
							ecgAlarm.ModCSI100Alarm = true;
						}

						else
						{
							ecgAlarm.ModCSI100Alarm = false;
						}

						if (ecgAlarm.CSI30Alarm || ecgAlarm.CSI50Alarm || ecgAlarm.CSI100Alarm || ecgAlarm.ModCSI100Alarm)
						{
							ecgAlarm.Id = Guid.NewGuid();
							ecgAlarm.AlarmTimeStamp = decodedMessage.TimeStamp;
							ecgAlarm.PatientID = decodedMessage.PatientID;
							ecgAlarm.CSI30 = Convert.ToInt32(decodedMessage.CSI30);
							ecgAlarm.CSI50 = Convert.ToInt32(decodedMessage.CSI50);
							ecgAlarm.CSI100 = Convert.ToInt32(decodedMessage.CSI100);
							ecgAlarm.ModCSI100 = Convert.ToInt32(decodedMessage.ModCSI100);
							ecgAlarm.PatientCSIThreshold30 = Csi30Threshold;
							ecgAlarm.PatientCSIThreshold50 = Csi50Threshold;
							ecgAlarm.PatientCSIThreshold100 = Csi100Threshold;
							ecgAlarm.PatientModCSIThreshold100 = ModCSI100Threshold;

							if ((ecgAlarm.AlarmTimeStamp - timeForLatestAlarm).TotalSeconds >= 90 || counter == 0)
							{
								counter += 1;
								timeForLatestAlarm = ecgAlarm.AlarmTimeStamp;
								ShowAlarm(ecgAlarm);
								_apiService.PostAlarm(ecgAlarm);
							}

							else
							{
								Debug.WriteLine("An alarm has already occured within last 1.5 min");
							}


						}

						else
						{
							Debug.WriteLine("No alarm");
						}
					}
				}

				catch (Exception ex)
				{
					Debug.WriteLine("Exception occurred while receiving for alarm " + ex.Message);
				}
			}
		}

		private void ShowAlarm(EcgAlarm ecgAlarm)
		{
			Device.BeginInvokeOnMainThread(async () =>
			{
				await Show(ecgAlarm);
			});

		}

		private async Task Show(EcgAlarm ecgAlarm)
		{
			Debug.WriteLine("DisplayAlert should have been shown");
			try
			{
				await Shell.Current.DisplayAlert("Alarm!", "Found on: CSI30-" + ecgAlarm.CSI30Alarm.ToString() + ", CSI50-" + ecgAlarm.CSI50Alarm.ToString() + ", CSI100-" + ecgAlarm.CSI100Alarm.ToString() + ", ModCSI100-" + ecgAlarm.ModCSI100Alarm.ToString(), "OK");
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Exception occurred while displaying alert: " + ex.Message);
			}
		}
	}
}
