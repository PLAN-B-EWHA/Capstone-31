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
