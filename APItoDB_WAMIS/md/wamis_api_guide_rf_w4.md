
# WAMIS OpenAPI 호출 가이드 - 강수량 일자료

이 문서는 WAMIS의 '강수량 일자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_dtdata`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명                     | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------------------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키          | `YOUR_API_KEY` |
| `obscd`    | String | Yes  | 관측소 코드              | `10011100`    |
| `startdt`  | String | No   | 조회기간 시작일 (YYYYMMDD) | `20240101`    |
| `enddt`    | String | No   | 조회기간 종료일 (YYYYMMDD) | `20240131`    |
| `output`   | String | No   | 출력 포맷                | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type   | 설명               |
| :---- | :----- | :----------------- |
| `ymd` | String | 관측일 (YYYYMMDD)  |
| `rf`  | String | 강수량 (mm)         |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "ymd": "20240101",
      "rf": "0.5"
    },
    {
      "ymd": "20240102",
      "rf": "0.0"
    }
    // ... more results
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_dtdata"

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
