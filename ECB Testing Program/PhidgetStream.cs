using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phidget22;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ECB_Testing_Program
{
    class PhidgetStream
    {
        public Phidget phidget;
        private string name;
        private string units;
        private double gain; // This is the adjustment needed for the unit conversion from voltage to recorded value
        private double offset;
        private List<double> values, times; //List<double> times;
        public double[] val = {0};
        public double[] t = {0};
        //private Tuple<double, double> values;

        #region Constructors
        public PhidgetStream(Phidget p)
        {
            phidget = p;
            name = "";
            units = null;
            gain = 1;
            offset = 0;
            values = new List<double>();
            times = new List<double>();
        }
        public PhidgetStream(Phidget _phidget, string phidget_name)
        {
            phidget = _phidget;
            name = phidget_name;
            units = null;
            gain = 1;
            offset = 0;
            values = new List<double>();
            times = new List<double>();
        }
        #endregion

        #region Getters and Setters
        public string getName()
        {
            return name;
        }

        public void setName(string phidget_name)
        {
            name = phidget_name;
        }
        
        public string getUnits()
        {
            return units;
        }
        public void setUnits(string phidget_units)
        {
            units = phidget_units;
        }
        public void setGain(double phidget_gain)
        {
            gain = phidget_gain;
        }
        public void setOffset(double phidget_offset)
        {
            offset = phidget_offset;
        }
        public double getGain()
        {
            return gain;
        }
        public double getOffset()
        {
            return offset;
        }
        public Tuple<double, double> getPoint(int index)
        {
            return new Tuple<double, double>(values[index], times[index]);
        }
        public void addPoint(double value, double time)
        {
            // Convert to the approperate units by using y = m*x + b
            values.Add(gain * value + offset);
            times.Add(time);
            val = values.ToArray();
            t = times.ToArray();
        }
        public void addCaculatedPoint(double time, double variable1, double variable2, double c1, double c2, double c3)
        {
            // Convert to the approperate units by using y = c1 * v1 + c2 * v2 + c3
            values.Add(c1*variable1 + c2*variable2 + c3);
            times.Add(time);
            val = values.ToArray();
            t = times.ToArray();
        }
        public double[] getValues()
        {
            return values.ToArray();
        }
        public double[] getTimes()
        {
            return times.ToArray();
        }
        public void Clear()
        {
            values = new List<double>();
            times = new List<double>();
            t = new double[] {0};
            val = new double[] {0};
        }
        #endregion


    }
}
