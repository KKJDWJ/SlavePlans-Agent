# Task-010 검증 기록

## Architecture

`SettingsViewModel → IGitHubService → GitHubService → GitHubClient → GitHub REST API`

GitHub 계층은 Local Task/Conversation Repository와 분리되어 있으며 읽기 전용 `GET /repos/{owner}/{repo}`만 구현한다.

## Credential 및 보안

- Token 저장: Windows Credential Manager, Target `SlaveSplit/GitHub`
- 설정 저장: `github_settings.json`에 Enabled, Owner, Repository, Timeout만 저장
- GitHub 로그: 시각, Method, Endpoint, Status, Elapsed, Success만 기록
- Token, Authorization Header, PasswordBox 값은 설정과 로그에 기록하지 않음
- API에서 받은 Open URL은 HTTPS 및 `github.com` Host 검증 후에만 실행

## 자동 테스트

- HTTP 200 Repository DTO → 내부 Model 매핑
- User-Agent `SlaveSplit/0.1` 및 Bearer 인증 Header 적용
- 401 AuthenticationFailed
- 403 Forbidden / Rate Limit
- 404 RepositoryNotFound
- Network Error → Disconnected
- Timeout → Disconnected
- Token 설정/로그 미포함
- Offline 상태에서 Local Task 생성과 Memo 기록
- Settings 상태별 WPF 렌더링

## 실제 API 확인

2026-08-26에 Token 없이 `https://api.github.com/repos/KKJDWJ/SlaveSplit`을 호출했다. GitHub API 연결에는 성공했으나 응답은 `404 Repository Not Found`였다. Repository가 Private인 경우 유효 Personal Access Token을 Settings에 입력해야 실제 정보 조회가 가능하다. 실제 값을 임의로 가정하지 않았다.

## 스크린샷

- `screenshots/Task-010/01-github-settings.png`
- `screenshots/Task-010/02-connecting.png`
- `screenshots/Task-010/03-connected.png` (Mock Service 상태 검증)
- `screenshots/Task-010/04-repository-info.png` (Mock DTO UI 검증)
- `screenshots/Task-010/05-connection-failed.png`

## Build

Target은 .NET Framework 4.7.2와 C# 7.x이다. 검증 PC에는 4.7.2 Targeting Pack이 없어 VS2017 MSBuild에 설치된 Reference Assemblies 경로를 지정했다.
