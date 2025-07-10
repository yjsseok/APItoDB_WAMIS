# 한국농어촌공사 농업용저수지 정보 API 가이드: 수위조회

본 문서는 한국농어촌공사에서 제공하는 '농업용저수지 수위조회' API의 명세와 사용법을 안내합니다. 이 API는 특정 저수지의 코드를 이용해 지정된 기간 동안의 수위, 저수율 등 상세한 관측 정보를 조회하는 기능을 제공합니다.

## 1. API 기본 정보

- **API 명 (국문):** 농업용저수지 수위조회
- **API 명 (영문):** `reservoirlevel`
- **엔드포인트 URL:** `http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoirlevel/`
- **데이터 형식:** XML

## 2. 요청 파라미터 (Request)

| 항목명(영문) | 항목명(국문) | 항목크기 | 구분 | 샘플 데이터 | 설명 |
| --- | --- | --- | --- | --- | --- |
| `serviceKey` | 서비스키 | 100 | **필수** | (공공데이터포털 발급 키) | 공공데이터포털에서 발급받은 인증키 |
| `numOfRows` | 한 페이지 결과 수 | 4 | 선택 | `30` | 한 페이지에 보여줄 결과 개수 (기본값: 30) |
| `pageNo` | 페이지 번호 | 4 | 선택 | `1` | 조회할 페이지 번호 (기본값: 1) |
| `fac_code` | 저수지 코드 | 10 | **필수** | `4423010045` | 조회하려는 저수지의 고유 코드 |
| `date_s` | 조회시작날짜 | 8 | **필수** | `20150901` | 조회 기간의 시작일 (YYYYMMDD 형식) |
| `date_e` | 조회끝날짜 | 8 | **필수** | `20150931` | 조회 기간의 종료일 (YYYYMMDD 형식) |
| `county` | 저수지 위치 | 30 | 선택 | `충청남도` | 조회하려는 저수지가 위치한 시/군 이름 |

**※ `fac_code` 또는 `county` 중 하나 이상의 파라미터는 반드시 입력해야 합니다.**
**※ 최대 조회 기간:**
- 저수지별 조회: 최대 365일
- 시도/시군별 조회: 최대 31일

## 3. 응답 파라미터 (Response)

| 항목명(영문) | 항목명(국문) | 항목크기 | 구분 | 샘플 데이터 | 설명 |
| --- | --- | --- | --- | --- | --- |
| `returnReasonCode` | 결과코드 | 2 | **필수** | `00` | 요청 처리 결과 (정상: 00) |
| `returnAuthMsg` | 결과메시지 | 50 | **필수** | `NORMAL SERVICE` | 요청 처리 결과 메시지 |
| `totalCount` | 전체 결과 수 | 4 | **필수** | `30` | 조회된 전체 데이터 수 |
| `numOfRows` | 한 페이지 결과 수 | 4 | **필수** | `30` | 한 페이지 결과 수 |
| `pageNo` | 페이지 번호 | 4 | **필수** | `1` | 현재 페이지 번호 |
| `items` | 목록 | - | **필수** | - | 조회된 데이터 목록을 포함하는 컨테이너 |
| `item.fac_code` | 저수지코드 | 10 | **필수** | `4423010045` | 저수지 고유 코드 |
| `item.fac_name` | 저수지이름 | 20 | **필수** | `탑정` | 저수지 이름 |
| `item.county` | 저수지위치 | 30 | **필수** | `충청남도 논산시` | 저수지 소재지 (시/군/구) |
| `item.check_date` | 측정날짜 | 8 | **필수** | `20150901` | 데이터 측정 날짜 (YYYYMMDD) |
| `item.water_level` | 저수지 수위 | 10 | **필수** | `25.32` | 저수지 수위 (단위: m) |
| `item.rate` | 저수율 | 10 | **필수** | `35.3` | 저수율 (단위: %) |

## 4. 요청/응답 예시

### 요청 예시 (Request Example)
```
http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoirlevel/?fac_code=4423010045&date_s=20150901&date_e=20150931&serviceKey=서비스키&numOfRows=30&pageNo=1&county=충청남도
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
        <check_date>20150901</check_date>
        <water_level>25.32</water_level>
        <rate>35.3</rate>
      </item>
      <!-- ... 추가적인 item ... -->
    </items>
    <numOfRows>30</numOfRows>
    <pageNo>1</pageNo>
    <totalCount>30</totalCount>
  </body>
</response>
```
