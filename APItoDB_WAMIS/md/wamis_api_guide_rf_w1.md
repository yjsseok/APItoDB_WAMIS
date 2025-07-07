tnstjeofh
# WAMIS OpenAPI 호출 가이드 - 강수량 관측소 검색

이 문서는 WAMIS의 '강수량 관측소 검색' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_dubrfobs`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명         | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키 | `YOUR_API_KEY` |
| `basin`    | String | No   | 유역코드     | `10` (한강)   |
| `oper`     | String | No   | 운영여부     | `y` (운영중)  |
| `mngorg`   | String | No   | 관할기관     | `1` (환경부)  |
| `obsknd`   | String | No   | 관측방법     | `2` (T/M)     |
| `keynm`    | String | No   | 관측소명     |               |
| `sort`     | String | No   | 정렬방법     | `1` (관측소코드) |
| `output`   | String | No   | 출력 포맷    | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field    | Type   | 설명           |
| :------- | :----- | :------------- |
| `bbsnnm` | String | 대권역명       |
| `obscd`  | String | 관측소 코드    |
| `obsnm`  | String | 관측소명       |
| `clsyn`  | String | 운영상태       |
| `obsknd` | String | 관측방법       |
| `sbsncd` | String | 표준유역코드   |
| `mngorg` | String | 관할기관       |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "bbsnnm": "한강",
      "obscd": "10011100",
      "obsnm": "강화",
      "clsyn": "운영중",
      "obsknd": "T/M",
      "sbsncd": "100111",
      "mngorg": "환경부"
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
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_dubrfobs"

# 요청 변수 설정 (한강 유역의 운영중인 관측소 검색)
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "basin": "10",
    "oper": "y",
    "output": "json"
}

try:
    # GET 요청 보내기
    response = requests.get(url, params=params, timeout=10)
    response.raise_for_status()  # HTTP 오류가 발생하면 예외를 발생시킴

    # 응답 데이터 출력
    print("--- API Response ---")
    print(json.dumps(response.json(), indent=2, ensure_ascii=False))

except requests.exceptions.RequestException as e:
    print(f"An error occurred: {e}")
except json.JSONDecodeError:
    print("Failed to parse JSON response.")

```

---
