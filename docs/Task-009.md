# Task-009 검증 기록

## 구현 범위

- Monday–Sunday `DateRange`와 `WeeklySummary`
- 오늘, 어제, 그제, 이번 주, 지난주, 월/일, 슬래시, ISO 날짜 Parser
- `TaskQuery` 기반 Repository 조회 계층
- 날짜별 Completed 그룹, In Progress, Blocked, Carry Over, Created 통계
- Previous / This / Next Week 및 Custom Date
- Detailed / Compact `WorkReportFormatter`와 Clipboard 복사
- Agent의 이번 주, 지난주, 날짜 지정, 주간 미완료 Intent
- `TaskSource.Local`, nullable ExternalId/ExternalUrl
- Weekly Report Navigation과 Modern Card UI

## 자동 검증

- 2026-08-26 기준 이번 주: 2026-08-24 ~ 2026-08-30
- 완료 4, 진행 1, Blocked 1 및 날짜별 완료 그룹 검증
- 지난주: 2026-08-17 ~ 2026-08-23
- `8월 26일`, `8/26`, `2026-08-26` Parser 검증
- 상세 보고서 Memo/BlockedReason 및 Compact Count 검증
- Agent This Week / Last Week / Date Intent 검증
- Clipboard와 Custom Date 화면 검증
- Local Source/null External ID 기본값과 JSON 재실행 검증
- 1,000건 This Week + Keyword 조회: 1ms 미만(메모리 Repository 기준)
- MainWindow Weekly 화면 런타임 로딩 검증

## 스크린샷

- `screenshots/Task-009/01-weekly-dashboard.png`
- `screenshots/Task-009/02-completed-by-date.png`
- `screenshots/Task-009/03-blocked-inprogress.png`
- `screenshots/Task-009/04-weekly-report.png`
- `screenshots/Task-009/05-custom-date.png`

## 빌드 참고

Target Framework는 .NET Framework 4.7.2로 유지했다. 검증 PC에 4.7.2 Targeting Pack이 없어 VS2017 MSBuild에 설치된 Reference Assemblies 경로를 지정했다.
