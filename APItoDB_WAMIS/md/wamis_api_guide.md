
# WAMIS OpenAPI 호출 가이드

이 문서는 WAMIS (물관리 정보 시스템)의 OpenAPI를 호출하여 데이터를 요청하고 처리하는 프로그램을 개발하기 위한 기술 명세 가이드입니다.

---

## 공통 정보

- **기본 URL**: `http://www.wamis.go.kr:8080/wamis/openapi`
- **인증**: 대부분의 요청에 인증키가 필요합니다. WAMIS에서 발급받은 키를 `serviceKey` 파라미터에 담아 전송해야 합니다.
- **데이터 포맷**: `output` 파라미터를 사용하여 `json` (기본값) 또는 `xml` 형식을 지정할 수 있습니다.

---

## 1. 강수량 관측소 제원 조회

선택한 강수량 관측소의 상세 제원 정보를 조회합니다.

### 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/rf_obsinfo`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명         | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd`    | String | Yes  | 관측소 코드  | `10041127`    |
| `output`   | String | No   | 출력 포맷    | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field       | Type   | 설명                           |
| :---------- | :----- | :----------------------------- |
| `obsnm`     | String | 관측소명 (한글)                |
| `obsnmeng`  | String | 관측소명 (영문)                |
| `obscd`     | String | 관측소 코드                    |
| `mngorg`    | String | 관할기관                       |
| `bbsnnm`    | String | 수계                           |
| `sbsncd`    | String | 표준유역코드                   |
| `opendt`    | String | 관측개시일 (YYYY-MM-DD)        |
| `obsknd`    | String | 관측기종                       |
| `addr`      | String | 주소 (지번주소)                |
| `lon`       | String | 경도 (GPS 좌표계, 예: 127.953) |
| `lat`       | String | 위도 (GPS 좌표계, 예: 36.97)   |
| `shgt`      | String | 해발고도 (EL.m)                |
| `hrdtstart` | String | 시자료 보유 시작일 (YYYYMMDD)  |
| `hrdtend`   | String | 시자료 보유 최종일 (YYYYMMDD)  |
| `dydtstart` | String | 일자료 보유 시작일 (YYYYMMDD)  |
| `dydtend`   | String | 일자료 보유 최종일 (YYYYMMDD)  |

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
    "obscd": "10041127",           # 조회할 관측소 코드
    "output": "json"               # 응답 형식
}

try:
    # GET 요청 보내기
    response = requests.get(url, params=params, timeout=10)
    response.raise_for_status()  # HTTP 오류가 발생하면 예외를 발생시킴

    # 응답 상태 코드 확인
    print(f"Status Code: {response.status_code}")

    # JSON 데이터 파싱
    data = response.json()

    # 결과 출력
    print("--- API Response Data ---")
    print(json.dumps(data, indent=2, ensure_ascii=False))

    # 특정 데이터 접근 예시
    if data.get('list'):
        observatory_info = data['list'][0]
        print(f"\n--- Parsed Information ---")
        print(f"관측소명: {observatory_info.get('obsnm')}")
        print(f"주소: {observatory_info.get('addr')}")
        print(f"관할기관: {observatory_info.get('mngorg')}")

except requests.exceptions.RequestException as e:
    print(f"An error occurred: {e}")
except json.JSONDecodeError:
    print("Failed to parse JSON response.")
    print(f"Response text: {response.text}")

```

---

## 2. (다음 API 추가 위치)

*여기에 다음 API 명세를 위와 동일한 형식으로 추가할 수 있습니다.*

