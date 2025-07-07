# WAMIS OpenAPI 호출 가이드 - 기상자료 관측소 제원

이 문서는 WAMIS의 '기상자료 관측소 제원' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_obsinfo`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | String | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd` | 문자 | Yes | 관측소코드 | `10011100` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `wtobscd` | 문자 | 기상관측소코드 | |
| `obsnm` | 문자 | 관측소명 | |
| `sbsncd` | 문자 | 표준유역코드 | |
| `clsyn` | 문자 | 운영상태 | |
| `obskdcd` | 문자 | 관측기종 | |
| `mggvcd` | 문자 | 관할기관 | |
| `opndt` | 문자 | 관측개시일자 | 년월일 8자리 숫자(YYYY-MM-DD) |
| `lat` | 문자 | 위도(GPS좌표계) | 예시)37.751 |
| `lon` | 문자 | 경도(GPS좌표계) | 예시)128.891 |
| `tmx` | 문자 | TM좌표계 x좌표 | 예시)366659 |
| `tmy` | 문자 | TM좌표계 y좌표 | 예시)474047 |
| `addr` | 문자 | 주소(지번주소) | |
| `bbsncd` | 문자 | 수계 | |
| `obselm` | 문자 | 해발표고(EL.m) | |
| `thrmlhi` | 문자 | 온도계 지상높이(m) | |
| `prselm` | 문자 | 기압계 해발표고(EL.m) | |
| `wvmlhi` | 문자 | 풍속계 지상높이(m) | |
| `hytmlhi` | 문자 | 우량계 지상높이(m) | |
| `nj` | 문자 | 노장(m) | 가로x세로 |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "wtobscd": "10011100",
      "obsnm": "강화",
      "sbsncd": "100111",
      "clsyn": "운영중",
      "obskdcd": "AWS",
      "mggvcd": "환경부",
      "opndt": "2000-01-01",
      "lat": "37.751",
      "lon": "128.891",
      "tmx": "366659",
      "tmy": "474047",
      "addr": "인천광역시 강화군 강화읍",
      "bbsncd": "한강",
      "obselm": "10.0",
      "thrmlhi": "1.5",
      "prselm": "100.0",
      "wvmlhi": "10.0",
      "hytmlhi": "0.5",
      "nj": "20x20"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_obsinfo"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "10011100",
    "output": "json"
}

try:
    # GET 요청 보내기
    response = requests.get(url, params=params, timeout=10)
    response.raise_for_status()

    # 응답 데이터 출력
    print("--- API Response ---")
    print(json.dumps(response.json(), indent=2, ensure_ascii=False))

except requests.exceptions.RequestException as e:
    print(f"An error occurred: {e}")
except json.JSONDecodeError:
    print("Failed to parse JSON response.")

```

---