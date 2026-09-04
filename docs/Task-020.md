# Task-020 — Offline Local LLM PoC

## 실행

1. Ollama를 실행하고 Granite 모델을 설치한다.
2. SlaveSplit에서 `Local LLM PoC` 메뉴를 연다.
3. `Ollama 연결 / 모델 탐색`을 눌러 모델을 선택한다. 설치된 모델 중 이름에 `granite`가 있는 모델을 우선한다.
4. 단건 비교 또는 `필수 문장 전체 실행`을 실행한다.

결과는 `%LOCALAPPDATA%\SlaveSplit\Task020\comparison-results.jsonl`에 JSON Lines 형식으로 누적된다.

## 안전 경계

- `GraniteOllamaService`는 Ollama `/api/tags`, `/api/generate`만 호출한다.
- Granite 출력은 `LlmTaskParseResult`로 역직렬화한 뒤 표시하고 기록하는 데만 사용한다.
- 기존 파서는 실제 Agent를 호출하지 않고 기존 Safety/NLP 구성 요소를 읽기 전용으로 관찰한다.
- PoC 코드에서 Task 쓰기, GitHub 호출, Tool 실행 경로는 연결하지 않는다.

Patch-01부터 Current Parser 결과는 별도 정규식 Probe가 아니라 `AgentConversationService`의 실제 Parser → Resolver 경로에서 얻는다. Benchmark 전용 `ConversationContext`를 사용하고 Tool 실행 직전에 DryRun으로 차단한다.

## Benchmark 신뢰성

- 각 테스트의 기대값, Current Parser 실제 DryRun 결과, Granite raw/parsed 결과와 JSON·Schema·Semantic·Evaluation·Final 판정을 표로 표시한다.
- `targetIndexes`는 중첩 배열이나 문자열을 허용하지 않는 엄격한 `int[]`로 검증한다.
- Cold start와 warm latency를 분리한다.
- Slave 프로세스 RAM, 모든 Ollama 프로세스 RAM, `/api/ps`의 model memory/VRAM을 분리한다.
- 실제 실패 회귀 문장 `2번만 Done으로 해줘`를 고정 테스트로 포함한다.

Patch-01 검증에서 이 회귀 문장의 실제 Chat 분석 경로 결과는 `READ_TASK / [] / null`이었다. 기존 PoC의 100%는 별도 Probe가 `UPDATE_TASK / [2] / Done`을 만들어 발생한 잘못된 수치다.

## 측정

각 요청의 시작/종료 시각, latency, 모델, 입출력 길이, JSON 파싱 성공, SlaveSplit process working set, 요청 구간의 앱 CPU 사용률을 저장한다. GPU/VRAM은 1차 PoC 범위에서 제외한다.

## 최초 연결 확인 (2026-08-31)

- Ollama API: 연결 성공
- 모델: `ibm/granite4.2:3b` (Granite 4.2 3.7B, Q4_K_M)
- 최초 직접 probe: 모델 load 포함 약 24초
- JSON 생성: 성공
- 스키마 의미 정확도: 단순 probe에서 enum/status 불일치가 관찰되어 전체 필수 문장 세트 평가 필요

최종 PASS 여부는 화면에서 동일 PC의 필수 문장 전체 실행 결과로 판단한다. 최초 probe만으로 모델 채택 결론을 내리지 않는다.

## Patch-02 결과 (동일 20개, 2026-08-31)

| Metric | Before | After |
|---|---:|---:|
| Current Parser Accuracy | 70.0% | 70.0% |
| Granite Semantic Accuracy | 30.0% | 55.0% |
| Granite Schema Success | 55.0% | 100.0% |
| Granite Final Accuracy | 30.0% | 55.0% |
| Warm Average Latency | 6,372 ms | 10,790 ms |

- Min / Max: 9,265 / 13,941 ms
- Cold Start: 모델이 이미 Ollama에 적재되어 있어 이번 공식 실행에서는 N/A
- Slave RAM: 72.2 MB
- Ollama + llama-server RAM: 11,297 MB
- Ollama `/api/ps` Model Memory: 12,922.6 MB
- VRAM: 2,430.6 MB
- Benchmark Path Match: PASS

Structured JSON Schema로 타입 안정성은 해결했지만 Semantic 85% 기준에는 크게 미달했고 warm latency도 악화됐다. Granite 3B Prompt 튜닝은 여기서 종료하고 동일 Benchmark로 다른 Local Model 또는 상위 Granite 모델을 비교하는 것이 타당하다.

## Patch-03 교차 분석

기존 Current Parser PASS와 Granite Final PASS를 변경하지 않고 다음 네 그룹으로 분류한다.

- A: Parser PASS / LLM PASS
- B: Parser PASS / LLM FAIL
- C: Parser FAIL / LLM PASS — Local LLM이 Parser를 보완한 문장
- D: Parser FAIL / LLM FAIL — 양쪽 모두 미해결인 문장

화면의 상세 표에서 각 행의 Group과 `Expected / Parser Result / LLM Result`를 확인할 수 있다. `Hybrid Cross Analysis`에는 그룹별 개수, 양쪽 정확도, Oracle Hybrid Coverage 및 C/D 문장 전체가 표시된다. Oracle Coverage는 완벽한 선택기를 가정한 이론상 상한이며 실제 Hybrid Agent 정확도가 아니다.
