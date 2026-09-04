# Task-033 Function Patch-04 — `<` 사용자 명시적 제목 구분자

작성일: 2026-09-02

## 목적
SlaveSplit 개인 사용 규칙으로 `<`를 명시적인 **업무 제목 구분자**로 사용한다.
자연어 추측보다 사용자가 직접 지정한 경계를 우선한다.

## 1. 핵심 규칙
업무 등록 문장에 `<`가 있으면 **첫 번째 `<` 이전 문자열을 Title로 확정**한다.

```text
SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상 < 이거 오늘 업무로 등록해줘
```

Expected:

```text
Title = SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상
```

LLM이 해당 Title을 다시 추측/축약/재작성하지 않는다.

## 2. Description
`내용은`, `내용:` 등 명시적 표현 뒤를 Description으로 추출한다.

```text
SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상
< 이거 오늘 업무로 등록해줘,
내용은 PC 재부팅 시, Serial Port 번호 변경됨
< 이거야
```

Expected:

```text
Title = SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상
Description = PC 재부팅 시, Serial Port 번호 변경됨
```

`< 이거야`, `< 이렇게`, `< 이걸로` 같은 후행 대화 표현은 저장하지 않는다.

## 3. 단순 Split 금지
`input.Split('<')`로 조각을 그대로 저장하지 않는다.

처리 순서:

```text
1. 첫 번째 < 위치 탐색
2. 이전 문자열 → Title
3. 이후 → Command 영역
4. Command 영역에서 DueDate/DueTime/Priority 등 기존 해석
5. `내용은` / `내용:` → Description
6. 후행 대화 표현 제거
```

## 4. 기존 자연어 유지
`<`가 없는 입력은 기존 Parser/LLM 흐름을 그대로 사용한다.

```text
오늘 MCC 로그 분석 업무 등록해줘
```

`<` 문법은 선택 사항이다.

## 5. 우선순위

```text
명시적 `<` Title
→ 명시적 `제목은`
→ 기존 Rule Parser
→ 필요 시 Local LLM
```

사용자가 지정한 Title이 최우선이다.

## 6. GitHub Mapping

```text
GitHub Issue Title = Task.Title
GitHub Issue Body = Task.Description
```

Agent 명령 문장 자체가 Issue Title에 포함되면 안 된다.

## 7. 원문 보존
저장 전 앞뒤 공백과 명령용 표현만 제거한다.

다음 장비 문자열/기호는 임의 변경하지 않는다.

```text
SDC A4-2
AOI#1
Serial Port
PMAC
```

## 8. 자동 테스트

### P04-01
```text
MCC 로그 분석 < 오늘 업무로 등록해줘
```
Expected:
```text
Title = MCC 로그 분석
DueDate = Today
```

### P04-02
```text
MCC 로그 분석 < 오늘 업무로 등록해줘, 내용은 신규 버전과 구버전 비교
```
Expected:
```text
Title = MCC 로그 분석
Description = 신규 버전과 구버전 비교
```

### P04-03
```text
MCC 로그 분석 < 오늘 업무로 등록해줘, 내용은 신규/구버전 비교 < 이거야
```
Expected:
```text
Title = MCC 로그 분석
Description = 신규/구버전 비교
```

### P04-04 — 실제 사용 문장
```text
SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상 < 이거 오늘 업무로 등록해줘, 내용은 PC 재부팅 시, Serial Port 번호 변경됨 < 이거야
```
Expected:
```text
Title = SDC A4 EVEN SIP AOI#1 Serial Port 번호 바뀌는 현상
Description = PC 재부팅 시, Serial Port 번호 변경됨
DueDate = Today
```

### P04-05
```text
SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인 < 업무 등록해줘
```
Expected:
```text
Title = SDC A4-2 PH2 STR08 CTRL PMAC 주소 확인
```

### P04-06 — Legacy Regression
```text
오늘 MCC 로그 분석 업무 등록해줘
```
기존 CREATE 정상.

### P04-07 — GitHub Mapping
Parsed:
```text
Title = Test A
Description = Detail B
```
Expected DTO:
```text
Title = Test A
Body contains Detail B
```
실제 GitHub Mutation 금지.

## 9. 완료 조건
- [ ] 첫 번째 `<` 이전 = Title
- [ ] Title LLM 재작성 금지
- [ ] `내용은` Description 추출
- [ ] `< 이거야` 등 후행 표현 제거
- [ ] 여러 `<` 안전 처리
- [ ] 장비명/기호 원문 유지
- [ ] DueDate/DueTime/Priority 기존 해석 유지
- [ ] GitHub Title = Task Title
- [ ] GitHub Body = Description
- [ ] `<` 없는 기존 명령 Regression PASS
- [ ] Wrong Mutation = 0
- [ ] Production Mutation = 0
- [ ] GitHub Mutation = 0

## 10. 범위 제한
이번 Patch에서는 UI, Board, Reminder, Status 정책, Query Scope, Local LLM 모델, GitHub 인증을 변경하지 않는다.

**`<` 사용자 문법의 Title/Description 구조화만 수정한다.**

## 최종 보고
```text
Task-033 Function Patch-04 Result

Explicit Title Delimiter:
Description Parsing:
Multiple Delimiter:
Trailing Phrase Cleanup:
Equipment Text Preserve:
GitHub Mapping:
Legacy Create Regression:

Patch Tests
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
