# QA Agent Memory

## 프로젝트 기본 정보
- gRPC 포트: 9001 (HTTP/2, h2c)
- 인증: AuthInterceptor (x-userid + authorization Bearer 토큰)
- AnonymousMethods: /login.LoginService/Login, /login.LoginService/VerityLogin
- DB: AccountDB (accountdb), GameDB (gamedb)
- Redis: localhost:6379

## 핵심 파일 경로
- AuthInterceptor: `FrogTailGameServer/GrpcServices/AuthInterceptor.cs`
- Program.cs: `FrogTailGameServer/Program.cs`
- ErrorCode: `Share/Common/ErrorCode.cs`
- common.proto: `FrogTailGameServer/Protos/common.proto`
- TestClient: `TestClient/Program.cs`

## 검증 패턴 메모

### accountId vs userId 혼용 주의
- `x-userid` 헤더에는 userId(GameDB)가 들어옴
- 계정연동 API는 AccounDB -> AccessToken -> AccountId -> GameDB-> userID 추출

### MockAccountLinkValidator ValidKeys 설정 위치
- `appsettings.Development.json`의 `AccountLinkValidator:ValidKeys` 섹션
- `appsettings.json`에는 `ValidKeys: []` (빈 배열) 로 기본값 포함 — 운영 환경에서 실제 검증기로 교체 전제
- 두 파일 모두 `.gitignore`의 `appsettings*.json` 패턴에 의해 git 추적 제외됨 (확인완료)

### appsettings.json 보안 민감 정보 노출 이슈
- appsettings.json에 Firebase private_key, DB 패스워드, Redis 패스워드가 평문으로 존재
- gitignore로 추적 제외되어 있으나, 이미 이전 커밋에 올라갔을 가능성 별도 확인 필요
- 운영 배포 시 환경변수 또는 Secret Manager 전환 권고 사항으로 기록

### 마지막 연동 해제 방지 로직
- AccountLinkRemove에서 links.Count <= 1 체크 후 ACCOUNT_LINK_LAST_LINK 반환
- DeleteAccountLinkInfo는 linkId + accountId 조건으로 소유자 검증 포함

### 빌드 경고 현황 (기존 누적)
- CS8601, CS8618, CS8603 등 경고 18개 존재 (신규 기능 추가분 아님, 기존 Data/FireBase/Program 레이어)
- 빌드 오류: 0개 (서버 기준)

### WpfTestClient 구조 (20260221 검증 완료)
- `WpfTestClient/WpfTestClient.csproj`: net8.0-windows, UseWPF, CommunityToolkit.Mvvm 8.3.2
- Proto 참조: Link + ProtoRoot 방식으로 서버 Protos 공유 (common/login/shop)
- App.xaml: StartupUri 없음, OnStartup에서 수동 Composition Root
- GrpcClientService: IDisposable, GetOrCreateChannel(), BuildAuthHeaders() 패턴
- MainViewModel: [ObservableProperty] + [RelayCommand(CanExecute)] + IsBusy finally 패턴

### 반복 발생 주의 패턴 (WPF gRPC 클라이언트)
- `SocketsHttpHandler`는 `System.Net.Http` 네임스페이스 — using 누락 시 CS0246 빌드 오류
- csproj에서 proto 참조 변경 시 실제 파일 존재 여부 반드시 확인 (auth.proto -> login.proto 전환 누락 사례)
- Window.Resources는 XAML 최상단 선언이 컨벤션 (하단 선언도 런타임 동작은 정상)

## 검증된 패턴
- ErrorCode enum 값과 proto 숫자 값이 일치해야 gRPC 직렬화 정상 동작
- proto의 서비스명이 AnonymousMethods 경로에 영향 (package.ServiceName/MethodName)
- `DBContextExecuteTransaction` 콜백에서 `return false`시 트랜잭션 롤백 + response null 유지
- WPF에서 ConfigureAwait(false) 미사용 시 await 후 UI 스레드 복귀 — Dispatcher.Invoke 불필요
