# WAMIS OpenAPI 호출 가이드 - 유량 측정성과

이 문서는 WAMIS의 '유량 측정성과' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/wkw_flwsrrslst`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | String | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd` | 문자 | Yes | 관측소코드 | `1001602` |
| `startyear` | 문자 | Yes | 관측기간-시작년도 (YYYY) | `2023` |
| `endyear` | 문자 | Yes | 관측기간-종료년도 (YYYY) | `2023` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `obsymd` | 문자 | 측정년월일 | |
| `obssthm` | 문자 | 시작시 분 | |
| `obsedhm` | 문자 | 종료시 분 | |
| `stwl` | 문자 | 수위 시작 | |
| `edwl` | 문자 | 수위 종료 | |
| `avgwl` | 문자 | 수위 평균 | |
| `rivwith` | 문자 | 하폭 | |
| `care` | 문자 | 단면적 | |
| `wspd` | 문자 | 유속 | |
| `flw` | 문자 | 유량 | |
| `obsway` | 문자 | 측정방법 | |
| `docnm` | 문자 | 인용문헌 | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "obsymd": "20230101",
      "obssthm": "0900",
      "obsedhm": "1000",
      "stwl": "1.50",
      "edwl": "1.52",
      "avgwl": "1.51",
      "rivwith": "50.0",
      "care": "100.0",
      "wspd": "1.2",
      "flw": "120.0",
      "obsway": "유속면적법",
      "docnm": "2023년 유량측정보고서"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/wkw_flwsrrslst"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "1001602",
    "startyear": "2023",
    "endyear": "2023",
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