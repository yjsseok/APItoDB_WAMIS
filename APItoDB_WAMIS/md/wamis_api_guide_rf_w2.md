
# WAMIS OpenAPI 호출 가이드 - 강수량 관측소 제원

이 문서는 WAMIS의 '강수량 관측소 제원' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_obsinfo`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명         | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd`    | String | Yes  | 관측소 코드  | `10011100`    |
| `output`   | String | No   | 출력 포맷    | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field       | Type   | 설명                           |
| :---------- | :----- | :----------------------------- |
| `obsnm`     | String | 관측소명(한글)                 |
| `obsnmeng`  | String | 관측소명(영문)                 |
| `obscd`     | String | 관측소코드                     |
| `mngorg`    | String | 관할기관                       |
| `bbsnnm`    | String | 수계                           |
| `sbsncd`    | String | 표준유역코드                   |
| `opendt`    | String | 관측개시일                     |
| `obsknd`    | String | 관측기종                       |
| `addr`      | String | 주소(지번주소)                 |
| `lon`       | String | 경도(GPS좌표계)                |
| `lat`       | String | 위도(GPS좌표계)                |
| `shgt`      | String | 해발고(EL.m)                   |
| `hrdtstart` | String | 자료보유기간-시자료-시작일     |
| `hrdtend`   | String | 자료보유기간-시자료-최종일     |
| `dydtstart` | String | 자료보유기간-일자료-시작일     |
| `dydtend`   | String | 자료보유기간-일자료-종료일     |

### 샘플 응답 (JSON)

```json
{
  "list": [
    {
      "addr": "충청북도 충주시 안림동",
      "bbsnnm": "한강",
      "dydtend": "자료없음",
      "dydtstart": "자료없음",
      "hrdtend": "자료없음",
      "hrdtstart": "자료없음",
      "lat": "36.97",
      "lon": "127.953",
      "mngorg": "기상청",
      "obscd": "10041127",
      "obsknd": "지상",
      "obsnm": "충주",
      "obsnmeng": "127",
      "opendt": "1972-01-01",
      "sbsncd": "100414",
      "shgt": "115.12"
    }
  ]
}
```

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_obsinfo"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "10011100",
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
