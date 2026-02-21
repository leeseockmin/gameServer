# WpfTestClient — FrogTail gRPC 테스트 클라이언트

FrogTailGameServer의 gRPC API를 UI에서 직접 테스트할 수 있는 WPF 클라이언트입니다.

---

## 실행 방법

```bash
cd WpfTestClient
dotnet run
```

기본 서버 주소: `http://localhost:9001`

---

## 화면 구성

### 1. 서버 설정

- **서버 주소** 입력 후 **채널 재구성** 버튼으로 gRPC 채널을 새로 연결합니다.

---

### 2. 현재 세션

로그인 성공 후 세션 정보(`LoginType`, `UserId`, `Token`)가 표시됩니다.

---

### 3. 로그인 패널

| 입력 필드 | 설명 |
|-----------|------|
| DeviceId | 기기 고유 ID |
| NickName | 사용자 닉네임 |
| LoginType | `Guest` / `Google` 등 로그인 유형 선택 (ComboBox) |
| AccessToken | 아래 케이스별로 다르게 입력 |

#### LoginType 케이스별 AccessToken 입력 방법

| 케이스 | LoginType | AccessToken |
|--------|-----------|-------------|
| Guest 신규 로그인 | Guest | **비워두세요** — 서버에서 GuestToken 발급 후 자동 채움 |
| Guest 재로그인 | Guest | 신규 로그인 후 자동 채워진 GuestToken 값 그대로 사용 |
| Firebase 로그인 | Google 등 | Firebase JWT AccessToken 입력 |

로그인 버튼: **로그인** 1개

로그인 성공 후 서버에서 GuestToken이 반환되면 AccessToken 필드에 자동으로 채워집니다.
재로그인 시 별도 수정 없이 바로 "로그인" 버튼을 누르면 됩니다.

---

### 4. API 호출 (인증 필요)

| 버튼 | 설명 |
|------|------|
| ShopList 조회 | 로그인 후 사용 가능. x-userid + authorization 헤더 자동 첨부 |
| 로그 초기화 | Request/Response 로그 초기화 |

---

### 5. 패킷 시나리오

여러 RPC를 순서대로 실행하는 자동화 시나리오를 구성할 수 있습니다.

#### 사용 방법

1. **시나리오 구성** 버튼 클릭 → PacketListWindow 팝업 열림
2. 좌측 "사용 가능한 패킷" 목록에서 패킷 선택
3. **추가 ->** 버튼으로 우측 "실행 순서" 목록에 추가
4. 우측 목록에서 **위 / 아래** 버튼으로 순서 조정, **제거** 버튼으로 삭제
5. **확인** 버튼으로 저장 (취소 시 변경 내용 무시)
6. 메인 화면으로 돌아오면 "구성:" 옆에 시나리오 요약이 표시됨
7. **시나리오 실행** 버튼 클릭 → 순서대로 RPC 실행, `[1/3]` 형태로 로그 출력

#### 등록된 패킷 목록

| 패킷 | 인증 필요 | 설명 |
|------|-----------|------|
| LoginService.Login | 아니오 | 로그인 (Guest 신규/재로그인, Firebase) |
| LoginService.VerityLogin | 아니오 | 로그인 토큰 검증 |
| ShopService.ShopList | 예 | 상점 목록 조회 |

인증이 필요한 패킷(RequiresAuth=true)은 세션이 없으면 경고 메시지 출력 후 스킵됩니다.

---

### 6. 로그 뷰어

- **Request Log**: 서버로 보낸 요청 내용
- **Response Log**: 서버에서 받은 응답 내용

---

## 전체 테스트 순서 (권장)

### 단계 1 — Guest 신규 로그인

1. LoginType: `Guest`, AccessToken: 비워둠
2. "로그인" 클릭
3. 확인: `ErrorCode=Success`, `UserId > 0`, `GuestToken` 발급됨
4. AccessToken 필드에 GuestToken 자동 채워짐 확인

### 단계 2 — ShopList 조회 (신규 로그인 세션)

1. "ShopList 조회" 클릭
2. 확인: `ErrorCode=Success`, ShopCount 출력됨

### 단계 3 — Guest 재로그인

1. LoginType: `Guest`, AccessToken: 자동 채워진 GuestToken 그대로
2. "로그인" 클릭
3. 확인: 동일 `UserId` 반환, GuestToken 미발급

### 단계 4 — ShopList 조회 (재로그인 세션)

1. "ShopList 조회" 클릭
2. 확인: `ErrorCode=Success`

### 시나리오 자동 실행 (단계 1~4 통합)

1. "시나리오 구성" → 순서: `Login -> Login -> ShopList` 구성
2. "시나리오 실행" 클릭
3. `[1/3]`, `[2/3]`, `[3/3]` 순서로 실행 로그 확인

---

## 주의 사항

- 서버가 `http://localhost:9001` 에서 HTTP/2로 실행 중이어야 합니다.
- TLS 없이 일반 HTTP/2를 사용합니다 (`EnableMultipleHttp2Connections = true`).
- Firebase 로그인은 유효한 Firebase JWT AccessToken이 필요합니다.
