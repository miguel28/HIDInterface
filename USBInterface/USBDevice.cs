using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; 

namespace USBInterface
{
    public class USBDevice
    {
        
        #region Native Methods
#if WIN64
        public const string DLL_FILE_NAME = "hidapi64.dll";
#else
        public const string DLL_FILE_NAME = "hidapi.dll";
#endif

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr hid_open(ushort vendor_id, ushort product_id, IntPtr serial_number);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static void hid_close(IntPtr device);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static int hid_set_nonblocking(IntPtr device, int nonblock);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static int hid_write(IntPtr device, IntPtr data, int length);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static int hid_read(IntPtr device, IntPtr data, int length);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static int hid_get_manufacturer_string(IntPtr device, IntPtr str, UInt32 size);

        [DllImport(DLL_FILE_NAME, CallingConvention = CallingConvention.Cdecl)]
		private extern static int hid_get_product_string(IntPtr device, IntPtr str, UInt32 size);

        #endregion


        public bool HIDisOpen = false;
        public byte[] BufferOUT;
        public byte[] BufferIN;

		private IntPtr DeviceHandle;
		private IntPtr WStringPointer = Marshal.AllocHGlobal(255);
		private byte[] ByteArray = new byte[255];

        private int WStreamPointer = 0;
        private int RStreamPointer = 0;
        private int ReportLenght = 64;

        public USBDevice(int reportLen = 64)
		{
            ReportLenght = reportLen;
            BufferOUT = new byte[ReportLenght+1];
            BufferIN = new byte[ReportLenght+1];
		}
        public USBDevice(ushort VendorID, ushort ProductID, int reportLen = 64) 
		{
            Open(VendorID, ProductID);
            ReportLenght = reportLen;
            BufferOUT = new byte[ReportLenght + 1];
            BufferIN = new byte[ReportLenght + 1];
		}
		private string ParseWString()
		{
			try
			{
				Marshal.Copy(WStringPointer, ByteArray, 0, 100);
				ByteArray = Encoding.Convert (Encoding.UTF8, Encoding.ASCII, ByteArray); 
				string str = Encoding.ASCII.GetString(ByteArray);
				Marshal.FreeHGlobal (WStringPointer);
				WStringPointer = IntPtr.Zero;
				WStringPointer = Marshal.AllocHGlobal(255);
				int a = str.IndexOf ("?");
				if (a > 100)
					a = 100;
				return str.Substring (0, a);
			}
			catch(Exception e)
			{
				return "Error Parsing String: " + e.Message;
			}
		}
		public string Description()
		{
            string ret = "";
            if(HIDisOpen)
		    {
				hid_get_manufacturer_string(DeviceHandle, WStringPointer, 255);
				Console.WriteLine ("Manufacturer: " + ParseWString());
                ret += "Manufacturer: " + ParseWString() + '\n';
				hid_get_product_string(DeviceHandle, WStringPointer, 255);
				Console.WriteLine ("Product Name: " + ParseWString());
                ret += "Product Name: " + ParseWString() + '\n';
		    }
            return ret;
		}

		public void Open(ushort VendorID, ushort ProductID)
		{
			if(!HIDisOpen)
			{
				DeviceHandle = hid_open(VendorID, ProductID, IntPtr.Zero);
				if(DeviceHandle != IntPtr.Zero)HIDisOpen=true;
			}
		}
        public void ReOpen(ushort VendorID, ushort ProductID)
        {
            if (HIDisOpen)
                Close();
            DeviceHandle = hid_open(VendorID, ProductID, IntPtr.Zero);
            if (DeviceHandle != IntPtr.Zero)
            {
                HIDisOpen = true;
            }
        }
		public void Close()
		{
		     if(HIDisOpen)
		     {
		        hid_close(DeviceHandle);
		        hid_set_nonblocking(DeviceHandle,1);
		        HIDisOpen=false;    
		     }
		}

		private void CleanBufferOUT()
		{
			int i;
            for (i = 0; i < ReportLenght+1; i++) BufferOUT[i] = 0x00;     
		    
		}
		private void CleanBufferIN()
		{
			int i;
            for (i = 0; i < ReportLenght + 1; i++) BufferIN[i] = 0x00;     
		}
        private void SetBufferOut(byte[] data)
        {
            CleanBufferOUT();
            BufferOUT[0] = 0;
            for (int i = 0; i < data.Length; i++)
            {
                BufferOUT[i+1] = data[i];
            }
        }

        public int SendBuffer()
		{
            int Result = 0;
            if(HIDisOpen)
			{
				int size = Marshal.SizeOf(BufferOUT[0]) * BufferOUT.Length;
				IntPtr pnt = Marshal.AllocHGlobal(size);
				Marshal.Copy(BufferOUT, 0, pnt, BufferOUT.Length);
                Result = hid_write(DeviceHandle, pnt, ReportLenght + 1);
			}
		    else Result = -1;

            if (Result < 0)
                throw new Exception("USB Has been disconected!");

            return Result;
		}
		public int ReciveBuffer()
		{
            int Result = 0;
            CleanBufferIN();
		    if(HIDisOpen)
		    {
		        //res = hid_read_timeout(DeviceHandle, BufferIN, 65,1);
				int size = Marshal.SizeOf(BufferIN[0]) * BufferIN.Length;
				IntPtr pnt = Marshal.AllocHGlobal(size);
                Result = hid_read(DeviceHandle, pnt, ReportLenght + 1);
				Marshal.Copy (pnt, BufferIN, 0, BufferIN.Length);
		    }
            else Result= - 1;

            if (Result < 0)
                throw new Exception("USB Has been disconected!");

            return Result;
		}
        public void WriteString(string str)
        {
            if (str.Length > ReportLenght)
                return;

            byte[] array = Encoding.ASCII.GetBytes(str);
            SetBufferOut(array);
            SendBuffer();
        }
        public string ReadString()
        {
            ReciveBuffer();
            return Encoding.Default.GetString(BufferIN);
        }
        public string ReadBytesString()
        {
            string ret ="";
            ReciveBuffer();
            for (int i = 0; i < ReportLenght + 1; i++)
            {
                int temp = (int)BufferIN[i];
                ret += temp.ToString() + ",";
            }
            return ret;
        }

        public void StreamWriteBegin()
        {
            CleanBufferOUT();
            WStreamPointer = 1;
        }
        public void StreamWriteChar(char c)
        {
            if (WStreamPointer < ReportLenght + 1)
            {
                BufferOUT[WStreamPointer] = (byte)c;
                WStreamPointer++;
            }
        }
        public void StreamWriteInt16(short num)
        {
            if (WStreamPointer + 1 < ReportLenght+1)
            {
                BufferOUT[WStreamPointer] = (byte)((num >> 8) & 0xff);
                WStreamPointer++;
                BufferOUT[WStreamPointer] = (byte)(num & 0xff);
                WStreamPointer++;
            }
        }
        public void StreamWriteInt32(int num)
        {
            if (WStreamPointer + 3 < ReportLenght + 1)
            {
                BufferOUT[WStreamPointer] = (byte)((num >> 24) & 0xff);
                WStreamPointer++;
                BufferOUT[WStreamPointer] = (byte)((num >> 16) & 0xff);
                WStreamPointer++;
                BufferOUT[WStreamPointer] = (byte)((num >> 8) & 0xff);
                WStreamPointer++;
                BufferOUT[WStreamPointer] = (byte)(num&0xff);
                WStreamPointer++; 
            }
        }

        public void StreamReadBegin()
        {
            RStreamPointer = 0;
        }
        public char StreamReadChar()
        {
            char ret = ' ';
            if (RStreamPointer < ReportLenght + 1)
            {
                ret = (char)BufferIN[RStreamPointer];
                RStreamPointer++;
            }
            return ret;
        }
        public short StreamReadInt16()
        {
            short ret = 0;
            if (RStreamPointer + 3 < ReportLenght + 1)
            {
                ret |= (short)(BufferIN[RStreamPointer] << 8);
                RStreamPointer++;
                ret |= (short)(BufferIN[RStreamPointer]);
                RStreamPointer++;
            }
            return ret;
        }
        public int StreamReadInt32()
        {
            int ret = 0;
            if (RStreamPointer + 3 < ReportLenght + 1)
            {
                ret |= (int)(BufferIN[RStreamPointer] << 24);
                RStreamPointer++;
                ret |= (int)(BufferIN[RStreamPointer] << 16);
                RStreamPointer++;
                ret |= (int)(BufferIN[RStreamPointer] << 8);
                RStreamPointer++;
                ret |= (int)(BufferIN[RStreamPointer]);
                RStreamPointer++;
            }
            return ret;
        }
        public float StreamReadFloat()
        {
            float ret = (float)System.BitConverter.ToSingle(BufferIN, RStreamPointer);
            RStreamPointer+=4;
            return ret;
        }

    }
}
