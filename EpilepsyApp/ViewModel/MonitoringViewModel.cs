using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EpilepsyApp.CortrumDevice;
using EpilepsyApp.DTO;
using EpilepsyApp.Events;
using EpilepsyApp.Models;
using EpilepsyApp.Services;
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
	[QueryProperty("Username", "Username")]

	public partial class MonitoringViewModel : ObservableObject
	{
		[ObservableProperty] string username;

		[ObservableProperty] string ecgChannel;

		private readonly Random _random = new();
		private readonly List<DateTimePoint> _values = new();
		private readonly DateTimeAxis _customAxis;
		public BLEservice BLEservice { get; set; }
		public BLEdevice BleDevice { get; set; } //This is the device that is selected in the UI
		public IService EKGservice { get; private set; }
		public ICharacteristic EKGCharacteristic { get; private set; }
		private bool _measurementIsStarted { get; set; }
		private ObservableCollection<EKGSampleDTO> _ekgSamples = new ObservableCollection<EKGSampleDTO>();
		public ObservableCollection<EKGSampleDTO> EKGSamples { get { return _ekgSamples; } set { _ekgSamples = value; } }
		private readonly IDecoder decoder;
		public ObservableCollection<BLEdevice> ListOfDeviceCandidates
		{
			get { return _listOfDeviceCandidates; }
			set
			{
				_listOfDeviceCandidates = value;
			}
		} //This is the list of devices that is shown in the UI
		private ObservableCollection<BLEdevice> _listOfDeviceCandidates = new ObservableCollection<BLEdevice>();

		public MonitoringViewModel(BLEservice ble, IDecoder decoder)
		{
			BLEservice = ble;
			this.decoder = decoder;

			ecgChannel = "ECG Channel 1";

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
				AnimationsSpeed = TimeSpan.FromMilliseconds(0),
				SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
			};

			XAxes = new Axis[] { _customAxis };


			decoder.ECGDataReceivedEvent += HandleECGDataReceivedEvent;
		}

		public ObservableCollection<ISeries> Series { get; set; }

		public Axis[] XAxes { get; set; }

		public object Sync { get; } = new object();

		public bool IsReading { get; set; } = true;

		//private async Task ReadData()
		//{
		//	// to keep this sample simple, we run the next infinite loop 
		//	// in a real application you should stop the loop/task when the view is disposed 

		//	//while (IsReading)
		//	//{
		//	//	await Task.Delay(100);

		//	//	// Because we are updating the chart from a different thread 
		//	//	// we need to use a lock to access the chart data. 
		//	//	// this is not necessary if your changes are made in the UI thread. 
		//	//	lock (Sync)
		//	//	{
		//	//		_values.Add(new DateTimePoint(DateTime.Now, _random.Next(0, 10)));
		//	//		if (_values.Count > 250) _values.RemoveAt(0);

		//	//		// we need to update the separators every time we add a new point 
		//	//		_customAxis.CustomSeparators = GetSeparators();
		//	//	}
		//	//}
		//}

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
					//TODO: Den connecter direkte til det første device den finder, bør laves om så man selv skal udvælge det
					//if (ListOfDeviceCandidates.Count >= 1)
					//{
					//    await ConnectToDeviceCandidateAsync(ListOfDeviceCandidates.First());
					//}


				}
				catch (Exception ex)
				{
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
				Debug.WriteLine($"Unable to check Bluetooth availability: {ex.Message}");
				await Shell.Current.DisplayAlert($"Unable to check Bluetooth availability", $"{ex.Message}.", "OK");
			}
		}

		async Task SelectDevice(ObservableCollection<BLEdevice> ListOfDeviceCandidates)
		{
			if (ListOfDeviceCandidates.Count == 0)
			{
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
					Debug.WriteLine($"Unable to connect to {BLEservice.BleDevice.Name} {BLEservice.BleDevice.Id}: {ex.Message}.");
					await Shell.Current.DisplayAlert($"{BLEservice.BleDevice.Name}", $"Unable to connect to {BLEservice.BleDevice.Name}.", "OK");
				}
			}
			catch (Exception ex2)
			{
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
				var time = DateTimeOffset.Now.LocalDateTime;

				var decoded_data = decoder.DecodeBytes(bytessigned);

				//Add the newest sample to the list
				EKGSampleDTO item = new EKGSampleDTO { RawBytes = bytessigned, Timestamp = time };
				EKGSamples.Add(item);

				_ = sendDataAsync(decoded_data);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("heart rate measurement found " + ex);
			}
		}

		private async Task sendDataAsync(ECGBatchData item)
		{

			//await Task.Run(() => mqttService.Publish_RawData(item));
		}

		private void HandleECGDataReceivedEvent(object sender, ECGDataReceivedEventArgs e)
		{
			lock (Sync)
			{
				try
				{
					int ecg1 = (e.ECGBatch.ECGChannel1[0] + e.ECGBatch.ECGChannel1[1] + e.ECGBatch.ECGChannel1[2] + e.ECGBatch.ECGChannel1[3] + e.ECGBatch.ECGChannel1[4] + e.ECGBatch.ECGChannel1[5]) / 6;
					int ecg2 = (e.ECGBatch.ECGChannel1[6] + e.ECGBatch.ECGChannel1[7] + e.ECGBatch.ECGChannel1[8] + e.ECGBatch.ECGChannel1[9] + e.ECGBatch.ECGChannel1[10] + e.ECGBatch.ECGChannel1[11]) / 6;

					_values.Add(new DateTimePoint(DateTime.Now, ecg1));
					_values.Add(new DateTimePoint(DateTime.Now, ecg2));

					if (_values.Count > 125) _values.RemoveAt(0);

					// we need to update the separators every time we add a new point 
					_customAxis.CustomSeparators = GetSeparators();

				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message + ": Adding to ECGSamples failed in MeasurementPageViewModel, ECGSamples count: " + _values.Count);
				}
			}
		}
	}
}
