using Newtonsoft.Json;
using System;
using System.Globalization; // For NumberStyles

namespace WamisDataCollector.Models // WamisDataCollector.Models 네임스페이스 사용
{
    public class SafeDoubleConverter : JsonConverter<double?>
    {
        public override double? ReadJson(JsonReader reader, Type objectType, double? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                string stringValue = reader.Value?.ToString();
                if (string.IsNullOrEmpty(stringValue) ||
                    stringValue.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("-", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("&nbsp;", StringComparison.OrdinalIgnoreCase)) // 일반적인 웹 공백 문자 추가
                {
                    return null; // 알려진 비숫자 문자열은 null로 처리
                }

                // 쉼표(,)가 포함된 숫자 문자열 처리 (예: "1,234.56")
                if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValueWithComma))
                {
                    return doubleValueWithComma;
                }
                // 일반적인 double.TryParse 시도
                if (double.TryParse(stringValue, out double doubleValue))
                {
                     return doubleValue;
                }
                return null; // 위 모든 경우에 해당하지 않으면 변환 실패로 간주하고 null 반환
            }

            if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                try
                {
                    return Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null; // 변환 중 예외 발생 시 null 반환
                }
            }

            // 예상치 못한 타입이면 null 반환
            // 또는 로깅 후 예외 발생: throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
            Console.WriteLine($"[SafeDoubleConverter] Unexpected token type: {reader.TokenType} for value: {reader.Value}");
            return null;
        }

        public override void WriteJson(JsonWriter writer, double? value, JsonSerializer serializer)
        {
            // 이 컨버터는 주로 읽기용이므로 쓰기 작업은 기본 직렬화에 맡기거나,
            // 특별한 쓰기 로직이 필요 없다면 NotSupportedException을 발생시킬 수 있습니다.
            serializer.Serialize(writer, value);
        }
    }
}
