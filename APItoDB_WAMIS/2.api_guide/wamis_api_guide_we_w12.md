# WAMIS OpenAPI 호출 가이드 - 기상자료 시자료

이 문서는 WAMIS의 '기상자료 시자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_hrdata`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | String | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd` | 문자 | Yes | 관측소코드 | `10011100` |
| `startdt` | 문자 | No | 관측기간-시작일 (YYYYMMDD) | `20240101` |
| `enddt` | 문자 | No | 관측기간-종료일 (YYYYMMDD) | `20240101` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `ymdh` | 문자 | 관측일시 (YYYYMMDDHH) | |
| `ta` | 문자 | 기온(°C) | |
| `hm` | 문자 | 상대습도(%) | |
| `td` | 문자 | 이슬점온도(°C) | |
| `ps` | 문자 | 해면기압(hPa) | |
| `ws` | 문자 | 풍속(m/s) | |
| `wd` | 문자 | 풍향(방향) | 예시)WSW 서남서풍 |
| `sihr1` | 문자 | 일사량(mj/m2) | |
| `catot` | 문자 | 전운량 | 구름이 덮고 있는 하늘의 비율(0 ~ 10), 맑음(0~5), 구름많음(6~8), 흐림(9~10) |
| `sdtot` | 문자 | 적설량(cm) | |
| `sshr1` | 문자 | 일조량(J) | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "ymdh": "2024010101",
      "ta": "-2.5",
      "hm": "80",
      "td": "-5.0",
      "ps": "1012.5",
      "ws": "2.1",
      "wd": "NW",
      "sihr1": "0.0",
      "catot": "5",
      "sdtot": "0.0",
      "sshr1": "0.0"
    },
    {
      "ymdh": "2024010102",
      "ta": "-2.8",
      "hm": "82",
      "td": "-5.2",
      "ps": "1012.8",
      "ws": "1.8",
      "wd": "NW",
      "sihr1": "0.0",
      "catot": "6",
      "sdtot": "0.0",
      "sshr1": "0.0"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_hrdata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "10011100",
    "startdt": "20240101",
    "enddt": "20240101",
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