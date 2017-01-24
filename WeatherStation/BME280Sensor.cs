﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace WeatherStation
{
    //Some of this code was taken from David Shoemaker Lesson 203 V2 for BME280 (can be found in github)

    public class BME280_CalibrationData
    {

        //BME280 Registers
        public UInt16 dig_T1 { get; set; }
        public Int16 dig_T2 { get; set; }
        public Int16 dig_T3 { get; set; }

        public UInt16 dig_P1 { get; set; }
        public Int16 dig_P2 { get; set; }
        public Int16 dig_P3 { get; set; }
        public Int16 dig_P4 { get; set; }
        public Int16 dig_P5 { get; set; }
        public Int16 dig_P6 { get; set; }
        public Int16 dig_P7 { get; set; }
        public Int16 dig_P8 { get; set; }
        public Int16 dig_P9 { get; set; }

        public UInt16 dig_H1 { get; set; }
        public Int16 dig_H2 { get; set; }
        public Int16 dig_H3 { get; set; }
        public Int16 dig_H4 { get; set; }
        public Int16 dig_H5 { get; set; }
        public Int16 dig_H6 { get; set; }
    }

    public class BME280Sensor
    {
        //The following are available in the BME280 data sheet https://cdn-shop.adafruit.com/datasheets/BST-BME280_DS001-10.pdf

        //Connecting SDO to GND results in slave address 1110110 (0x76); connection it to VDDIO results in slave address 1110111 (0x77)
        const byte BME280_Address = 0x77;
        const byte BME280_Signature = 0x60;

        enum eRegisters : byte
        {
            BME280_REGISTER_DIG_T1 = 0x88,
            BME280_REGISTER_DIG_T2 = 0x8A,
            BME280_REGISTER_DIG_T3 = 0x8C,

            BME280_REGISTER_DIG_P1 = 0x8E,
            BME280_REGISTER_DIG_P2 = 0x90,
            BME280_REGISTER_DIG_P3 = 0x92,
            BME280_REGISTER_DIG_P4 = 0x94,
            BME280_REGISTER_DIG_P5 = 0x96,
            BME280_REGISTER_DIG_P6 = 0x98,
            BME280_REGISTER_DIG_P7 = 0x9A,
            BME280_REGISTER_DIG_P8 = 0x9C,
            BME280_REGISTER_DIG_P9 = 0x9E,

            BME280_REGISTER_DIG_H1_REG = 0xA1,
            BME280_REGISTER_DIG_H2_LSB = 0xE1,
            BME280_REGISTER_DIG_H2_MSB = 0xE2,
            BME280_REGISTER_DIG_H3_REG = 0xE3,
            BME280_REGISTER_DIG_H4_MSB = 0xE4,
            BME280_REGISTER_DIG_H4_LSB = 0xE5,
            BME280_REGISTER_DIG_H5_MSB = 0xE6,
            BME280_REGISTER_DIG_H5_LSB = 0xE5,
            BME280_REGISTER_DIG_H6_REG = 0xE7,

            BME280_REGISTER_CHIPID = 0xD0,
            BME280_REGISTER_VERSION = 0xD1,
            BME280_REGISTER_SOFTRESET = 0xE0,

            BME280_REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            BME280_REGISTER_CONTROLHUMID = 0xF2,
            BME280_REGISTER_CONTROL = 0xF4,
            BME280_REGISTER_CONFIG = 0xF5,

            BME280_REGISTER_PRESSUREDATA_MSB = 0xF7,
            BME280_REGISTER_PRESSUREDATA_LSB = 0xF8,
            BME280_REGISTER_PRESSUREDATA_XLSB = 0xF9, // bits <7:4>

            BME280_REGISTER_TEMPDATA_MSB = 0xFA,
            BME280_REGISTER_TEMPDATA_LSB = 0xFB,
            BME280_REGISTER_TEMPDATA_XLSB = 0xFC, // bits <7:4>

            BME280_REGISTER_HUMIDDATA_MSB = 0xFD,
            BME280_REGISTER_HUMIDDATA_LSB = 0xFE,
        };

        //String for the friendly name of the I2C bus 
        const string I2CControllerName = "I2C1";
        //Create an I2C device
        private I2cDevice bme280 = null;
        //Create new calibration data for the sensor
        BME280_CalibrationData CalibrationData;
        //Variable to check if device is initialized
        bool init = false;

        public bool Initialised
        {
            get { return this.init; }
        }

        //Method to initialise the BME280 sensor
        public async Task Initialise()
        {
       

            Debug.Write("BME280 - Initialise.");

            try
            {
                //Instantiate the I2CConnectionSettings using the device address of the BME280
                I2cConnectionSettings settings = new I2cConnectionSettings(BME280_Address);

                //Set the I2C bus speed of connection to fast mode
                settings.BusSpeed = I2cBusSpeed.FastMode;

                //Use the I2CBus device selector to create an advanced query syntax string
                string aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                //Use the Windows.Devices.Enumeration.DeviceInformation class to create a collection using the advanced query syntax string
                DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);

                //Instantiate the the BME280 I2C device using the device id of the I2CBus and the I2CConnectionSettings
                bme280 = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                //Check if device was found
                if (bme280 == null)
                {
                    Debug.Write("Device not found");
                }
                else
                {
                    try
                    {
                        //Make sure the I2C device is initialized
                        if (!init) await Begin();
                    }
                    catch (Exception e)
                    {
                        init = false;
                        Debug.Write("Exception: " + e.Message + "\n" + e.StackTrace);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Write("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }

        }
        private async Task Begin()
        {


            Debug.Write("BME280 - Begin");
            byte[] WriteBuffer = new byte[] { (byte)eRegisters.BME280_REGISTER_CHIPID };
            byte[] ReadBuffer = new byte[] { 0xFF };

            //Read the device signature
            bme280.WriteRead(WriteBuffer, ReadBuffer);
            Debug.Write("BME280 Signature: " + ReadBuffer[0].ToString());

            //Verify the device signature
            if (ReadBuffer[0] != BME280_Signature)
            {
                Debug.Write("BME280 - Begin Signature Mismatch.");
                return;
            }

            //Set the initalize variable to true
            init = true;

            //Read the coefficients table
            CalibrationData = await ReadCoefficeints();

            //Write control register
            await WriteControlRegister();

            //Write humidity control register
            await WriteControlRegisterHumidity();
        }

        //Method to write 0x03 to the humidity control register
        private async Task WriteControlRegisterHumidity()
        {
            byte[] WriteBuffer = new byte[] { (byte)eRegisters.BME280_REGISTER_CONTROLHUMID, 0x03 };
            bme280.Write(WriteBuffer);
            await Task.Delay(1);
            return;
        }

        //Method to write 0x3F to the control register
        private async Task WriteControlRegister()
        {
            byte[] WriteBuffer = new byte[] { (byte)eRegisters.BME280_REGISTER_CONTROL, 0x3F };
            bme280.Write(WriteBuffer);
            await Task.Delay(1);
            return;
        }

        //Method to read a 16-bit value from a register and return it in little endian format
        private UInt16 ReadUInt16_LittleEndian(byte register)
        {
            UInt16 value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            writeBuffer[0] = register;

            bme280.WriteRead(writeBuffer, readBuffer);
            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            value = (UInt16)(h + l);
            return value;
        }

        //Method to read an 8-bit value from a register
        private byte ReadByte(byte register)
        {
            byte value = 0;
            byte[] writeBuffer = new byte[] { 0x00 };
            byte[] readBuffer = new byte[] { 0x00 };

            writeBuffer[0] = register;

            bme280.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[0];
            return value;
        }

        //Method to read the caliberation data from the registers
        private async Task<BME280_CalibrationData> ReadCoefficeints()
        {
            // 16 bit calibration data is stored as Little Endian, the helper method will do the byte swap.
            CalibrationData = new BME280_CalibrationData();

            // Read temperature calibration data
            CalibrationData.dig_T1 = ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T1);
            CalibrationData.dig_T2 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T2);
            CalibrationData.dig_T3 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_T3);

            // Read presure calibration data
            CalibrationData.dig_P1 = ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P1);
            CalibrationData.dig_P2 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P2);
            CalibrationData.dig_P3 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P3);
            CalibrationData.dig_P4 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P4);
            CalibrationData.dig_P5 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P5);
            CalibrationData.dig_P6 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P6);
            CalibrationData.dig_P7 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P7);
            CalibrationData.dig_P8 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P8);
            CalibrationData.dig_P9 = (Int16)ReadUInt16_LittleEndian((byte)eRegisters.BME280_REGISTER_DIG_P9);

            CalibrationData.dig_H1 = ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H1_REG);
            CalibrationData.dig_H2 = (Int16)((ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H2_MSB) << 8) | ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H2_LSB));
            CalibrationData.dig_H3 = ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H3_REG);
            CalibrationData.dig_H4 = (Int16)((ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H4_MSB) << 4) | (ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H4_LSB) & 0xF));
            CalibrationData.dig_H5 = (Int16)((ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H5_MSB) << 4) | (ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H5_LSB) >> 4));
            CalibrationData.dig_H6 = ReadByte((byte)eRegisters.BME280_REGISTER_DIG_H6_REG);

            await Task.Delay(1);
            return CalibrationData;
        }


        //t_fine carries fine temperature as global value
        Int32 t_fine = Int32.MinValue;
        //Method to return the temperature in DegC. Resolution is 0.01 DegC. Output value of “5123” equals 51.23 DegC.
        private double BME280_compensate_T_double(Int32 adc_T)
        {
            double var1, var2, T;

            //The temperature is calculated using the compensation formula in the BME280 datasheet
            var1 = ((adc_T / 16384.0) - (CalibrationData.dig_T1 / 1024.0)) * CalibrationData.dig_T2;
            var2 = ((adc_T / 131072.0) - (CalibrationData.dig_T1 / 8192.0)) * CalibrationData.dig_T3;

            t_fine = (Int32)(var1 + var2);

            T = (var1 + var2) / 5120.0;
            return T;
        }


        //Method to returns the pressure in Pa, in Q24.8 format (24 integer bits and 8 fractional bits).
        //Output value of “24674867” represents 24674867/256 = 96386.2 Pa = 963.862 hPa
        private Int64 BME280_compensate_P_Int64(Int32 adc_P)
        {

            Int64 var1, var2, p;

            //The pressure is calculated using the compensation formula in the BME280 datasheet
            var1 = t_fine - 128000;
            var2 = var1 * var1 * (Int64)CalibrationData.dig_P6;
            var2 = var2 + ((var1 * (Int64)CalibrationData.dig_P5) << 17);
            var2 = var2 + ((Int64)CalibrationData.dig_P4 << 35);
            var1 = ((var1 * var1 * (Int64)CalibrationData.dig_P3) >> 8) + ((var1 * (Int64)CalibrationData.dig_P2) << 12);
            var1 = (((((Int64)1 << 47) + var1)) * (Int64)CalibrationData.dig_P1) >> 33;
            if (var1 == 0)
            {
                Debug.Write("BME280_compensate_P_Int64 Jump out to avoid / 0");
                return 0; //Avoid exception caused by division by zero
            }
            //Perform calibration operations as per datasheet: https://cdn-shop.adafruit.com/datasheets/BST-BME280_DS001-10.pdf
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = ((Int64)CalibrationData.dig_P9 * (p >> 13) * (p >> 13)) >> 25;
            var2 = ((Int64)CalibrationData.dig_P8 * p) >> 19;
            p = ((p + var1 + var2) >> 8) + ((Int64)CalibrationData.dig_P7 << 4);
            return p;
        }

        // Returns humidity in %RH as unsigned 32 bit integer in Q22.10 format (22 integer and 10 fractional bits).
        // Output value of “47445” represents 47445/1024 = 46.333 %RH
        private Int32 BME280_compensate_H_int32(Int32 adc_H)
        {
            Int32 v_x1_u32r;
            v_x1_u32r = (t_fine - ((Int32)76800));
            v_x1_u32r = (((((adc_H << 14) - (((Int32)CalibrationData.dig_H4) << 20) - (((Int32)CalibrationData.dig_H5) * v_x1_u32r)) +
            ((Int32)16384)) >> 15) * (((((((v_x1_u32r * ((Int32)CalibrationData.dig_H6)) >> 10) * (((v_x1_u32r *
            ((Int32)CalibrationData.dig_H3)) >> 11) + ((Int32)32768))) >> 10) + ((Int32)2097152)) *
            ((Int32)CalibrationData.dig_H2) + 8192) >> 14));
            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) * ((Int32)CalibrationData.dig_H1)) >> 4));
            v_x1_u32r = (v_x1_u32r < 0 ? 0 : v_x1_u32r);
            v_x1_u32r = (v_x1_u32r > 419430400 ? 419430400 : v_x1_u32r);
            return (Int32)(v_x1_u32r >> 12);
        }

      
        double bme280_compensate_humidity_double(Int32 v_uncom_humidity_s32)
        {
            int BME280_INIT_VALUE = 0;
            int BME280_INVALID_DATA = 0;
            double var_h = BME280_INIT_VALUE;

            var_h = (((double)t_fine) - 76800.0);

            if (var_h != BME280_INIT_VALUE)
                var_h = (v_uncom_humidity_s32 - (((double)CalibrationData.dig_H4) * 64.0 +
                ((double)CalibrationData.dig_H5) / 16384.0 * var_h)) *
                (((double)CalibrationData.dig_H2) / 65536.0 *
                (1.0 + ((double)CalibrationData.dig_H6) / 67108864.0 * var_h *
                (1.0 + ((double)CalibrationData.dig_H3) / 67108864.0 * var_h)));
            else
                return BME280_INVALID_DATA;

            var_h = var_h * (1.0 - ((double)CalibrationData.dig_H1) * var_h / 524288.0);

            if (var_h > 100.0)
                var_h = 100.0;
            else if (var_h < 0.0)
                var_h = 0.0;

            return var_h;
        }

        public async Task<float> ReadTemperature()
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the MSB, LSB and bits 7:4 (XLSB) of the temperature from the BME280 registers
            byte tmsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_MSB);
            byte tlsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_LSB);
            byte txlsb = ReadByte((byte)eRegisters.BME280_REGISTER_TEMPDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            Int32 t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the temperature in degC
            double temp = BME280_compensate_T_double(t);

            //Return the temperature as a float value
            return (float)temp;
        }

        public async Task<float> ReadPreasure()
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the temperature first to load the t_fine value for compensation
            if (t_fine == Int32.MinValue)
            {
                await ReadTemperature();
            }

            //Read the MSB, LSB and bits 7:4 (XLSB) of the pressure from the BME280 registers
            byte tmsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_MSB);
            byte tlsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_LSB);
            byte txlsb = ReadByte((byte)eRegisters.BME280_REGISTER_PRESSUREDATA_XLSB); // bits 7:4

            //Combine the values into a 32-bit integer
            Int32 t = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);

            //Convert the raw value to the pressure in Pa
            Int64 pres = BME280_compensate_P_Int64(t);

            //Return the temperature as a float value
            return ((float)pres) / 256;
        }

        //Method to take the sea level pressure in Hectopascals(hPa) as a parameter and calculate the altitude using current pressure.
        public async Task<float> ReadAltitude(float seaLevel)
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the pressure first
            float pressure = await ReadPreasure();
            //Convert the pressure to Hectopascals(hPa)
            pressure /= 100;

            //Calculate and return the altitude using the international barometric formula
            return 44330.0f * (1.0f - (float)Math.Pow((pressure / seaLevel), 0.1903f));
        }

        public async Task<float> ReadHumidity()
        {
            //Make sure the I2C device is initialized
            if (!init) await Begin();

            //Read the temperature first to load the t_fine value for compensation
            if (t_fine == Int32.MinValue)
            {
                await ReadTemperature();
            }

            //Read the MSB and LSB of the humidity from the BME280 registers
            byte hmsb = ReadByte((byte)eRegisters.BME280_REGISTER_HUMIDDATA_MSB);
            byte hlsb = ReadByte((byte)eRegisters.BME280_REGISTER_HUMIDDATA_LSB);

            //Combine the values into a 32-bit integer
            Int32 h = (hmsb << 8) + hlsb;

            //Convert the raw value to the humidity in %
            double humidity = bme280_compensate_humidity_double(h);

            //Return the humidity as a float value
            return ((float)humidity);
        }
    }



}
