# Task-002 - 업무 데이터 모델 및 Local Task Repository

## 목적

업무 생성, 상태 변경, 완료 이력 및 재실행 후 복원을 지원하는 로컬 업무 관리 기반을 구현한다.

## 구현 범위

- WorkTask/TaskHistory 및 상태·우선순위 Enum
- ITaskRepository와 JSON 기반 LocalTaskRepository
- 원자적 파일 교체, 오류 로그, 손상 파일 보호
- TaskService 업무 생성/시작/완료/차단/Todo 복귀
- 실제 데이터 기반 TodayView 및 Chat Summary
- 빠른 업무 등록과 카드 상태 Action

## 제외 범위

AI, 자연어 처리, GitHub, 알림, Drag & Drop 및 Cloud Sync는 구현하지 않는다.

## 구현 상세

기본 저장 위치는 `%LOCALAPPDATA%\SlaveSplit`이며 `SLAVESPLIT_DATA_DIR` 환경 변수 또는 생성자 인자로 테스트 저장 위치를 주입할 수 있다. JSON은 임시 파일에 직렬화한 후 교체하며 기존 파일이 손상된 경우 조회는 빈 결과로 안전하게 처리하되 변경 작업은 실패시켜 원본을 덮어쓰지 않는다.

## UI 요구사항

Today 화면에서 실제 상태 Summary, 상태별 카드, 오늘 완료 목록, Title 중심 빠른 등록, 선택 입력과 Start/Block/Done/Reopen Action을 제공한다.

## 테스트 방법

`.tmp/TaskRepositorySmokeTest.cs`는 격리 폴더에 업무를 생성하고 상태를 변경한 뒤 Repository를 새로 생성하여 JSON 복원, History 및 날짜별 완료 조회를 확인한다. `.tmp/WindowSmokeTest.cs`는 App 리소스와 MainWindow 생성을 확인한다.

## 완료 조건

- Debug Rebuild 경고 0, 오류 0
- 생성/시작/완료/차단 및 History 저장
- 재실행 상태 복원과 날짜별 Done 필터 통과
- MainWindow 런타임 생성 성공
