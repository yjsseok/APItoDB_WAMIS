# WAMIS OpenAPI 호출 가이드 - 댐수문정보 시자료

이 문서는 WAMIS의 '댐수문정보 시자료' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkd/mn_hrdata`

### 요청 변수 (Request Parameters)

| Parameter | Type | 필수 | 설명 | 예시 / 기본값 |
| :-------- | :--- | :--- | :--- | :------------ |
| `serviceKey` | 문자 | Yes | 발급받은 인증키 | `YOUR_API_KEY` |
| `damcd` | 문자 | Yes | 댐 코드 | `1001001` |
| `startdt` | 문자 | No | 관측기간-시작일 (YYYYMMDD) | `20240101` |
| `enddt` | 문자 | No | 관측기간-종료일 (YYYYMMDD) | `20240101` |
| `output` | 문자 | No | 출력 포맷 | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field | Type | 설명 | 비고 |
| :---- | :--- | :--- | :--- |
| `obsdh` | 문자 | 관측일시 | |
| `rwl` | 문자 | 저수위(EL.m) | |
| `ospilwl` | 문자 | 방수로수위(EL.m) | |
| `rsqty` | 문자 | 저수량(Mm3) | |
| `rsrt` | 문자 | 저수율(%) | |
| `iqty` | 문자 | 유입량(m3/s) | |
| `etqty` | 문자 | 공용량(백만m3) | |
| `tdqty` | 문자 | 총 방류량(m3/s) | |
| `edqty` | 문자 | 발전방류량(m3/s) | |
| `spdqty` | 문자 | 여수로방류량(m3/s) | |
| `otltdqty` | 문자 | 기타방류량(m3/s) | |
| `itqty` | 문자 | 취수량(m3/s) | |
| `dambsarf` | 문자 | 댐유역평균우량(mm) | |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "obsdh": "2024010101",
      "rwl": "190.0",
      "ospilwl": "180.0",
      "rsqty": "200.0",
      "rsrt": "80.0",
      "iqty": "50.0",
      "etqty": "10.0",
      "tdqty": "40.0",
      "edqty": "30.0",
      "spdqty": "5.0",
      "otltdqty": "5.0",
      "itqty": "10.0",
      "dambsarf": "10.0"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkd/mn_hrdata"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "damcd": "1001001",
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