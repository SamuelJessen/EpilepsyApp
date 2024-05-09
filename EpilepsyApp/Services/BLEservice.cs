using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpilepsyApp.CortrumDevice;

namespace EpilepsyApp.Services
{
   public class BLEservice
   {
      public BLEdevice
         BleDevice
      {
         get;
         set;
      } //this is the device that we will connect to. It is used to read and write to the device.

      public List<BLEdevice> DeviceList { get; private set; } //List of found devices

      public IBluetoothLE
         bleInterface
      {
         get;
         private set;
      } //this is the bluetooth object interface that we will use to check for bluetooth availability and to ask for permission for bluetooth.

      public IAdapter
         AdapterInterface
      {
         get;
         private set;
      } // this is the adapter interface that we will use to scan for devices and to connect to devices.

      public IDevice
         DeviceInterface
      {
         get;
         set;
      } //this is the interface for the device that we will connect to. It is used to read and write to the device.

      public BLEservice() //Contructor that sets up BLE to connect to cortrium device
      {
         bleInterface = CrossBluetoothLE.Current;
         AdapterInterface = CrossBluetoothLE.Current.Adapter;
         AdapterInterface.ScanTimeout = 10000; // scan for 10 seconds
         AdapterInterface.ScanMode =
            ScanMode.LowLatency; //Low latency is the fastest scan mode, but it uses the most power. Balanced is a good compromise between speed and power consumption. Low power is the slowest scan mode, but it uses the least power.

         AdapterInterface.DeviceDiscovered += Adapter_DeviceDiscovered;
         AdapterInterface.DeviceConnected += Adapter_DeviceConnected;
         AdapterInterface.DeviceDisconnected += Adapter_DeviceDisconnected;
         AdapterInterface.DeviceConnectionLost += Adapter_DeviceConnectionLost;

         bleInterface.StateChanged += BluetoothLE_StateChanged;

         //no devices on start-up, retrieve the previous set devices from securestorage
         if (BleDevice == null)
         {
            //Get the device from storage using method GetCortriumDeviceFromStorage()
            GetCortriumDeviceFromStorage();

         }
      }

      //SecureStorage is used to store the device name and id,
      //so that the user does not have to re-enter the device name and id every time the app is started.
      private async void GetCortriumDeviceFromStorage()
      {
         //Check if the device is null, if there is not a device stored in the securestorage, then we will return
         if (BleDevice != null)
         {
            return;
         }

         try
         {
            // read device id from storage
            var deviceName = await SecureStorage.GetAsync("device_name");
            var deviceIdString = await SecureStorage.GetAsync("device_id");

            if (string.IsNullOrWhiteSpace(deviceIdString))
            {
               throw new Exception("No device ID stored.");
            }

            if (!Guid.TryParse(deviceIdString, out var deviceId))
            {
               throw new Exception($"Invalid device ID stored: {deviceIdString}.");
            }

            BleDevice = new BLEdevice() {Id = deviceId, Name = deviceName};

            return;
         }
         catch (Exception ex) when (
            ex is FormatException ||
            ex is OverflowException ||
            ex is Exception)
         {
            Debug.WriteLine(ex.Message);
            await ShowToastAsync("Bluetooth Low Energy", $"Error {ex.Message}.");
            return;
         }
      }

      public async void SaveCortriumDeviceToStorage()
      {
         try
         {
            if (BleDevice == null)
            {
               throw new Exception("No device to save.");
            }

            if (BleDevice.Id.Equals(Guid.Empty))
            {
               throw new Exception("No device ID to save.");
            }

            // save device id to storage
            await SecureStorage.SetAsync("device_name", BleDevice.Name);
            await SecureStorage.SetAsync("device_id", BleDevice.Id.ToString());
         }
         catch (Exception ex) when (
            ex is FormatException ||
            ex is OverflowException ||
            ex is Exception)
         {
            Debug.WriteLine(ex.Message);
            await ShowToastAsync("Bluetooth Low Energy", $"Error {ex.Message}.");
         }
      }


      //this method is called when the bluetooth state changes, fx when user turns on/off device ble
      private void BluetoothLE_StateChanged(object sender, BluetoothStateChangedArgs e)
      {
         MainThread.BeginInvokeOnMainThread(async () =>
         {
            try
            {
               await ShowToastAsync("Bluetooth Low Energy", $"Bluetooth state is {e.NewState}.");
               Debug.WriteLine($"BLE changed state: {e.NewState}.");
            }
            catch
            {
               await ShowToastAsync("Bluetooth Low Energy", $"Bluetooth state has changed.");
            }
         });
      }

      #region BluetoothPermissions

#if ANDROID // If not android.os, then code is not compiled.

      public async Task<PermissionStatus> CheckBluetoothPermissions()
      {
        PermissionStatus status = PermissionStatus.Unknown;
        try
        {
            status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to check Bluetooth LE permissions: {ex.Message}.");
            await Shell.Current.DisplayAlert($"Unable to check Bluetooth LE permissions", $"{ex.Message}.", "OK");
        }
        return status;
      }

      public async Task<PermissionStatus> RequestBluetoothPermissions()
      {
        PermissionStatus status = PermissionStatus.Unknown;

        if (DeviceInfo.Platform != DevicePlatform.Android) //Not an android
        {

            return status;
        }


        try
        {
            status = await Permissions.RequestAsync<Permissions.Bluetooth>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to request Bluetooth LE permissions: {ex.Message}.");
            await Shell.Current.DisplayAlert($"Unable to request Bluetooth LE permissions", $"{ex.Message}.", "OK");
        }
        return status;
      }

    //public async Task RequestBluetooth()
    //{
    //    if (DeviceInfo.Platform != DevicePlatform.Android) //Not an android
    //    {
    //        //Todo: Create permissions for ios 
    //        return;
    //    }

    //    var status = PermissionStatus.Unknown;  //BLE state unknown at start

    //    //if(DeviceInfo.Version.Major >= 12) //Android 12 and above is where new ble api is used{}

    //    status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

    //    if (status == PermissionStatus.Granted)
    //    {
    //        return;
    //    }

    //    if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
    //    {
    //        await Shell.Current.DisplayAlert("Needs permission", "Cortrium needs BLE", "OK");
    //    }

    //    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

    //    if (status != PermissionStatus.Granted)
    //    {
    //        await Shell.Current.DisplayAlert("Permission required", "Location permission for bluetooth scanning.", "We do not store your location.", "OK");
    //    }
    //}

#elif IOS
       //public async Task<PermissionStatus> CheckBluetoothPermissions()
       //{
       //    => await Permissions.CheckStatusAsync<BluetoothLEPermissions>();
       //}
#elif WINDOWS
#endif

      #endregion BluetoothPermissions

      public async Task<List<BLEdevice>> ScanForDevicesAsync()
      {
         try
         {
            DeviceList = new List<BLEdevice>(); //clear the list of devices

            Plugin.BLE.Abstractions.ScanFilterOptions searchFilter = new Plugin.BLE.Abstractions.ScanFilterOptions();
            searchFilter.ServiceUuids = CortriumUUIDs.BLE_SERVICE_UUID_C3TESTER;

            await AdapterInterface.StartScanningForDevicesAsync(searchFilter);
            //await Task.Delay(10000); // wait for 10 seconds
            await AdapterInterface.StopScanningForDevicesAsync();
            Debug.WriteLine($"{DeviceList.Count} devices");

         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Unable to scan nearby Bluetooth LE devices: {ex.Message}.");
            await Shell.Current.DisplayAlert($"Unable to scan nearby Bluetooth LE devices", $"{ex.Message}.", "OK");
         }

         return DeviceList;
      }


      public async Task ShowToastAsync(string title, string message)
      {
         await Shell.Current.DisplayAlert(title, message, "OK");
      }




      #region DeviceEventArgs

      //This method will sort for devices that are already connected or paired to the system.
      private void Adapter_DeviceDiscovered(object sender, DeviceEventArgs e)
      {
         BLEdevice
            deviceCandidate =
               DeviceList.FirstOrDefault(d =>
                  d.Id == e.Device.Id); //this will check if the device is already in the "global" list
         if (deviceCandidate == null)
         {
            //create new device candidate and add it to the list of devices with props from Plugin.Ble DeviceEventargs
            if (e.Device.Name == null)
            {
               return;
            }

            BLEdevice foundDevice = new BLEdevice();
            foundDevice.Name = e.Device.Name;
            foundDevice.Id = e.Device.Id;
            //søg efter navn der starter med ("C3") eller ("B17") fra Assure appen i java
            if (foundDevice.Name.StartsWith("C3") || foundDevice.Name.StartsWith("B17"))
            {
               DeviceList.Add(foundDevice); //store the discovered devices'

            }
            //await ShowToastAsync("Discovered BLE devices", $"Found {e.Device.State.ToString().ToLower()} {e.Device.Name}.");
         }
      }

      private void Adapter_DeviceConnectionLost(object sender, DeviceErrorEventArgs e)
      {
         //begin invoke on main thread is used to update the UI from a background thread
         MainThread.BeginInvokeOnMainThread(async () =>
         {
            try
            {
               await ShowToastAsync("Connection error", $"{e.Device.Name} connection is lost.");
            }
            catch
            {
               await ShowToastAsync("Connection error", $"Device connection is lost.");
            }
         });
      }

      private void Adapter_DeviceConnected(object sender, DeviceEventArgs e)
      {
         MainThread.BeginInvokeOnMainThread(async () =>
         {
            try
            {
               await ShowToastAsync("Connection succes", $"{e.Device.Name} is connected.");
            }
            catch
            {
               await ShowToastAsync("Connection succes", $"Device is connected.");
            }
         });
      }

      private void Adapter_DeviceDisconnected(object sender, DeviceEventArgs e)
      {
         MainThread.BeginInvokeOnMainThread(async () =>
         {
            try
            {
               await ShowToastAsync("Connection stopped", $"{e.Device.Name} is disconnected.");
            }
            catch
            {
               await ShowToastAsync("Connection stopped", $"Device is disconnected.");
            }
         });
      }

      #endregion DeviceEventArgs

      #region legacy code

      //Method to connect to a previously connected device with a known UUID. 
      //To call this method use await like this await ConnectToPreviousDeviceAsync();
      //public async void ConnectToPreviousDevice()
      //{
      //    await Adapter.ConnectToKnownDeviceAsync(BleDevice.Id);

      //    List<DeviceCandidate> devices = await ScanForDevicesAsync();

      //    var device = devices.FirstOrDefault(d => d.Id == BleDevice.Id); //Find the device in the list of devices
      //    await Adapter.ConnectToDeviceAsync(device);

      //}

      //This method retreives a services based on a UUID.
      //Each service has a unique 128-bit service UUID (Universally Unique Identifier),
      //which allows the app to identify the service on the cortirum device that it wants to communicate with.
      //public async Task<IService> getCortriumDeviceServices(string CortriumDeviceUUID)
      //{
      //    var service = await Device.GetServiceAsync(Guid.Parse(CortriumDeviceUUID));

      //    return service;

      //}


      //GattCharacteristic represents a characteristic of a service that  BLE device provides.
      //A characteristic is a value container that describes a particular aspect of a service.
      //It contains a value that represents a distinct piece of data on the device.
      //For example, the battery level characteristic has a value that represents the battery level of the device.
      //Each characteristic has a UUID and may have one or more descriptors that further define its behavior.
      // A client can read, write, or be notified of changes to the value of a characteristic.
      //private async Task<GattCharacteristic> getGattCharacteristic(IService service)
      //{
      //    if (service == null)
      //    {
      //        return null;

      //    }
      //    var characteristic = await service.GetCharacteristicAsync(Guid.Parse("MY_CHARACTERISTIC_UUID"));

      //    return characteristic;
      //}


      //MS link to BLE GATT: https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/gatt-client
      //Subscribe to notifications from a characteristic. There are two things to take care of before getting notifications.
      //Write to Client Characteristic Configuration Descriptor(CCCD)'
      //Handle the Characteristic.ValueChanged event

      //public async Task<bool> SubscribeToNotificationsAsync()
      //{
      //    if (CharacteristicInterface == null)
      //    {
      //        return false;
      //    }
      //    //Write to Client Characteristic Configuration Descritor (CCCD)
      //    await CharacteristicInterface.WriteAsync(new byte[] { 0x01, 0x00 });
      //    //Handle the Characteristic.ValueChanged event
      //    CharacteristicInterface.ValueUpdated += (o, args) =>
      //    {
      //        var bytes = args.Characteristic.Value;
      //        //Do something with the bytes
      //        //Assure user that cortrium device is sending notifications
      //    };
      //    return true;
      //}

      ////Unsubscribe from notifications from a characteristic.
      //public async Task<bool> UnsubscribeFromNotificationsAsync()
      //{
      //    if (CharacteristicInterface == null)
      //    {
      //        return false;
      //    }
      //    //Write to Client Characteristic Configuration Descritor (CCCD)
      //    await CharacteristicInterface.WriteAsync(new byte[] { 0x00, 0x00 });
      //    //Handle the Characteristic.ValueChanged event
      //    CharacteristicInterface.ValueUpdated -= (o, args) =>
      //    {
      //        var bytes = args.Characteristic.Value;
      //        //Do something with the bytes
      //        //Inform user that it will no longer receive notifications
      //    };
      //    return true;
      //}

      #endregion legacy code

      //LEGACY: ScanForBLEDevicesAsync will scan for devices for 15 seconds and then stop searching. It will return a list of devices.
      //Avoid performing ble device operations like Connect, Read, Write etc while scanning for devices. Scanning is battery-intensive.
      //try to stop scanning before performing device operations(connect/read/write/etc)
      //try to stop scanning as soon as you find the desired device
      //never scan on a loop, and set a time limit on your scan
      //public async Task<List<DeviceCandidate>> ScanForDevicesAsync()
      //{  //This method will scan as long as _bleService.AdapterInterface.ScanTimeout is set.

      //    DeviceList = new List<DeviceCandidate>(); //clear the list of devices
      //    try
      //    {
      //        IReadOnlyList<IDevice> systemDevices = AdapterInterface.GetSystemConnectedOrPairedDevices(CortriumUUIDs.CORTRIUM_C3_DATA_SERVICE); //this will return a list of devices that are already connected or paired to the system.
      //        foreach (var systemDevice in systemDevices)
      //        {
      //            //søg efter navn der starter med ("C3") eller ("B17") fra Assure appen i java
      //            DeviceCandidate deviceCandidate = DeviceList.FirstOrDefault(d => d.Id == systemDevice.Id); //check if the device is already in the list
      //            if (deviceCandidate == null)
      //            {
      //                await AdapterInterface.StopScanningForDevicesAsync();
      //                DeviceList.Add(new DeviceCandidate
      //                {
      //                    Id = systemDevice.Id,
      //                    Name = systemDevice.Name,
      //                });
      //                await ShowToastAsync("Discovered BLE devices", $"Found {systemDevice.State.ToString().ToLower()} device {systemDevice.Name}.");
      //            }
      //        }
      //        await AdapterInterface.StartScanningForDevicesAsync(CortriumUUIDs.CORTRIUM_C3_DATA_SERVICE);
      //    }
      //    catch (Exception ex)
      //    {
      //        Debug.WriteLine($"Unable to scan nearby Bluetooth LE devices: {ex.Message}.");
      //        await Shell.Current.DisplayAlert($"Unable to scan nearby Bluetooth LE devices", $"{ex.Message}.", "OK");
      //    }

      //    return DeviceList;
      //}

      //public async Task<List<DeviceCandidate>> ScanForDevicesAsync()
      //{
      //    DeviceList = new List<DeviceCandidate>(); //clear the list of devices
      //    try
      //    {
      //        AdapterInterface.DeviceDiscovered += (s, a) =>
      //        {
      //            DeviceCandidate deviceCandidate = DeviceList.FirstOrDefault(d => d.Id == a.Device.Id); //check if the device is already in the list
      //            if (deviceCandidate == null)
      //            {
      //                DeviceList.Add(new DeviceCandidate
      //                {
      //                    Id = a.Device.Id,
      //                    Name = a.Device.Name,
      //                });
      //                ShowToastAsync("Discovered BLE devices", $"Found {a.Device.State.ToString().ToLower()} device {a.Device.Name}.").Wait();
      //            }
      //        };

      //        await AdapterInterface.StartScanningForDevicesAsync(CortriumUUIDs.CORTRIUM_C3_DATA_SERVICE);
      //        await Task.Delay(AdapterInterface.ScanTimeout);
      //        await AdapterInterface.StopScanningForDevicesAsync();
      //    }
      //    catch (Exception ex)
      //    {
      //        Debug.WriteLine($"Unable to scan nearby Bluetooth LE devices: {ex.Message}.");
      //        await Shell.Current.DisplayAlert($"Unable to scan nearby Bluetooth LE devices", $"{ex.Message}.", "OK");
      //    }

      //    return DeviceList;
      //}


      //Method to async write a toast message to user interface with a simple string message.
   }
}
