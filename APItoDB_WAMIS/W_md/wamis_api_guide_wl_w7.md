
# WAMIS OpenAPI 호출 가이드 - 수위 관측소 제원

이 문서는 WAMIS의 '수위 관측소 제원' API를 호출하기 위한 기술 명세입니다.

---

## 엔드포인트 정보

- **HTTP Method**: `GET`
- **Request URL**: `http://www.wamis.go.kr:8080/wamis/openapi/wkw/wl_obsinfo`

### 요청 변수 (Request Parameters)

| Parameter  | Type   | 필수 | 설명         | 예시 / 기본값 |
| :--------- | :----- | :--- | :----------- | :------------ |
| `serviceKey` | String | Yes  | 발급받은 인증키 | `YOUR_API_KEY` |
| `obscd`    | String | Yes  | 관측소 코드  | `1001602`     |
| `output`   | String | No   | 출력 포맷    | `json` (기본값) |

### 출력 결과 (Response Fields)

| Field        | Type   | 설명                           |
| :----------- | :----- | :----------------------------- |
| `obsnm`      | String | 관측소명(한글)                 |
| `obsnmeng`   | String | 관측소명(영문)                 |
| `wlobscd`    | String | 관측소코드                     |
| `mggvcd`     | String | 관할기관                       |
| `bbsncd`     | String | 수계                           |
| `sbsncd`     | String | 표준유역코드                   |
| `obsopndt`   | String | 관측개시일 (YYYYMMDD)          |
| `obskdcd`    | String | 관측기종                       |
| `rivnm`      | String | 하천명                         |
| `bsnara`     | String | 유역면적(㎢)                   |
| `rvwdt`      | String | 하폭(m)                        |
| `bedslp`     | String | 하상경사(degree)               |
| `rvmjctdis`  | String | 하구(합류점)로부터의 거리(㎞)   |
| `wsrdis`     | String | 수원으로부터의 거리(㎞)        |
| `addr`       | String | 주소(지번주소)                 |
| `lon`        | String | 경도(GPS좌표계)                |
| `lat`        | String | 위도(GPS좌표계)                |
| `tmx`        | String | TM좌표계 x좌표                 |
| `tmy`        | String | TM좌표계 y좌표                 |
| `gdt`        | String | 영점표고(EL.m)                 |
| `wltel`      | String | 수준거표고(EL.m)               |
| `tdeyn`      | String | 조석영향                       |
| `mxgrd`      | String | 최고독수(m)                    |
| `sistartobsdh` | String | 시작시자료 (YYYYMMDDHH)        |
| `siendobsdh` | String | 마지막시자료 (YYYYMMDDHH)      |
| `olstartobsdh` | String | 시작일자료 (YYYYMMDD)          |
| `olendobsdh` | String | 마지막일자료 (YYYYMMDD)        |

### 호출 예제 코드 (Python)

```python
import requests
import json

# API 엔드포인트 URL
url = "http://www.wamis.go.kr:8080/wamis/openapi/wkw/wl_obsinfo"

# 요청 변수 설정
params = {
    "serviceKey": "YOUR_API_KEY",  # 여기에 실제 발급받은 인증키를 입력하세요.
    "obscd": "1001602",
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
