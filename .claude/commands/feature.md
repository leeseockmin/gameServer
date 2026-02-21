아래 기능 개발 요청에 대해 CTO → 시니어 개발자 → 개발자(A 또는 B) → DBA 협업 → QA 순서로 전체 워크플로우를 진행해줘.

요청 기능: $ARGUMENTS

---

## 진행 순서

### Step 1. CTO — 기술 타당성 검토
`cto` 에이전트를 호출해서 아래 내용을 결정해줘:
- 요청 기능이 현재 기술 스택(gRPC, MySQL, Redis)으로 구현 가능한지 타당성 검토
- 구현 방향 (어떤 기술/패턴을 쓸지) 결정
- 시니어 개발자에게 전달할 기술 지시사항 도출

### Step 2. 시니어 개발자 — 설계
CTO 결정 내용을 바탕으로 `senior-developer` 에이전트를 호출해서:
- gRPC 서비스 구조 설계 (Proto 메시지, RPC 메서드명)
- 필요한 클래스/계층 구조 설계
- DB가 필요한 경우 필요한 테이블/컬럼 목록 정의
- 개발자 A 또는 B 중 누가 담당할지 결정 (현재 작업 중인 브랜치 없으면 A 우선)

### Step 3. 개발자 — 구현
시니어 설계를 바탕으로 `developer-a` 또는 `developer-b` 에이전트를 호출해서:
- feature/{기능명} 브랜치 생성
- 패킷 클래스 작성 → generate-proto.bat 실행
- gRPC 서비스 구현
- DB 작업이 필요한 경우 → Step 3-1 진행

#### Step 3-1. DBA 협업 (DB 작업이 있는 경우에만)
`dba` 에이전트를 호출해서:
- 개발자가 정의한 엔티티 클래스 컬럼을 전달
- DDL (CREATE/ALTER/DROP TABLE) 수령 → 마이그레이션방법.txt 기준으로 마이그레이션 실행
- INSERT/UPDATE/DELETE/SELECT 쿼리 수령 → Data/Logic/ 에 Dapper 코드로 작성

마이그레이션방법.txt 기준 실행 명령:
```powershell
Add-Migration {마이그레이션명} -Context {AccountDBContext|GameDBContext} -Project DB.Data -OutputDir "Migrations\{Account|Game}Migrations"
Script-Migration -From 0 -To {마이그레이션명}   # 확인용
Update-Database -Context {Context} -Project DB.Data -StartupProject FrogTailGameServer
```

- 단위 테스트 작성 (현재 브랜치에서, 별도 브랜치 생성 안 함)
- 커밋 & PR 생성 (Conventional Commits 형식)

### Step 4. QA — 검증
`qa` 에이전트를 호출해서:
- 기능 테스트 시나리오 작성 (Happy Path + Negative + 경계값)
- 인증/보안 시나리오 포함
- 배포 전 체크리스트 점검
- QA 결과 보고서 작성 (승인 / 미승인)

### 필수 참조.
각각의 agent는 skills 하위에있는 {agent이름}.md 를 확인하고, skills 에 맞게 진행

각 단계가 완료된 후 결과를 요약해서 다음 단계로 넘겨줘.
QA가 미승인 판정을 내리면 해당 이슈를 개발자에게 피드백하고 재작업 후 QA를 다시 진행해줘.

