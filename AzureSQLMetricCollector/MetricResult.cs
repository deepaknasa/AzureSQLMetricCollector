using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureSQLMetricCollector
{
    internal class MetricResult
    {
        public MetricResult()
        {
            value = new List<Value>();
        }
        public List<Value> value { get; set; }
        public TimeStampData DTU_Percent
        {
            get
            {
                return
                    value
                    .SelectMany(v => v.Timeseries)
                    .SelectMany(t => t.Data)
                    .First();
            }
        }
    }

    public class Value
    {
        public Value()
        {
            Timeseries = new List<Timeseries>();
        }
        public List<Timeseries> Timeseries { get; set; }
    }

    public class Timeseries
    {
        public Timeseries()
        {
            Data = new List<TimeStampData>();
        }
        public List<TimeStampData> Data { get; set; }
    }

    public class TimeStampData
    {
        public TimeStampData()
        {
            Average = -1;
            None = -1;
            Minimum = -1;
            Maximum = -1;
            Total = -1;
            Count = -1;
        }
        public DateTime TimeStamp { get; set; }
        public double Average { get; set; }
        public double None { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Total { get; set; }
        public double Count { get; set; }
    }
}
