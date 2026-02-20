## QA 결과: WPF gRPC TestClient + GrpcShopService (재검증)

- 최초 검증 일시: 2026-02-21
- 재검증 일시: 2026-02-21
- 검증자: QA 담당자
- 대상 브랜치: feature/wpf-test-client
- 재검증 사유: BUG-001~004 수정 후 최종 승인 판정

---

### 검증 범위

#### 최초 검증 (2026-02-21)
- 기능 테스트: WpfTestClient 구조, GrpcShopService, Program.cs DI, TestClient proto 참조 등 31개 시나리오
- 예외 테스트: 미포함 (구조 검증 단계)
- 회귀 테스트: GrpcAuthService 유지 확인 포함

#### 재검증 (2026-02-21)
- BUG-001 수정 확인: `WpfTestClient/Services/GrpcClientService.cs` — `using System.Net.Http;` 1번째 줄 추가 여부
- BUG-002 수정 확인: `TestClient/TestClient.csproj` — `auth.proto` → `login.proto` 변경 여부, Link 속성 일치 여부
- BUG-003 수정 확인: `WpfTestClient/MainWindow.xaml` — `Window.Resources` 위치, `xmlns:local` 선언, 중복 블록 제거 여부
- BUG-004 수정 확인: `FrogTailGameServer/WHY_NO_SWAGGER.md` — 파일 존재 및 3가지 핵심 내용 포함 여부

---

### 결과 요약 (재검증)

- 통과: 4개
- 실패: 0개
- 미검증: 0개

---

### 체크리스트 상세 결과 (최초 검증 — 2026-02-21)

#### 1. WpfTestClient.csproj
- [x] TargetFramework: net8.0-windows
- [x] UseWPF: true
- [x] Nullable: enable
- [x] Grpc.Net.Client, Google.Protobuf, Grpc.Tools, CommunityToolkit.Mvvm 패키지 포함
- [x] Proto 파일이 Link + ProtoRoot 방식으로 서버 Protos 참조
- [x] common.proto, login.proto, shop.proto 모두 포함

#### 2. App.xaml / App.xaml.cs
- [x] App.xaml에 StartupUri 없음 (OnStartup에서 수동 생성)
- [x] OnStartup: GrpcClientService -> MainViewModel -> MainWindow 순서 의존성 주입
- [x] OnExit: _grpcService?.Dispose() 호출 확인

#### 3. MainWindow.xaml
- [FAIL → FIXED] Window.Resources가 Window 최하단(172-177번 줄)에 선언됨 — 컨벤션 위반 (Minor)
- [x] InverseBoolConverter가 Window.Resources에 정의됨
- [x] 버튼(채널 재구성, ServerAddress TextBox)의 IsEnabled가 IsBusy + InverseBoolConverter로 바인딩
- [x] RequestTextBox_TextChanged / ResponseTextBox_TextChanged 이벤트 핸들러로 ScrollToEnd 연결
- [x] GuestToken TextBox 존재, ViewModel GuestToken에 바인딩
- [x] Request Log / Response Log 두 영역으로 분리

#### 4. MainViewModel.cs
- [x] [ObservableProperty]: IsBusy, RequestLog, ResponseLog, GuestToken, SessionInfo, ServerAddress, DeviceId, NickName 모두 선언됨
- [x] [NotifyCanExecuteChangedFor]: LoginGuestNewCommand, LoginGuestReLoginCommand, GetShopListCommand 세 Command 모두 적용
- [x] LoginGuestNewAsync / LoginGuestReLoginAsync / GetShopListAsync 모두 async Task + [RelayCommand]
- [x] try-catch-finally에서 IsBusy = false가 finally에 위치
- [x] LoginGuestNewAsync 성공 후 GuestToken = response.GuestToken으로 자동 채움
- [x] GetShopListAsync 시작 시 _grpcService.IsAuthenticated 체크 후 미로그인이면 얼리 리턴

#### 5. GrpcClientService.cs
- [FAIL → FIXED] using System.Net.Http 누락으로 SocketsHttpHandler 빌드 오류 발생 (Major)
- [x] IDisposable 구현 및 _channel?.Dispose() 호출
- [x] GetOrCreateChannel()로 채널 지연 생성 (null 체크 포함)
- [x] BuildAuthHeaders()에서 x-userid, authorization: Bearer {token} 형식 정확
- [x] IsAuthenticated: _userId와 _userToken 둘 다 체크
- [x] Guest 신규/재로그인 메서드 분리 (LoginGuestNewAsync / LoginGuestReLoginAsync)

#### 6. GrpcShopService.cs
- [x] ShopService.ShopServiceBase 상속
- [x] GameTableManager DI 생성자 주입
- [x] ShopList 메서드 override, Task<ShopListResponse> 반환
- [x] 예외 catch 후 ErrorCode.UnknowError 반환
- [x] itemTable.GetItemTable(1) null 체크 후 조건부 Add

#### 7. Program.cs
- [x] builder.Services.AddSingleton<GameTableManager>() 등록 확인 (50번 줄)
- [x] app.MapGrpcService<GrpcShopService>() 등록 확인 (105번 줄)
- [x] app.MapGrpcService<GrpcAuthService>() 회귀 확인 (104번 줄) — 기존 인증 서비스 유지됨

#### 8. TestClient/Program.cs
- [x] LoginService.LoginServiceClient 사용 (AuthService 아님)
- [x] VerityLoginAsync 사용 (오타 원본 유지)
- [x] ShopListAsync / ShopListRequest 사용
- [x] Guest 신규 로그인 (빈 AccessToken) 시나리오 포함
- [x] Guest 재로그인 (GuestToken 사용) 시나리오 포함
- [FAIL → FIXED] TestClient.csproj가 auth.proto를 참조하는데 해당 파일이 존재하지 않음 (Major)

#### 9. WHY_NO_SWAGGER.md
- [FAIL → FIXED] 파일이 존재하지 않음 (Minor)

---

### BUG별 재검증 상세 (2026-02-21)

#### BUG-001 — using System.Net.Http; 누락 (Major) [FIXED]

- 확인 파일: `WpfTestClient/Services/GrpcClientService.cs`
- 검증 결과: 1번째 줄에 `using System.Net.Http;` 정상 추가 확인
- 판정: PASS

#### BUG-002 — auth.proto 참조 오류 (Major) [FIXED]

- 확인 파일: `TestClient/TestClient.csproj`
- 검증 결과:
  - `Include="..\FrogTailGameServer\Protos\login.proto"` 로 변경 확인
  - `Link="Protos\login.proto"` 로 변경 확인
  - `auth.proto` 참조 잔존 없음 확인
- 판정: PASS

#### BUG-003 — Window.Resources 위치 오류 및 xmlns:local 미선언 (Minor) [FIXED]

- 확인 파일: `WpfTestClient/MainWindow.xaml`
- 검증 결과:
  - `xmlns:local="clr-namespace:WpfTestClient"` 가 `<Window>` 태그(4번째 줄)에 정상 선언 확인
  - `<Window.Resources>` 블록이 9~13번째 줄, `<Grid Margin="8">` (15번째 줄) 앞에 위치 확인
  - `BooleanToVisibilityConverter`, `InverseBoolConverter` 정상 등록 확인
  - 하단 중복 `Window.Resources` 블록 제거 확인 (파일 178번 줄까지 스캔, 중복 없음)
- 판정: PASS

#### BUG-004 — Swagger 미지원 사유 문서 부재 (Minor) [FIXED]

- 확인 파일: `FrogTailGameServer/WHY_NO_SWAGGER.md`
- 검증 결과:
  - 파일 존재 확인
  - HTTP/2 비호환 (Swagger UI는 HTTP/1.1 기반, Kestrel HTTP/2 전용) 내용 포함 확인
  - OpenAPI ↔ Protobuf 매핑 불가 내용 포함 확인
  - 대안으로 WPF TestClient 및 TestClient(콘솔) 안내 포함 확인
- 판정: PASS

---

### 발견된 버그

| ID | 심각도 | 내용 | 담당자 | 상태 |
|----|--------|------|--------|------|
| BUG-001 | Major | GrpcClientService.cs에 `using System.Net.Http` 누락으로 빌드 실패 | 개발자 B | FIXED |
| BUG-002 | Major | TestClient.csproj가 존재하지 않는 `auth.proto`를 참조하여 빌드 실패 | 개발자 B | FIXED |
| BUG-003 | Minor | `Window.Resources`가 XAML 최하단에 선언되어 컨벤션 위반 (`xmlns:local` 미선언 포함) | 개발자 B | FIXED |
| BUG-004 | Minor | `WHY_NO_SWAGGER.md` 문서 파일 미생성 | 개발자 B | FIXED |

신규 발견된 버그 없음

---

### 빌드 결과

- WpfTestClient: 성공 (경고 0, 오류 0)
- FrogTailGameServer: 성공 (경고 18, 오류 0) — 경고 18개는 기존 누적분, 신규 없음
- TestClient: 성공 (경고 0, 오류 0)

---

### 배포 승인 여부

APPROVED

BUG-001~004 전 항목 수정 확인 완료. 신규 Critical/Major 버그 없음. 전 프로젝트 빌드 오류 0개. 배포를 승인합니다.
