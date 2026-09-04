# Task-001 - 프로젝트 기본 골격 및 Messenger UI Shell

## 목적

SlaveSplit의 WPF/MVVM 기반 구조와 AI Messenger + Personal Work Dashboard 형태의 실행 가능한 UI Shell을 구축한다.

## 구현 범위

- .NET Framework 4.8.1 WPF 솔루션
- MainWindow, MVVM Navigation, ChatView
- Today/Tasks/History/Settings Placeholder
- AI/User Message Bubble, 입력 및 전송
- Dummy Today Summary와 Quick Action 영역
- Dark Theme, 공통 Typography/Button/Card 스타일
- 새 메시지 자동 스크롤

## 제외 범위

AI/GitHub API, 실제 Task 저장 및 상태 처리, 데이터베이스, 자연어 분석, Reminder는 구현하지 않는다.

## 구현 상세

`MainViewModel.CurrentViewModel`과 DataTemplate을 사용해 View를 전환한다. `ChatViewModel`이 메시지 컬렉션과 전송 Command를 소유하며 View 코드비하인드는 자동 스크롤만 처리한다. 색상과 공통 Control Style은 ResourceDictionary로 분리했다.

## UI 요구사항

Dark Theme, 218px 고정 Navigation, 유동 Chat 영역, 최소 창 크기 1000x650, 메시지 구분, Today Summary, Quick Action 및 상시 입력 영역을 제공한다.

## 테스트 방법

1. `SlaveSplit.sln`을 빌드한다.
2. `SlaveSplit.exe`를 실행한다.
3. 각 Navigation 화면을 선택한다.
4. 메시지를 입력하고 Enter 또는 Send 버튼으로 전송한다.
5. 창 크기를 최소 크기 이상에서 변경한다.

## 완료 조건

- Debug Rebuild 경고 0, 오류 0
- 앱 프로세스 응답 및 MainWindow 핸들 생성
- 요구 View, Navigation, Bubble, 입력/전송, Summary, Theme 구현
