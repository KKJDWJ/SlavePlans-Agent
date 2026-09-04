# Task-033 — 실사용 1차 개선 통합

작성일: 2026-09-02
대상: SlaveSplit Agent

## 1. 목적

Task-032 이후 실제 업무 사용에서 발견된 문제를 한 번에 개선한다.

이번 작업은 두 축이다.

- UI: Messenger First 구조로 단순화
- 기능: 실사용에서 확인된 Parser / Context / GitHub Sync / Local AI 문제 수정

기존 Dark + Purple 색상 톤과 Agent Core 구조는 유지한다.

---

# 2. UI 개선

## UI-01 — 표시 언어 통일

현재 `Daily Summary`, `Add task`, `BLOCKED`, `DONE TODAY` 등 영어와 한글이 혼재한다.

사용자 노출 일반 UI는 **한국어 중심으로 통일**한다.

예:

- Daily Summary → 오늘 요약
- Add task → 업무 추가
- Todo → 할 일
- In Progress → 진행 중
- Blocked → 보류
- Done → 완료

GitHub, Local LLM 등 고유 기술명은 억지로 번역하지 않는다.
내부 Enum/Class 이름은 변경 대상이 아니다.

## UI-02 — Messenger First UI

메인 UI를 기능별 관리 프로그램 형태에서 **메신저 중심 업무 Agent** 형태로 변경한다.

평소 사용의 중심은 Chat 하나로 둔다.

Today / Tasks / Summary 같은 결과는 가능한 경우 Chat 내부에서 업무 카드, 요약 카드, 목록 UI로 표현한다.

Board처럼 넓은 화면이 필요한 기능은 전용 View 전환을 허용한다.

향후 Web UI로 확장할 수 있도록 UI와 Agent/Application Core 결합도를 높이지 않는다.

## UI-03 — Board 카드/컬럼 크기 고정

현재 제목 길이에 따라 Board Layout이 변한다.

4개 Status Column은 제목 길이와 관계없이 동일/균등 폭을 기본으로 한다.

```text
할 일 | 진행 중 | 보류 | 완료
```

Task Card도 일정한 크기를 유지한다.

긴 제목은 최대 2줄 정도 표시 후 말줄임 처리한다.

```text
SDC A4-2 PH2 STR08 CTRL
PMAC 주소 확인...
```

전체 제목/내용은 Card Detail에서 확인한다.

## UI-04 — 아이콘 기반 Navigation

카카오톡 같은 간결한 Navigation 개념을 참고하되 그대로 복제하지 않는다.

좁은 Icon Sidebar + Hover Tooltip을 사용한다.

```text
💬  → 대화
▦   → 업무 보드
◷   → 기록
⚙   → 설정
```

선택된 아이콘은 기존 Purple Accent로 표시한다.

일반 모드의 메인 Navigation은 최소화한다.

Today / Weekly 등은 Chat 또는 업무 View 내부에서 접근한다.

색상은 현재 SlaveSplit의 Dark Background + Purple Accent + 기존 Status Color를 유지한다.

## UI-05 — 관리자 모드

개발/검증 기능은 일반 사용 UI에서 숨긴다.

설정:

```text
관리자 모드 [OFF]
```

기본값은 OFF.

관리자 모드 ON에서만 다음 개발 기능을 노출한다.

- Local LLM PoC
- Agent Test Runner
- GitHub 진단/개발 기능

관리자 모드는 UI 노출만 제어한다.
Task 데이터나 Agent 실행 규칙을 바꾸면 안 된다.

---

# 3. 기능 개선

## FUNC-01 — GitHub Refresh 정합성

현상:
GitHub에서 Issue/내용을 제거했는데 SlaveSplit에서 Refresh 후 기존 항목이 남는다.

Refresh는 단순 Append/Update가 아니라 현재 Remote 상태와 Local/Cache/View 상태를 다시 맞추는 동기화가 되어야 한다.

```text
Refresh
→ GitHub 현재 데이터 재조회
→ Local/Cache/View 비교
→ Remote에 없는 stale 항목 처리
→ 화면 Collection 재구성
```

수정 전에 원인을 구분한다.

1. GitHub API 응답에 항목이 남아 있는가
2. Cache에만 남아 있는가
3. Repository에서는 없어졌는데 ViewModel Collection이 남는가
4. Workspace별 Cache가 섞였는가

Workspace별 Cache는 격리한다.

## FUNC-02 — Title / Description 파싱

실패 입력:

```text
SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인 < 이거 오늘 업무로 등록해줘 내용은 신규 버전, 구버전 비교 이거로 하면 되.
```

기대 결과:

```text
Title = SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인
Description = 신규 버전, 구버전 비교
DueDate = Today
```

`<` 앞부분을 제목으로 사용하는 입력 패턴을 지원한다.

명시적 표현도 지원한다.

```text
제목은 MCC 로그 분석
내용은 신규 버전, 구버전 비교
오늘 업무로 등록해줘
```

Agent 명령 문장 전체를 Title에 저장하지 않는다.

## FUNC-03 — Recent Action Context

직전 수정 대상을 기억해 짧은 후속 대화를 처리한다.

필요 Context 예:

```text
LastTargetTask
LastAction
LastChangedField
LastOldValue
LastNewValue
```

지원 예:

```text
1번 오후 3시로 바꿔줘
→ 오후 3시로
→ 아니 4시로
→ 방금 시간 변경한 거 오후 5시로
```

직전 대상/Field가 명확하면 LLM 없이 Resolver/Context로 처리할 수 있게 한다.

## FUNC-04 — 모호한 시간 처리

현재 `3시까지 완료`를 03:00으로 조용히 확정하는 문제가 있다.

명확한 표현:

```text
오후 3시 → 15:00
오전 3시 → 03:00
15시 → 15:00
```

오전/오후 없는 `3시`처럼 위험하게 모호한 경우 임의로 03:00을 저장하지 않는다.

정의된 안전한 업무시간 정책이 없으면 clarification을 요청한다.

Wrong Mutation은 0이어야 한다.

## FUNC-05 — 실제 Chat Local AI 사용 불가 문제

Test Runner Preflight에서는:

```text
Endpoint = http://localhost:11434
Model = qwen3:4b-instruct
Generate = PASS
```

인데 실제 Chat에서는:

```text
자연어 해석이 필요한 명령인데 Local AI를 사용할 수 없습니다.
```

가 발생한다.

Test Runner와 실제 Chat의 다음 경로를 비교한다.

- Configuration
- DI Registration
- Service Instance
- Endpoint
- Model Name
- Enabled State
- Connection State
- Fallback 호출 경로

실제 Agent Chat과 Test Runner가 동일한 Local AI 설정을 사용해야 한다.

`GraniteOllamaService` 클래스명은 직접 원인이 아니면 이번 수정과 분리한다.

## FUNC-06 — 날짜 없는 신규 업무 기본 DueDate

실사용:

```text
AGENT 줘 패버리기 업무 등록해줘
```

등록은 됐지만 `오늘 업무` 조회에서 빠졌다.

정책:

일반적으로 날짜 없이 `업무 등록해줘`라고 하면 기본 DueDate를 Today로 설정한다.

```text
CreatedAt = Today
DueDate = Today
DueTime = null
Reminder = OFF
```

명시적 날짜는 그대로 사용한다.

```text
내일 업무로 등록해줘 → Tomorrow
금요일 업무로 등록해줘 → Friday
```

일정 없는 Task는 명시적으로 지원한다.

```text
일정 없이 XXX 업무 등록해줘
→ DueDate = null
```

## FUNC-07 — Query Context / 목록 Drill-down

실패:

```text
User: 전체 업무
Slave: Todo 2건

User: todo 2개 항목
Slave: 조건에 맞는 업무가 없습니다.
```

조회 결과를 Candidate/Query Context로 유지한다.

지원:

```text
전체 업무
→ 상태별 집계

Todo 보여줘
→ 현재 Scope Todo 상세

Todo 2개 항목
→ 방금 집계한 Todo 상세

그 2개 보여줘
→ 직전 결과 상세

그중 1번
→ 직전 Candidate Context 1번
```

Summary와 Detail Query는 같은 Scope를 사용한다.

## FUNC-08 — 전체 업무 / 오늘 업무 Scope 정합성

현재 `전체 업무` 요청에 `오늘 업무 현황입니다`라고 응답하는 문제가 있다.

정의:

```text
전체 업무 → 현재 Workspace 전체 업무
오늘 업무 → DueDate = Today
```

응답 Copy도 실제 Scope와 일치해야 한다.

Summary Count와 Detail 결과도 일치해야 한다.

---

# 4. 실사용 Regression Scenario

## SCENARIO-01 — Title / Description

입력:

```text
SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인 < 이거 오늘 업무로 등록해줘 내용은 신규 버전, 구버전 비교 이거로 하면 되.
```

Expected:

```text
Title = SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인
Description = 신규 버전, 구버전 비교
DueDate = Today
```

## SCENARIO-02 — Time Follow-up

```text
오늘 업무중 1번을 오후 3시까지 완료로 변경해줘
→ DueTime = 15:00

아니 오후 4시로
→ Same Task
→ DueTime = 16:00
```

## SCENARIO-03 — Ambiguous Time

```text
1번 3시까지 완료로 변경해줘
```

Expected:

```text
03:00로 무조건 저장하지 않음
Wrong Mutation = 0
```

## SCENARIO-04 — Default Today

```text
AGENT 줘 패버리기 업무 등록해줘
→ DueDate = Today

오늘 업무
→ 방금 생성한 Task 포함
```

## SCENARIO-05 — No Schedule

```text
일정 없이 테스트 업무 등록해줘
```

Expected:

```text
DueDate = null
DueTime = null
Reminder = OFF
```

## SCENARIO-06 — Summary → Drill-down

Fixture:

```text
Todo A
Todo B
```

```text
전체 업무
→ Todo = 2

todo 2개 항목
→ A, B
```

## SCENARIO-07 — Candidate Follow-up

Todo A/B 조회 후:

```text
그중 1번 완료해줘
```

Expected:

```text
A = DONE
B unchanged
Wrong Mutation = 0
```

## SCENARIO-08 — Scope Copy

```text
전체 업무
```

Expected:

```text
전체 업무 현황
```

`오늘 업무 현황`이라고 응답하면 FAIL.

## SCENARIO-09 — Runtime Local AI

Precondition:

```text
Local AI Enabled
Ollama Connected
qwen3:4b-instruct Available
```

실제 Agent Chat Fallback 실행.

Expected:

```text
LLM Called = 1
LLM Success = 1
Local AI unavailable = false
```

## SCENARIO-10 — GitHub Refresh

Fixture:

```text
Remote = Issue A
Local/View = Issue A + Issue B
```

Refresh Expected:

```text
View = Issue A
Issue B stale display removed
다른 Workspace 영향 없음
```

---

# 5. UI 수동 검증

- [ ] 일반 UI 한국어 통일
- [ ] Messenger First 화면
- [ ] Icon Sidebar
- [ ] Hover Tooltip
- [ ] 기존 Dark/Purple Theme 유지
- [ ] 관리자 모드 OFF에서 Test Runner 숨김
- [ ] 관리자 모드 ON에서 개발 도구 접근
- [ ] Board 4개 Column 폭 안정
- [ ] 긴 제목 때문에 Column 크기 변하지 않음
- [ ] Card 긴 제목 2줄 + 말줄임
- [ ] Card Detail에서 전체 내용 확인

---

# 6. 자동 테스트

기존 Regression에 위 실사용 Scenario를 추가한다.

Test Runner 버튼은 유지한다.

```text
[현재 Task 테스트]
[전체 테스트 실행]
[결과 복사]
```

자동 테스트:

```text
Wrong Mutation = 0
Production Mutation = 0
GitHub Mutation = 0
```

GitHub Refresh는 Fake/Mock Remote Repository로 검증한다.

실제 GitHub Issue를 자동 테스트에서 변경하지 않는다.

---

# 7. 완료 조건

UI:
- 한국어 중심 표시 언어 통일
- Messenger First UI
- Icon Sidebar + Tooltip
- 기존 색상 톤 유지
- Board Column/Card 크기 안정
- 관리자 모드
- 일반 모드에서 개발 메뉴 숨김

기능:
- GitHub Refresh stale item 수정
- Title/Description 분리
- `<` 제목 구분 지원
- Recent Action Context
- 짧은 후속 수정 지원
- 모호한 시간 Wrong Mutation 방지
- 실제 Chat Local AI 연결 수정
- 날짜 없는 일반 업무 = Today
- `일정 없이` 지원
- Query Result Context
- Todo Drill-down
- `그중 1번` Candidate 연결
- 전체/오늘 업무 Scope 분리
- Query Scope와 응답 문구 일치

Regression:
- 신규 실사용 Scenario PASS
- 기존 Regression PASS
- Failed = 0
- Unresolved = 0
- Wrong Mutation = 0
- Production Mutation = 0
- GitHub Mutation = 0

---

# 8. 범위 확장 금지

이번 Task에서는 추가하지 않는다.

- 새로운 Priority/Status
- 반복 일정
- Snooze
- 모바일 Push
- 새로운 Board 기능
- Task Dependency
- GitHub Project 구조 변경
- Web 구현 자체
- Theme 전면 교체

단, 향후 Web UI 확장을 방해하는 UI/Core 직접 결합은 만들지 않는다.

---

# 9. 최종 보고

```text
Task-033 Result

UI
Language Unification:
Messenger First:
Icon Navigation:
Tooltip:
Theme Preserve:
Board Fixed Layout:
Admin Mode:

Function
GitHub Refresh:
Title/Description:
Recent Action Context:
Ambiguous Time Safety:
Runtime Local AI:
Default DueDate:
No-Schedule Task:
Query Drill-down:
Candidate Follow-up:
Scope Consistency:

Current Task Test
Total:
Passed:
Failed:
Unresolved:
Wrong Mutation:
Production Mutation:
GitHub Mutation:

Full Regression
Total:
Passed:
Failed:
Skipped:
Wrong Mutation:

Manual UI
Messenger:
Navigation:
Board:
Admin Mode:

Final:
PASS / CONDITIONAL PASS / FAIL
```

---

# 핵심 목표

이번 Task 이후 SlaveSplit은:

```text
메신저처럼 간단하게 말하고
→ Agent가 앞 대화를 기억하고
→ 업무를 정확히 구조화하고
→ 필요할 때 Board/UI로 확인하는
```

개인 업무 Agent에 가까워져야 한다.

실사용에서 실패한 문장은 수정 후 버리지 말고 Regression Test 자산으로 계속 누적한다.
