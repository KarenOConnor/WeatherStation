using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using System.Text;
using Newtonsoft.Json;
using Windows.UI.Core;
using Windows.UI.Popups;
using System;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WeatherStation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string strTemperature;
        private string strHumidity;
        private string strPressure;

        public MainPage()
        {
            this.InitializeComponent();
            Display();
        }

        private async void Display()
        {
            BME280Sensor BME280 = new BME280Sensor();

            //Initalise the sensor
            await BME280.Initialise();

            //Read the Temperature, Humidity and Pressure
           
            while (BME280.Initialised)
            {
                strTemperature = BME280.ReadTemperature().Result.ToString();
                strHumidity = BME280.ReadHumidity().Result.ToString();
                strPressure = BME280.ReadPreasure().Result.ToString();

                SendDataToIoTSuite();

                //write temperature and humidity out to the screen
                txtTemperature.Text = strTemperature;
                txtHumidity.Text = strHumidity;

                //delay for 20 seconds
                await Task.Delay(20000);

            }
           

        }

        private async void SendDataToIoTSuite()
        {
            await Task.Run(async () => { await AzureIoTHub.SendDataToAzure(Convert.ToDouble(strTemperature), Convert.ToDouble(strHumidity)); });
        }

      
    }
}
