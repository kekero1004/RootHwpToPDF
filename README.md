# RootHwpToPDF
한글문서를 PDF로 변환하는 SoftWare 입니다. 


# HWP to PDF 변환기

## 소개

HWP to PDF 변환기는 한글(HWP) 문서를 PDF 형식으로 변환하는 도구입니다. 한글 프로그램이 설치된 Windows 환경에서 작동하며, 간편한 GUI 인터페이스와 명령줄(CLI) 옵션을 모두 제공합니다.

### 주요 기능
- 한글(HWP, HWPX) 파일을 PDF로 변환
- 단일 파일 또는 여러 파일 일괄 변환
- 폴더 내 모든 한글 파일 검색 및 변환
- 워터마크 추가 기능
- PDF 압축 레벨 조정
- 페이지 범위 지정 변환

## 요구 사항

- Windows 7 이상
- .NET 8.0 런타임 (자체 포함 배포판은 필요 없음)
- 한글(한컴오피스) 설치 (2010 이상 버전)

## 설치 방법

### 방법 1: 다운로드하여 실행
1. [릴리스 페이지](https://github.com/kekero1004/hwptopdf/releases)에서 최신 버전의 `HwpToPdf.exe` 파일을 다운로드합니다.
2. 다운로드한 파일을 원하는 위치에 저장합니다.
3. 실행하기 전에 한글 프로그램이 설치되어 있는지 확인합니다.

### 방법 2: 소스 코드에서 빌드
1. 소스 코드를 클론하거나 다운로드합니다.
2. `single_exe_build.bat` 파일을 실행하여 단일 EXE 파일로 빌드합니다.
3. 또는 Visual Studio에서 프로젝트를 열고 빌드합니다.

## 사용 방법

### GUI 모드
1. `HwpToPdf.exe` 파일을 더블 클릭하여 실행합니다.
2. "파일 선택" 버튼을 클릭하여 변환할 한글 파일을 선택합니다.
3. 또는 "폴더 선택" 버튼을 클릭하여 폴더 내 모든 한글 파일을 추가합니다.
4. "PDF 저장 경로"에서 출력 폴더를 지정합니다.
5. 필요한 경우 변환 옵션을 설정합니다.
6. "변환" 버튼을 클릭하여 PDF 변환을 시작합니다.

### CLI 모드
```bash
HwpToPdf.exe 변환할파일.hwp [옵션]
HwpToPdf.exe -i 파일경로.hwp -o 출력폴더 -w 워터마크텍스트
```

#### 주요 CLI 옵션
- `-i, --input`: 입력 파일 또는 폴더 경로
- `-o, --output`: 출력 폴더 경로
- `-w, --watermark`: 워터마크 텍스트
- `-p, --password`: 비밀번호 (암호화된 HWP 파일용)
- `-c, --compression`: 압축 레벨 (0-9)
- `-r, --recursive`: 폴더 내 하위 폴더까지 검색
- `--overwrite`: 기존 파일 덮어쓰기
- `--open`: 변환 후 PDF 파일 열기

## 프로젝트 구조

```
HwpToPdf/
├── API/                  # API 관련 코드
├── CLI/                  # 명령줄 인터페이스 관련 코드
├── Controllers/          # 변환 컨트롤러
├── Models/               # 데이터 모델 클래스
│   ├── ConversionJob.cs  # 변환 작업 정보
│   ├── ConversionOptions.cs  # 변환 옵션
│   ├── FileItem.cs       # 파일 항목 모델
│   ├── HwpDocument.cs    # 한글 문서 모델
│   └── PdfOutput.cs      # PDF 출력 모델
├── Resources/            # 리소스 파일
├── Services/             # 핵심 서비스
│   ├── HwpComInteropService.cs  # 한글 COM 연동 서비스
│   ├── IHwpService.cs    # 한글 서비스 인터페이스
│   └── ITextPdfService.cs  # PDF 후처리 서비스
├── Utils/                # 유틸리티 클래스
├── ViewModels/           # MVVM 뷰모델
├── Views/                # UI 뷰
│   └── WPF/              # WPF UI 구현
├── Program.cs            # 진입점
├── HwpToPdf.csproj       # 프로젝트 파일
├── check_hwp.bat         # 한글 설치 확인 스크립트
└── single_exe_build.bat  # 단일 EXE 빌드 스크립트
```

## 변환 프로세스

1. 한글(HWP) 파일 로드
2. 한글 COM 객체를 통해 PDF로 변환
3. 필요 시 PDF 후처리 (워터마크, 압축, 페이지 추출 등)
4. 결과 PDF 파일 저장

## 빌드 방법

상세한 빌드 방법은 [how_to_build_single_exe.md](how_to_build_single_exe.md) 파일을 참조하세요.

## 문제 해결

### 한글 프로그램 관련 문제
- 한글 프로그램이 설치되어 있는지 확인하세요.
- `check_hwp.bat` 스크립트를 실행하여 한글 COM 객체가 제대로 등록되어 있는지 확인하세요.
- 한글 프로그램을 관리자 권한으로 한 번 실행한 후 다시 시도하세요.

### 변환 실패
- 한글 파일이 비밀번호로 보호되어 있는 경우, 비밀번호를 제공해야 합니다.
- 한글 파일이 손상되었거나 잘못된 형식인 경우 변환이 실패할 수 있습니다.
- 한글 문서에 특수한 기능이나 매크로가 포함된 경우 변환에 문제가 있을 수 있습니다.

### 기타 문제
- 로그 파일은 `%LOCALAPPDATA%\HwpToPdf\logs` 폴더에 저장됩니다.
- 문제 해결에 도움이 필요한 경우 로그 파일을 확인하세요.

## 라이선스

이 프로젝트는 MIT 라이선스에 따라 배포됩니다. 자세한 내용은 LICENSE 파일을 참조하세요. 
