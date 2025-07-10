# 한국농어촌공사 농업용저수지 정보 API 가이드: 에러 코드

본 문서는 한국농어촌공사 농업용저수지 정보 API 사용 시 발생할 수 있는 공통 에러 코드를 안내합니다. 에러는 크게 **공공데이터포털 에러**와 **제공기관 에러**로 나뉩니다.

## 1. 공공데이터포털 에러 코드

공공데이터포털 시스템에서 발생하는 에러로, 주로 인증키, 요청 제한, 서비스 상태 등과 관련이 있습니다.

| 에러코드 | 에러 메시지 | 설명 |
| --- | --- | --- |
| `12` | `NO_OPENAPI_SERVICE_ERROR` | 해당 오픈 API 서비스가 없거나 폐기되었습니다. |
| `20` | `SERVICE_ACCESS_DENIED_ERROR` | 서비스 접근이 거부되었습니다. (예: 활용 신청하지 않은 IP에서 호출) |
| `22` | `LIMITED_NUMBER_OF_SERVICE_REQUESTS_EXCEEDS_ERROR` | 서비스 요청 제한 횟수를 초과했습니다. (일일 트래픽 등) |
| `30` | `SERVICE_KEY_IS_NOT_REGISTERED_ERROR` | 등록되지 않은 서비스키입니다. 인증키를 확인하세요. |
| `31` | `DEADLINE_HAS_EXPIRED_ERROR` | 서비스 활용 기간이 만료되었습니다. |
| `32` | `UNREGISTERED_IP_ERROR` | 등록되지 않은 IP 주소입니다. |
| `99` | `UNKNOWN_ERROR` | 기타 알 수 없는 에러가 발생했습니다. |

### 공공데이터포털 에러 응답 형식

```xml
<OpenAPI_ServiceResponse>
  <cmmMsgHeader>
    <errMsg>SERVICE ERROR</errMsg>
    <returnAuthMsg>SERVICE_KEY_IS_NOT_REGISTERED_ERROR</returnAuthMsg>
    <returnReasonCode>30</returnReasonCode>
  </cmmMsgHeader>
</OpenAPI_ServiceResponse>
```

## 2. 제공기관 에러 코드

API를 제공하는 한국농어촌공사 시스템에서 발생하는 에러로, 주로 요청 파라미터나 데이터 조회 결과와 관련이 있습니다.

| 에러코드 | 에러 메시지 | 설명 |
| --- | --- | --- |
| `10` | `INVALID_REQUEST_PARAMETER_ERROR` | 잘못된 요청 파라미터입니다. 필수 파라미터나 값의 형식을 확인하세요. |
| `13` | `GATEWAY_AUTHENTICATION_FAILED` | 게이트웨이 키 인증에 실패했습니다. |
| `99` | `NO_DATA` | 조회된 데이터가 없습니다. |
