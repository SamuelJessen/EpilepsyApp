using System.Diagnostics;
using EpilepsyApp.Events;
using EpilepsyApp.Models;

namespace EpilepsyApp.Services
{
	public interface IDecoder
	{
		public ECGBatchData DecodeBytes(sbyte[] data);
		event EventHandler<ECGDataReceivedEventArgs> ECGDataReceivedEvent;

	}

	public class DecodingByteArray : IDecoder
	{

		private static bool firstRunAfterConnection = true;
		private static int C1_value = 0;
		private static int C2_value = 0;
		private static int C3_value = 0;
		private static int[] decoded_iECG1 = new int[12];
		private static int[] decoded_iECG2 = new int[12];
		private static int[] decoded_iECG3 = new int[12];

		// Battery
		private float[] fVoltage = { 4.121f, 3.969f, 3.856f, 3.753f, 3.668f, 3.609f, 3.572f, 3.537f, 3.497f, 3.438f, 3.305f };
		private float[] fHoursLeft = { 229.63f, 206.52f, 183.41f, 159.83f, 136.72f, 113.61f, 90.50f, 67.39f, 44.28f, 20.70f, 0.00f };
		private float ERROR_CODE_CONVERT_V_BAT_CALCULATION_FAILED = -1f;

		public event EventHandler<ECGDataReceivedEventArgs> ECGDataReceivedEvent;

		protected virtual void OnECGDataReceived(ECGDataReceivedEventArgs e) //when measurement button is clicked event is fired
		{
			ECGDataReceivedEvent?.Invoke(this, e);
		}

		public ECGBatchData DecodeBytes(sbyte[] data)
		{
			int index = 0;
			int C1_compression = 0;
			int C2_compression = 0;
			int C3_compression = 0;

			int telegramLength = data[index++]; // Index 0
			int telegramID = data[index++]; // Index 1
			int indexCounter = data[index++]; // Index 2
			int nSamples = data[index++]; // Index 3

			// Accelerometer data accounts for index 4-7 and will not be used at first
			//int Acc_X = Convert_16bit_Sign_Value((((data[index] << 4) & 0xFF0) | ((data[index + 1] >> 4) & 0x0F)) << 4);
			//int Acc_Y = Convert_16bit_Sign_Value((((data[index + 1] & 0x0F) << 8) | (data[index + 2] & 0xFF)) << 4);
			//int Acc_Z = Convert_16bit_Sign_Value((((data[index + 3] << 4) & 0xFF0) | ((data[index + 4] >> 4) & 0x0F)) << 4)
			//Acc_X = Acc_X / 16;
			//Acc_Y = Acc_Y / 16;
			//Acc_Z = Acc_Z / 16;

			index += 4;

			int Events = data[index++] & 0x0F; // Index 8
			int LOD_Active = 0x00;

			if (Events != 0x00)
			{
				if ((Events & 0x01) == 0x01) // Battery event
				{
					int value = (((data[index] << 8) & 0xFF00) | ((data[index + 1] & 0x00FF)));
					index += 2;
					float fVbat = (float)(value) * 0.6f * 3f * 3f / 1024f;
					float fPercent = Convert_to_Percent(fVbat);

					//reportNewBatteryLevel(fPercent, fVbat)
				}
				else if ((Events & 0x02) == 0x02) // LOD event - whatever that is :3
				{
					int value = (((data[index] << 8) & 0xFF00) | ((data[index + 1] & 0x00FF)));
					index += 2;
					LOD_Active = value;
				}
				else //TODO: Unused Button event from ASSURE
				{
					index += 2;
				}
			}

			// Flag first run to ensure the subsequent samples are decoded correctly based on the previous samples
			if (firstRunAfterConnection)
			{
				C1_value = 0;
				C2_value = 0;
				C3_value = 0;
			}

			if (nSamples > 12)
			{
				C1_compression = (int)((((long)data[index] << 24) & 0xFF000000) | (((long)data[index + 1] << 16) & 0x00FF0000) | (((long)data[index + 2] << 8) & 0x0000FF00) | (((long)data[index + 3]) & 0x000000FF));
				C2_compression = (int)((((long)data[index + 4] << 24) & 0xFF000000) | (((long)data[index + 5] << 16) & 0x00FF0000) | (((long)data[index + 6] << 8) & 0x0000FF00) | (((long)data[index + 7]) & 0x000000FF));
				C3_compression = (int)((((long)data[index + 8] << 24) & 0xFF000000) | (((long)data[index + 9] << 16) & 0x00FF0000) | (((long)data[index + 10] << 8) & 0x0000FF00) | (((long)data[index + 11]) & 0x000000FF));
				index += 12;
			}
			else if (nSamples > 8)
			{
				C1_compression = ((data[index] << 16) & 0xFF0000) | ((data[index + 1] << 8) & 0x00FF00) | ((data[index + 2]) & 0x0000FF); // Index 11-13
				C2_compression = ((data[index + 3] << 16) & 0xFF0000) | ((data[index + 4] << 8) & 0x00FF00) | ((data[index + 5]) & 0x0000FF); // Index 14-16
				C3_compression = ((data[index + 6] << 16) & 0xFF0000) | ((data[index + 7] << 8) & 0x00FF00) | ((data[index + 8]) & 0x0000FF); // Index 17-19
				index += 9;
			}
			else
			{
				C1_compression = ((data[index] << 8) & 0xFF00) | ((data[index + 1] & 0x00FF));
				C2_compression = ((data[index + 2] << 8) & 0xFF00) | ((data[index + 3] & 0x00FF));
				C3_compression = ((data[index + 4] << 8) & 0xFF00) | ((data[index + 5] & 0x00FF));
				index += 6;
			}

			int C1_diff = 0;
			int C2_diff = 0;
			int C3_diff = 0;
			int bitmask;
			int tmp_buf;

			for (int i = nSamples - 1; i >= 0; i--)
			{
				bitmask = (C1_compression >> (i * 2)) & 0x03;

				if (bitmask == 2)
				{
					tmp_buf = ((data[index] << 16) & 0xFF0000) | ((data[index + 1] << 8) & 0x00FF00) | ((data[index + 2]) & 0x0000FF);
					C1_diff = Convert_24bit_Sign_Value(tmp_buf);
					index += 3;
				}
				else if (bitmask == 1)
				{
					tmp_buf = ((data[index] << 8) & 0xFF00) | (data[index + 1] & 0x00FF);
					C1_diff = Convert_16bit_Sign_Value(tmp_buf);
					index += 2;
				}
				else
				{
					tmp_buf = (data[index] & 0x00FF);
					C1_diff = Convert_8bit_Sign_Value(tmp_buf);
					index += 1;
				}

				bitmask = (C2_compression >> (i * 2)) & 0x03;

				if (bitmask == 2)
				{
					tmp_buf = ((data[index] << 16) & 0xFF0000) | ((data[index + 1] << 8) & 0x00FF00) | ((data[index + 2]) & 0x0000FF);
					C2_diff = Convert_24bit_Sign_Value(tmp_buf);
					index += 3;
				}
				else if (bitmask == 1)
				{
					tmp_buf = ((data[index] << 8) & 0xFF00) | (data[index + 1] & 0x00FF);
					C2_diff = Convert_16bit_Sign_Value(tmp_buf);
					index += 2;
				}
				else
				{
					tmp_buf = (data[index] & 0x00FF);
					C2_diff = Convert_8bit_Sign_Value(tmp_buf);
					index += 1;
				}

				bitmask = (C3_compression >> (i * 2)) & 0x03;

				//Decodes the compression mask (0=1byte value, 1=2byte value, 2=3byte value)
				//Tuple<int, int> C3_params = DecodeChannelDifference(bitmask, index, data, C3_diff);
				//C3_diff = C3_params.Item1; // C3 difference
				//index = C3_params.Item2; // Index

				if (bitmask == 2)
				{
					tmp_buf = ((data[index] << 16) & 0xFF0000) | ((data[index + 1] << 8) & 0x00FF00) | ((data[index + 2]) & 0x0000FF);
					C3_diff = Convert_24bit_Sign_Value(tmp_buf);
					index += 3;
				}
				else if (bitmask == 1)
				{
					tmp_buf = ((data[index] << 8) & 0xFF00) | (data[index + 1] & 0x00FF);
					C3_diff = Convert_16bit_Sign_Value(tmp_buf);
					index += 2;
				}
				else
				{
					tmp_buf = (data[index] & 0x00FF);
					C3_diff = Convert_8bit_Sign_Value(tmp_buf);
					index += 1;
				}


				if (index >= telegramLength)
				{
					System.Diagnostics.Debug.WriteLine("JKN", "decode error : ( index > Length ) : " + index + " >= " + telegramLength);
				}

				C1_value += C1_diff;
				C2_value += C2_diff;
				C3_value += C3_diff;

				int iECGindex = nSamples - 1 - i;

				// Adding each sample to one batch
				decoded_iECG1[iECGindex] = C1_value;
				decoded_iECG2[iECGindex] = C2_value;
				decoded_iECG3[iECGindex] = C3_value;
			}

			if (firstRunAfterConnection)
				firstRunAfterConnection = false;

			// Adding batch to each channel
			ECGBatchData ecgData = new ECGBatchData()
			{
				ECGChannel1 = decoded_iECG1,
				ECGChannel2 = decoded_iECG2,
				ECGChannel3 = decoded_iECG3,
			};

			try
			{
				if (ecgData != null)
				{
					OnECGDataReceived(new ECGDataReceivedEventArgs { ECGBatch = ecgData });
					return ecgData;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message + ": Adding batch failed in DecodingByteArray");
				return null;
			}

			return null;
		}

		#region Battery
		public float Convert_to_Percent(float fVBat)
		{
			float fResult = 100f * Convert_to_Hours(fVBat) / fHoursLeft[0];
			return fResult;
		}

		public float Convert_to_Hours(float fVBat)
		{
			int iCount = fVoltage.Length;

			///float fVCorrect = fVBat;// - fVoltage[iCount-1];

			for (int i = 1; i < iCount; i++)
			{
				if (fVBat >= fVoltage[i])
				{
					float fDiff = fVBat - fVoltage[i];
					float fDiff_X = fVoltage[i - 1] - fVoltage[i];
					float fDiff_Y = fHoursLeft[i - 1] - fHoursLeft[i];

					float fResult = fHoursLeft[i] + fDiff_Y * (fDiff / fDiff_X);
					if (fResult > fHoursLeft[0])
						fResult = fHoursLeft[0];
					if (fResult < 0f)
						fResult = 0f;
					return fResult;
				}
			}

			return ERROR_CODE_CONVERT_V_BAT_CALCULATION_FAILED;// -1f
		}
		#endregion

		#region Decoding differences
		private static Tuple<int, int> DecodeChannelDifference(int bitmask, int index, sbyte[] data, int Cx_diff)
		{
			int tmp_buf = 0;
			if (bitmask == 2)
			{
				tmp_buf = ((data[index] << 16) & 0xFF0000) | ((data[index + 1] << 8) & 0x00FF00) | ((data[index + 2]) & 0x0000FF);
				Cx_diff = Convert_24bit_Sign_Value(tmp_buf);
				index += 3;
			}
			else if (bitmask == 1)
			{
				tmp_buf = ((data[index] << 8) & 0xFF00) | (data[index + 1] & 0x00FF);
				Cx_diff = Convert_16bit_Sign_Value(tmp_buf);
				index += 2;
			}
			else
			{
				tmp_buf = (data[index] & 0x00FF);
				Cx_diff = Convert_8bit_Sign_Value(tmp_buf);
				index += 1;
			}

			return Tuple.Create(Cx_diff, index);
		}

		private static int Convert_24bit_Sign_Value(int tmp_buf)
		{
			if (tmp_buf > 0x7FFFFF)
				tmp_buf = (int)(tmp_buf | 0xFF000000);
			return tmp_buf;
		}

		private static int Convert_16bit_Sign_Value(int tmp_buf)
		{
			if (tmp_buf > 0x7FFF)
				tmp_buf = (int)(tmp_buf | 0xFFFF0000);

			return tmp_buf;
		}

		private static int Convert_8bit_Sign_Value(int tmp_buf)
		{
			if (tmp_buf > 0x7F)
				tmp_buf = (int)(tmp_buf | 0xFFFFFF00);

			return tmp_buf;
		}
		#endregion
	}
}
