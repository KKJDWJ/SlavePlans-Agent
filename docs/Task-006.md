# Task-006 - Multi Command 및 후속 업무 자동 생성

## 목적

한 사용자 메시지의 완료, Memo, Follow-Up, 수정 등 여러 동작을 순서대로 검증·실행하고 통합 결과를 제공한다.

## 구현 범위

- AgentCommandBatch/AgentBatchResult/AgentCommandResult
- Batch JSON Parsing과 순차 실행
- ADD_MEMO, CREATE_FOLLOWUP, UPDATE_TASK, UNBLOCK_TASK
- ParentTaskId 및 양방향 관계 History
- 명시적 TaskHistoryAction
- 유사 업무 중복 방지
- PendingCommandContext와 번호 확인
- Batch 부분 실패 응답
- 상태별 남은 업무 및 Memo 포함 완료 조회

## 제외 범위

Repository 전체 Transaction Rollback, Semantic Similarity, Pending Context 영구 저장 및 Multi Command UI 편집은 구현하지 않는다.

## 구현 상세

Hybrid Agent는 단일 JSON을 Batch 하나로 감싸 하위 호환을 유지한다. Batch 내 성공한 원본 Task ID는 뒤따르는 Memo와 Follow-Up의 기본 대상으로 전달된다. 각 명령은 독립적으로 결과를 기록하며 실패 후에도 안전한 후속 명령을 처리한다. 복수 후보는 Pending 상태로 저장하고 30분 내 번호 선택 전까지 변경하지 않는다.

## 데이터 관계

후속 Task는 ParentTaskId를 저장한다. 원본에는 FollowUpCreated와 Child ID, 후속 Task에는 CreatedFromTask와 Parent ID History를 기록한다.

## 테스트 방법

`.tmp/MultiCommandSmokeTest.cs`에서 완료+Memo+Follow-Up, Description 수정, 중복 방지, Block/Unblock, Confirmation, 번호 선택, 부분 실패, 내일 DueDate 및 기존 로직 적용 시나리오를 검증한다.

## 완료 조건

- .NET Framework 4.7.2/C# 7.x VS2017 빌드
- Batch Parser/Executor 및 신규 Action 구현
- Parent/Child/Memo History와 중복 방지
- Pending Confirmation 및 부분 실패 검증
- 최종 업무 시나리오와 Runtime 실행 통과
