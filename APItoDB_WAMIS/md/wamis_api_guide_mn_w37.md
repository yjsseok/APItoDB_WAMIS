# WAMIS OpenAPI 호출 가이드 - 댐수문정보 월자료

이 문서는 WAMIS의 '댐수문정보 월자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkd/mn_mndata`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | 문자 | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `damcd` | 문자 | Yes | 댐 코드 | `1001001` |
| `startyear` | 문자 | No | 관측기간-시작년도 (YYYY) | `2023` |
| `endyear` | 문자 | No | 관측기간-종료년도 (YYYY) | `2023` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `obsymd` | 문자 | 관측년월 | |
| `mnwl` | 문자 | 저수위 최저(EL.m) | |
| `avwl` | 문자 | 저수위 평균 | |
| `mxwl` | 문자 | 저수위 최고 | |
| `mniqty` | 문자 | 유입량 최저(m3/s) | |
| `aviqty` | 문자 | 유입량 평균 | |
| `mxiqty` | 문자 | 유입량 최고 | |
| `mntdqty` | 문자 | 총방류량 최저(m3/s) | |
| `avtdqty` | 문자 | 총방류량 평균 | |
| `mxtdqty` | 문자 | 총방류량 최고 | |
| `mnsqty` | 문자 | 수문방류량 최저(m3/s) | |
| `avsqty` | 문자 | 수문방류량 평균 | |
| `mxsqty` | 문자 | 수문방류량 최고 | |
| `mnrf` | 문자 | 유역평균우량 최저(mm) | |
| `avrf` | 문자 | 유역평균우량 평균 | |
| `mxrf` | 문자 | 유역평균우량 최고 | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "obsymd": "202301",
      "mnwl": "180.0",
      "avwl": "185.0",
      "mxwl": "190.0",
      "mniqty": "40.0",
      "aviqty": "50.0",
      "mxiqty": "60.0",
      "mntdqty": "30.0",
      "avtdqty": "40.0",
      "mxtdqty": "50.0",
      "mnsqty": "0.0",
      "avsqty": "5.0",
      "mxsqty": "10.0",
      "mnrf": "0.0",
      "avrf": "10.0",
      "mxrf": "20.0"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkd/mn_mndata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "damcd": "1001001",
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