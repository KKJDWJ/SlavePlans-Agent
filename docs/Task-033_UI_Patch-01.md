# Task-033 UI Patch-01 — Messenger / Board Layout 재수정

작성일: 2026-09-02

## 목적
현재 결과는 기존 Sidebar 텍스트를 아이콘으로 바꾼 수준이다.
기능 추가 없이 UI만 빠르게 재수정한다.

## 1. 로봇 아이콘 제거
Sidebar 최상단의 큰 로봇 얼굴 아이콘을 제거한다.
SlaveSplit 브랜드 영역이 이미 있으므로 중복 장식은 필요 없다.

## 2. Sidebar 최소화
일반 모드에서는 다음 4개만 유지한다.

```text
💬 대화
▦ 업무 보드
◷ 기록
⚙ 설정
```

- 화면에는 아이콘만 표시
- Hover 시 Tooltip으로 메뉴명 표시
- 선택 메뉴만 기존 Purple Accent 사용
- 불필요한 장식/공간 최소화

## 3. 개발 상태 UI 정리
상단 `Local Agent`, `GitHub` 상태 Badge가 일반 업무 화면에 필수 정보가 아니라면 설정 또는 관리자 모드로 이동한다.
Workspace Selector는 유지한다.

## 4. Board 4열 강제 배치
현재 `할 일 / 진행 중`이 지나치게 넓어 `보류 / 완료`가 화면 밖으로 밀린다.

기본 Desktop Window에서는 반드시 한 화면에:

```text
할 일 | 진행 중 | 보류 | 완료
```

4개 Column을 동시에 표시한다.

```text
ColumnWidth ≈ AvailableBoardWidth / 4
```

4개 Column은 동일 폭을 사용한다.
Task 개수나 제목 길이에 따라 Column 폭이 변하면 안 된다.

## 5. Horizontal Scroll
기본 Desktop 창 크기에서 Horizontal Scroll이 발생하면 FAIL이다.

Window가 실제 최소 지원 폭보다 작아 Card 최소 폭을 유지할 수 없는 경우에만 가로 Scroll을 허용한다.

## 6. Card 크기
Card Width는 Column 내부 폭에 맞춰 고정한다.

긴 제목 때문에 Card/Column이 넓어지면 안 된다.

제목은:
- 최대 2줄
- 초과 시 Ellipsis
- Card 높이 무한 증가 금지

예:

```text
SDC A4-2 PH2 STR08 CTRL
PMAC 주소 확인...
```

전체 내용은 Detail에서 확인한다.

## 7. 목표 형태

```text
┌───┬────────────────────────────────────────────────────┐
│💬 │ Task Board                          [Workspace ▼]  │
│▦  │                                                    │
│◷  │ 할 일       진행 중       보류        완료         │
│   │ ┌───────┐   ┌───────┐   ┌───────┐   ┌───────┐   │
│   │ │Task A │   │Task C │   │       │   │Task D │   │
│   │ ├───────┤   └───────┘   │       │   └───────┘   │
│   │ │Task B │                                       │
│   │ └───────┘                                       │
│   │                                                    │
│⚙  │                                                    │
└───┴────────────────────────────────────────────────────┘
```

## 8. 색상
변경하지 않는다.

```text
Dark Background
Purple Accent
기존 Status Color
```

이번 Patch는 Layout 수정이다.

## 9. 완료 조건
- [ ] 큰 로봇 아이콘 제거
- [ ] Sidebar 4개 아이콘 중심
- [ ] Hover Tooltip
- [ ] 선택 아이콘 Purple Accent
- [ ] Workspace Selector 유지
- [ ] 일반 화면 개발 상태 UI 최소화
- [ ] Board 4개 상태 동시 표시
- [ ] 4개 Column 동일 폭
- [ ] 제목 길이에 따른 Column 폭 변화 없음
- [ ] Card Width 고정
- [ ] 제목 최대 2줄 + Ellipsis
- [ ] 일반 Desktop 크기에서 Horizontal Scroll 없음
- [ ] 기존 Dark/Purple Theme 유지
- [ ] 기존 기능 Regression 영향 없음

## 10. 범위 금지
이번 Patch에서는 다음을 하지 않는다.

- Parser 수정
- Agent 기능 수정
- GitHub Sync 수정
- Local LLM 수정
- 새 기능 추가
- Theme 변경

**UI Layout만 수정하고 빠르게 완료한다.**

## 핵심
기존 UI에서 글자만 아이콘으로 바꾸는 것이 아니다.

```text
Sidebar 자체를 최소화
+
Board 4개 상태를 한 화면에서 즉시 확인
```

이 두 가지가 이번 Patch의 완료 기준이다.
