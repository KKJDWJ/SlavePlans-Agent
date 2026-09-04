# Task-003 - Messenger Command Agent

## 목적

Chat 메시지를 Rule 기반 Intent로 변환하여 TaskService를 통해 업무를 생성, 변경 및 조회한다.

## 구현 범위

- IAgentService와 RuleBasedAgentService
- AgentRequest/AgentResponse/AgentIntent
- ConversationContext와 최근 표시 번호
- 생성, 시작, 완료, Blocked, 오늘 남은 업무, 오늘/어제 완료 및 검색
- 제목 중복과 잘못된 번호 방어
- ChatViewModel 비동기 Agent Pipeline과 처리 상태
- Agent 이벤트 및 오류 Logging

## 제외 범위

LLM, 외부 API, 대화 영구 저장, 복합 문장의 다중 명령 처리는 구현하지 않는다.

## 구현 상세

Rule Engine은 조회 명령을 먼저 검사한 뒤 상태 변경과 생성을 판정한다. 번호는 실제 Task ID와 분리된 ConversationContext 매핑이며 새 목록을 표시할 때 교체된다. 제목 검색 결과가 여러 건이면 목록과 새 번호를 표시하고 상태를 변경하지 않는다.

## UI 요구사항

사용자 Bubble 추가 후 비동기로 Agent를 실행하고 `Slave is thinking...` 상태를 표시한다. 결과는 Assistant Bubble로 추가되며 기존 자동 Scroll을 사용한다.

## 테스트 방법

`.tmp/AgentSmokeTest.cs`에서 생성 → 목록 → 번호 시작 → 번호 완료 → 오늘 완료 → Blocked → 중복/잘못된 번호 → Repository 재시작 → 어제 완료 → Unknown 시나리오를 실행한다.

## 완료 조건

- .NET Framework 4.7.2/C# 7.x 대상 VS2017 빌드
- 주요 Intent와 Context 번호 처리
- 중복 방어, 오류 응답 및 즉시 Dashboard 반영
- 재실행 후 완료 업무 조회 및 MainWindow 실행 성공
