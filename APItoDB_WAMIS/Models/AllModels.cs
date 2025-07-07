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
        public string DamCode { get; set; }

        [JsonIgnore]
        public string StationCode => Obscd ?? WlObscd ?? WtObscd ?? DamCode;

        [JsonProperty("obsnm")]
        public string StationName { get; set; }

        [JsonProperty("damnm")]
        public string DamName { get; set; }

        [JsonIgnore]
        public string Name => StationName ?? DamName;

        [JsonIgnore]
        public string StationType { get; set; }
    }

    public class StationResponse
    {
        public List<StationInfo> List { get; set; }
    }

    // 강수량 데이터 모델
    public class RainfallData
    {
        [JsonProperty("ymdh")]
        public string Ymdh { get; set; }

        [JsonProperty("ymd")]
        public string Ymd { get; set; }

        [JsonProperty("ym")]
        public string Ym { get; set; }

        [JsonProperty("rf")]
        private double? RainfallHourlyDaily { get; set; }

        [JsonProperty("dtrf")]
        private double? RainfallMonthly { get; set; }

        [JsonIgnore]
        public double? Rainfall => RainfallHourlyDaily ?? RainfallMonthly;
    }
    public class RainfallResponse { public List<RainfallData> List { get; set; } }

    // 수위 데이터 모델
    public class WaterLevelData
    {
        [JsonProperty("ymdh")]
        public string Ymdh { get; set; }

        [JsonProperty("ymd")]
        public string Ymd { get; set; }

        [JsonProperty("wl")]
        public double? WaterLevel { get; set; }
    }
    public class WaterLevelResponse { public List<WaterLevelData> List { get; set; } }

    // 기상 데이터 모델
    public class WeatherData
    {
        [JsonProperty("ymdh")]
        public string Ymdh { get; set; }
        [JsonProperty("ymd")]
        public string Ymd { get; set; }
        [JsonProperty("ta")]
        public double? TempAvg { get; set; } // 시자료: 기온
        [JsonProperty("taavg")]
        private double? TempAvgDaily { get; set; } // 일자료: 평균기온
        [JsonIgnore]
        public double? Temperature => TempAvg ?? TempAvgDaily;
        [JsonProperty("hm")]
        public double? Humidity { get; set; }
        [JsonProperty("ws")]
        public double? WindSpeed { get; set; }
    }
    public class WeatherResponse { public List<WeatherData> List { get; set; } }

    // 유량 측정성과 모델
    public class FlowMeasurementData
    {
        [JsonProperty("obsymd")]
        public string ObsYmd { get; set; }
        [JsonProperty("avgwl")]
        public double? AvgWl { get; set; }
        [JsonProperty("flw")]
        public double? Flow { get; set; }
    }
    public class FlowMeasurementResponse { public List<FlowMeasurementData> List { get; set; } }

    // 유량 일자료 모델
    public class FlowDailyData
    {
        [JsonProperty("ymd")]
        public string Ymd { get; set; }
        [JsonProperty("fw")]
        public double? Flow { get; set; }
    }
    public class FlowDailyResponse { public List<FlowDailyData> List { get; set; } }

    // 댐 수문 데이터 모델
    public class DamData
    {
        [JsonProperty("obsdh")]
        public string Obsdh { get; set; } // 시자료
        [JsonProperty("obsymd")]
        public string Obsymd { get; set; } // 일/월자료
        [JsonProperty("rwl")]
        public double? Rwl { get; set; } // 저수위
        [JsonProperty("iqty")]
        public double? Iqty { get; set; } // 유입량
        [JsonProperty("tdqty")]
        public double? Tdqty { get; set; } // 총방류량
    }
    public class DamResponse { public List<DamData> List { get; set; } }
}
