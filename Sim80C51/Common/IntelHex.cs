using System.IO;

namespace Sim80C51.Common
{
    /// <summary>
    /// IntelHexStructure provides the internal data structure which will be used by the IntelHex class.
    /// This class is used for internal processing and is declared public to allow the application that instantiates
    /// the IntelHex class access to the internal storage.
    /// </summary>
    public class IntelHexStructure
	{
		public ushort address;  //< The 16-bit address field.
								//< The 8-bit array data field, which has a maximum size of 256 bytes.
		public byte[] data = new byte[IntelHex.IHEX_MAX_DATA_LEN / 2];
		public int dataLen;     //< The number of bytes of data stored in this record.
		public int type;        //< The Intel HEX8 record type of this record.
		public byte checksum;   //< The checksum of this record.

		private byte CalcChecksum()
		{
			// Add the data count, type, address, and data bytes together
			byte cChecksum = (byte)dataLen;
			cChecksum += (byte)type;
			cChecksum += (byte)address;
			cChecksum += (byte)((address & 0xFF00) >> 8);
			for (int i = 0; i < dataLen; i++)
			{
				cChecksum += data[i];
			}
			// Two's complement on checksum
			return (byte)(~cChecksum + 1);
		}

		public void SetChecksum()
		{
			checksum = CalcChecksum();
		}

		public bool Verify()
        {
			byte chec = CalcChecksum();	
            return checksum == chec;
        }
    }

	/// <summary>
	/// IntelHex is the base class to work with Intel Hex records.
	/// This class will contain all necessary functions to process data using the Intel Hex record standard.
	/// </summary>
	public static class IntelHex
	{
		// Offsets and lengths of various fields in an Intel HEX8 record
		const int IHEX_COUNT_OFFSET = 1;
		const int IHEX_COUNT_LEN = 2;
		const int IHEX_ADDRESS_OFFSET = 3;
		const int IHEX_ADDRESS_LEN = 4;
		const int IHEX_TYPE_OFFSET = 7;
		const int IHEX_TYPE_LEN = 2;
		const int IHEX_DATA_OFFSET = 9;
		const int IHEX_CHECKSUM_LEN = 2;
		public const int IHEX_MAX_DATA_LEN = 512;
		// Ascii hex encoded length of a single byte
		const int IHEX_ASCII_HEX_BYTE_LEN = 2;
		// Start code offset and value
		const int IHEX_START_CODE_OFFSET = 0;
		const char IHEX_START_CODE = ':';

		public const int IHEX_TYPE_DATA = 0;                     //< Data Record
		public const int IHEX_TYPE_EOF = 1;                      //< End of File Record
		public const int IHEX_TYPE_02 = 2;                     //< Extended Segment Address Record
		public const int IHEX_TYPE_03 = 3;                     //< Start Segment Address Record
		public const int IHEX_TYPE_04 = 4;                     //< Extended Linear Address Record
		public const int IHEX_TYPE_05 = 5;                     //< Start Linear Address Record


		/// <summary>
		/// Initializes a new IntelHex structure that is returned upon successful completion of the function,
		/// including up to 16-bit integer address, 8-bit data array, and size of 8-bit data array.
		/// </summary>
		/// <param name="type">The type of Intel HEX record to be defined by the record.</param>
		/// <param name="address">The 16-, 24-, or 32-bit address of the record.</param>
		/// <param name="data">An array of 8-bit data bytes.</param>
		/// <param name="dataLen">The number of data bytes passed in the array.</param>
		/// <returns>IntelHexStructure instance or null, if null then query Status class variable for the error.</returns>
		public static IntelHexStructure? NewRecord(int type, ushort address, byte[] data, int dataLen)
		{
			// Data length size check, assertion of irec pointer
			if (dataLen < 0 || dataLen > IHEX_MAX_DATA_LEN / 2)
			{
				return null;
			}

			IntelHexStructure irec = new();
			irec.type = type;
			irec.address = address;
			if (data != null)
            {
                Array.Copy(data, irec.data, (long)dataLen);
            }

            irec.dataLen = dataLen;
			irec.SetChecksum();

			return irec;
		}

		/// <summary>
		/// Utility function to read an Intel HEX8 record from a file
		/// </summary>
		/// <param name="inStream">An instance of the StreamReader class to allow reading the file data.</param>
		/// <returns>IntelHexStructure instance or null, if null then query Status class variable for the error.</returns>
		public static IntelHexStructure? Read(StreamReader inStream)
		{
            string? recordBuff;
			int dataCount, i;

			try
			{
				// Read Line will return a line from the file.
				recordBuff = inStream.ReadLine();
			}
			catch (Exception)
			{
				return null;
			}

			// Check if we hit a newline
			if (recordBuff == null || recordBuff.Length == 0)
			{
				return null;
			}

			// Size check for start code, count, address, and type fields
			if (recordBuff.Length < (1 + IHEX_COUNT_LEN + IHEX_ADDRESS_LEN + IHEX_TYPE_LEN))
			{
				return null;
			}

			// Check the for colon start code
			if (recordBuff[IHEX_START_CODE_OFFSET] != IHEX_START_CODE)
			{
				return null;
			}

			IntelHexStructure irec = new();

			// Copy the ASCII hex encoding of the count field into hexBuff, convert it to a usable integer
			dataCount = Convert.ToInt16(recordBuff.Substring(IHEX_COUNT_OFFSET, IHEX_COUNT_LEN), 16);

			// Copy the ASCII hex encoding of the address field into hexBuff, convert it to a usable integer
			irec.address = Convert.ToUInt16(recordBuff.Substring(IHEX_ADDRESS_OFFSET, IHEX_ADDRESS_LEN), 16);

			// Copy the ASCII hex encoding of the address field into hexBuff, convert it to a usable integer
			irec.type = Convert.ToInt16(recordBuff.Substring(IHEX_TYPE_OFFSET, IHEX_TYPE_LEN), 16);

			// Size check for start code, count, address, type, data and checksum fields
			if (recordBuff.Length < (1 + IHEX_COUNT_LEN + IHEX_ADDRESS_LEN + IHEX_TYPE_LEN + dataCount * 2 + IHEX_CHECKSUM_LEN))
			{
				return null;
			}

			// Loop through each ASCII hex byte of the data field, pull it out into hexBuff,
			// convert it and store the result in the data buffer of the Intel HEX8 record
			for (i = 0; i < dataCount; i++)
			{
				// Times two i because every byte is represented by two ASCII hex characters
				irec.data[i] = Convert.ToByte(recordBuff.Substring(IHEX_DATA_OFFSET + 2 * i, IHEX_ASCII_HEX_BYTE_LEN), 16);
			}
			irec.dataLen = dataCount;

			// Copy the ASCII hex encoding of the checksum field into hexBuff, convert it to a usable integer
			irec.checksum = Convert.ToByte(recordBuff.Substring(IHEX_DATA_OFFSET + dataCount * 2, IHEX_CHECKSUM_LEN), 16);

			if (!irec.Verify())
			{
				return null;
			}

			return irec;
		}

		// Utility function to write an Intel HEX8 record to a file
		public static void Write(IntelHexStructure irec, StreamWriter outStream)
		{
			// Check that the data length is in range
			if (irec.dataLen > IHEX_MAX_DATA_LEN / 2)
			{
				return;
			}

			try
			{
				// Write the start code, data count, address, and type fields
				outStream.Write(string.Format("{0}{1:X2}{2:X4}{3:X2}", IHEX_START_CODE, irec.dataLen, irec.address, irec.type));
				// Write the data bytes
				for (int i = 0; i < irec.dataLen; i++)
                {
                    outStream.Write(string.Format("{0:X2}", irec.data[i]));
                }
                // Last but not least, the checksum
                outStream.WriteLine(string.Format("{0:X2}", irec.checksum));
			}
			catch (Exception)
			{
				return;
			}

			return;
		}

		/// <summary>
		/// Utility function to print the information stored in an Intel HEX8 record
		/// </summary>
		/// <param name="verbose">A boolean set to false by default, if set to true will provide extended information.</param>
		/// <returns>String which provides the output of the function, this does not write directly to the console.</returns>
		public static string Print(IntelHexStructure irec, bool verbose = false)
		{
			int i;
			string returnString;

			if (verbose)
			{
				returnString = string.Format("Intel HEX8 Record Type: \t{0}\n", irec.type);
				returnString += string.Format("Intel HEX8 Record Address: \t0x{0:X4}\n", irec.address);
				returnString += string.Format("Intel HEX8 Record Data: \t[");
				for (i = 0; i < irec.dataLen; i++)
				{
					if (i + 1 < irec.dataLen)
                    {
                        returnString += string.Format("0x{0:X02}, ", irec.data[i]);
                    }
                    else
                    {
                        returnString += string.Format("0x{0:X02}", irec.data[i]);
                    }
                }
				returnString += string.Format("]\n");
				returnString += string.Format("Intel HEX8 Record Checksum: \t0x{0:X2}\n", irec.checksum);
			}
			else
			{
				returnString = string.Format("{0}{1:X2}{2:X4}{3:X2}", IHEX_START_CODE, irec.dataLen, irec.address, irec.type);
				for (i = 0; i < irec.dataLen; i++)
                {
                    returnString += string.Format("{0:X2}", irec.data[i]);
                }

                returnString += string.Format("{0:X2}", irec.checksum);
			}
			return (returnString);
		}
	}
}
