using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace MAS345_GUI
{
    public class MasSerialPort : SerialPort
    {
#if DEBUG
        private MasEmulator Emulator;
        private bool _IsOpen;
        public MasSerialPort()
        {
            Emulator = new MasEmulator();
            _IsOpen = false;
        }

        public bool isOpen
        {
            get
            {
                return _IsOpen;
            }
        }
        public new void Open()
        {
            _IsOpen = true;
        }
        public new void Close()
        {
            _IsOpen = false;
        }
#endif
        public bool ContinousMode = false;

        public MeasureUnit ReadData()
        {
            //MeasureUnit ReturnValue = new MeasureUnit(DateTime.Now, MeasureType.DcCurrent, 1.0);
            DateTime ReadedDateTime;
            MeasureType ReadedType;
            Double ReadedValue;
#if DEBUG
            Thread.Sleep(1000);
            ReturnValue = Emulator.GiveNextMeasure();
#else
            double ValueMultiplier = 1;
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            byte[] input_data = new byte[14];
            String input_string = "";

            WriteLine(""); //Send the empty line to the device
            
            Thread.Sleep(1000);
            Read(input_data, 0, 14);

            input_string = enc.GetString(input_data).Trim();
            //MessageBox.Show("'" + input_string + "'");

            switch (input_string.Substring(9, 4).TrimStart())
            {
                case "mV":
                    ValueMultiplier = 0.001;
                    break;
                case "mA":
                    ValueMultiplier = 0.001;
                    break;
                case "kOhm":
                    ValueMultiplier = 1000;
                    break;
                case "MOhm":
                    ValueMultiplier = 1000 * 1000;
                    break;
            }

            if (input_string.Substring(3, 1).Equals("-"))
            {
                ValueMultiplier *= -1;
            }

            ReadedDateTime = DateTime.Now;
            /*ReturnValue.Unit = input_string.Substring(9, 4);*/

            switch(input_string.Substring(0, 2))
            {
                case "AC":
                    if (input_string[input_string.Length -1].Equals('A'))
                    {
                        ReadedType = MeasureType.AcCurrent;
                    }
                    else
                    {
                        ReadedType = MeasureType.AcVoltage;
                    }
                    break;
                case "DC":
                    if (input_string[input_string.Length - 1].Equals('A'))
                    {
                        ReadedType = MeasureType.DcCurrent;
                    }
                    else
                    {
                        ReadedType = MeasureType.DcVoltage;
                    }
                    break;
                case "OH":
                    ReadedType = MeasureType.Resistance;
                    break;
                case "DI":
                    ReadedType = MeasureType.Diode;
                    break;
                case "TE":
                    ReadedType = MeasureType.Temperature;
                    break;
                case "CA":
                    ReadedType = MeasureType.Capacity;
                    break;
                default:
                    ReadedType = MeasureType.hFe;
                    break;
            }

            try
            {
                ReadedValue = double.Parse(input_string.Substring(4, 5).Trim(),
                                           System.Globalization.NumberStyles.AllowDecimalPoint,
                                           System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            catch
            {
                throw (new InvalidDataException());
            }
            ReadedValue *= ValueMultiplier;
                 
#endif
            return new MeasureUnit(ReadedDateTime, ReadedType, ReadedValue);
        }
    }

    [Serializable()]
    public class MeasureUnit
    {
        public DateTime Time { get; set; }
        public MeasureType Type { get; set; }
        public Double Value;

        public string Unit
        {
            get
            {
                string ReturnValue = "";
                if ((Type == MeasureType.AcCurrent) || (Type == MeasureType.DcCurrent))
                {
                    ReturnValue = "A";
                }
                if ((Type == MeasureType.AcVoltage) || (Type == MeasureType.DcVoltage) || (Type == MeasureType.Diode))
                {
                    ReturnValue = "V";
                }
                if (Type == MeasureType.Temperature)
                {
                    ReturnValue = "˚C";
                }
                if (Type == MeasureType.Capacity)
                {
                    ReturnValue = "nF";
                }
                if (Type == MeasureType.Resistance)
                {
                    /*if (OrigValue >= 1000)
                    {
                        ReturnValue = "kΩ";
                    }
                    else
                    {
                        ReturnValue = "Ω";
                    }*/
                    ReturnValue = "Ω";
                }
                return ReturnValue;
            }
        }
        public string ScaledValueWithUnit
        {
            get
            {
                string ReturnValue = "";
                if (Value >= 1000)
                {
                    ReturnValue = (Value / 1000).ToString() + " k" + Unit;
                }
                else
                {
                    ReturnValue = Value.ToString() + " " + Unit;
                }
                return ReturnValue;
            }
        }

        public MeasureUnit(DateTime NewTime, MeasureType NewType, Double NewValue)
        {
            this.Time = NewTime;
            this.Type = NewType;
            this.Value = NewValue;
        }
        public MeasureUnit(MeasureUnit TheOther): this(TheOther.Time, TheOther.Type, TheOther.Value ) {}
        protected MeasureUnit() { }
    }
    public enum MeasureType
    {
        Unknown,
        AcCurrent,
        AcVoltage,
        DcCurrent,
        DcVoltage,
        Resistance,
        Diode,
        Temperature,
        Capacity,
        hFe
    };

#if DEBUG
    class MasEmulator
    {
        private MeasureUnit _LastMeasure;
        public MasEmulator()
        {
            _LastMeasure = new MeasureUnit(DateTime.Now, MeasureType.DcVoltage, 0.0);
        }

        public MeasureUnit GiveNextMeasure()
        {
            Random rand = new Random();
            _LastMeasure.Time = DateTime.Now;
            _LastMeasure.Value = rand.Next(200000);
            _LastMeasure.Value /= 1000;
            _LastMeasure.Value -= 100;
            /*_LastMeasure.Value++;
            if (_LastMeasure.Value == 10.0)
            {
                if (_LastMeasure.Type == MeasureType.DcVoltage)
                {
                    _LastMeasure.Type = MeasureType.DcCurrent;
                }
                else if (_LastMeasure.Type == MeasureType.DcCurrent)
                {
                    _LastMeasure.Type = MeasureType.DcVoltage;
                }
                _LastMeasure.Value = 0.0;
            }*/
            return new MeasureUnit(_LastMeasure);
        }
    }
#endif
}
