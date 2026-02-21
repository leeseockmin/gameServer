# 결정: WPF 테스트 클라이언트 도입 (2026-02-21)

## 결정 배경
- 기존 Console TestClient는 반복 테스트, 시나리오 전환, 헤더 조작이 불편
- QA 팀이 CLI 없이 시나리오 검증 필요
- gRPC-only 서버이므로 Swagger 대안 필요

## 확정 프로젝트 구조
- 프로젝트명: `WpfTestClient`
- 위치: `C:\Users\user\Desktop\GameServer\gameServer\WpfTestClient\`
- Target Framework: net8.0-windows
- 기존 TestClient는 유지 (CI/자동화용)

## 포함할 기능
1. 서버 주소 설정 (IP:Port 입력)
2. 로그인 패널: Guest 신규 / Guest 재로그인 / Firebase 토큰 로그인
3. 인증 헤더 자동 관리 (로그인 후 userId, userToken 자동 세팅)
4. API 호출 패널: 등록된 gRPC RPC 목록에서 선택 후 실행
5. 요청/응답 JSON 뷰어 (Protobuf → JSON 변환 표시)
6. 결과 로그 뷰 (타임스탬프, ErrorCode, 상세)

## 제외할 기능 (1차 범위 밖)
- 부하 테스트 (별도 도구 사용)
- 자동화 스크립트 실행
- 실시간 스트리밍 RPC (현재 서버에 없음)
