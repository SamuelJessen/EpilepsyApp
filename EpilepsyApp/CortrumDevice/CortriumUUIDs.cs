using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpilepsyApp.CortrumDevice
{
   public class CortriumUUIDs
   {

      //readonly to ensure that its value cannot be changed after initialization.
      //The Guid.Parse method is used to create a new Guid object from the string representation of the UUID.
      public static readonly Guid GENERIC_INFORMATION_SERVICE = Guid.Parse("00001800-0000-1000-8000-00805f9b34fb");
      public static readonly Guid DEVICE_NAME = Guid.Parse("00002a00-0000-1000-8000-00805f9b34fb");
      public static readonly Guid DEVICE_APPEARANCE = Guid.Parse("00002a01-0000-1000-8000-00805f9b34fb");
      public static readonly Guid PERIPHERAL_PRIVACY_FLAG = Guid.Parse("00002a02-0000-1000-8000-00805f9b34fb");
      public static readonly Guid RECONNECT_ADDRESS = Guid.Parse("00002a03-0000-1000-8000-00805f9b34fb");
      public static readonly Guid PREFERRED_CONNECTION_PARAMETERS = Guid.Parse("00002a04-0000-1000-8000-00805f9b34fb");

      public static readonly Guid DEVICE_INFORMATION_SERVICE = Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_NAME = Guid.Parse("00002a29-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_MODEL = Guid.Parse("00002a24-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_SERIAL_NUMBER = Guid.Parse("00002a25-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_FIRMWARE_REVISION = Guid.Parse("00002a26-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_HARDWARE_REVISION = Guid.Parse("00002a27-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_SOFTWRE_REVISION = Guid.Parse("00002a28-0000-1000-8000-00805f9b34fb");
      public static readonly Guid MANUFACTURE_SYSTEM_ID = Guid.Parse("00002a23-0000-1000-8000-00805f9b34fb");
      public static readonly Guid IEEE_CERITIFICATION_DATA = Guid.Parse("00002a2a-0000-1000-8000-00805f9b34fb");
      public static readonly Guid PLUG_AND_PLAY_ID = Guid.Parse("00002a50-0000-1000-8000-00805f9b34fb");

      public static readonly Guid GENERIC_ATTRIBUTES_SERVICE = Guid.Parse("00001801-0000-1000-8000-00805f9b34fb");
      public static readonly Guid SERVICE_CHANGED = Guid.Parse("00002a05-0000-1000-8000-00805f9b34fb");

      public static readonly Guid USER_CHARACTERISTIC_CONFIG = Guid.Parse("00002901-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CLIENT_CHARACTERISTIC_CONFIG = Guid.Parse("00002902-0000-1000-8000-00805f9b34fb");

      public static readonly Guid[] CORTRIUM_C3_DATA_SERVICE = new Guid[] { Guid.Parse("0000ffc0-0000-1000-8000-00805f9b34fb") }; // this is the guid that identifies the cortrium device to clients
      public static readonly Guid CORTRIUM_C3_MISC_DATA = Guid.Parse("0000ffc1-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG1_DATA = Guid.Parse("0000ffc3-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG2_DATA = Guid.Parse("0000ffc4-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG3_DATA = Guid.Parse("0000ffc5-0000-1000-8000-00805f9b34fb");

      public static readonly Guid CORTRIUM_C3_MISC_FILE = Guid.Parse("0000ffd1-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG1_FILE = Guid.Parse("0000ffd3-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG2_FILE = Guid.Parse("0000ffd4-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_ECG3_FILE = Guid.Parse("0000ffd5-0000-1000-8000-00805f9b34fb");

      public static readonly Guid CORTRIUM_C3_MODE = Guid.Parse("0000ffcc-0000-1000-8000-00805f9b34fb");

      public static readonly Guid CORTRIUM_C3_FILE_COMMAND = Guid.Parse("0000ffcd-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_FILE_INFO = Guid.Parse("0000ffce-0000-1000-8000-00805f9b34fb");
      public static readonly Guid CORTRIUM_C3_FILE_STATUS = Guid.Parse("0000ffca-0000-1000-8000-00805f9b34fb");

      public static readonly Guid CORTRIUM_C3_UNITS = Guid.Parse("0000ffaa-0000-1000-8000-00805f9b34fb");

      public static readonly Guid[] BLE_SERVICE_UUID_C3TESTER = new Guid[] { Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e") }; // to connect to BluetoothGattService
      public static readonly Guid BLE_CHARACTERISTIC_UUID_Tx = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e"); //write
      public static readonly Guid[] BLE_CHARACTERISTIC_UUID_Rx = new Guid[] { Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e") }; // Read service BluetoothGattCharacteristic

   }

}
