using System;

namespace APItoDB_WAMIS.A_Models
{
    public class ASOS_ErrorResponse
    {
        public string Error { get; set; }
        public string Message { get; set; }
        public int? Code { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}