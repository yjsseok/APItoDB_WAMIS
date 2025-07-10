using System.Xml.Serialization;

namespace WamisWaterLevelDataApi.Models
{
    [XmlRoot("OpenAPI_ServiceResponse")]
    public class KrcOpenApiErrorResponse
    {
        [XmlElement("cmmMsgHeader")]
        public KrcCmmMsgHeader CmmMsgHeader { get; set; }
    }

    public class KrcCmmMsgHeader
    {
        [XmlElement("errMsg")]
        public string ErrMsg { get; set; }

        [XmlElement("returnAuthMsg")]
        public string ReturnAuthMsg { get; set; }

        [XmlElement("returnReasonCode")]
        public string ReturnReasonCode { get; set; }
    }

    // For provider errors (non-OpenAPI portal errors), the structure is similar to normal responses' header.
    // We can reuse KrcHeader or define a specific one if needed.
    // For now, assuming provider errors might also use a similar structure or be identified by returnReasonCode in KrcHeader.
}
