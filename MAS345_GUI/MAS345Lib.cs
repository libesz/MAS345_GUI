using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace MAS345GUI
{
    class MeasureUnit
    {
        public DateTime Time { get; set; }
        public MeasureType Type { get; set; }
        public Double Value { get; set; }

        public MeasureUnit(DateTime Time, MeasureType Type, Double Value)
        {
            this.Time = Time;
            this.Type = Type;
            this.Value = Value;
        }
    }
    enum MeasureType
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

    class MasSerialPort : SerialPort
    {
        private bool _DebugMode;
        public bool DebugMode
        {
            get
            {
                return _DebugMode;
            }
            set
            {
                _DebugMode = value;
            }
        }

        public override bool isOpen
        {
            get
            {
                if (_DebugMode) return true;
                else return base.IsOpen;
            }
        }
        public override void Open()
        {
            if (_DebugMode) return;
            else base.Open();
        }

    }

    class MasEmulator
    {
        private MeasureUnit _LastMeasure;
        public MasEmulator()
        {
            _LastMeasure = new MeasureUnit(DateTime.Now, MeasureType.DcVoltage, 1.0);
        }

        public string RawMeasure()
        {
            string ReturnValue = "";
            return ReturnValue;
        }

        private MeasureUnit GiveNextMeasure()
        {
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
                _LastMeasure.Value = 1.0;
            }
            return _LastMeasure;
        }
    }
}
