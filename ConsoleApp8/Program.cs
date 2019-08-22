using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;

namespace Get_CPU_Temp5
{
    class ApiModel
    {
        public double NewValue { get; set; }
    }

    class Program
    {
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        static async Task GetSystemInfo()
        {
            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.Accept(updateVisitor);

            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature && computer.Hardware[i].Sensors[j].Value.HasValue)
                        {
                            Console.WriteLine(computer.Hardware[i].Sensors[j].Name + ":" + computer.Hardware[i].Sensors[j].Value.ToString() + "C" + "\r");
                            var model = new ApiModel { NewValue = computer.Hardware[i].Sensors[j].Value.Value };
                            using (var client = new HttpClient())
                            {
                                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(model));
                                await client.PostAsync("https://apktest.azurewebsites.net/api/temperature", content);
                            }
                        }
                    }
                }
            }
            computer.Close();
        }

        static void GetSystemInfoHDD()
        {
            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer comp = new Computer();
            {
                comp.HDDEnabled = true;
            }
            comp.Open();

            foreach (var hardware in comp.Hardware)
            {
                if (hardware.HardwareType == HardwareType.HDD)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature)
                            Console.WriteLine("Temperatura dysku:" + sensor.Value.GetValueOrDefault());
                }
            }
            comp.Close();
        }


        static void Main(string[] args)
        {
            while (true)
            {

                GetSystemInfo().Wait();
            }
        }
    }
}