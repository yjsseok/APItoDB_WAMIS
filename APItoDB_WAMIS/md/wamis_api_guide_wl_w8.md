
# WAMIS OpenAPI 호출 가이드 - 수위 시자료

이 문서는 WAMIS의 '수위 시자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/wl_hrdata`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명                     | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------------------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키          | `YOUR_API_KEY` |
| `obscd`    | String | Yes  | 관측소 코드              | `1001602`     |
| `startdt`  | String | No   | 조회기간 시작일 (YYYYMMDD) | `20240101`    |
| `enddt`    | String | No   | 조회기간 종료일 (YYYYMMDD) | `20240101`    |
| `output`   | String | No   | 출력 포맷                | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field  | Type   | 설명                  |
| :----- | :----- | :-------------------- |
| `ymdh` | String | 관측일시 (YYYYMMDDHH) |
| `wl`   | String | 수위 (EL.m)           |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "ymdh": "2024010101",
      "wl": "1.23"
    },
    {
      "ymdh": "2024010102",
      "wl": "1.25"
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
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/wl_hrdata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "1001602",
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
