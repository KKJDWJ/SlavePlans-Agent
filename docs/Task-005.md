# Task-005 - Company AI Provider Architecture

## 목적

AI 통신, JSON 명령 해석 및 업무 실행을 분리하고 Company AI 장애 시 Local Rule Agent로 동작하는 Hybrid Pipeline을 구축한다.

## 구현 범위

- IAiProvider, MockAiProvider, CompanyAiProvider Skeleton
- AiRequest/AiResponse/AgentCommand JSON Contract
- AgentCommandParser와 제한된 Context Builder
- HybridAgentService의 검증·실행·Fallback
- Provider Manager, 상태 및 Test Connection
- ICredentialProvider와 환경 변수 Token 공급자
- Settings AI/Connection/Agent/Storage 화면
- Header AI Connected/Local Mode 상태
- Embedded System Prompt

## 제외 범위

실제 Company API Request/Response, Streaming, Retry, Multi Command 및 Provider 설정 영구 저장은 구현하지 않는다.

## 구현 상세

Provider는 구조화된 JSON만 반환하고 Hybrid Agent가 Parse, Confidence 확인 및 Task 존재 검증 후 TaskService를 호출한다. Provider 실패, Timeout, Invalid JSON, Unknown 또는 낮은 Confidence는 한 차례 Rule Agent로 Fallback한다. Company Provider는 실제 규격을 추측하지 않고 Request Builder, 인증 및 Response Parser 확장 지점만 제공한다.

## UI 요구사항

Chat Header는 Provider 상태를 표시하고 Settings에서 Mock/Company 선택, Endpoint, Model, Timeout 및 Test Connection을 제공한다. Token은 UI나 로그에 표시하지 않는다.

## 테스트 방법

`.tmp/AiProviderSmokeTest.cs`에서 Mock Task 생성, Provider 실패, Invalid JSON, 존재하지 않는 Task, Timeout, Wrapped JSON 및 Test Connection을 검증한다. `.tmp/WindowSmokeTest.cs`에서 Chat과 Settings 렌더링을 확인한다.

## 완료 조건

- .NET Framework 4.7.2/C# 7.x VS2017 빌드
- AI Provider/Parser/Hybrid/Fallback 계층 구현
- Timeout, Cancellation, 상태 및 Settings 연동
- 실제 Task 검증과 Runtime XAML 테스트 통과
