# Task-004 - Messenger 대화 저장 및 Session History

## 목적

사용자와 Agent 메시지를 업무 데이터와 독립적으로 영구 저장하고, Daily Conversation과 과거 대화 조회를 제공한다.

## 구현 범위

- Conversation 및 확장 Message 모델
- IConversationRepository/LocalConversationRepository
- Newtonsoft.Json 기반 원자적 JSON 저장
- ConversationService의 Daily Session, 마지막 Session, New Chat
- Chat 메시지 즉시 저장 및 최근 100개 복원
- History 목록, Message Viewer, Content 검색, 삭제 확인
- RelatedTaskId 및 Conversation별 Agent Context 초기화

## 제외 범위

Cloud Sync, Semantic Search, 대화 번호 Context 영구 복원 및 Archive UI는 구현하지 않는다.

## 구현 상세

`conversations.json`, `messages.json`, `conversation_state.json`을 Task 저장 파일과 분리한다. 사용자 메시지는 Agent 실행 전에 저장하고 Assistant/System 메시지는 처리 직후 저장한다. 날짜가 변경되면 전송 시 새 Daily Conversation으로 전환한다. 대화 삭제는 연결 메시지만 제거하며 Task Repository에는 접근하지 않는다.

## UI 요구사항

Chat Header에 Conversation 제목과 New Chat을 표시한다. History는 Conversation 목록과 공통 Message Bubble Viewer로 구성하며 검색과 2단계 삭제 확인을 제공한다.

## 테스트 방법

`.tmp/ConversationSmokeTest.cs`에서 저장 파일, RelatedTaskId, 재실행 복원, 검색, New Chat, 삭제 후 Task 유지, 날짜 변경과 손상 JSON 대응을 확인한다. `.tmp/WindowSmokeTest.cs`에서 Chat과 History의 실제 WPF 렌더링을 확인한다.

## 완료 조건

- .NET Framework 4.7.2/C# 7.x 대상 VS2017 빌드
- User/Assistant 메시지 영구 저장과 마지막 Session 복원
- Daily Session, History/Search/New Chat/Delete 동작
- 손상 파일 안전 처리 및 Runtime XAML 검증 통과
