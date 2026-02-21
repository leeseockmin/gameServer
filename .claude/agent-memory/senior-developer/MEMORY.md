# Senior Developer Memory

## 프로젝트 핵심 구조 (확인 완료)

- **메인 서버**: `FrogTailGameServer/` — gRPC, Kestrel 포트 9001 (HTTP/2)
- **DB 접근 진입점**: `FrogTailGameServer/DB/DataBaseManager.cs` (DB 폴더가 FrogTailGameServer 안에 있음, 루트 DB/ 폴더 아님)
- **EF Core DBContext**: `Data/AccountDB/AccountDBContext.cs`, `Data/GameDB/GameDBContext.cs` (프로젝트명: DB.Data)
- **Dapper 쿼리**: `Data/Logic/AccountDBLogic/`, `Data/Logic/GameDBLogic/`
- **Redis 세션**: `Common/Redis/RedisClient.cs`
- **인증 인터셉터**: `FrogTailGameServer/GrpcServices/AuthInterceptor.cs`
- **Proto 자동 생성**: `generate-proto.bat` -> `PacketToProtoGenerator/` -> `FrogTailGameServer/Protos/`

## 코딩 규칙 (확인 완료)

- `ConfigureAwait(false)` 사용 금지 (ASP.NET Core/gRPC 환경에서 SynchronizationContext 없음)
- DDL에 FK 제약 조건 금지 (애플리케이션에서 무결성 관리)
- `AnonymousMethods`에 없는 gRPC 메서드는 모두 인증 검증 대상

## 패킷 명명 규칙

- 요청: `CG{기능명}ReqPacket`, 응답: `GC{기능명}AnsPacket`
- 폴더명: `{기능명}Packet` -> proto 파일명: `{기능명소문자}.proto`
- `common.proto`는 자동 생성 대상 아님 (수동 관리)

## 마이그레이션 명령어 (PMC 기준)

```powershell
Add-Migration {명} -Context AccountDBContext -Project DB.Data -OutputDir "Migrations\AccountMigrations"
Update-Database -Context AccountDBContext -Project DB.Data -StartupProject FrogTailGameServer
```

## README 작성 완료

`C:\Users\user\Desktop\GameServer\gameServer\README.md` — 2026-02-21 작성
