using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using USBInterface;

namespace HIDTestToll
{
    public class TestMoudle
    {
        public static void Main(string[] args)
        {
            var list = USBDevice.HIDEnumerate();
            foreach (var item in list)
            {
                Console.WriteLine(item.Description());
            }

            //var PulseReader = new USBDevice(0x1130, 0x6837);
            //while (true)
            //{
            //    try
            //    {
            //        Console.WriteLine(PulseReader.ReciveBuffer());
            //    }
            //    catch (Exception)
            //    {
            //        break;
            //    }
            //}
            //PulseReader.Close();
        }
    }
}
