#  나의 표정 친구

> 자폐 스펙트럼 아동이 타인의 감정을 이해하고 적절한 표정을 표현하도록 돕는 AI 기반 디지털 치료제

## 📌 프로젝트 소개

나의 표정 친구는 자폐 스펙트럼(ASD) 아동이 타인의 표정을 인지하고 자신의 표정을 표현하는 능력을 향상시키기 위한 AR 기반 모바일 애플리케이션입니다. Unity의 AR 기술과 AI 기반 실시간 얼굴 인식을 활용하여 아이들이 즐겁게 학습할 수 있는 게임 환경을 제공하며, 부모/교사에게는 아동의 학습 데이터를 제공하여 체계적인 맞춤 교육을 지원합니다.


### 🎯 핵심 가치

- **과학적 근거**: FACS 기반 정량적 표정 분석
- **임상 검증**: 선행 연구(JeStiMulE, JEMIME)에서 입증된 가상 환경 훈련의 실제 상황 전이 효과
- **가정 연계**: 부모 매개 중재(Parent-mediated intervention) 기반의 지속적 치료 지원
- **접근성**: 시공간 제약 없는 모바일 디지털 치료제로 치료 사각지대 해소

## 🎮 주요 기능

### 1. 표정 훈련
자폐 아동이 다양한 표정의 캐릭터를 보고 따라하면, AR 기술로 아이의 표정을 인식하여 실시간 피드백을 제공하는 학습 게임

### 2. 표정 인식 퀴즈
직접 주변 사람들의 표정을 카메라에 담아 인물의 표정을 통해 감정을 학습하는 퀴즈 형식 게임

### 3. 상황 맥락 학습
짧은 이야기 속 현실 공간의 상황에 몰입하여 상황을 인지하고 맥락에 맞는 표정을 짓는 실전 훈련 게임


### 4. 보호자/교사 대시보드
아동의 표정 학습 진행 상황과 숙련도를 그래프와 차트로 시각화하여 제공. 가정 내 오프라인 미션을 통한 부모 아동 소통 지원


## 👥 팀 정보
**31팀 PLAN_B**

|이름|역할|
|----|--------|
| 강수련 | 클라이언트 개발(Unity, AR) |
| 이수지 | 백엔드 개발(Spring, DB) |
| 권도연 | 기획 및 디자인 |


## ⚙️ 기술 스택
#### Environment
<img src="https://img.shields.io/badge/unity-FFFFFF?style=for-the-badge&logo=unity&logoColor=black"> <img src="https://img.shields.io/badge/android-3DDC84?style=for-the-badge&logo=android&logoColor=white"> <img src="https://img.shields.io/badge/git-F05032?style=for-the-badge&logo=git&logoColor=white"> <img src="https://img.shields.io/badge/github-181717?style=for-the-badge&logo=github&logoColor=white">

#### Development
<img src="https://img.shields.io/badge/-C%23-000000?logo=Csharp&style=flat"> <img src="https://img.shields.io/badge/java-007396?style=for-the-badge&logo=java&logoColor=white"> <img src="https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB">  <img src="https://img.shields.io/badge/springboot-6DB33F?style=for-the-badge&logo=springboot&logoColor=white"> <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white"> 

#### Communication
<img src="https://img.shields.io/badge/notion-000000?style=for-the-badge&logo=notion&logoColor=white"> <img src="https://img.shields.io/badge/slack-4A154B?style=for-the-badge&logo=slack&logoColor=white">

## 📊 기대 효과

### 학습의 일반화 (Generalization)
선행 연구에서 입증된 바와 같이, AR 게임을 통해 습득한 기술이 실제 사회적 상황으로 전이되는 효과 (미훈련 데이터 감정 인식률 37.39% → 61.21% 향상)

### 치료 접근성 향상
- 시공간 제약 없는 모바일 디지털 치료제
- 고비용 오프라인 센터 치료 의존도 감소
- 치료 사각지대 아동에게 평등한 교육 기회 제공

### 객관적 평가 시스템
- 미세 안면 근육 움직임의 수치 데이터 변환
- 구체적이고 객관적인 발달 지표 제시
- 조기 선별 및 예후 예측 데이터 축적



<details>
<summary>Frontend</summary>

# MyExpressionFriend Front

MyExpressionFriend Front는 아동의 표현 학습, 대화 학습, 오프라인 미션, 보호자/치료사 통계, 리포트, 알림, 관리자 기능을 제공하는 React 기반 웹 프론트엔드입니다. Spring Boot 백엔드 API(`myexpressionfriend-api`)와 연동하며, 보호자, 치료사, 관리자 역할별 화면을 제공합니다.

## 프로젝트 개요

이 프론트엔드는 다음 기능을 담당합니다.

- 이메일/비밀번호 기반 로그인, 회원가입, JWT 인증 상태 관리
- 보호자 홈, 통계, 오프라인 미션, 리포트, 알림 화면
- 치료사 홈, 아동 관리, 통계 분석, 오프라인 미션 배정/검토, 리포트 화면
- 관리자 회원 권한 관리, RAG 디버그/자료 등록, 시나리오 생성/검수, Realtime 설정 화면
- 아동 프로필 이미지 표시, 권한별 화면 노출 제어
- 백엔드 API `/api/...` 및 업로드 파일 `/uploads/...` 프록시 연동
- SPA 배포 환경에서 새로고침 fallback을 위한 `_redirects` 파일 제공

## 기술 스택

- Node.js / npm
- React 19
- React DOM
- React Router DOM 7
- Vite 7
- Tailwind CSS 4
- Recharts

## 소스 코드 구조

```text
src
├── App.jsx                    라우팅 및 역할별 페이지 진입점
├── main.jsx                   React 애플리케이션 bootstrap
├── components                 공통 레이아웃, 사이드바, 보호 라우트, 테마 토글
├── contexts                   인증 상태와 테마 상태 Context
├── lib                        API helper, 아동 권한 helper, Markdown/미션 공통 유틸
├── pages                      보호자/치료사/관리자 화면 컴포넌트
└── styles                     전역 CSS 및 Tailwind 기반 스타일
```

주요 파일은 다음과 같습니다.

```text
src/lib/api.js                 API URL 생성, fetch wrapper, 에러/응답 payload 처리
src/lib/childUtils.js          아동 권한, 프로필 이미지 URL, 나이/성별 표시 helper
src/contexts/AuthContext.jsx   로그인, 로그아웃, access token, 사용자 상태 관리
src/pages/DashboardPage.jsx    사용자 역할과 URL에 따른 화면 분기
src/pages/AdminRealtimeConfigPage.jsx   운영자 Realtime 설정 조회/수정 화면
src/styles/index.css           전체 애플리케이션 스타일
public/_redirects              정적 배포 환경 SPA fallback 설정
```

## 설치 방법

### 1. 필수 프로그램

다음 프로그램이 설치되어 있어야 합니다.

- Node.js 20 이상 권장
- npm
- Git

백엔드 API를 함께 실행하려면 별도 백엔드 저장소(`myexpressionfriend-api`)의 README.md에 따라 Spring Boot 서버와 PostgreSQL을 실행해야 합니다.

### 2. 저장소 클론

```bash
git clone <repository-url>
cd myexpressionfriend-front
```

통합 제출 레포에서 사용하는 경우에는 프론트 폴더로 이동합니다.

```bash
cd frontend
```

### 3. 의존성 설치

`package-lock.json`이 포함되어 있으므로 재현 가능한 설치에는 `npm ci`를 권장합니다.

```bash
npm ci
```

개발 중 lockfile 갱신이 필요한 경우에는 다음 명령을 사용할 수 있습니다.

```bash
npm install
```

### 4. 환경 변수 설정

`.env.example`을 복사해 `.env`를 만듭니다.

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

macOS/Linux 또는 Git Bash:

```bash
cp .env.example .env
```

기본 환경 변수:

```env
VITE_API_BASE_URL=http://localhost:8080
```

| 변수 | 설명 | 기본값 |
| --- | --- | --- |
| `VITE_API_BASE_URL` | 백엔드 API 서버 주소 | `http://localhost:8080` |

개발 서버에서는 Vite proxy가 `/api`와 `/uploads` 요청을 `VITE_API_BASE_URL`로 전달합니다. 프론트 코드에서는 `/api/...` 상대 경로를 사용합니다.

## 실행 방법

개발 서버 실행:

```bash
npm run dev
```

기본 접속 주소:

```text
http://localhost:5173
```

백엔드가 `http://localhost:8080`에서 실행 중이면 Vite proxy를 통해 API 요청이 전달됩니다.

## 빌드 방법

프로덕션 빌드:

```bash
npm run build
```

빌드 결과물은 다음 경로에 생성됩니다.

```text
dist/
```

빌드 결과물을 로컬에서 확인하려면 다음 명령을 사용합니다.

```bash
npm run preview
```

preview 서버 기본 주소는 Vite가 출력하는 로컬 URL을 확인합니다. 일반적으로 `http://localhost:4173`입니다.

## 테스트 방법

현재 저장소에는 별도의 자동화 테스트 스크립트가 정의되어 있지 않습니다. `package.json`의 scripts는 다음과 같습니다.

```text
dev      Vite 개발 서버 실행
build    프로덕션 번들 생성
preview  빌드 결과물 미리보기
```

따라서 현재 기준의 기본 검증 절차는 다음과 같습니다.

```bash
npm ci
npm run build
npm run preview
```

수동 검증 항목:

- 로그인/회원가입 화면이 표시되는지 확인
- 보호자, 치료사, 관리자 계정으로 로그인 후 역할별 화면 진입 확인
- `/api` 요청이 백엔드로 전달되는지 브라우저 Network 탭에서 확인
- `/app`, `/app/analysis`, `/app/offline`, `/app/reports`, `/app/admin/...` 화면 새로고침 시 SPA fallback이 동작하는지 확인
- 모바일 폭에서 보호자/치료사/관리자 사이드바 메뉴를 열고 닫을 수 있는지 확인

자동화 테스트를 추가할 경우 `package.json`에 `test` 스크립트를 추가하고 이 섹션을 갱신해야 합니다.

## 샘플 데이터

프론트엔드 저장소에는 애플리케이션 동작을 위한 별도 샘플 데이터 파일이 포함되어 있지 않습니다.

화면에 표시되는 사용자, 아동, 게임 기록, 대화 기록, 오프라인 미션, 리포트, 알림, 관리자 데이터는 백엔드 API와 데이터베이스에서 조회합니다. 샘플 시나리오나 DB 초기 데이터가 필요한 경우 백엔드 저장소의 README.md와 `src/main/resources/data`를 참고합니다.

## 데이터베이스 및 데이터 사용

프론트엔드는 직접 데이터베이스에 연결하지 않습니다. 모든 영속 데이터는 백엔드 API를 통해 사용합니다.

주요 API 데이터:

- 사용자 인증 및 사용자 역할
- 아동 프로필과 보호자/치료사 권한
- 표정 학습 요약, 기록, 히스토리
- 대화 학습 진행도, 기록, 히스토리
- 오프라인 미션 목록, 상세, 제출, 검토
- AI 리포트 목록과 상세
- 알림 및 알림 설정
- 관리자 RAG, 시나리오, Realtime 설정

브라우저에는 인증 흐름과 사용자 경험을 위해 최소한의 클라이언트 상태가 저장될 수 있습니다. 실제 업무 데이터의 원본은 백엔드와 데이터베이스입니다.

## 사용한 오픈소스

이 프로젝트의 주요 오픈소스 및 외부 라이브러리는 다음과 같습니다.

| 이름 | 용도 |
| --- | --- |
| React | UI 컴포넌트와 화면 상태 관리 |
| React DOM | React 브라우저 렌더링 |
| React Router DOM | SPA 라우팅 |
| Vite | 개발 서버, 번들링, preview |
| Tailwind CSS | 유틸리티 기반 스타일링 |
| Recharts | 통계/차트 시각화 |
| npm | 패키지 설치 및 스크립트 실행 |

정확한 버전은 `package.json`과 `package-lock.json`에 고정되어 있습니다.

## 배포 참고

이 프로젝트는 SPA입니다. 정적 호스팅에 배포할 때 `/app/...` 같은 하위 경로에서 새로고침해도 `index.html`로 fallback되어야 합니다.

Netlify 계열 배포를 위해 다음 파일이 포함되어 있습니다.

```text
public/_redirects
```

다른 정적 호스팅을 사용하는 경우에도 모든 프론트 라우트가 `index.html`로 fallback되도록 서버 rewrite 설정을 추가해야 합니다.

## 재현 절차 요약

저장소를 clone한 뒤 프론트를 다시 생성하는 최소 절차는 다음과 같습니다.

```bash
git clone <repository-url>
cd myexpressionfriend-front
npm ci
cp .env.example .env
npm run build
npm run preview
```

통합 제출 레포에서 사용하는 경우:

```bash
git clone <repository-url>
cd <repository>/frontend
npm ci
cp .env.example .env
npm run build
npm run preview
```

Windows PowerShell에서 `.env` 파일을 생성할 때는 다음 명령을 사용할 수 있습니다.

```powershell
Copy-Item .env.example .env
```

실행 후 브라우저에서 개발 서버 주소 또는 preview 서버 주소로 접속합니다.


</details>

<details>
<summary>Backend</summary>

# MyExpressionFriend API

MyExpressionFriend API는 아동의 표현 학습, 대화 학습, 오프라인 미션, 보호자/치료사 리포트 기능을 제공하는 Spring Boot 기반 백엔드 서버입니다. 보호자 앱, 치료사 화면, Unity 학습 클라이언트가 사용할 수 있는 REST API와 인증, 통계, 알림, RAG 기반 생성 기능을 포함합니다.

## 프로젝트 개요

이 서버는 다음 기능을 담당합니다.

- 이메일/비밀번호 기반 회원가입 및 로그인, JWT 인증, refresh token 쿠키 관리
- 보호자, 치료사, 관리자 역할 기반 접근 제어
- 아동 정보 등록, 수정, 삭제 및 보호자/치료사 권한 관리
- 표정 학습 및 대화 학습 결과 저장
- 아동별 홈 화면, 기록, 참여도, 주간 진행도, 통계 조회
- 오프라인 미션 생성, 제출, 검토
- 치료사 리포트 및 AI 리포트 생성/조회
- 알림 및 알림 설정 관리
- 시나리오 데이터 관리, 승인, Unity 클라이언트용 학습 데이터 제공
- OpenAI Realtime client secret 발급
- RAG 문서 색인, 검색, 프롬프트 기반 텍스트 생성 지원

## 기술 스택

- Java 17
- Spring Boot 3.5.12
- Gradle Wrapper
- Spring Security
- Spring Data JPA / JDBC
- PostgreSQL 17
- pgvector
- Spring AI
- Google GenAI Java SDK
- OpenAI Realtime API 연동 설정
- Springdoc OpenAPI / Swagger UI
- Lombok
- Caffeine Cache
- JUnit 5 / Spring Boot Test

## 소스 코드 구조

```text
src/main/java/myexpressionfriend_api
├── admin          관리자용 사용자/테스트 데이터 API
├── auth           회원가입, 로그인, 사용자, refresh token
├── child          아동 정보, 보호자/치료사 권한, PIN 관리
├── common         공통 설정, 예외 처리, 파일 저장, LLM 클라이언트
├── game           표정/대화 학습 결과 저장 및 학습 이력
├── homework       오프라인 미션 생성, 제출, 검토
├── notification   알림, 알림 이벤트, 알림 설정
├── player         게임 플레이어 선택 정보
├── rag            RAG 문서 색인, 검색, 생성 API
├── realtime       OpenAI Realtime client secret 발급
├── report         보호자/치료사용 리포트
├── scenario       대화 시나리오 데이터, 승인, Unity 제공 API
├── security       JWT 필터, 인증 실패/권한 실패 핸들러
└── statistics     홈/기록 화면용 통계, 대화 오류 패턴 분석
```

주요 리소스 파일은 다음과 같습니다.

```text
src/main/resources/application.properties       공통 설정
src/main/resources/application-dev.properties   로컬 개발 설정
src/main/resources/prompts/rag                  RAG/리포트/미션 생성 프롬프트
src/main/resources/data/scenarios               샘플 시나리오 JSON
docker/postgres/init                            PostgreSQL 확장 초기화 SQL
docs                                           추가 SQL 패치 문서
```

## 설치 방법

### 1. 필수 프로그램

다음 프로그램이 설치되어 있어야 합니다.

- JDK 17 이상
- Docker Desktop 또는 Docker Engine
- Git

Gradle은 별도로 설치하지 않아도 됩니다. 저장소에 포함된 Gradle Wrapper(`gradlew`, `gradlew.bat`)를 사용합니다.

### 2. 저장소 클론

```bash
git clone <repository-url>
cd myexpressionfriend-api
```

### 3. PostgreSQL 실행

Docker Compose로 PostgreSQL과 pgvector 환경을 실행합니다.

```bash
docker compose up -d
```

`compose.yaml`은 다음 DB를 생성합니다.

- host port: `5433`
- database: `MyExpressionFriend`
- username: `myuser`
- password: `mypassword`

초기 DB 생성 시 `docker/postgres/init/001_pgvector_extensions.sql`이 실행되어 `vector`, `hstore`, `uuid-ossp` 확장이 생성됩니다. 이미 기존 Docker 볼륨이 만들어진 뒤라면 init SQL이 다시 실행되지 않으므로, 확장이 필요한 경우 DB에서 직접 SQL을 실행하거나 볼륨을 새로 만들어야 합니다.

### 4. 환경 변수 설정

로컬에서 Docker DB를 사용할 때는 아래 환경 변수를 설정합니다.

Windows PowerShell:

```powershell
$env:SPRING_DATASOURCE_URL="jdbc:postgresql://localhost:5433/MyExpressionFriend"
$env:SPRING_DATASOURCE_USERNAME="myuser"
$env:SPRING_DATASOURCE_PASSWORD="mypassword"
$env:JWT_SECRET="change-this-secret-to-a-long-random-value"
```

macOS/Linux:

```bash
export SPRING_DATASOURCE_URL="jdbc:postgresql://localhost:5433/MyExpressionFriend"
export SPRING_DATASOURCE_USERNAME="myuser"
export SPRING_DATASOURCE_PASSWORD="mypassword"
export JWT_SECRET="change-this-secret-to-a-long-random-value"
```

선택 환경 변수:

| 변수 | 설명 | 기본값 |
| --- | --- | --- |
| `PORT` | 서버 포트 | `8080` |
| `SPRING_PROFILE` | Spring profile | `dev` |
| `APP_CORS_ALLOWED_ORIGIN_PATTERNS` | CORS 허용 origin | `http://localhost:5173` |
| `APP_STORAGE_BASE_PATH` | 업로드 파일 저장 경로 | `uploads` |
| `LLM_ENABLED` | LLM 기능 사용 여부 | `false` |
| `OPENAI_REALTIME_ENABLED` | OpenAI Realtime 발급 API 사용 여부 | `false` |
| `OPENAI_API_KEY` | OpenAI API key | 없음 |
| `OLLAMA_BASE_URL` | Ollama embedding 서버 주소 | `http://localhost:11434` |
| `OLLAMA_EMBEDDING_MODEL` | RAG embedding 모델 | `qwen3-embedding` |

기본 `dev` 프로파일은 `spring.jpa.hibernate.ddl-auto=update`를 사용하므로, 로컬 개발 환경에서는 애플리케이션 실행 시 JPA 엔티티 기준으로 테이블이 생성/갱신됩니다.

## 실행 방법

Windows:

```powershell
.\gradlew.bat bootRun
```

macOS/Linux:

```bash
./gradlew bootRun
```

서버 실행 후 다음 주소를 사용할 수 있습니다.

- API 서버: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger-ui.html`
- OpenAPI JSON: `http://localhost:8080/v3/api-docs`
- Health check: `http://localhost:8080/actuator/health`

## 빌드 방법

Windows:

```powershell
.\gradlew.bat clean build
```

macOS/Linux:

```bash
./gradlew clean build
```

빌드 결과 JAR 파일은 `build/libs/` 아래에 생성됩니다.

테스트를 제외하고 실행 가능한 JAR만 생성하려면 다음 명령을 사용할 수 있습니다.

```bash
./gradlew clean bootJar -x test
```

Windows에서는 같은 명령을 `.\gradlew.bat`으로 실행하면 됩니다.

## 테스트 방법

전체 테스트:

```bash
./gradlew test
```

Windows:

```powershell
.\gradlew.bat test
```

테스트 리포트는 다음 경로에서 확인할 수 있습니다.

```text
build/reports/tests/test/index.html
```

일부 통합 테스트나 애플리케이션 컨텍스트 테스트는 PostgreSQL 연결 설정이 필요할 수 있습니다. 테스트 전에 `docker compose up -d`로 DB를 실행하고, 설치 방법의 환경 변수를 동일하게 설정합니다.

## 샘플 데이터

샘플 시나리오 데이터는 다음 위치에 포함되어 있습니다.

```text
src/main/resources/data/scenarios/Seoyeon_batch_fixed.json
```

이 파일은 대화 학습 시나리오 초기 데이터로 사용할 수 있는 JSON 데이터입니다. 애플리케이션의 시나리오 로더 또는 관리자용 시나리오 API를 통해 개발/테스트 환경에서 사용할 수 있습니다.

RAG, 리포트, 오프라인 미션 생성에 사용하는 프롬프트 템플릿은 다음 위치에 있습니다.

```text
src/main/resources/prompts/rag/
```

업로드 파일과 시나리오 export 파일은 기본적으로 다음 경로에 저장됩니다.

```text
uploads/
uploads/exports/scenarios/
```

## 데이터베이스

이 프로젝트는 PostgreSQL을 기본 데이터베이스로 사용합니다. 주요 데이터는 JPA 엔티티로 관리됩니다.

- 사용자, 역할, refresh token
- 아동 프로필 및 보호자/치료사 권한
- 표정 학습 세션과 시도 기록
- 대화 학습 세션과 턴 기록
- 시나리오, 대화 턴, 선택지
- 오프라인 미션과 제출/검토 기록
- 리포트
- 알림 및 알림 설정
- 통계 요약
- RAG 문서 메타데이터 및 vector store

`dev` 프로파일은 로컬 개발 편의를 위해 JPA `ddl-auto=update`를 사용합니다. 기본 프로파일의 공통 설정은 `ddl-auto=validate`이므로, 운영 또는 배포 환경에서는 DB 스키마가 엔티티와 일치해야 합니다.

추가 SQL 문서는 `docs/`에 있습니다.

```text
docs/add-statistics-trend-confidence-columns.sql
docs/fix-dialogue-offline-outcome-counts.sql
```

## 사용한 오픈소스

이 프로젝트의 주요 오픈소스 및 외부 라이브러리는 다음과 같습니다.

| 이름 | 용도 |
| --- | --- |
| Spring Boot | 웹 서버, 설정, 의존성 관리 |
| Spring Security | 인증/인가 |
| Spring Data JPA | ORM 및 repository |
| PostgreSQL JDBC Driver | PostgreSQL 연결 |
| pgvector | vector embedding 저장 및 검색 |
| Spring AI | Ollama embedding, pgvector vector store 연동 |
| Springdoc OpenAPI | Swagger UI 및 OpenAPI 문서 |
| JJWT | JWT 생성 및 검증 |
| Lombok | DTO/엔티티 보일러플레이트 감소 |
| Caffeine | 로컬 캐시 |
| Apache PDFBox | PDF 문서 처리 |
| Google GenAI Java SDK | Gemini 계열 LLM 호출 |
| JUnit 5 / Spring Boot Test | 테스트 |

## 재현 절차 요약

저장소를 clone한 뒤 서버를 재생성하는 최소 절차는 다음과 같습니다.

```bash
git clone <repository-url>
cd myexpressionfriend-api
docker compose up -d
```

Windows PowerShell:

```powershell
$env:SPRING_DATASOURCE_URL="jdbc:postgresql://localhost:5433/MyExpressionFriend"
$env:SPRING_DATASOURCE_USERNAME="myuser"
$env:SPRING_DATASOURCE_PASSWORD="mypassword"
$env:JWT_SECRET="change-this-secret-to-a-long-random-value"
.\gradlew.bat clean build
.\gradlew.bat bootRun
```

macOS/Linux:

```bash
export SPRING_DATASOURCE_URL="jdbc:postgresql://localhost:5433/MyExpressionFriend"
export SPRING_DATASOURCE_USERNAME="myuser"
export SPRING_DATASOURCE_PASSWORD="mypassword"
export JWT_SECRET="change-this-secret-to-a-long-random-value"
./gradlew clean build
./gradlew bootRun
```

실행 후 `http://localhost:8080/swagger-ui.html`에서 API 목록을 확인할 수 있습니다.


</details>

<details>
<summary>Documents</summary>

기타 문서 설명...

</details>
