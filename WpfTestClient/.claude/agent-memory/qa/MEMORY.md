# QA Agent Memory — FrogTailGameServer

## 프로젝트 기본 정보

- gRPC HTTP/2 포트: 9001 (launchSettings.json 기준)
- 인증 헤더: `x-userid` + `Authorization: Bearer {userToken}`
- 에러 기준: `ErrorCode.Success` (0)
- 빌드 명령: `dotnet build` (각 프로젝트 폴더에서)
- QA 보고서 경로: `C:\Users\user\Desktop\GameServer\gameServer\FrogTailGameServer\.claude\qa-reports\`

## 빌드 검증 주의 사항

- 전체 솔루션 빌드(`dotnet build` 루트) 시 Common, DB.Data 의존 프로젝트 경고 포함됨
- FrogTailGameServer 단독 빌드: `dotnet build FrogTailGameServer\FrogTailGameServer.csproj` 로 확인해야 정확함
- 기존 null 관련 경고(CS8601, CS8603, CS8618 등)는 Common/DB.Data 기존 코드 문제 — 이번 변경과 무관

## 핵심 파일 경로

- launchSettings: `FrogTailGameServer\Properties\launchSettings.json`
- gRPC 서비스: `FrogTailGameServer\GrpcServices\`
- WpfTestClient ViewModel: `WpfTestClient\ViewModels\MainViewModel.cs`
- WpfTestClient gRPC 서비스: `WpfTestClient\Services\GrpcClientService.cs`
- PacketItem 등록: `WpfTestClient\Models\PacketItem.cs` (새 RPC 추가 시 여기에 등록)
- TestClient 시나리오: `TestClient\Program.cs`

## 패킷 시나리오 구조 (feature/unified-login-packet-scenario 이후)

- `PacketItem.All`: LoginService.Login, LoginService.VerityLogin, ShopService.ShopList 3개 등록
- RequiresAuth=true 패킷은 세션 없으면 경고 후 스킵 (경고: ExecutePacketAsync 내부)
- Guest 신규: AccessToken="" -> 서버 GuestToken 발급 -> AccessToken 필드 자동 채움
- Guest 재로그인: AccessToken={GuestToken}

## QA 체크리스트 패턴

- launchSettings: launchBrowser/launchUrl(swagger) 없는지, applicationUrl=0.0.0.0:9001
- ViewModel: Command 단일화, CanExecute 분리 여부, CollectionChanged 이벤트 연결
- XAML: 버튼 수, ComboBox 존재, 레이아웃 열 구조
- code-behind: Enum.GetValues로 ComboBox 설정 (어셈블리 참조 이슈로 XAML ObjectDataProvider 불가)

## 상세 문서

- `patterns.md`: 아직 없음 (필요 시 생성)
