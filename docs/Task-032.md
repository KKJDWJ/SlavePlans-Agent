# Task-032 — Task Board

Version: 1.0
Date: 2026-09-02

## 1. 목적

SlaveSplit에 GitHub Projects처럼 현재 업무 상태를 한눈에 보는 **Task Board**를 추가한다.

```text
TODO        IN PROGRESS      BLOCKED       DONE
------------------------------------------------
업무 A      업무 C           업무 E        업무 F
업무 B      업무 D
```

새 업무 시스템을 만드는 것이 아니다.

기존 Task Repository의:

```text
Status
DueDate
DueTime
Priority
Workspace
```

를 Board로 시각화한다.

## 2. 핵심 구조

```text
Task Repository
→ Board ViewModel
→ Status Columns
→ Task Cards
```

Board 전용 Repository를 만들지 않는다.

Chat과 Board는 같은 Task/Application Service를 사용한다.

## 3. Navigation

Sidebar에 추가:

```text
보드
```

화면 제목:

```text
Task Board
```

Task-025 UI 스타일을 유지한다.

## 4. Columns

기존 Status 4개 그대로 사용:

```text
TODO
IN PROGRESS
BLOCKED
DONE
```

각 Header에 Count 표시:

```text
TODO 4
IN PROGRESS 2
BLOCKED 1
DONE 6
```

새 Status를 만들지 않는다.

## 5. Workspace

Task-028 Workspace Selector를 재사용한다.

```text
[ MachineVision ▾ ]    Task Board
```

Workspace 변경 시 해당 Repository Board를 Reload한다.

다른 Workspace Task가 섞이면 안 된다.

## 6. Task Card

Compact하게 다음만 표시한다.

```text
Priority
Title
DueDate
DueTime
Reminder 상태(optional)
```

예:

```text
[긴급]
MCC 로그 분석
오늘 18:00  🔔
```

Status는 Column으로 이미 표현되므로 Card에서 반복하지 않는다.

NORMAL Priority는 UI가 복잡하면 Badge를 생략해도 된다.

## 7. Overdue

```text
Status != DONE
AND DueDate < Today
```

이면 기한 초과 표시.

예:

```text
기한 지남 · 09-01
```

DONE은 Overdue 표시하지 않는다.

## 8. Card Detail

Card 클릭 시 기존 Task Detail이 있으면 재사용한다.

없으면 최소 Detail Panel:

```text
Title
Status
Priority
DueDate
DueTime
Reminder
Workspace
```

Task-032에서 대규모 편집 화면은 만들지 않는다.

## 9. Drag & Drop

Card를 다른 Column으로 Drag & Drop하면 Status 변경.

```text
TODO → IN PROGRESS
IN PROGRESS → DONE
BLOCKED → TODO
DONE → IN PROGRESS
```

반드시 기존 Status Update Application Service를 사용한다.

UI가 Repository를 직접 수정하지 않는다.

## 10. Same Column

```text
TODO → TODO
```

Expected:

```text
Mutation = 0
```

불필요한 GitHub Sync도 하지 않는다.

## 11. CompletedAt

기존 규칙 재사용:

```text
Non-Done → DONE
CompletedAt = Now

DONE → Non-Done
CompletedAt = null
```

Board 전용 CompletedAt 규칙을 만들지 않는다.

## 12. Drop Safety

Drop 처리:

```text
Task 확인
Workspace 확인
Target Status 확인
현재 Status 확인
→ Application Service
→ Repository
→ 기존 GitHub Sync
```

다른 Workspace Task를 잘못 변경하면 안 된다.

## 13. 실패 Rollback

Status 변경 실패 시 UI만 이동된 상태로 남으면 안 된다.

```text
Drop
→ Update FAIL
→ 원래 Column 복원
→ 오류 표시
```

UI와 실제 데이터가 항상 일치해야 한다.

## 14. Chat ↔ Board 동기화

Chat:

```text
1번 완료해줘
```

→ Board에서 해당 Task가 DONE으로 이동.

Board:

```text
TODO → IN PROGRESS
```

→ Chat에서:

```text
진행중 업무 보여줘
```

조회 시 해당 Task가 나와야 한다.

## 15. Refresh

다음 변경을 Board가 반영해야 한다.

```text
Workspace 변경
Task CREATE
Status 변경
DueDate 변경
DueTime 변경
Priority 변경
Repository Reload
```

현재 프로젝트의 Observable/Event/Messaging 구조가 있으면 재사용한다.

## 16. DONE Column

DONE 업무가 많아져도 Layout이 무너지지 않게 Scroll을 사용한다.

Archive 기능은 이번 Task에서 만들지 않는다.

## 17. Layout

Task-025 Design System 유지:

```text
Compact
Clean
Consistent
Dark
```

Board는 App Layout이다.

```text
Main Content Padding
Left/Right = 24
Top/Bottom ≈ 20
```

전체 Board를 불필요한 Centered MaxWidth Container에 넣지 않는다.

4개 Column이 Main 영역을 균등하게 사용한다.

작은 Window에서는 Horizontal Scroll 허용.

## 18. Column / Card UI

Column은 거대한 색상 Panel로 만들지 않는다.

```text
TODO 3
────────────
[Card]
[Card]
```

Card 권장:

```text
Padding 10~12
Gap 6~8
Title 12~13
Meta 10~11
```

과도한 Shadow/Border/Accent 금지.

GitHub Projects의 개념만 참고하고 그대로 복제하지 않는다.

## 19. Drag Visual Feedback

Drag 중:

```text
Target Column Highlight
Drop 위치 확인
```

정도만 제공한다.

과도한 Animation은 필요 없다.

## 20. Reminder

Task-031 Reminder가 있는 Task는 작은 표시만 한다.

```text
🔔 18:00
```

Reminder 기능 자체를 Board에서 다시 구현하지 않는다.

## 21. Local LLM

Board Drag & Drop은 LLM을 사용하지 않는다.

명확한 Status Update이므로 기존 Application Service를 직접 호출한다.

## 22. GitHub

기존 GitHub Sync 정책을 그대로 사용한다.

Board 때문에 GitHub Project 구조나 Field를 변경하지 않는다.

## 23. 자동 테스트

Test Runner에 추가:

```text
Task-032 / Task Board
```

UI Pixel Test는 필요 없다.

Board ViewModel / Grouping / Drag Command 의미를 테스트한다.

### TEST-032-01 Status Group

Fixture:

```text
A TODO
B IN_PROGRESS
C BLOCKED
D DONE
```

Expected:

```text
TODO = A
IN_PROGRESS = B
BLOCKED = C
DONE = D
```

### TEST-032-02 Count

Fixture:

```text
TODO 3
IN_PROGRESS 2
BLOCKED 1
DONE 4
```

Expected Counts:

```text
3 / 2 / 1 / 4
```

### TEST-032-03 Workspace Isolation

Workspace-A:
```text
A1 TODO
```

Workspace-B:
```text
B1 TODO
B2 DONE
```

Board = Workspace-B

Expected:

```text
TODO = B1
DONE = B2
A1 excluded
```

### TEST-032-04 TODO → IN_PROGRESS

Fixture:

```text
A TODO
```

Drag A → IN_PROGRESS

Expected:

```text
Status = IN_PROGRESS
Mutation = 1
```

### TEST-032-05 IN_PROGRESS → DONE

Fixture:

```text
A IN_PROGRESS
CompletedAt = null
```

Drag → DONE

Expected:

```text
Status = DONE
CompletedAt = FakeClock.Now
Mutation = 1
```

### TEST-032-06 DONE → IN_PROGRESS

Fixture:

```text
A DONE
CompletedAt != null
```

Drag → IN_PROGRESS

Expected:

```text
Status = IN_PROGRESS
CompletedAt = null
Mutation = 1
```

### TEST-032-07 Same Column

```text
A TODO
Drag A → TODO
```

Expected:

```text
Mutation = 0
```

### TEST-032-08 Invalid Workspace Drop

Workspace-A Task를 Workspace-B Board Command로 Drop.

Expected:

```text
Mutation = 0
Wrong Mutation = 0
```

### TEST-032-09 Overdue

FakeClock Date:

```text
2026-09-02
```

Fixture:

```text
A 2026-09-01 TODO
B 2026-09-01 DONE
```

Expected:

```text
A Overdue = true
B Overdue = false
```

### TEST-032-10 DueTime Display

Fixture:

```text
A Today 18:00
B Today DueTime null
```

Expected:

```text
A = Today + 18:00
B = Today only
```

### TEST-032-11 Priority Display

Fixture:

```text
A URGENT
B HIGH
C NORMAL
D LOW
```

Expected:

Task-029 Priority 의미와 일치.

### TEST-032-12 External Refresh

Fixture:

```text
A TODO
```

Application Service에서 A → DONE.

Expected:

```text
Board TODO에서 제거
Board DONE에 추가
```

### TEST-032-13 Board → Repository

Board에서:

```text
A TODO → IN_PROGRESS
```

Repository Query Expected:

```text
A Status = IN_PROGRESS
```

### TEST-032-14 Failure Rollback

Fake Status Update = FAIL.

```text
A TODO
Drag → DONE
```

Expected:

```text
Board = TODO
Repository = TODO
Wrong Mutation = 0
```

### TEST-032-15 Chat Status Regression

```text
1번 완료해줘
```

기존 Status Update 정상.

### TEST-032-16 DueDate Regression

```text
1번 내일로 미뤄줘
```

Task-027 정상.

### TEST-032-17 Priority Regression

```text
1번 긴급으로 바꿔줘
```

Task-029 정상.

### TEST-032-18 Briefing Regression

```text
오늘 뭐부터 해야 해?
```

Task-030 정상.

### TEST-032-19 Reminder Regression

시간/Reminder Task를 Board Load.

Expected:

```text
DueTime 유지
Reminder 유지
```

Status 변경 때문에 Reminder 데이터가 임의 변경되지 않는다.

## 24. 수동 UI 테스트

자동 테스트 후 실제 화면 확인:

1. `보드` 진입
2. 4개 Column 확인
3. Count 확인
4. Workspace 변경
5. Workspace별 Board 격리
6. Priority Badge
7. DueDate/DueTime
8. Overdue
9. Card 클릭 Detail
10. TODO → IN PROGRESS Drag
11. 즉시 Column 이동
12. Chat `진행중 업무 보여줘`
13. IN PROGRESS → DONE
14. Chat에서 완료 상태 확인
15. DONE → IN PROGRESS
16. CompletedAt 의미 확인
17. Same Column Drop No-op

## 25. 완료 조건

- Sidebar `보드`
- Task Board 화면
- TODO / IN PROGRESS / BLOCKED / DONE
- Column Count
- Workspace Selector
- Workspace 격리
- Compact Task Card
- Priority
- DueDate
- DueTime
- Overdue
- Reminder 상태 표시
- Card Detail
- Drag & Drop Status Update
- Same Column No-op
- CompletedAt 기존 의미 유지
- 실패 Rollback
- Chat → Board Refresh
- Board → Repository 반영
- Board → Chat 일관성
- 기존 Regression PASS
- Task-032 자동 테스트 추가
- Current Task Failed = 0
- Full Regression Failed = 0
- Wrong Mutation = 0
- Production Mutation = 0
- GitHub Mutation = 0

## 26. 범위 확장 금지

이번 Task에서 추가하지 않는다.

- 사용자 정의 Column
- 새로운 Status
- Swimlane
- WIP Limit
- Archive
- 복잡한 Board Filter
- 여러 Workspace 통합 Board
- Task Dependency
- Repository 간 Task 이동
- GitHub Projects UI 복제

## 27. 최종 보고

```text
Task-032 Result

Board Screen: PASS / FAIL
Status Columns: PASS / FAIL
Counts: PASS / FAIL
Workspace Isolation: PASS / FAIL
Task Card: PASS / FAIL
Priority: PASS / FAIL
DueDate/Time: PASS / FAIL
Overdue: PASS / FAIL
Task Detail: PASS / FAIL
Drag & Drop: PASS / FAIL
No-op: PASS / FAIL
CompletedAt: PASS / FAIL
Rollback: PASS / FAIL
Chat/Board Sync: PASS / FAIL
Regression: PASS / FAIL

Current Task Test
Total:
Passed:
Failed:
Wrong Mutation:
Production Mutation:
GitHub Mutation:

Full Regression
Total:
Passed:
Failed:
Skipped:
Wrong Mutation:

Manual UI Test:
Board:
Workspace:
Drag & Drop:
Detail:
Chat Sync:

Final:
PASS / CONDITIONAL PASS / FAIL
```

# 핵심 원칙

Task Board는 별도의 업무 시스템이 아니다.

같은 Task를:

```text
Chat에서는 자연어로 관리하고
Board에서는 눈으로 보고 움직인다.
```

따라서:

```text
Chat에서 완료 → Board DONE
Board에서 진행중 이동 → Chat에서도 IN_PROGRESS
```

가 항상 일치해야 한다.

**Board는 기존 SlaveSplit Agent의 시각적 작업 화면이다.**
