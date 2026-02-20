# Swagger를 추가하지 않는 이유

## 결론

현재 FrogTailGameServer는 **gRPC 전용 서버**로, Swagger(OpenAPI) 추가가 구조적으로 불가능합니다.

## 기술적 이유

### 1. HTTP 프로토콜 비호환

- Swagger UI는 HTTP/1.1 기반의 REST API 문서화 도구입니다.
- 현재 서버는 Kestrel이 **HTTP/2 전용**으로 구성되어 있습니다.
- 브라우저는 HTTP/2(h2c)로 직접 gRPC 호출이 불가하므로 Swagger UI에서 테스트할 수 없습니다.

### 2. OpenAPI ↔ Protobuf 매핑 불가

- Swagger는 OpenAPI 스펙(JSON/YAML)으로 API를 정의합니다.
- gRPC는 Protobuf(`.proto`) 파일로 서비스/메시지를 정의합니다.
- 두 스펙은 타입 시스템과 스트리밍 모델이 다르며, 자동 변환 도구가 없습니다.

### 3. grpc-gateway 방식의 한계

REST ↔ gRPC 변환을 위한 grpc-gateway 패턴이 존재하나 현재 팀에 적합하지 않습니다:

- 별도 HTTP/1.1 포트 추가 필요
- 모든 `.proto` 파일에 `google.api.http` 어노테이션 추가 필요 (대규모 변경)
- 팀 규모 대비 유지보수 비용 과다

## 대안: WPF TestClient

`WpfTestClient` 프로젝트를 별도로 제공합니다.

- gRPC 채널을 직접 연결하여 모든 RPC 호출 가능
- Guest 신규 로그인 / 재로그인 / ShopList 조회 테스트 지원
- 실행 방법: `WpfTestClient/WpfTestClient.csproj` 빌드 후 실행 (Windows 전용)

콘솔 방식으로 테스트하려면 `TestClient/Program.cs` 를 실행하세요.
