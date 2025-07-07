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
        // 공통 시간/날짜 필드
        [JsonProperty("ymdh")] public string Ymdh { get; set; } // 시자료 시간
        [JsonProperty("ymd")] public string Ymd { get; set; }   // 일자료 날짜

        // 시자료 필드 (w12)
        [JsonProperty("ta")] public double? Ta { get; set; }       // 기온
        [JsonProperty("hm")] public double? Hm { get; set; }       // 상대습도
        [JsonProperty("td")] public double? Td { get; set; }       // 이슬점온도
        [JsonProperty("ps")] public double? Ps { get; set; }       // 해면기압
        [JsonProperty("ws")] public double? Ws { get; set; }       // 풍속
        [JsonProperty("wd")] public string Wd { get; set; }     // 풍향
        [JsonProperty("sihr1")] public double? Sihr1 { get; set; } // 일사량
        [JsonProperty("catot")] public double? Catot { get; set; } // 전운량
        [JsonProperty("sdtot")] public double? Sdtot { get; set; } // 적설량
        [JsonProperty("sshr1")] public double? Sshr1 { get; set; } // 일조량

        // 일자료 필드 (w13)
        [JsonProperty("taavg")] public double? Taavg { get; set; } // 평균기온
        [JsonProperty("tamin")] public double? Tamin { get; set; } // 최저기온
        [JsonProperty("tamax")] public double? Tamax { get; set; } // 최고기온
        [JsonProperty("wsavg")] public double? Wsavg { get; set; } // 평균풍속
        [JsonProperty("wsmax")] public double? Wsmax { get; set; } // 최대풍속
        [JsonProperty("wdmax")] public string Wdmax { get; set; } // 최대풍향
        [JsonProperty("hmavg")] public double? Havg { get; set; } // 상대습도평균
        [JsonProperty("hmmin")] public double? Hmin { get; set; } // 상대습도최소
        [JsonProperty("evs")] public double? Evs { get; set; }     // 증발량소형
        [JsonProperty("evl")] public double? Evl { get; set; }     // 증발량대형
        [JsonProperty("catotavg")] public double? Catotavg { get; set; } // 평균운량
        [JsonProperty("psavg")] public double? Psavg { get; set; } // 해면기압
        [JsonProperty("psmax")] public double? Psmax { get; set; } // 최대해면기압
        [JsonProperty("psmin")] public double? Psmin { get; set; } // 최저해면기압
        [JsonProperty("sdmax")] public double? Sdmax { get; set; } // 최심신적설량
        [JsonProperty("tdavg")] public double? Tdavg { get; set; } // 이슬점온도
        [JsonProperty("siavg")] public double? Siavg { get; set; } // 일사량
        [JsonProperty("ssavg")] public double? Ssavg { get; set; } // 일조시간
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

        [JsonProperty("fw")] public string fw { get; set; }
        //  [JsonConverter(typeof(SafeDoubleConverter))] // 커스텀 컨버터 적용
        public double? Flow { get; set; }
    }
    public class FlowDailyResponse { public List<FlowDailyData> List { get; set; } }

    // 댐 수문 데이터 모델
    public class DamData
    {
        // 공통 시간/날짜 필드
        [JsonProperty("obsdh")] public string Obsdh { get; set; } // 시자료 시간
        [JsonProperty("obsymd")] public string Obsymd { get; set; } // 일자료/월자료 날짜

        // 시자료 필드 (w35)
        [JsonProperty("rwl")] public double? Rwl { get; set; } // 저수위
        [JsonProperty("ospilwl")] public double? Ospilwl { get; set; } // 방수로수위
        [JsonProperty("rsqty")] public double? Rsqty { get; set; } // 저수량
        [JsonProperty("rsrt")] public double? Rsrt { get; set; } // 저수율
        [JsonProperty("iqty")] public double? Iqty { get; set; } // 유입량
        [JsonProperty("etqty")] public double? Etqty { get; set; } // 공용량
        [JsonProperty("tdqty")] public double? Tdqty { get; set; } // 총 방류량
        [JsonProperty("edqty")] public double? Edqty { get; set; } // 발전방류량
        [JsonProperty("spdqty")] public double? Spdqty { get; set; } // 여수로방류량
        [JsonProperty("otltdqty")] public double? Otltdqty { get; set; } // 기타방류량
        [JsonProperty("itqty")] public double? Itqty { get; set; } // 취수량
        [JsonProperty("dambsarf")] public double? Dambsarf { get; set; } // 댐유역평균우량

        // 일자료 필드 (w36) - 시자료와 겹치는 필드는 JsonProperty로 구분
        // rwl, iqty, tdqty, edqty, spdqty, otltdqty는 시자료와 동일한 속성 사용
        [JsonProperty("rf")] public double? Rf { get; set; } // 댐유역평균강우량 (일자료)

        // 월자료 필드 (w37)
        [JsonProperty("mnwl")] public double? Mnwl { get; set; } // 저수위 최저
        [JsonProperty("avwl")] public double? Avwl { get; set; } // 저수위 평균
        [JsonProperty("mxwl")] public double? Mxwl { get; set; } // 저수위 최고
        [JsonProperty("mniqty")] public double? Mniqty { get; set; } // 유입량 최저
        [JsonProperty("aviqty")] public double? Aviqty { get; set; } // 유입량 평균
        [JsonProperty("mxiqty")] public double? Mxiqty { get; set; } // 유입량 최고
        [JsonProperty("mntdqty")] public double? Mntdqty { get; set; } // 총방류량 최저
        [JsonProperty("avtdqty")] public double? Avtdqty { get; set; } // 총방류량 평균
        [JsonProperty("mxtdqty")] public double? Mxtdqty { get; set; } // 총방류량 최고
        [JsonProperty("mnsqty")] public double? Mnsqty { get; set; } // 수문방류량 최저
        [JsonProperty("avsqty")] public double? Avsqty { get; set; } // 수문방류량 평균
        [JsonProperty("mxsqty")] public double? Mxsqty { get; set; } // 수문방류량 최고
        [JsonProperty("mnrf")] public double? Mnrf { get; set; } // 유역평균우량 최저
        [JsonProperty("avrf")] public double? Avrf { get; set; } // 유역평균우량 평균
        [JsonProperty("mxrf")] public double? Mxrf { get; set; } // 유역평균우량 최고
    }
    public class DamResponse { public List<DamData> List { get; set; } }
}
