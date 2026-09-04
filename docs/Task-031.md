# Task-031 — Optional Task Time & Reminder

Version: 1.0  
Date: 2026-09-01

---

# 1. 목적

현재 SlaveSplit은 업무의 날짜(DueDate)는 관리하지만 시간은 관리하지 않는다.

Task-031에서는 **사용자가 시간을 지정한 업무에만** DueTime과 Reminder를 추가한다.

핵심 동작:

```text
오늘 MCC 로그 분석 업무 등록해줘
→ DueDate = Today
→ DueTime = null
→ Reminder 없음
```

```text
오늘 오후 6시에 MCC 로그 분석 업무 등록해줘
→ DueDate = Today
→ DueTime = 18:00
→ Reminder = 17:50
```

시간을 지정하지 않은 기존 업무에는 알림을 만들지 않는다.

---

# 2. 핵심 원칙

```text
DueTime == null
→ Reminder 없음

DueTime != null
→ 기본 Reminder = DueDateTime - 10분
```

사용자가 별도 알림 시간을 지정하면 그 값을 사용한다.

예:

```text
오늘 18시에 MCC 확인하고 30분 전에 알려줘
```

Expected:

```text
DueTime = 18:00
ReminderOffset = 30분
ReminderAt = 17:30
```

---

# 3. Task 시간 모델

기존 Task에 필요한 시간 정보를 추가한다.

예:

```text
DueDate
DueTime
ReminderOffsetMinutes
ReminderEnabled
```

또는 현재 모델 구조에 맞는 동등한 형태를 사용한다.

중요한 의미:

```text
DueDate = 날짜
DueTime = 선택적 시간
```

DueTime은 nullable이다.

기존 Task의 DueTime은 모두 null로 유지한다.

기존 업무에 임의로 시간을 채우지 않는다.

---

# 4. CREATE — 시간 지정

지원:

```text
오늘 오후 6시에 MCC 로그 분석 업무 등록해줘
오늘 18시 FMP 확인 업무 등록해줘
내일 오전 9시 장비 확인 업무 등록해줘
내일 9시 30분 보고서 작성 업무 등록해줘
```

Expected:

```text
DueDate
DueTime
```

이 함께 저장된다.

---

# 5. 시간 없는 CREATE

```text
오늘 MCC 로그 분석 업무 등록해줘
```

Expected:

```text
DueDate = Today
DueTime = null
ReminderEnabled = false
```

기존 Task-023/027 동작을 변경하지 않는다.

---

# 6. 기본 Reminder

시간이 지정되면 기본적으로:

```text
10분 전
```

알림을 생성한다.

예:

```text
Due = 18:00
Reminder = 17:50
```

단, 사용자가 명시적으로 알림을 끄면 생성하지 않는다.

---

# 7. 사용자 지정 Reminder

지원:

```text
오늘 18시에 MCC 확인하고 30분 전에 알려줘
내일 오전 9시 업무 1시간 전에 알려줘
```

Expected:

```text
ReminderOffsetMinutes = 30
```

또는:

```text
ReminderOffsetMinutes = 60
```

---

# 8. 시간 Update

기존 업무에도 시간을 추가/변경할 수 있다.

```text
1번 오후 6시로 해줘
1번 시간을 18시로 바꿔줘
MCC 업무 내일 오전 9시로 옮겨줘
```

Expected:

```text
DueDate/DueTime Update
```

시간을 새로 설정하면 기본 Reminder 10분 전을 적용한다.

기존에 사용자 지정 Reminder Offset이 있다면
불필요하게 10분으로 덮어쓰지 않는다.

---

# 9. 시간 제거

지원:

```text
1번 시간 없애줘
1번 시간만 지워줘
```

Expected:

```text
DueTime = null
ReminderEnabled = false
```

DueDate는 유지한다.

업무 자체를 삭제하지 않는다.

---

# 10. Reminder 제거

지원:

```text
1번 알림 꺼줘
1번 알림 없애줘
```

Expected:

```text
DueTime = 기존 값 유지
ReminderEnabled = false
```

시간 제거와 알림 제거를 구분한다.

---

# 11. Reminder 다시 활성화

시간이 존재하는 업무에서:

```text
1번 알림 켜줘
```

Expected:

```text
ReminderEnabled = true
ReminderOffsetMinutes = 기존 값 또는 기본 10
```

DueTime이 없는 업무라면 임의 시간을 만들지 않는다.

```text
TIME_REQUIRED
Mutation = 0
```

으로 안전하게 처리한다.

---

# 12. 시간 표현

최소 다음 표현을 지원한다.

```text
오전 9시
오후 6시
18시
18:30
9시 30분
오후 6시 반
```

24시간 형식으로 Normalize한다.

예:

```text
오후 6시 → 18:00
오전 9시 → 09:00
6시 반 → 문맥상 안전하게 해석 가능한 경우 06:30
```

오전/오후가 없어 의미가 위험하게 모호한 경우
임의 판단하지 않는다.

필요 시 Qwen Fallback 또는 clarification 상태를 사용한다.

---

# 13. 날짜 + 시간

기존 Date Semantics를 재사용한다.

```text
오늘 오후 6시
내일 오전 9시
9월 3일 오후 2시
```

새로운 날짜 시스템을 만들지 않는다.

---

# 14. Windows Notification

Reminder 시간이 되면 Windows 사용자 알림을 표시한다.

예:

```text
SlaveSplit

10분 뒤 예정된 업무가 있습니다.

MCC 로그 분석
예정 시간: 18:00
```

사용자 지정 Reminder인 경우 실제 남은 시간을 기준으로 표시한다.

---

# 15. System Tray

SlaveSplit에 System Tray 동작을 추가한다.

앱이 실행 중일 때 창을 닫더라도
설정된 동작에 따라 Tray에서 계속 실행할 수 있게 한다.

Tray 최소 기능:

```text
SlaveSplit 열기
오늘 할 일
종료
```

Tray 아이콘 자체는 단순하게 유지한다.

복잡한 Tray 메뉴를 만들지 않는다.

---

# 16. 창 닫기와 실제 종료

Reminder를 사용하려면 SlaveSplit Process가 살아 있어야 한다.

따라서 일반 Window Close 시:

```text
Tray로 최소화
```

하는 동작을 지원한다.

Tray 메뉴의:

```text
종료
```

를 선택하면 실제 Process를 종료한다.

처음부터 Windows Service를 만들지 않는다.

---

# 17. Reminder Service

구조 예:

```text
Task Repository
      ↓
Reminder Service
      ↓
현재 시간 확인
      ↓
Reminder 대상 판정
      ↓
Windows Notification
```

Reminder Service는 Repository 데이터를 기준으로 동작한다.

LLM이 Reminder 발생 여부를 결정하지 않는다.

---

# 18. 중복 알림 방지

같은 Reminder가 반복해서 계속 뜨면 안 된다.

동일 업무 / 동일 Reminder 시점에 대해:

```text
Notification 1회
```

만 발생해야 한다.

앱 내부 Tick/Timer가 여러 번 실행되어도 중복 표시하지 않는다.

---

# 19. 앱 재시작

Reminder 정보는 Task와 함께 저장한다.

SlaveSplit 재시작 시:

```text
Repository Load
→ 아직 지나지 않은 Reminder 복원
```

이미 지나간 알림을 앱 시작과 동시에 무더기로 띄우지 않는다.

Task-031에서는 Missed Reminder 별도 기능을 만들지 않는다.

---

# 20. 완료 업무

업무가 DONE이 되면 해당 업무의 Reminder는 더 이상 발생하지 않는다.

CompletedAt 기존 의미는 유지한다.

업무를 다시 TODO/IN_PROGRESS로 열었을 때
미래 Reminder가 유효하면 다시 대상이 될 수 있다.

---

# 21. DueDate/DueTime 변경

업무 시간을 변경하면 Reminder 시각도 다시 계산한다.

예:

```text
기존 Due = 18:00
Reminder = 17:50

1번 19시로 바꿔줘
```

Expected:

```text
Due = 19:00
Reminder = 18:50
```

사용자 지정 Offset이 30분이었다면:

```text
Reminder = 18:30
```

---

# 22. Workspace

Reminder는 Task-028 Workspace 정보를 유지한다.

알림 내용에서 필요하면 Workspace를 작게 표시할 수 있다.

예:

```text
[MachineVision]
10분 뒤 예정된 업무
Edge Detection 테스트
18:00
```

Repository 간 Reminder가 섞여 잘못된 Task를 가리키면 안 된다.

---

# 23. GitHub

DueTime/Reminder를 위해 GitHub Project 구조를 자동 변경하지 않는다.

GitHub에 해당 Field가 없다면 Local 정보로 관리한다.

기존 GitHub Issue/Project Sync를 깨뜨리지 않는다.

---

# 24. Local LLM

Qwen은 자연어 시간 해석 보조에만 사용할 수 있다.

Qwen이:

- Timer를 직접 생성
- Windows Notification 호출
- Repository 변경
- Tray 제어

를 하면 안 된다.

모든 실행은 기존 Resolver/Safety/Application 계층을 통과한다.

---

# 25. UI 표시

시간이 있는 업무는 기존 DueDate 옆에 Compact하게 표시한다.

예:

```text
오늘 18:00
내일 09:30
```

시간이 없는 업무는 기존처럼 날짜만 표시한다.

Reminder가 켜져 있으면 작은 알림 상태 표시를 사용할 수 있다.

Row/Card 높이를 크게 늘리지 않는다.

---

# 26. 자동 테스트

Test Runner에:

```text
Task-031 / Time & Reminder
```

Group을 추가한다.

FakeClock / FakeNotificationService를 사용한다.

실제 Windows 알림을 자동 테스트 중 띄우지 않는다.

---

## TEST-031-01 — No Time

Input:

```text
오늘 테스트 업무 등록해줘
```

Expected:

```text
DueDate = Today
DueTime = null
ReminderEnabled = false
```

---

## TEST-031-02 — Time Create

Input:

```text
오늘 오후 6시에 테스트 업무 등록해줘
```

Expected:

```text
DueDate = Today
DueTime = 18:00
ReminderEnabled = true
ReminderOffset = 10
```

---

## TEST-031-03 — 24h Time

Input:

```text
내일 18:30 테스트 업무 등록해줘
```

Expected:

```text
DueDate = Tomorrow
DueTime = 18:30
ReminderOffset = 10
```

---

## TEST-031-04 — Custom Reminder

Input:

```text
오늘 18시에 테스트 업무 등록하고 30분 전에 알려줘
```

Expected:

```text
DueTime = 18:00
ReminderOffset = 30
```

---

## TEST-031-05 — Time Update

Fixture:

```text
1. Today DueTime null
```

Input:

```text
1번 오후 6시로 해줘
```

Expected:

```text
DueTime = 18:00
ReminderEnabled = true
ReminderOffset = 10
```

---

## TEST-031-06 — Time Remove

Fixture:

```text
1. Today 18:00 Reminder ON
```

Input:

```text
1번 시간 없애줘
```

Expected:

```text
DueDate = Today
DueTime = null
ReminderEnabled = false
```

---

## TEST-031-07 — Reminder Disable

Fixture:

```text
1. Today 18:00 Reminder ON
```

Input:

```text
1번 알림 꺼줘
```

Expected:

```text
DueTime = 18:00
ReminderEnabled = false
```

---

## TEST-031-08 — Reminder Without Time

Fixture:

```text
1. Today DueTime null
```

Input:

```text
1번 알림 켜줘
```

Expected:

```text
TIME_REQUIRED
Mutation = 0
```

---

## TEST-031-09 — Notification 10 Minutes Before

FakeClock:

```text
17:49
```

Task:

```text
Due 18:00
ReminderOffset 10
```

17:49:

```text
Notification = 0
```

FakeClock → 17:50

Expected:

```text
Notification = 1
```

---

## TEST-031-10 — Duplicate Prevention

Reminder Service를 동일 시점에서 여러 번 Tick.

Expected:

```text
Notification = 1
```

---

## TEST-031-11 — Done Excluded

Task:

```text
Due 18:00
Reminder 17:50
Status Done
```

FakeClock = 17:50

Expected:

```text
Notification = 0
```

---

## TEST-031-12 — Restart Restore

미래 Reminder 저장 후 Service 재생성.

Expected:

```text
Future Reminder restored
Duplicate Notification = 0
```

---

## TEST-031-13 — Past Reminder

Task:

```text
ReminderAt = 과거
```

앱 시작.

Expected:

```text
Notification = 0
```

---

## TEST-031-14 — DueTime Change

Fixture:

```text
DueTime = 18:00
Offset = 10
```

Input:

```text
1번 19시로 바꿔줘
```

Expected:

```text
DueTime = 19:00
ReminderAt = 18:50
```

---

## TEST-031-15 — Custom Offset Preserve

Fixture:

```text
DueTime = 18:00
Offset = 30
```

Input:

```text
1번 19시로 바꿔줘
```

Expected:

```text
DueTime = 19:00
Offset = 30
ReminderAt = 18:30
```

---

## TEST-031-16 — Workspace Isolation

Workspace-A/B 각각 Reminder Task 존재.

Expected:

```text
Notification Task/Workspace Match
Cross Workspace Reference = 0
```

---

## TEST-031-17 — Date Regression

Input:

```text
1번 내일로 미뤄줘
```

Expected:

Task-027 정상.

---

## TEST-031-18 — Priority Regression

Input:

```text
1번 긴급으로 바꿔줘
```

Expected:

Task-029 정상.

---

## TEST-031-19 — Briefing Regression

Input:

```text
오늘 뭐부터 해야 해?
```

Expected:

Task-030 정상.

---

# 27. 수동 테스트

자동 테스트 PASS 후 실제 Windows에서 확인한다.

1. 현재 시각 기준 약 11~12분 뒤 테스트 업무 생성
2. DueTime 저장 확인
3. SlaveSplit 창 닫기
4. Tray에 SlaveSplit 존재 확인
5. Reminder 시각까지 대기
6. Windows 알림 1회 표시 확인
7. 알림 내용의 업무명/예정시간 확인
8. Tray에서 SlaveSplit 다시 열기
9. Tray `종료`로 실제 Process 종료 확인

수동 테스트를 위해 Production GitHub Task를 만들 필요는 없다.
가능하면 Local/Test Task를 사용한다.

---

# 28. 완료 조건

- DueTime nullable 추가
- 시간 없는 기존 Task 알림 없음
- 자연어 시간 CREATE
- 자연어 시간 UPDATE
- 시간 제거
- 기본 10분 전 Reminder
- 사용자 지정 Reminder Offset
- Reminder ON/OFF
- Windows Notification
- System Tray
- Window Close → Tray
- Tray Exit → Process 종료
- Reminder Service
- 중복 알림 방지
- 재시작 시 미래 Reminder 복원
- 과거 Reminder 무더기 알림 없음
- Done Task 알림 제외
- Workspace 격리
- Compact 시간 UI
- Task-027 Regression PASS
- Task-028 Regression PASS
- Task-029 Regression PASS
- Task-030 Regression PASS
- Task-031 자동 테스트 추가
- Current Task Failed = 0
- Full Regression Failed = 0
- Wrong Mutation = 0
- Production Mutation = 0
- GitHub Mutation = 0

---

# 29. 범위 확장 금지

Task-031에서 추가하지 않는다.

- 반복 일정
- 매일/매주 Reminder
- Snooze
- Missed Reminder 목록
- 이메일/Slack 알림
- 모바일 Push
- Windows Service
- PC 부팅 자동 실행 설정 UI
- Calendar 연동
- Task Board

Task Board는 다음 Task에서 진행한다.

---

# 30. 최종 보고

```text
Task-031 Result

Optional DueTime: PASS / FAIL
Time Parsing: PASS / FAIL
Time Update: PASS / FAIL
Default Reminder: PASS / FAIL
Custom Reminder: PASS / FAIL
Reminder Toggle: PASS / FAIL
Notification: PASS / FAIL
Duplicate Prevention: PASS / FAIL
Restart Restore: PASS / FAIL
Done Exclusion: PASS / FAIL
System Tray: PASS / FAIL
Workspace Isolation: PASS / FAIL
Regression: PASS / FAIL

Current Task Test
Total:
Passed:
Failed:
LLM Called:
Wrong Mutation:
Production Mutation:
GitHub Mutation:

Full Regression
Total:
Passed:
Failed:
Skipped:
Wrong Mutation:

Manual Windows Test:
Notification:
Tray:
Exit:

Final:
PASS / CONDITIONAL PASS / FAIL
```

---

# 핵심 원칙

시간은 모든 업무의 필수값이 아니다.

```text
날짜만 지정
→ 기존 업무
→ 알림 없음
```

```text
날짜 + 시간 지정
→ 시간 업무
→ 기본 10분 전 알림
```

SlaveSplit은 **사용자가 시간을 지정했을 때만** 알림을 만든다.

그리고 Reminder 판단과 실행은 LLM이 아니라
저장된 Task 데이터와 Reminder Service가 담당한다.
