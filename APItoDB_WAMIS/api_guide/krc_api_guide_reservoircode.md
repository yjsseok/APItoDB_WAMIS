# 한국농어촌공사 농업용저수지 정보 API 가이드: 코드조회

본 문서는 한국농어촌공사에서 제공하는 '농업용저수지 코드조회' API의 명세와 사용법을 안내합니다. 이 API는 저수지 이름이나 위치(시/군)를 이용해 해당 저수지의 코드와 상세 정보를 조회하는 기능을 제공합니다.

## 1. API 기본 정보

- **API 명 (국문):** 농업용저수지 코드조회
- **API 명 (영문):** `reservoircode`
- **엔드포인트 URL:** `http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoircode/`
- **데이터 형식:** XML

## 2. 요청 파라미터 (Request)

| 항목명(영문) | 항목명(국문) | 항목크기 | 구분 | 샘플 데이터 | 설명 |
| --- | --- | --- | --- | --- | --- |
| `serviceKey` | 서비스키 | 100 | **필수** | (공공데이터포털 발급 키) | 공공데이터포털에서 발급받은 인증키 |
| `numOfRows` | 한 페이지 결과 수 | 4 | 선택 | `10` | 한 페이지에 보여줄 결과 개수 (기본값: 10) |
| `pageNo` | 페이지 번호 | 4 | 선택 | `1` | 조회할 페이지 번호 (기본값: 1) |
| `fac_name` | 저수지 이름 | 20 | 선택 | `탑정` | 조회하려는 저수지의 이름 |
| `county` | 저수지 위치 | 30 | 선택 | `충청남도` | 조회하려는 저수지가 위치한 시/군 이름 |

**※ `fac_name` 또는 `county` 중 하나 이상의 파라미터는 반드시 입력해야 합니다.**

## 3. 응답 파라미터 (Response)

| 항목명(영문) | 항목명(국문) | 항목크기 | 구분 | 샘플 데이터 | 설명 |
| --- | --- | --- | --- | --- | --- |
| `returnReasonCode` | 결과코드 | 2 | **필수** | `00` | 요청 처리 결과 (정상: 00) |
| `returnAuthMsg` | 결과메시지 | 50 | **필수** | `NORMAL SERVICE` | 요청 처리 결과 메시지 |
| `totalCount` | 전체 결과 수 | 4 | **필수** | `1` | 조회된 전체 데이터 수 |
| `numOfRows` | 한 페이지 결과 수 | 4 | **필수** | `1` | 한 페이지 결과 수 |
| `pageNo` | 페이지 번호 | 4 | **필수** | `1` | 현재 페이지 번호 |
| `items` | 목록 | - | **필수** | - | 조회된 데이터 목록을 포함하는 컨테이너 |
| `item.fac_code` | 저수지코드 | 10 | **필수** | `4423010045` | 저수지 고유 코드 |
| `item.fac_name` | 저수지이름 | 20 | **필수** | `탑정` | 저수지 이름 |
| `item.county` | 저수지위치 | 30 | **필수** | `충청남도 논산시` | 저수지 소재지 (시/군/구) |

## 4. 요청/응답 예시

### 요청 예시 (Request Example)
```
http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoircode/?county=충청남도&fac_name=탑정&serviceKey=서비스키&numOfRows=1&pageNo=1
```

### 응답 예시 (Response Example)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<response>
  <header>
    <returnReasonCode>00</returnReasonCode>
    <returnAuthMsg>NORMAL SERVICE</returnAuthMsg>
  </header>
  <body>
    <items>
      <item>
        <fac_code>4423010045</fac_code>
        <fac_name>탑정</fac_name>
        <county>충청남도 논산시</county>
      </item>
    </items>
    <numOfRows>1</numOfRows>
    <pageNo>1</pageNo>
    <totalCount>1</totalCount>
  </body>
</response>
```