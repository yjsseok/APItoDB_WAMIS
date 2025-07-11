using System.Xml.Serialization;
using System.Collections.Generic;

namespace KRC_Services.Models
{
    [XmlRoot("response")]
    public class KrcReservoirLevelResponse
    {
        [XmlElement("header")]
        public KrcHeader Header { get; set; } // Reusing KrcHeader from KrcReservoirCode.cs

        [XmlElement("body")]
        public KrcReservoirLevelBody Body { get; set; }
    }

    public class KrcReservoirLevelBody
    {
        [XmlElement("item")] // XmlArray 및 XmlArrayItem 대신 XmlElement 사용
        public List<KrcReservoirLevelItem> Items { get; set; }

        [XmlElement("numOfRows")]
        public int NumOfRows { get; set; }

        [XmlElement("pageNo")]
        public int PageNo { get; set; }

        [XmlElement("totalCount")]
        public int TotalCount { get; set; }
    }

    public class KrcReservoirLevelItem
    {
        [XmlElement("fac_code")]
        public string FacCode { get; set; }

        [XmlElement("fac_name")]
        public string FacName { get; set; }

        [XmlElement("county")]
        public string County { get; set; }

        [XmlElement("check_date")]
        public string CheckDate { get; set; } // YYYYMMDD

        [XmlElement("water_level")]
        public string WaterLevel { get; set; } // string to handle potential non-numeric values before parsing

        [XmlElement("rate")]
        public string Rate { get; set; } // string to handle potential non-numeric values before parsing
    }
}
