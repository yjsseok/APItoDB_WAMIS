# WAMIS OpenAPI 호출 가이드 - 유량 자료 실시간일유량

이 문서는 WAMIS의 '유량 자료 실시간일유량' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/flw_dtdata`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | 문자 | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd` | 문자 | Yes | 관측소코드 | `1001602` |
| `year` | 문자 | No | 관측년도 (YYYY) | `2023` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `ymd` | 문자 | 관측일 (YYYYMMDD) | |
| `fw` | 문자 | 유량(m3/s) | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "ymd": "20230101",
      "fw": "120.5"
    },
    {
      "ymd": "20230102",
      "fw": "118.2"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/flw_dtdata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "1001602",
    "year": "2023",
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