# QA Agent Memory

## 프로젝트 개요
- FrogTailGameServer: gRPC + Protobuf 기반 게임 서버
- 서버 포트: gRPC HTTP/2 — 9001
- WpfTestClient: WPF 기반 GUI 테스트 클라이언트 (데스크탑)
- TestClient: 콘솔 기반 테스트 클라이언트

## WpfTestClient 구조 (검증 완료: 2026-02-21, f6afb47)
- WpfTestClient/WpfTestClient.csproj
- WpfTestClient/App.xaml, App.xaml.cs
- WpfTestClient/MainWindow.xaml, MainWindow.xaml.cs
- WpfTestClient/InverseBoolConverter.cs
- WpfTestClient/Models/PacketItem.cs
- WpfTestClient/Services/GrpcClientService.cs
- WpfTestClient/ViewModels/MainViewModel.cs
- WpfTestClient/ViewModels/PacketListViewModel.cs
- WpfTestClient/Views/PacketListWindow.xaml (git-tracked)
- WpfTestClient/Views/PacketListWindow.xaml.cs

## PacketListWindow.xaml UI 확정 레이아웃 (d98a47a 기준)
- 가운데 StackPanel: "추가 ->" 버튼만 존재 (Width="76"), "<- 제거" 버튼 없음
- 오른쪽 하단: "위", "아래", "제거" 버튼 모두 Width="50"
- GroupBox Padding="4" (왼쪽, 오른쪽 모두)

## GrpcClientService 메서드 목록
- LoginAsync(loginType, deviceId, nickName, accessToken)
- VerityLoginAsync(loginType, accessToken)
- GetShopListAsync()

## 빌드 검증 패턴
- WpfTestClient 빌드: dotnet build --no-restore (약 3초)
- 경고 0개, 오류 0개 통과 기준
