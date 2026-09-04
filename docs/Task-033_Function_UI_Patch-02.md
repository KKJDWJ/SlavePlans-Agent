# Task-033 Function/UI Patch-02 — 업무 현황 정합성 긴급 수정

작성일: 2026-09-02

## 목적

실사용 중 업무 현황 표시가 서로 다르게 보여 사용자가 실제 상태를 오해할 수 있는 문제가 확인되었다.

이번 Patch는 기능 확장 없이 **업무 현황의 조회/표시 정합성과 Chat 사용성만 빠르게 수정**한다.

---

# 1. 현재 확인된 문제

## 문제 A — `현재 업무` 조회 실패

```text
현재 업무좀 보여줘
→ 조건에 맞는 업무가 없습니다.
```

`현재 업무`, `내 업무`, `업무 현황`, `지금 업무 뭐 있어?`는 현재 Workspace 업무 현황 조회로 인식해야 한다.

---

## 문제 B — Summary Count와 상세 목록 불일치

실사용 응답:

```text
Todo 1건
In Progress 2건
Blocked 0건
Done 0건

- 진행 중: Task A
- 진행 중: Task B
```

Todo가 1건이라고 집계됐는데 Todo 상세가 출력되지 않는다.

Summary Count와 상세 목록은 반드시 **동일한 Query Result**를 사용해야 한다.

```text
Count 계산용 Query
Detail 출력용 Query
```

를 따로 실행해서 결과가 달라지는 구조를 만들지 않는다.

---

# 2. Dashboard와 Chat의 Status 의미 통일

현재 Dashboard에는:

```text
Todo
In progress
Blocked
Done today
```

가 표시되고 Chat에는:

```text
Todo
In Progress
Blocked
Done
```

가 표시된다.

`Done`과 `Done today`는 의미가 다르므로 사용자가 같은 지표로 오해할 수 있다.

## 수정 원칙

일반 업무 현황은 동일한 기준으로 표시한다.

권장:

```text
할 일
진행 중
보류
완료
```

오늘 완료만 별도 지표로 보여주고 싶다면 반드시 명확하게:

```text
오늘 완료
```

라고 표시하고 일반 `완료`와 섞지 않는다.

---

# 3. `오늘 업무` 의미 고정

`오늘 업무`는 기존 Date Semantics를 유지한다.

```text
DueDate = Today
모든 Status
```

응답은 Status별로 정확히 분류한다.

예:

```text
오늘 업무 3건입니다.

할 일 1건
1. OCF CIM 알람 분석

진행 중 2건
2. PMAC 주소 확인
3. AGENT 수정

보류 0건
완료 0건
```

Count가 1이면 해당 Task가 반드시 상세에도 존재해야 한다.

---

# 4. `현재 업무` 의미

다음 표현을 현재 Workspace 업무 조회 Intent로 처리한다.

```text
현재 업무
현재 업무 보여줘
현재 업무좀 보여줘
내 업무
내 업무 보여줘
업무 현황
지금 업무 뭐 있어?
전체 업무
```

기본 Scope:

```text
Current Workspace
All DueDate
All Status
```

단 `오늘`이 명시되면 Today Scope를 사용한다.

---

# 5. 신규 Task Today 포함 검증

날짜 없이 일반 등록:

```text
XXX 업무 등록해줘
```

Task-033 정책:

```text
DueDate = Today
DueTime = null
```

이어야 한다.

등록 직후:

```text
오늘 업무
```

조회에 반드시 포함되어야 한다.

실제 저장된 DueDate와 화면 Response를 함께 검증한다.

---

# 6. Query Result 단일화

업무 현황 생성 시 먼저 하나의 Task Collection을 만든다.

```text
Query
→ ResultTasks
→ Status Grouping
→ Count
→ Detail
→ Dashboard/View
```

Count와 Detail은 동일한 `ResultTasks`에서 파생한다.

가능하면 Dashboard도 동일한 Application Query/Status 정의를 재사용한다.

UI별로 Status 집계 규칙을 따로 만들지 않는다.

---

# 7. 응답 문구 개선

사용자가 오해하지 않도록 범위를 명확하게 표시한다.

```text
오늘 업무 현황
현재 Workspace 전체 업무 현황
오늘 완료 업무
```

`전체 업무` 요청에 `오늘 업무 현황`이라고 응답하면 안 된다.

`Done`과 `Done Today`를 같은 의미처럼 표시하면 안 된다.

---

# 8. Chat Smooth Scrolling

실사용 중 Chat Scroll이 메시지 단위로 딱딱 끊긴다.

일반 메신저처럼 **Pixel 기반 Smooth Scrolling**으로 수정한다.

요구:

```text
Wheel
→ Pixel 단위 자연스러운 Scroll
→ 긴 메시지 중간에서도 정지 가능
```

새 메시지 자동 Scroll 정책:

```text
사용자가 이미 Bottom 근처
→ 새 메시지 시 Bottom 이동

사용자가 과거 메시지를 읽는 중
→ 강제로 Bottom 이동하지 않음
```

---

# 9. 자동 Regression

## TEST-P02-01

```text
현재 업무좀 보여줘
```

Expected:

```text
업무 조회 Intent
조건에 맞는 업무가 실제로 0건일 때만 Empty
```

## TEST-P02-02

Fixture:

```text
Todo A
InProgress B
InProgress C
```

Query:

```text
오늘 업무
```

Expected:

```text
Todo Count = 1
Todo Detail = A

InProgress Count = 2
InProgress Detail = B,C
```

## TEST-P02-03

날짜 없이 생성:

```text
테스트 업무 등록해줘
```

Expected:

```text
DueDate = Today
```

Follow-up:

```text
오늘 업무
```

Expected:

```text
생성 Task 포함
```

## TEST-P02-04

```text
전체 업무
```

Expected:

```text
전체 Workspace Scope
Response Copy = 전체 업무 현황
```

## TEST-P02-05

Fixture:

```text
Done Task
CompletedAt = Today
```

Expected:

```text
전체 Status Done = 1
오늘 완료 = 1
```

두 지표의 이름/의미를 구분한다.

## TEST-P02-06

Fixture:

```text
Done Task
CompletedAt = Yesterday
```

Expected:

```text
전체 Status Done = 1
오늘 완료 = 0
```

이를 통해 `완료`와 `오늘 완료`가 섞이지 않는지 검증한다.

---

# 10. UI 수동 확인

```text
[ ] Dashboard 상태명이 한국어로 명확함
[ ] 완료와 오늘 완료 의미가 구분됨
[ ] Chat Count와 실제 상세 항목 일치
[ ] Todo 1건이면 Todo 상세 1건 표시
[ ] 4개 Status가 같은 기준으로 보임
[ ] Chat Scroll이 메시지 단위로 끊기지 않음
[ ] 과거 메시지 읽는 중 새 메시지가 강제로 아래로 끌지 않음
```

---

# 11. 범위 제한

이번 Patch에서는 하지 않는다.

- 새로운 Status
- 새로운 Board 기능
- Theme 변경
- Reminder 기능 변경
- GitHub 구조 변경
- Local LLM 모델 변경

**업무 현황 정합성 + 조회 표현 + Scroll UX만 수정한다.**

---

# 12. 완료 조건

```text
현재 업무 조회 정상
오늘 업무 조회 정상
전체 업무 조회 정상
Summary Count = Detail Count
Dashboard = Chat Status 의미 일치
완료 / 오늘 완료 의미 명확
날짜 없는 신규 업무 Today 포함
Smooth Pixel Scrolling
기존 Regression PASS
Wrong Mutation = 0
Production Mutation = 0
GitHub Mutation = 0
```

## 최종 보고

```text
Task-033 Patch-02 Result

Current Work Query:
Today Work Query:
All Work Query:
Summary/Detail Consistency:
Dashboard/Chat Consistency:
Done vs DoneToday:
Default Today:
Smooth Scroll:

Current Patch Tests
Total:
Passed:
Failed:

Full Regression
Total:
Passed:
Failed:
Skipped:
Unresolved:
Wrong Mutation:
Production Mutation:
GitHub Mutation:

Final:
PASS / CONDITIONAL PASS / FAIL
```
