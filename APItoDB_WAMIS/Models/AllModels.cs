using Newtonsoft.Json;
using System.Collections.Generic;

namespace WamisDataCollector.Models
{
    // 공통: 관측소/댐 목록 응답
    public class StationInfo
    {
        [JsonProperty("obscd")]
        private string Obscd { get; set; }
        [JsonProperty("wlobscd")]
        private string WlObscd { get; set; }
        [JsonProperty("wtobscd")]
        private string WtObscd { get; set; }
        [JsonProperty("damcd")]
        private string Damcd { get; set; }

        [JsonIgnore]
        public string StationCode { get { return Obscd ?? WlObscd ?? WtObscd ?? Damcd; } set { Obscd = value; } }

        [JsonProperty("obsnm")]
        private string Obsnm { get; set; }
        [JsonProperty("damnm")]
        private string Damnm { get; set; }

        [JsonIgnore]
        public string Name { get { return Obsnm ?? Damnm; } set { Obsnm = value; } }

        [JsonIgnore]
        public string StationType { get; set; }
    }
    public class StationResponse { public List<StationInfo> List { get; set; } }

    // 강수량 데이터 모델
    public class RainfallData
    {
        [JsonProperty("ymdh")] public string Ymdh { get; set; }
        [JsonProperty("ymd")] public string Ymd { get; set; }
        [JsonProperty("ym")] public string Ym { get; set; }
        [JsonProperty("rf")] private double? RF { get; set; }
        [JsonProperty("dtrf")] private double? DTRF { get; set; }
        [JsonIgnore] public double? Rainfall => RF ?? DTRF;
    }
    public class RainfallResponse { public List<RainfallData> List { get; set; } }

    // 수위 데이터 모델
    public class WaterLevelData
    {
        [JsonProperty("ymdh")] public string Ymdh { get; set; }
        [JsonProperty("ymd")] public string Ymd { get; set; }
        [JsonProperty("wl")] public double? WaterLevel { get; set; }
    }
    public class WaterLevelResponse { public List<WaterLevelData> List { get; set; } }

    // 기상 데이터 모델
    public class WeatherData
    {
        [JsonProperty("ymdh")] public string Ymdh { get; set; }
        [JsonProperty("ymd")] public string Ymd { get; set; }
        [JsonProperty("ta")] private double? TempHourly { get; set; }
        [JsonProperty("taavg")] private double? TempDaily { get; set; }
        [JsonIgnore] public double? Temperature => TempHourly ?? TempDaily;
        [JsonProperty("hm")] private double? HumHourly { get; set; }
        [JsonProperty("hmavg")] private double? HumDaily { get; set; }
        [JsonIgnore] public double? Humidity => HumHourly ?? HumDaily;
        [JsonProperty("ws")] private double? WsHourly { get; set; }
        [JsonProperty("wsavg")] private double? WsDaily { get; set; }
        [JsonIgnore] public double? WindSpeed => WsHourly ?? WsDaily;
    }
    public class WeatherResponse { public List<WeatherData> List { get; set; } }

    // 유량 측정성과 모델
    public class FlowMeasurementData
    {
        [JsonProperty("obsymd")] public string ObsYmd { get; set; }
        [JsonProperty("avgwl")] public double? AvgWl { get; set; }
        [JsonProperty("flw")] public double? Flow { get; set; }
    }
    public class FlowMeasurementResponse { public List<FlowMeasurementData> List { get; set; } }

    // 유량 일자료 모델
    public class FlowDailyData
    {
        [JsonProperty("ymd")] public string Ymd { get; set; }
        [JsonProperty("fw")] public double? Flow { get; set; }
    }
    public class FlowDailyResponse { public List<FlowDailyData> List { get; set; } }

    // 댐 수문 데이터 모델
    public class DamData
    {
        [JsonProperty("obsdh")] public string Obsdh { get; set; }
        [JsonProperty("obsymd")] public string Obsymd { get; set; }
        [JsonProperty("rwl")] private double? Rwl { get; set; }
        [JsonProperty("avwl")] private double? Avwl { get; set; }
        [JsonIgnore] public double? StorageWaterLevel => Rwl ?? Avwl;
        [JsonProperty("iqty")] private double? Iqty { get; set; }
        [JsonProperty("aviqty")] private double? Aviqty { get; set; }
        [JsonIgnore] public double? Inflow => Iqty ?? Aviqty;
        [JsonProperty("tdqty")] private double? Tdqty { get; set; }
        [JsonProperty("avtdqty")] private double? Avtdqty { get; set; }
        [JsonIgnore] public double? TotalOutflow => Tdqty ?? Avtdqty;
    }
    public class DamResponse { public List<DamData> List { get; set; } }
}
