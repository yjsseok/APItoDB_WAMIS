using System.Xml.Serialization;
using System.Collections.Generic;

namespace KRC_Services.Models
{
    [XmlRoot("response")]
    public class KrcReservoirCodeResponse
    {
        [XmlElement("header")]
        public KrcHeader Header { get; set; }

        [XmlElement("body")]
        public KrcReservoirCodeBody Body { get; set; }
    }

    public class KrcHeader
    {
        [XmlElement("returnReasonCode")]
        public string ReturnReasonCode { get; set; }

        [XmlElement("returnAuthMsg")]
        public string ReturnAuthMsg { get; set; }
    }

    public class KrcReservoirCodeBody
    {
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public List<KrcReservoirCodeItem> Items { get; set; }

        [XmlElement("numOfRows")]
        public int NumOfRows { get; set; }

        [XmlElement("pageNo")]
        public int PageNo { get; set; }

        [XmlElement("totalCount")]
        public int TotalCount { get; set; }
    }

    public class KrcReservoirCodeItem
    {
        [XmlElement("fac_code")]
        public string FacCode { get; set; }

        [XmlElement("fac_name")]
        public string FacName { get; set; }

        [XmlElement("county")]
        public string County { get; set; }
    }
}
