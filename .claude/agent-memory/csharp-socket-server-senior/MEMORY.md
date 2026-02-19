# 프로젝트 아키텍처 메모리

## 프로젝트 구조 (2026-02-19 기준)

```
gameServer/
├── FrogTailGameServer/   (ASP.NET Core Web, Port 9000)
├── Share/                 (공유 - 패킷, 에러코드)
├── Common/
├── Data/
└── FrogTailGameServer.sln
```

주의: SocketServer, SocketLib, RoomServer, MatchServer 는 2026-02-19 삭제됨. .sln에서도 제거 완료.
- MatchServer 삭제 시 함께 제거한 파일:
  - MatchServer/ 디렉토리 전체
  - FrogTailGameServer/Controllers/MatchController.cs
  - FrogTailGameServer/Services/MatchService.cs
  - Share/Packet/MatchPacket/ 디렉토리 전체
  - Program.cs에서 MatchService DI 등록 및 AddHttpClient("MatchServer") 코드 제거
  - PacketId.cs에서 Match 관련 enum 항목(200~204) 제거

## 패킷 형식

`[PacketSize(4)] [PacketId(4)] [JSON Body...]`
- HeaderSize = 8 bytes
- JSON 직렬화: Newtonsoft.Json

## 핵심 패킷 클래스 (Share 프로젝트)

- CGLoginReqPacket: DeviceId, NickName, OsType, LoginType, AccessToken (UserId 없음)
- GCLoginAnsPacket: UserToken, UserId
- CGMatchRequestReqPacket: TeamSize

## NuGet 패키지 버전 주의사항

Share.csproj가 Serilog.Sinks.File 7.0.0 + Serilog.Sinks.Console 6.1.1 사용
- 모든 프로젝트에서 동일 버전 사용 필수 (NU1605 에러 방지)
- Serilog.Sinks.File: 7.0.0
- Serilog.Sinks.Console: 6.1.1
- Serilog.AspNetCore: 8.0.0

## 포트 구성

- FrogTailGameServer: 9000 (HTTP/Web)
- MatchServer: 9100 (삭제됨, 2026-02-19)
