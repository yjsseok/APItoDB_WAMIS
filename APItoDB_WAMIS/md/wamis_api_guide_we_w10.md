
# WAMIS OpenAPI 호출 가이드 - 기상자료 관측소 검색

이 문서는 WAMIS의 '기상자료 관측소 검색' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_dwtwtobs`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명         | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키 | `YOUR_API_KEY` |
| `basin`    | String | No   | 유역코드     | `1` (한강)    |
| `oper`     | String | No   | 운영상태     | `y` (운영중)  |
| `keynm`    | String | No   | 관측소명     |               |
| `sort`     | String | No   | 정렬방법     | `1` (관측소코드) |
| `output`   | String | No   | 출력 포맷    | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field    | Type   | 설명           |
| :------- | :----- | :------------- |
| `bbsnnm` | String | 대권역명       |
| `obscd`  | String | 관측소 코드    |
| `obsnm`  | String | 관측소명       |
| `clsyn`  | String | 운영여부       |
| `obsknd` | String | 관측방법       |
| `sbsncd` | String | 표준유역코드   |
| `mngorg` | String | 관할기관       |
| `addr`   | String | 주소(지번주소) |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "bbsnnm": "한강",
      "obscd": "10041127",
      "obsnm": "충주",
      "clsyn": "운영중",
      "obsknd": "지상",
      "sbsncd": "100414",
      "mngorg": "기상청",
      "addr": "충청북도 충주시 안림동"
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
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/we_dwtwtobs"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "basin": "1",
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
