# CTO Memory — FrogTailGameServer

## 아키텍처 핵심 사실
- 서버는 **순수 gRPC only** (HTTP/2, port 9001). REST/HTTP1.1 엔드포인트 없음.
- Kestrel이 `HttpProtocols.Http2` 단독으로 바인딩됨 → HTTP/1.1 Swagger UI 접근 불가.
- Proto 파일 위치: `FrogTailGameServer/Protos/` (common.proto, login.proto, shop.proto)
- GrpcServices: `GrpcAuthService` (LoginService), `AuthInterceptor` (세션 검증)
- 인증 방식: gRPC metadata header (`x-userid`, `authorization: Bearer {token}`)

## 주요 기술 결정 기록
- **Swagger 도입 불가 (2026-02-21)**: gRPC 서비스는 OpenAPI 스펙과 호환 안 됨.
  대안: gRPC UI 툴 활용 (grpc-ui, Postman, BloomRPC).
- **WPF 테스트 클라이언트 도입 결정 (2026-02-21)**: 기존 Console TestClient 대체.
  프로젝트명 `WpfTestClient`, 솔루션 루트에 신규 생성. 상세: `decisions/wpf-test-client.md`
- **EF Core 모델 PascalCase 변환 승인 (2026-02-21)**: [Column("컬럼명")] 어노테이션 필수.
  Account.cs 등 EF Core 엔티티에 [Column] 없음 확인 → 빠뜨리면 런타임 오류.
- **PacketToProtoGenerator snake_case 오탐 확인 (2026-02-21)**: ToSnakeCase 함수 이미 구현됨 (Program.cs L252-253, WriteMessage L198). 작업 불필요.

## 현재 TestClient 상태
- 위치: `TestClient/` — Console App (.NET 8)
- 기능: Guest 로그인, 재로그인, ShopList (gRPC 직접 호출)
- 문제: CLI 환경 — 반복 테스트/시나리오 전환 불편

## 팀 구성 참고
- CTO, 시니어 개발자, 개발자 2명, QA, DBA
- 상세 결정사항은 `decisions/` 하위 파일에 기록
