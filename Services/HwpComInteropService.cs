using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HwpToPdf.Models;
using HwpToPdf.Utils;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;

namespace HwpToPdf.Services
{
    /// <summary>
    /// 한컴 오피스 COM Interop을 사용한 HWP 서비스 구현
    /// </summary>
    public class HwpComInteropService : IHwpService, IDisposable
    {
        private dynamic _hwpApplication;
        private bool _disposed = false;
        private const string HWP_SECURITY_KEY_PATH = @"Software\HNC\HwpAutomation\Modules";

        public HwpComInteropService()
        {
            try
            {
                // 먼저 레지스트리 설정을 시도
                ConfigureSecurityRegistry();
                
                // 한글 애플리케이션 초기화
                InitializeHwpApplication();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("한컴 오피스 초기화 실패", ex);
                throw new InvalidOperationException("한컴 오피스를 초기화할 수 없습니다. 올바르게 설치되어 있는지 확인하세요.", ex);
            }
        }

        private void ConfigureSecurityRegistry()
        {
            try
            {
                // 한글 보안 경고 비활성화를 위한 레지스트리 설정
                using (var key = Registry.CurrentUser.CreateSubKey(HWP_SECURITY_KEY_PATH))
                {
                    if (key != null)
                    {
                        // 파일 접근 보안 경고 비활성화
                        key.SetValue("FilePathCheckModule", 0);
                        key.SetValue("FilePathAccessModule", 0);
                        
                        // 추가 보안 설정 (자동 허용)
                        key.SetValue("AllowFileAccess", 1);
                        key.SetValue("AllowNetworkAccess", 1);
                        key.SetValue("ModuleSettings", 1);
                        LogHelper.LogInfo("한글 보안 설정을 위한 레지스트리 설정 완료");
                    }
                }
                
                // 추가 레지스트리 설정 - 모두 허용 기본값 설정
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\HNC\HwpAutomation\Security"))
                    {
                        if (key != null)
                        {
                            key.SetValue("OpenAccessFromWeb", 0);
                            key.SetValue("AccessFromWebSecurity", 0);
                            key.SetValue("OpenOtherFileAccess", 0);
                            key.SetValue("FilePathAccessSecurity", 0);
                            LogHelper.LogInfo("한글 추가 보안 설정 완료 (모두 허용)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("추가 레지스트리 설정 중 오류 발생", ex);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("레지스트리 설정 중 오류 발생", ex);
                // 계속 진행 (레지스트리 설정 실패해도 다음 단계 시도)
            }
        }

        public async Task<HwpDocument> LoadDocumentAsync(string filePath, string password = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 한글 문서 객체 생성
                    var hwpDocument = new HwpDocument(filePath);
                    hwpDocument.Password = password;

                    // 보안 설정 재적용
                    ApplySecuritySettings();

                    // 한글 문서 열기
                    int result = 0;

                    try
                    {
                        if (!string.IsNullOrEmpty(password))
                        {
                            // 비밀번호가 있는 경우
                            result = _hwpApplication.Open(filePath, "HWP", password);
                        }
                        else
                        {
                            // 비밀번호가 없는 경우
                            result = _hwpApplication.Open(filePath, "HWP", "");
                        }
                    }
                    catch (COMException)
                    {
                        // 비밀번호 관련 오류일 가능성이 높음
                        hwpDocument.IsPasswordProtected = true;
                        throw new UnauthorizedAccessException("문서가 비밀번호로 보호되어 있습니다.");
                    }

                    if (result != 0)
                    {
                        throw new IOException($"한글 문서를 열 수 없습니다. 오류 코드: {result}");
                    }

                    return hwpDocument;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"문서 로드 중 오류 발생: {filePath}", ex);
                    throw;
                }
            });
        }

        public async Task<bool> ValidateDocumentAsync(HwpDocument document)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return document.Validate();
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"문서 검증 중 오류 발생: {document.FilePath}", ex);
                    return false;
                }
            });
        }

        public async Task<PdfOutput> ConvertToPdfAsync(HwpDocument document, ConversionOptions options)
        {
            return await Task.Run(() =>
            {
                var startTime = DateTime.Now;
                string outputPath = Path.Combine(
                    options.OutputDirectory,
                    Path.GetFileNameWithoutExtension(document.FileName) + ".pdf"
                );

                // 덮어쓰기 설정에 따라 출력 경로 조정
                if (!options.OverwriteExisting && File.Exists(outputPath))
                {
                    int counter = 1;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(document.FileName);
                    
                    while (File.Exists(outputPath))
                    {
                        outputPath = Path.Combine(
                            options.OutputDirectory,
                            $"{fileNameWithoutExt}_{counter}.pdf"
                        );
                        counter++;
                    }
                }

                var pdfOutput = new PdfOutput(outputPath);
                pdfOutput.FilePath = document.FilePath; // 원본 파일 경로 설정

                try
                {
                    // 기존 파일 삭제 (덮어쓰기 위해)
                    if (File.Exists(outputPath))
                    {
                        try
                        {
                            File.Delete(outputPath);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError($"기존 PDF 파일 삭제 실패: {outputPath}", ex);
                        }
                    }

                    // 보안 설정 재적용
                    ApplySecuritySettings();

                    // 문서 열기 전 기존 문서 닫기
                    try
                    {
                        _hwpApplication.Clear(1);
                        LogHelper.LogInfo("기존 문서 닫기 성공");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("기존 문서 닫기 실패", ex);
                    }

                    // 한글 문서 열기
                    try
                    {
                        dynamic openResult;
                        if (!string.IsNullOrEmpty(document.Password))
                        {
                            openResult = _hwpApplication.Open(document.FilePath, "HWP", document.Password);
                        }
                        else
                        {
                            openResult = _hwpApplication.Open(document.FilePath, "HWP", "");
                        }

                        // 열기 결과 확인 (bool 또는 int 타입 모두 처리)
                        bool openSuccess = false;
                        
                        if (openResult is bool)
                        {
                            openSuccess = (bool)openResult;
                        }
                        else if (openResult is int)
                        {
                            openSuccess = ((int)openResult == 0);
                        }
                        else 
                        {
                            // 다른 타입의 경우 문자열로 변환 후 확인
                            string resultStr = Convert.ToString(openResult);
                            openSuccess = (resultStr == "0" || resultStr.ToLower() == "true");
                        }

                        if (!openSuccess)
                        {
                            LogHelper.LogError($"한글 문서 열기 실패. 결과: {openResult}");
                            throw new IOException($"한글 문서를 열 수 없습니다. 결과: {openResult}");
                        }
                        
                        LogHelper.LogInfo($"한글 문서 열기 성공: {document.FilePath}");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError($"한글 문서 열기 중 예외 발생: {document.FilePath}", ex);
                        throw;
                    }

                    // PDF로 저장 시도 (여러 방법 적용)
                    bool conversionSuccess = false;
                    Exception lastException = null;

                    // 방법 1: 표준 SaveAs 사용
                    if (!conversionSuccess)
                    {
                        try
                        {
                            LogHelper.LogInfo("PDF 변환 방법 1 시도 (표준 SaveAs)");
                            var saveResult = _hwpApplication.SaveAs(outputPath, "PDF", "");
                            int result = ConvertSaveResultToInt(saveResult);
                            
                            // 0 또는 1은 성공으로 간주
                            if (result == 0 || result == 1)
                            {
                                LogHelper.LogInfo($"PDF 변환 방법 1 성공, 반환 코드: {result}");
                                conversionSuccess = true;
                            }
                            else
                            {
                                LogHelper.LogWarning($"PDF 변환 방법 1 실패, 반환 코드: {result}");
                            }
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            LogHelper.LogError("PDF 변환 방법 1 실패", ex);
                        }
                    }

                    // 방법 2: SaveAs with SaveOption (한글 2022 지원)
                    if (!conversionSuccess)
                    {
                        try
                        {
                            LogHelper.LogInfo("PDF 변환 방법 2 시도 (SaveOption 사용)");
                            dynamic hwpSaveOption = _hwpApplication.CreateSaveOption();
                            var saveResult = _hwpApplication.SaveAs(outputPath, "PDF", hwpSaveOption);
                            int result = ConvertSaveResultToInt(saveResult);
                            
                            // 0 또는 1은 성공으로 간주
                            if (result == 0 || result == 1)
                            {
                                LogHelper.LogInfo($"PDF 변환 방법 2 성공, 반환 코드: {result}");
                                conversionSuccess = true;
                            }
                            else
                            {
                                LogHelper.LogWarning($"PDF 변환 방법 2 실패, 반환 코드: {result}");
                            }
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            LogHelper.LogError("PDF 변환 방법 2 실패", ex);
                        }
                    }

                    // 방법 3: FileSave API 사용
                    if (!conversionSuccess)
                    {
                        try
                        {
                            LogHelper.LogInfo("PDF 변환 방법 3 시도 (FileSave API)");
                            var saveResult = _hwpApplication.FileSave(outputPath, "PDF");
                            int result = ConvertSaveResultToInt(saveResult);
                            
                            // 0 또는 1은 성공으로 간주
                            if (result == 0 || result == 1)
                            {
                                LogHelper.LogInfo($"PDF 변환 방법 3 성공, 반환 코드: {result}");
                                conversionSuccess = true;
                            }
                            else
                            {
                                LogHelper.LogWarning($"PDF 변환 방법 3 실패, 반환 코드: {result}");
                            }
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            LogHelper.LogError("PDF 변환 방법 3 실패", ex);
                        }
                    }

                    // 방법 4: 프린트 메서드 사용 (가상 프린터 사용)
                    if (!conversionSuccess)
                    {
                        try
                        {
                            LogHelper.LogInfo("PDF 변환 방법 4 시도 (Print API)");
                            
                            // 인쇄 환경 설정
                            dynamic printInfo = _hwpApplication.GetCurrentPrintSettings();
                            printInfo.SetPrintToFilePermit(1);  // 파일로 인쇄 허용
                            printInfo.SetFileName(outputPath);  // 출력 파일명 설정
                            printInfo.SetFileFormat(0);         // PDF 형식 (0: PDF, 1: JPG, 2: BMP...)
                            
                            // 프린트 실행
                            _hwpApplication.Print(printInfo, true);
                            
                            LogHelper.LogInfo("PDF 변환 방법 4 성공");
                            conversionSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            LogHelper.LogError("PDF 변환 방법 4 실패", ex);
                        }
                    }

                    // 문서 닫기 (변경사항 저장 안 함)
                    try
                    {
                        _hwpApplication.Clear(1);
                        LogHelper.LogInfo("문서 닫기 성공");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("문서 닫기 실패", ex);
                    }

                    // 결과 확인 및 변환 성공/실패 처리
                    bool fileExists = File.Exists(outputPath);
                    bool validFile = false;
                    
                    if (fileExists)
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(outputPath);
                            validFile = fileInfo.Length > 100; // 최소 100바이트 이상이어야 유효한 PDF로 간주
                            LogHelper.LogInfo($"PDF 파일 생성됨: {outputPath}, 크기: {fileInfo.Length} 바이트");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError($"PDF 파일 검증 실패: {outputPath}", ex);
                        }
                    }

                    // 최종 결과 처리
                    if (validFile)
                    {
                        // 성공
                        pdfOutput.SetSuccess(DateTime.Now - startTime);
                        return pdfOutput;
                    }
                    else
                    {
                        // 실패 - 자세한 오류 정보 제공
                        string errorDetail = fileExists 
                            ? "PDF 파일이 생성되었으나 유효하지 않습니다." 
                            : "PDF 파일이 생성되지 않았습니다.";
                        
                        LogHelper.LogError($"PDF 변환 실패: {errorDetail}");
                        
                        // 마지막 예외가 있으면 사용, 없으면 일반 오류 메시지 사용
                        string errorMessage = lastException != null 
                            ? lastException.Message 
                            : errorDetail;
                        
                        pdfOutput.SetFailed(errorMessage, DateTime.Now - startTime);
                        return pdfOutput;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"PDF 변환 중 오류 발생: {document.FilePath}", ex);
                    pdfOutput.SetFailed(ex.Message, DateTime.Now - startTime);
                    return pdfOutput;
                }
            });
        }

        private int ConvertSaveResultToInt(dynamic saveResult)
        {
            try
            {
                return Convert.ToInt32(saveResult);
            }
            catch
            {
                // bool 타입인 경우 true는 1, false는 0으로 변환
                if (saveResult is bool)
                {
                    return (bool)saveResult ? 1 : 0;
                }
                // 그 외의 경우는 -1 (알 수 없는 결과)
                return -1;
            }
        }

        private void ApplySecuritySettings()
        {
            try
            {
                // 보안 설정 메서드는 한글 버전에 따라 존재 여부가 다를 수 있으므로
                // 호출 시 오류가 발생해도 계속 진행합니다.
                
                // 1. 메시지 박스 모드 설정 (메시지 창 숨김)
                try
                {
                    _hwpApplication.SetMessageBoxMode(0x10000);
                    LogHelper.LogInfo("메시지 박스 모드 설정 성공");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("메시지 박스 모드 설정 실패", ex);
                }
                
                // 2. 그 외 추가 보안 설정 시도
                try
                {
                    // 자동화 접근 제어 시도
                    Type hwpType = _hwpApplication.GetType();
                    hwpType.InvokeMember("AllowAccessFromOtherApplication", 
                        System.Reflection.BindingFlags.InvokeMethod, 
                        null, _hwpApplication, new object[] { 1 });
                    LogHelper.LogInfo("자동화 접근 제어 설정 성공");
                }
                catch
                {
                    // 무시 - 일부 버전에서는 지원하지 않을 수 있음
                }
                
                // 3. 자동 매크로 실행 허용 설정
                try
                {
                    _hwpApplication.AutomationSecurityLevel = 1; // 낮음 (자동 허용)
                    LogHelper.LogInfo("자동화 보안 수준 설정 성공");
                }
                catch
                {
                    // 무시 - 일부 버전에서는 지원하지 않을 수 있음
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("보안 설정 적용 중 오류", ex);
            }
        }

        public async Task<bool> IsPasswordProtectedAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 보안 설정 적용
                    ApplySecuritySettings();
                    
                    // 한글 문서 열기 시도
                    var result = _hwpApplication.Open(filePath, "HWP", "");
                    
                    // 문서 닫기
                    _hwpApplication.Clear(1);
                    
                    // 열기 성공 시 비밀번호 없음
                    return false;
                }
                catch
                {
                    // 열기 실패 시 비밀번호 있을 가능성 높음
                    return true;
                }
            });
        }

        public async Task CloseDocumentAsync(HwpDocument document)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 현재 문서 닫기
                    _hwpApplication.Clear(1);
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"문서 닫기 중 오류 발생: {document.FilePath}", ex);
                    throw;
                }
            });
        }

        private void InitializeHwpApplication()
        {
            try
            {
                // 한글 애플리케이션 초기화 - 여러 버전 지원
                Type hwpType = null;
                
                // 한글 버전 순서대로 시도 (최신 버전부터)
                string[] progIDs = new string[] 
                {
                    "HWPFrame.HwpObject.1",     // 한글 2022 이상 버전
                    "HWPFrame.HwpObject"        // 이전 버전
                };
                
                foreach (string progID in progIDs)
                {
                    hwpType = Type.GetTypeFromProgID(progID);
                    if (hwpType != null)
                    {
                        LogHelper.LogInfo($"한글 프로그램 발견: {progID}");
                        break;
                    }
                }
                
                if (hwpType == null)
                {
                    LogHelper.LogError("HWPFrame.HwpObject 타입을 찾을 수 없습니다. 한글이 설치되어 있지 않거나 COM 등록이 되지 않았습니다.");
                    throw new InvalidOperationException("한글이 설치되어 있지 않습니다. 한글 프로그램을 설치한 후 다시 시도하세요.");
                }

                // 한글 실행 시 명령줄 인수 지정 (보안 경고 비활성화)
                try
                {
                    object[] args = new object[] { "/nowarning", "/safe" };
                    _hwpApplication = Activator.CreateInstance(hwpType, args);
                }
                catch
                {
                    // 명령줄 인수를 지원하지 않는 경우 기본 방식으로 생성
                    _hwpApplication = Activator.CreateInstance(hwpType);
                }
                
                if (_hwpApplication == null)
                {
                    LogHelper.LogError("한글 객체를 생성할 수 없습니다.");
                    throw new InvalidOperationException("한글 객체를 생성할 수 없습니다. 한글 프로그램을 다시 설치해 보세요.");
                }

                // 백그라운드 실행 설정
                try
                {
                    // 보안 설정 적용
                    ApplySecuritySettings();
                    
                    // 화면에 표시 안 함
                    _hwpApplication.XHwpWindows.Active_XHwpWindow.Visible = 0;
                    
                    // 모듈 등록
                    _hwpApplication.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule");
                    
                    LogHelper.LogInfo("한글 애플리케이션 초기화 성공");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("한글 응용 프로그램 설정 중 오류 발생", ex);
                    throw new InvalidOperationException("한글 프로그램을 초기화하는 중 오류가 발생했습니다.", ex);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("한글 애플리케이션 초기화 중 예외 발생", ex);
                throw new InvalidOperationException("한글 프로그램 초기화 중 오류가 발생했습니다: " + ex.Message, ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 관리형 리소스 해제
                }

                // 비관리형 리소스 해제
                if (_hwpApplication != null)
                {
                    try
                    {
                        _hwpApplication.Clear(1);
                        _hwpApplication.Quit();
                        LogHelper.LogInfo("한글 애플리케이션 종료 성공");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("한글 종료 중 오류 발생", ex);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(_hwpApplication);
                        _hwpApplication = null;
                    }
                }

                _disposed = true;
            }
        }

        ~HwpComInteropService()
        {
            Dispose(false);
        }
    }
} 