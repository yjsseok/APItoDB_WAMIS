# WAMIS OpenAPI 호출 가이드 - 기상자료 일자료

이 문서는 WAMIS의 '기상자료 일자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_dtdata`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | 문자 | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd` | 문자 | Yes | 관측소코드 | `10011100` |
| `startdt` | 문자 | No | 관측기간-시작일 (YYYYMMDD) | `20240101` |
| `enddt` | 문자 | No | 관측기간-종료일 (YYYYMMDD) | `20240131` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `ymd` | 문자 | 관측일 (YYYYMMDD) | |
| `taavg` | 문자 | 평균기온(°C) | |
| `tamin` | 문자 | 최저기온(°C) | |
| `tamax` | 문자 | 최고기온(°C) | |
| `wsavg` | 문자 | 평균풍속(m/s) | |
| `wsmax` | 문자 | 최대풍속(m/s) | |
| `wdmax` | 문자 | 최대풍향(m/s) | |
| `hmavg` | 문자 | 상대습도평균(%) | |
| `hmmin` | 문자 | 상대습도최소(%) | |
| `evs` | 문자 | 증발량소형(mm) | |
| `evl` | 문자 | 증발량대형(mm) | |
| `catotavg` | 문자 | 평균운량 | 구름이 덮고 있는 하늘의 비율(0 ~ 10), 맑음(0~5), 구름많음(6~8), 흐림(9~10) |
| `psavg` | 문자 | 해면기압(hPa) | |
| `psmax` | 문자 | 최대해면기압(hPa) | |
| `psmin` | 문자 | 최저해면기압(hPa) | |
| `sdmax` | 문자 | 최심신적설량(cm) | |
| `tdavg` | 문자 | 이슬점온도(°C) | |
| `siavg` | 문자 | 일사량(cm) | |
| `ssavg` | 문자 | 일조시간(시간) | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "ymd": "20240101",
      "taavg": "5.0",
      "tamin": "0.0",
      "tamax": "10.0",
      "wsavg": "2.5",
      "wsmax": "5.0",
      "wdmax": "NW",
      "hmavg": "70",
      "hmmin": "60",
      "evs": "1.2",
      "evl": "2.0",
      "catotavg": "7",
      "psavg": "1015.0",
      "psmax": "1018.0",
      "psmin": "1012.0",
      "sdmax": "0.0",
      "tdavg": "3.0",
      "siavg": "100",
      "ssavg": "5.0"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_dtdata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "10011100",
    "startdt": "20240101",
    "enddt": "20240131",
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