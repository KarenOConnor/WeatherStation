using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Runtime.Serialization;
using Windows.Devices.Geolocation;
using Newtonsoft.Json;
using System.Diagnostics;

static class AzureIoTHub
{
    //Some of this code was taken from AzureIoTSuiteUWPDevice written by Olivier Bloch. That project is available in github

    [DataContract]
    internal class TelemetryData
    {
        [DataMember]
        internal double Temperature;

        [DataMember]
        internal double Humidity;

    }

    private static DeviceClient deviceClient;

    private static byte[] Serialize(object obj)
    {
        string json = JsonConvert.SerializeObject(obj);
        return Encoding.UTF8.GetBytes(json);

    }

    private static dynamic DeSerialize(byte[] data)
    {
        string text = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject(text);
    }

    //Azure IoT device Connectionstring values
    public static string deviceId = "{DeviceID}";
    public static string hostName = "{DeviceHostName}";
    public static string deviceKey = "{DevicePrimaryKey}";

    public static string deviceConnectionString = string.Format("HostName={0};DeviceId={1};SharedAccessKey={2}", hostName, deviceId, deviceKey);

    //Send Device Telemetry data for display on the Azure IoT Suite Telemetry section
    private static async void sendDeviceTelemetryData(double Temperature, double Humidity)
    {
        TelemetryData data = new TelemetryData();
        data.Temperature = Temperature;
        data.Humidity = Humidity;

        try
        {
            var msg = new Message(Serialize(data));
            if (deviceClient != null)
            {
                await deviceClient.SendEventAsync(msg);
            }
        }
        catch (System.Exception e)
        {
            Debug.Write("Exception while sending device telemetry data :\n" + e.Message.ToString());
        }
        Debug.Write("Sent telemetry data to IoT Suite\nTemperature=" + string.Format("{0:0.00}", Temperature) + "\nHumidity=" + string.Format("{0:0.00}", Humidity));

    }

    //Send the data to Azure IoT Suite
    public static async Task SendDataToAzure(double Temperature, double Humidity)
    { 

            connectToIoTSuite();
            sendDeviceTelemetryData(Temperature, Humidity);
            await Task.Delay(10);
    }

    //Connect to Azure IoT hub device
    public static void connectToIoTSuite()
    {
        try
        {
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Http1);
        }
        catch
        {
            Debug.Write("Error while trying to connect to IoT Hub");
        }
    }
 
}
