# Task-007 검증 기록

## 구현 범위

- 공통 `TaskDetailView` / `TaskDetailViewModel`
- 상태, 차단 사유, 설명, 우선순위, 기한, 완료 시각 Header
- 날짜 구분을 포함한 `TaskHistory` 시간순 Timeline
- Memo 추가와 Start / Complete / Block / Unblock / Todo 전환
- 업무 정보 수정 및 Updated History
- Parent / Follow-Up 표시와 양방향 Detail 이동, Back navigation
- `RelatedTaskId` 기반 관련 Conversation 표시
- Card 기반 Tasks 화면, 문자열 검색, 상태 Filter, 네 가지 Sort, Overdue 표시
- Today / Tasks / Chat / History에서 공유하는 Task navigation 기반

## 자동 검증

격리된 `.tmp/task007-test` 저장소에 Parent, Follow-Up, Done, Blocked 업무와 History를 생성해 다음을 확인했다.

1. Timeline Oldest → Newest 정렬
2. Follow-Up 조회
3. Memo JSON 영구 저장
4. Blocked → Todo 및 Unblocked History
5. Parent / Child navigation
6. Title 검색
7. Done Filter
8. Priority Sort
9. Repository 재생성 후 Description, ParentTaskId, Memo 유지
10. Tasks 및 Detail WPF 실제 렌더링

## 스크린샷

- `screenshots/Task-007/01-tasks.png`
- `screenshots/Task-007/02-done-task-detail.png`
- `screenshots/Task-007/03-blocked-task-detail.png`
- `screenshots/Task-007/04-parent-followup-detail.png`
- `screenshots/Task-007/05-activity-timeline.png`

## 빌드 환경 참고

프로젝트 Target Framework는 .NET Framework 4.7.2 그대로 유지했다. 현재 머신에는 4.7.2 Reference Assemblies가 없어, VS2017 MSBuild에 `FrameworkPathOverride`로 설치된 4.8.1 Reference Assemblies를 지정해 호환 빌드를 검증했다.
