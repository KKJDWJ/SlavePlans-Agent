# Task-008 검증 기록

## 구현 범위

- 시간대 Greeting과 테스트 가능한 `IClock` 기반 현재 날짜
- Todo / In Progress / Blocked / Done Today Summary
- NOW, NEXT, CARRY OVER, UPCOMING, BACKLOG, BLOCKED, DONE TODAY
- Quick Add와 Start / Complete / Block / Resume
- Yesterday 완료 건수와 Daily Summary 진입
- Repository 데이터를 실시간 집계하는 `DailySummaryService`
- Memo를 포함한 업무보고용 Text Work Log와 Clipboard 복사
- Agent의 오늘/어제 정리 명령과 동일 Summary Service 공유
- TaskService 변경 Event 기반 Dashboard 즉시 Refresh
- Dashboard 재진입/Refresh 시 날짜 변경 감지
- Today/Chat 시작 화면 설정 저장
- Ctrl+N, Ctrl+L, Esc 단축키

## 자동 검증 결과

1. 초기 Dashboard: Done 3, Blocked 1, Todo 1
2. Todo Start 후 NOW 이동
3. Complete 후 DONE TODAY 이동과 Count 증가
4. Blocked Resume 후 NOW 이동
5. Daily Work Log에 완료 업무와 Memo 포함
6. Agent와 UI가 동일 DailySummaryService 결과 사용
7. 다음 날 Done Today 0, Yesterday 4, 미완료 업무 유지
8. 기본 Startup Page Today 및 설정 영속성
9. Clipboard에 읽을 수 있는 Summary 복사
10. Repository 재생성 후 업무와 Memo 유지
11. WPF Dashboard / Summary 실제 렌더링

## 스크린샷

- `screenshots/Task-008/01-today-dashboard.png`
- `screenshots/Task-008/02-inprogress.png`
- `screenshots/Task-008/03-blocked.png`
- `screenshots/Task-008/04-done-today.png`
- `screenshots/Task-008/05-daily-summary.png`

## 빌드 참고

프로젝트 Target Framework는 .NET Framework 4.7.2이다. 현재 검증 PC에는 4.7.2 Targeting Pack이 없어 VS2017 MSBuild에 설치된 .NET Framework Reference Assemblies 경로를 지정해 빌드했다.
