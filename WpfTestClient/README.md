# WpfTestClient — FrogTail gRPC 테스트 클라이언트

FrogTailGameServer의 gRPC API를 GUI 환경에서 테스트하기 위한 WPF 애플리케이션입니다.
서버를 띄운 뒤 이 클라이언트를 실행하면 로그인, 상점 조회 등의 기능을 바로 테스트할 수 있습니다.

> **왜 Swagger가 아닌가?**
> 서버가 HTTP/2 전용 gRPC 서버라 Swagger(OpenAPI)를 붙일 수 없습니다.
> 자세한 이유는 `FrogTailGameServer/WHY_NO_SWAGGER.md` 를 참고하세요.

---

## 목차

1. [실행 전 필수 확인사항](#1-실행-전-필수-확인사항)
2. [서버 먼저 실행하기](#2-서버-먼저-실행하기)
3. [WPF TestClient 실행하기](#3-wpf-testclient-실행하기)
4. [화면 구성 설명](#4-화면-구성-설명)
5. [테스트 시나리오 따라하기](#5-테스트-시나리오-따라하기)
6. [자주 발생하는 오류 및 해결법](#6-자주-발생하는-오류-및-해결법)
7. [프로젝트 구조 설명](#7-프로젝트-구조-설명)

---

## 1. 실행 전 필수 확인사항

### 필요한 도구

| 도구 | 버전 | 확인 명령어 |
|------|------|-------------|
| .NET SDK | 8.0 이상 | `dotnet --version` |
| Windows OS | Windows 10 이상 | WPF는 Windows 전용입니다 |
| MySQL | 서버 팀에 문의 | 서버 DB 연결 필요 |
| Redis | 서버 팀에 문의 | 서버 세션 저장 필요 |

### .NET 8 SDK 설치 확인

```bash
dotnet --version
# 출력 예: 8.0.xxx
```

8.0이 출력되지 않으면 https://dotnet.microsoft.com/download/dotnet/8.0 에서 설치하세요.

---

## 2. 서버 먼저 실행하기

**WPF 클라이언트를 실행하기 전에 반드시 서버를 먼저 실행해야 합니다.**

### 방법 1 — Visual Studio에서 실행

1. `gameServer.sln` 또는 `FrogTailGameServer.sln` 열기
2. 시작 프로젝트를 `FrogTailGameServer` 로 설정
3. `F5` 또는 `Ctrl+F5` 로 실행
4. 콘솔에 아래 메시지가 나오면 준비 완료:
   ```
   Now listening on: http://localhost:9001
   Application started.
   ```

### 방법 2 — 터미널에서 실행

```bash
cd FrogTailGameServer
dotnet run
```

### 서버 정상 실행 확인

서버가 뜨면 터미널에 아래와 같은 로그가 보입니다:

```
[INF] Now listening on: http://[::]:9001
[INF] Application started. Press Ctrl+C to shut down.
```

포트 `9001`에서 gRPC 요청을 받을 준비가 된 상태입니다.

---

## 3. WPF TestClient 실행하기

### 방법 1 — Visual Studio에서 실행

1. 솔루션 탐색기에서 `WpfTestClient` 프로젝트 우클릭
2. `시작 프로젝트로 설정` 클릭
3. `F5` 로 실행

### 방법 2 — 터미널에서 실행

```bash
cd WpfTestClient
dotnet run
```

### 방법 3 — 빌드 후 exe 직접 실행

```bash
cd WpfTestClient
dotnet build
# 빌드 후 아래 경로의 exe 실행:
# WpfTestClient\bin\Debug\net8.0-windows\WpfTestClient.exe
```

실행하면 아래와 같은 창이 열립니다:

```
┌────────────────────────────────────────────────────────┐
│  서버 설정:  http://localhost:9001    [채널 재구성]       │
│  현재 세션: 세션 없음                                    │
├────────────────────────────────────────────────────────┤
│  로그인                                                  │
│  DeviceId: [wpf-test-device-001]  NickName: [WpfTestUser]│
│  GuestToken: [                              ]           │
│  [Guest 신규 로그인]  [Guest 재로그인]                    │
├────────────────────────────────────────────────────────┤
│  API 호출 (인증 필요)                                    │
│  [ShopList 조회]  [로그 초기화]                          │
├──────────────────────┬─────────────────────────────────┤
│  Request Log         │  Response Log                   │
│                      │                                 │
└──────────────────────┴─────────────────────────────────┘
```

---

## 4. 화면 구성 설명

### 서버 설정 영역

```
서버 주소: [http://localhost:9001]  [채널 재구성]
```

- **서버 주소**: 접속할 gRPC 서버 주소입니다. 로컬 개발 시 `http://localhost:9001` 그대로 사용합니다.
- **채널 재구성**: 서버 주소를 바꾼 후 이 버튼을 눌러야 새 주소로 연결됩니다.

> 서버 주소를 바꿀 때는 주소 입력 후 반드시 `채널 재구성` 버튼을 누르세요.
> 안 누르면 이전 서버로 계속 요청이 갑니다.

### 세션 정보 표시줄

```
현재 세션: UserId=42 | Token=eyJhbGciOi...
```

- 로그인 성공 후 현재 세션(UserId + UserToken 앞부분)이 표시됩니다.
- `세션 없음` 이면 아직 로그인하지 않은 상태입니다.

### 로그인 영역

| 필드 | 설명 | 예시 값 |
|------|------|---------|
| DeviceId | 기기 식별자. 아무 값이나 넣어도 됩니다. | `wpf-test-device-001` |
| NickName | 새 계정 생성 시 사용할 닉네임 | `TestUser` |
| GuestToken | Guest 재로그인 시 사용하는 토큰. **신규 로그인 후 자동으로 채워집니다.** | (자동 입력) |

**버튼 설명:**

| 버튼 | 동작 |
|------|------|
| Guest 신규 로그인 | AccessToken을 빈 값으로 전송 → 서버가 새 GuestToken 발급 |
| Guest 재로그인 | GuestToken 필드 값을 AccessToken으로 전송 → 이전 계정으로 로그인 |

### API 호출 영역

| 버튼 | 필요 조건 | 동작 |
|------|-----------|------|
| ShopList 조회 | 로그인 후 사용 가능 | 상점 목록 조회 (인증 헤더 자동 첨부) |
| 로그 초기화 | 없음 | Request/Response 로그 지우기 |

> `ShopList 조회`는 로그인하지 않으면 "먼저 로그인하세요" 메시지가 뜹니다.

### 로그 뷰어

- **Request Log (왼쪽)**: 내가 서버로 보낸 요청 내용
- **Response Log (오른쪽)**: 서버에서 받은 응답 내용
- 새 로그가 추가되면 자동으로 스크롤됩니다.
- 가운데 구분선을 드래그해서 좌우 넓이를 조정할 수 있습니다.

---

## 5. 테스트 시나리오 따라하기

### 시나리오 1 — Guest 신규 로그인 (처음 접속)

**목표**: 새 Guest 계정을 만들고 GuestToken을 발급받습니다.

1. 서버가 실행 중인지 확인합니다.
2. **DeviceId** 와 **NickName** 을 원하는 값으로 입력합니다.
3. **GuestToken 필드는 비워둡니다** (처음이니까 없어도 됩니다).
4. `Guest 신규 로그인` 버튼을 클릭합니다.
5. Response Log에서 아래 내용을 확인합니다:

```
[HH:mm:ss] [Guest 신규 로그인] 응답
[HH:mm:ss]   ErrorCode:  Success
[HH:mm:ss]   UserId:     42
[HH:mm:ss]   UserToken:  eyJhbGci...
[HH:mm:ss]   GuestToken: 550e8400-e29b-41d4-a716-446655440000   ← 이걸 복사해 두세요!
[HH:mm:ss]   [검증] GuestToken 발급: True, UserId > 0: True
```

- `ErrorCode: Success` → 정상입니다.
- `GuestToken` 값이 `WPF TestClient의 GuestToken 필드`에 자동으로 채워집니다.
- 세션 표시줄에 `UserId=42 | Token=eyJhbGci...` 가 보이면 로그인 완료입니다.

> **GuestToken은 소중히 보관하세요!**
> 이 토큰이 없으면 나중에 같은 계정으로 재접속할 수 없습니다.
> 실제 클라이언트(앱)는 이 토큰을 로컬 저장소에 보관해야 합니다.

---

### 시나리오 2 — Guest 재로그인 (기존 계정으로 재접속)

**목표**: 시나리오 1에서 받은 GuestToken으로 동일한 계정에 다시 로그인합니다.

**전제 조건**: 시나리오 1을 먼저 완료했어야 합니다.

1. **GuestToken 필드**에 시나리오 1에서 받은 GuestToken이 자동으로 채워져 있는지 확인합니다.
   - 없으면 직접 붙여넣기 하세요.
2. `Guest 재로그인` 버튼을 클릭합니다.
3. Response Log에서 아래 내용을 확인합니다:

```
[HH:mm:ss] [Guest 재로그인] 응답
[HH:mm:ss]   ErrorCode:  Success
[HH:mm:ss]   UserId:     42           ← 시나리오 1과 동일한 숫자여야 합니다!
[HH:mm:ss]   UserToken:  eyJhbGci...  ← 새로 발급된 세션 토큰
[HH:mm:ss]   GuestToken:              ← 재로그인 시에는 비어 있어야 정상
[HH:mm:ss]   [검증] GuestToken 미발급: True
```

- `UserId` 가 시나리오 1과 **동일한 숫자**인지 확인하세요. 같으면 정상입니다.
- `GuestToken` 이 비어 있는 것이 정상입니다 (재로그인 시에는 발급 안 함).

---

### 시나리오 3 — ShopList 조회

**목표**: 로그인 후 상점 목록을 조회합니다.

**전제 조건**: 시나리오 1 또는 2를 먼저 완료해서 로그인 상태여야 합니다.

1. 로그인 상태인지 세션 표시줄을 확인합니다 (`UserId=...`).
2. `ShopList 조회` 버튼을 클릭합니다.
3. Response Log에서 아래 내용을 확인합니다:

```
[HH:mm:ss] [GetShopList] 응답
[HH:mm:ss]   ErrorCode: Success
[HH:mm:ss]   ShopCount: 1
[HH:mm:ss]     ShopId=1, ItemCount=1
[HH:mm:ss]       ItemId=1, BuyCount=0
```

- `ErrorCode: Success` 이고 상점 데이터가 보이면 정상입니다.
- 인증 헤더(`x-userid`, `authorization: Bearer ...`)가 자동으로 첨부되어 전송됩니다.

---

### 전체 테스트 순서 요약

```
[서버 실행]
    ↓
[Guest 신규 로그인] → GuestToken 자동 채움
    ↓
[ShopList 조회] → 상점 목록 확인
    ↓
[Guest 재로그인] → 동일 UserId 확인
    ↓
[ShopList 조회] → 재로그인 후에도 정상 동작 확인
```

---

## 6. 자주 발생하는 오류 및 해결법

### 오류: `Unavailable — Connection refused`

```
[gRPC Error] Unavailable — Connection refused
```

**원인**: 서버가 실행되지 않았습니다.

**해결**: `FrogTailGameServer` 프로젝트를 먼저 실행하세요.

---

### 오류: `Unavailable — failed to connect to all addresses`

**원인**: 서버 주소가 잘못되었거나 포트가 다릅니다.

**해결**:
1. 서버 주소가 `http://localhost:9001` 인지 확인합니다.
2. 서버가 `9001` 포트로 실행 중인지 `FrogTailGameServer`의 `appsettings.json`을 확인합니다.
3. 서버 주소를 수정한 경우 `채널 재구성` 버튼을 누르세요.

---

### 오류: `[GetShopList] 먼저 로그인하세요. 세션 없음.`

**원인**: 로그인하지 않고 ShopList를 조회하려 했습니다.

**해결**: `Guest 신규 로그인` 을 먼저 실행하세요.

---

### 오류: `ErrorCode: UnknowError`

**원인**: 서버 내부 오류입니다.

**해결**:
1. 서버 콘솔 로그를 확인합니다 (빨간 오류 메시지 찾기).
2. DB(MySQL) 또는 Redis 연결 상태를 확인합니다.
3. 서버 팀에 문의합니다.

---

### 오류: `ErrorCode: InvalidNickName`

**원인**: NickName 필드가 비어 있습니다.

**해결**: NickName 필드에 값을 입력하세요.

---

### 버튼이 클릭되지 않을 때

**원인**: 이전 요청이 처리 중입니다 (IsBusy 상태).

**해결**: 이전 요청의 응답이 Response Log에 출력될 때까지 기다리세요. 서버 응답이 없으면 서버 상태를 확인합니다.

---

## 7. 프로젝트 구조 설명

```
WpfTestClient/
├── WpfTestClient.csproj          # 프로젝트 설정 (패키지, Proto 참조)
├── App.xaml / App.xaml.cs        # 앱 진입점, 의존성 조립(Composition Root)
├── MainWindow.xaml               # UI 레이아웃 정의 (XAML)
├── MainWindow.xaml.cs            # UI 코드비하인드 (자동 스크롤)
├── InverseBoolConverter.cs       # bool 반전 컨버터 (버튼 비활성화용)
├── ViewModels/
│   └── MainViewModel.cs          # 화면 로직 담당 (MVVM ViewModel)
└── Services/
    └── GrpcClientService.cs      # gRPC 채널 관리, API 호출 담당
```

### 각 파일의 역할

**`App.xaml.cs`**
- 앱이 시작될 때 `GrpcClientService`와 `MainViewModel`을 생성하고 `MainWindow`에 주입합니다.
- 앱 종료 시 gRPC 채널을 정상 종료합니다(`Dispose`).

**`MainViewModel.cs`**
- 화면에 표시되는 데이터(RequestLog, ResponseLog, SessionInfo 등)를 관리합니다.
- 버튼 클릭 시 실행되는 로직(`LoginGuestNewAsync`, `GetShopListAsync` 등)이 여기 있습니다.
- `IsBusy` 플래그로 요청 중에는 버튼이 자동으로 비활성화됩니다.

**`GrpcClientService.cs`**
- 실제 gRPC 서버 통신을 담당합니다.
- 채널(`GrpcChannel`) 생성 및 재생성을 관리합니다.
- 로그인 후 `SetSession()`으로 세션을 저장하고, `BuildAuthHeaders()`로 인증 헤더를 자동 구성합니다.

**`WpfTestClient.csproj`**
- `GrpcServices="Client"` 설정으로 서버의 `.proto` 파일을 **복사 없이** 직접 참조합니다.
- proto 파일이 수정되면 빌드 시 자동으로 클라이언트 코드가 재생성됩니다.

### Proto 파일 참조 구조

```
WpfTestClient/
└── (proto 파일 없음 — 서버 파일을 Link로 참조)

FrogTailGameServer/Protos/    ← 실제 파일 위치
├── common.proto
├── login.proto               ← LoginService.LoginServiceClient 생성
└── shop.proto                ← ShopService.ShopServiceClient 생성
```

> proto 파일을 수정하려면 `FrogTailGameServer/Protos/` 폴더의 파일을 수정하세요.
> WpfTestClient는 자동으로 변경사항을 반영합니다.

---

## 기타

### 콘솔 방식으로 테스트하고 싶다면

GUI 없이 터미널에서 빠르게 테스트하려면 `TestClient` 프로젝트를 사용하세요:

```bash
cd TestClient
dotnet run
```

자동으로 Guest 신규 로그인 → 재로그인 → ShopList 조회 순서로 실행됩니다.

### 문의

- 서버 연결 문제: 서버 담당자에게 문의
- 클라이언트 버그: `feature/wpf-test-client` 브랜치에 이슈 등록
