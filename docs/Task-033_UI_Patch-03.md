# Task-033 UI Patch-03 — 공통 Layout / 언어 정합성

작성일: 2026-09-02

## 목적

Task-033 UI 적용 후 화면별 Content 여백과 표시 언어가 서로 다르게 보이는 문제를 빠르게 수정한다.

이번 Patch는 **Layout + 표시 문구만 수정**한다.

---

# 1. 화면별 여백 통일

현재 다음 화면의 Content 시작 위치가 서로 다르다.

```text
대화
업무 보드
기록
```

각 View가 개별 Margin/Padding 값을 사용하지 않도록 한다.

공통 Layout Resource를 만든다.

예:

```text
AppContentPadding
AppContentTopPadding
AppContentHorizontalPadding
```

모든 Main View는 같은 기준을 사용한다.

목표:

```text
Sidebar 끝
    ↓
동일한 Left Padding
    ↓
대화 / 업무 보드 / 기록 / 설정
모두 같은 X 좌표에서 Content 시작
```

Top Padding도 동일한 기준을 사용한다.

---

# 2. 공통 Content Container

가능하면 각 화면에 숫자를 직접 넣지 말고 공통 Container/Style을 재사용한다.

예:

```text
MainContentStyle
```

적용 대상:

```text
Chat View
Task Board View
History View
Settings View
```

페이지를 추가해도 같은 여백을 자동으로 사용하도록 한다.

---

# 3. 표시 언어 잔여 영어 제거

현재 Summary에 다음 영어가 남아 있다.

```text
TODAY
Your work at a glance
```

한국어 UI 정책에 맞게 변경한다.

예:

```text
TODAY → 오늘
Your work at a glance → 오늘 업무를 한눈에 확인하세요
```

단, 문장이 길어 UI가 복잡해지면 더 짧게:

```text
오늘
업무 현황
```

처럼 사용해도 된다.

---

# 4. Status 표시 언어 통일

사용자 노출 Status는 다음 기준을 사용한다.

```text
Todo → 할 일
In Progress → 진행 중
Blocked → 보류
Done → 완료
Done Today → 오늘 완료
```

Dashboard, Chat Response, Board Header에서 같은 용어를 사용한다.

내부 Enum은 변경하지 않는다.

---

# 5. 버튼/메뉴 잔여 영어 확인

현재 화면 전체를 확인하여 사용자 노출 문자열의 잔여 영어를 정리한다.

예:

```text
+ New Chat → + 새 대화
+ Add task → + 업무 추가
Today → 오늘
History → 기록
Task Board → 업무 보드
```

단 다음은 고유명/기술명이라 유지 가능하다.

```text
SlaveSplit
GitHub
Local LLM
```

관리자 화면의 기술 용어까지 억지로 번역하지 않는다.

---

# 6. Chat Summary Layout

상단 Summary Card 역시 공통 Content Width 안에 위치시킨다.

Chat Title, Summary Card, Message Area, Input Area가 서로 다른 Left/Right 기준을 사용하지 않도록 한다.

개념:

```text
Chat Content Left
│
├─ Title
├─ Summary
├─ Messages
└─ Input
```

가능한 한 동일한 Content Boundary를 사용한다.

---

# 7. Board / History Layout

업무 보드와 기록 화면도 Chat과 동일한 Main Content Padding을 사용한다.

Board 내부 Column Layout은 Task-033 UI Patch-01 규칙을 유지한다.

```text
할 일 | 진행 중 | 보류 | 완료
```

4열 구조를 깨지 않는다.

---

# 8. 자동/수동 확인

## 수동 UI 확인

```text
[ ] 대화 Content 시작 X 좌표
[ ] 업무 보드 Content 시작 X 좌표
[ ] 기록 Content 시작 X 좌표
[ ] 설정 Content 시작 X 좌표
```

위 4개가 동일해야 한다.

추가:

```text
[ ] TODAY 제거
[ ] Your work at a glance 제거
[ ] New Chat 제거
[ ] Add task 제거
[ ] 사용자 Status 한국어 통일
[ ] 기존 Dark/Purple Theme 유지
[ ] Board 4열 Layout 유지
```

---

# 9. 범위 제한

이번 Patch에서는 하지 않는다.

```text
Parser 변경
DueDate 정책 변경
Query Logic 변경
Local AI 변경
GitHub Sync 변경
Board 기능 추가
Theme 변경
```

**공통 Layout과 사용자 노출 문자열만 수정한다.**

---

# 10. 완료 조건

```text
모든 Main View 동일 Content Padding
공통 Layout Resource/Style 사용
Chat 내부 주요 영역 Alignment 정리
Dashboard 잔여 영어 제거
버튼/메뉴 잔여 영어 제거
Status 한국어 용어 통일
기존 Board 4열 유지
기존 Theme 유지
기능 Regression 영향 없음
```

## 최종 보고

```text
Task-033 UI Patch-03 Result

Common Content Layout:
Chat Alignment:
Board Alignment:
History Alignment:
Settings Alignment:
Language Cleanup:
Status Terms:
Button/Menu Terms:
Theme Preserve:
Board Regression:

Final:
PASS / CONDITIONAL PASS / FAIL
```
