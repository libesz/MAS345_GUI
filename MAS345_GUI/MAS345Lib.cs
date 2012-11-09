using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace MAS345_GUI
{
    [Serializable()]
    public class MeasureUnit
    {
        public DateTime Time { get; set; }
        public MeasureType Type { get; set; }
        public Double Value { get; set; }

        public string Unit
        {
            get
            {
                string ReturnValue = "";
                if ((Type == MeasureType.AcCurrent) || (Type == MeasureType.DcCurrent))
                {
                    ReturnValue = "A";
                }
                if ((Type == MeasureType.AcVoltage) || (Type == MeasureType.DcVoltage))
                {
                    ReturnValue = "V";
                }
                return ReturnValue;
            }
        }
        public string ValueWithUnit
        {
            get
            {
                return Value + " " + Unit;
            }
        }

        public MeasureUnit(DateTime Time, MeasureType Type, Double Value)
        {
            this.Time = Time;
            this.Type = Type;
            this.Value = Value;
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
            MeasureUnit ReturnValue;
#if DEBUG
            Thread.Sleep(1000);
            ReturnValue = Emulator.GiveNextMeasure();
#else
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            byte[] input_data = new byte[14];
            String input_string = "";

            SerialPort port = e.Argument as SerialPort;
            while (true)
            {
                if (backgroundWorker1.CancellationPending) { e.Cancel = true; return; }
                try
                {
                    port.RtsEnable = false;
                    port.DtrEnable = true;
                    port.WriteLine("");
                    Thread.Sleep(1000);
                    port.Read(input_data, 0, 14);

                    input_string = enc.GetString(input_data);

                    backgroundWorker1.ReportProgress(1, input_string);
                }
                catch
                {
                    backgroundWorker1.ReportProgress(2, "Can't recieve data from " + serialPort1.PortName);
                }
            }
                String input_string = e.UserState as String;
                MAS345_data decodedData = new MAS345_data();

                decodedData.time = DateTime.Now.ToString();

                decodedData.unit = input_string.Substring(9, 4);

                if(input_string.Substring(0, 2).Equals("AC"))
                {
                    if (decodedData.unit.Substring(3, 1).Equals("A"))
                    {
                        decodedData.measureType = "AC Current";
                    }
                    else
                    {
                        decodedData.measureType = "AC Voltage";
                    }
                }
                else if (input_string.Substring(0, 2).Equals("DC"))
                {
                    if (decodedData.unit.Substring(3, 1).Equals("A"))
                    {
                        decodedData.measureType = "DC Current";
                    }
                    else
                    {
                        decodedData.measureType = "DC Voltage";
                    }
                }
                else if (input_string.Substring(0, 2).Equals("OH"))
                {
                    decodedData.measureType = "Resistance";
                }
                else if (input_string.Substring(0, 2).Equals("DI"))
                {
                    decodedData.measureType = "Diode";
                }
                else if (input_string.Substring(0, 2).Equals("TE"))
                {
                    decodedData.measureType = "Temperature";
                }
                else if (input_string.Substring(0, 2).Equals("CA"))
                {
                    decodedData.measureType = "Capacity";
                }
                else
                {
                    decodedData.measureType = "hFE";
                }

                if (input_string.Substring(3, 1).Equals("-"))
                {
                    decodedData.value = "-";
                }

                decodedData.unit = decodedData.unit.TrimStart();
                decodedData.value += input_string.Substring(4, 5);

#endif
            return ReturnValue;
        }
    }

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
            _LastMeasure.Time = DateTime.Now;
            _LastMeasure.Value++;
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
            }
            return new MeasureUnit(_LastMeasure);
        }
    }
#endif
}
