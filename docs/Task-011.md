# Task-011 검증 기록

## Architecture

`GitHubViewModel / GitHubIssueDetailViewModel → IGitHubService → GitHubService → GitHubClient → GitHub REST API`

- 목록: `GET /repos/{owner}/{repo}/issues`
- 상세: `GET /repos/{owner}/{repo}/issues/{number}`
- Read only이며 WorkTask Repository 쓰기 경로가 없다.

## 구현 내용

- GitHub API DTO와 내부 `GitHubIssueInfo` 분리
- Open / Closed / All, Page, PerPage, Sort, Direction Query
- PR DTO 제외
- Page별 Load More와 Issue Number 중복 제거
- 상태별 Session cache와 명시적 Refresh
- Number, Title, Body, Label, Assignee Local Search
- Recently Updated / Newest / Oldest Sort와 Label Filter
- Issue Detail, Null fallback, HTTPS github.com Browser 검증
- Loading, Empty, Not Connected, Offline, Authentication Error 상태
- Refresh 및 Detail 요청 Cancellation

## 자동 검증

- 51개 API DTO 중 PR 1개 제외, Issue 50개 반환
- Raw Page 기준 다음 Page 유지
- Null Body → `No description provided.`
- Null Assignee → `Unassigned`
- Label 및 ClosedAt Mapping
- 401 AuthenticationFailed
- Search와 Label Filter
- 다음 Page 중복 Number 제거
- 조회 전후 Local Task 0건 유지
- Open / Closed / Detail / Offline WPF 렌더링

## 실제 API 확인

2026-08-26 Token 없이 `KKJDWJ/SlaveSplit` Open Issue API를 호출했다. API에는 도달했지만 `404 Repository or issue not found`였으며 IssueCount는 0이었다. Private Repository라면 Task-010 Settings에 유효 Token을 저장한 후 실제 Issue를 조회해야 한다. Mock Issue는 UI 검증에만 사용했고 저장하지 않았다.

## 스크린샷

- `screenshots/Task-011/01-issue-list.png`
- `screenshots/Task-011/02-open-filter.png`
- `screenshots/Task-011/03-closed-filter.png`
- `screenshots/Task-011/04-issue-detail.png`
- `screenshots/Task-011/05-offline.png`

## Build

Target은 .NET Framework 4.7.2와 C# 7.x다. 현재 검증 PC에 4.7.2 Targeting Pack이 없어 VS2017 MSBuild에 설치된 Reference Assemblies 경로를 지정했다.
