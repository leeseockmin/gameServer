# QA Agent Memory

## 프로젝트 구조 핵심 정보

- 서버: `FrogTailGameServer/` — gRPC HTTP/2 전용, 포트 9001
- Proto 파일 위치: `FrogTailGameServer/Protos/` (common.proto, login.proto, shop.proto)
- QA 보고서 저장 경로: `FrogTailGameServer/.claude/qa-reports/`
- 테스트 클라이언트: `WpfTestClient/` (WPF GUI), `TestClient/` (콘솔)

## 확인된 패턴 및 규칙

### Proto 파일 참조
- TestClient.csproj와 WpfTestClient.csproj 모두 `Include + Link + ProtoRoot` 방식으로 서버 Protos 참조
- auth.proto는 제거됨. 로그인은 login.proto 사용 (LoginService)
- 서버 Protos 목록: common.proto, login.proto, shop.proto

### WPF 체크리스트
- Window.Resources는 반드시 Window 최상단 (`<Grid>` 앞)에 선언
- xmlns:local 은 Window 태그 속성에 선언: `xmlns:local="clr-namespace:WpfTestClient"`
- SynchronizationContext 덕분에 async 완료 후 UI 업데이트 시 Dispatcher.Invoke 불필요

### 검증 이력

| 날짜 | 기능 | 결과 | 비고 |
|------|------|------|------|
| 2026-02-21 | WPF gRPC TestClient + GrpcShopService | APPROVED (재검증 후) | BUG-001~004 수정 완료 |

## 자주 발생하는 버그 유형

1. using 누락: `using System.Net.Http` 없이 SocketsHttpHandler 사용 시 CS0246
2. csproj proto 참조 불일치: 파일명 변경 시 csproj 갱신 누락
3. XAML Resources 위치: Window.Resources가 Grid 아래에 선언되는 컨벤션 위반
