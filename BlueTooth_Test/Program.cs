using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Bluetooth.AttributeIds;
using InTheHand.Net.Sockets;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueTooth_Test
{
    /// <summary>
    /// 프로그램 진입점
    /// </summary>
    class Program
    {
        /// <summary>
        /// 로거
        /// </summary>
        private static log4net.ILog logger = null;

        /// <summary>
        /// 서비스 데몬용 타이머
        /// </summary>
        private static Timer loopTimer = null;

        /// <summary>
        /// 프로그램 종료 플래그
        /// </summary>
        private static bool shutDown = false;

        /// <summary>
        /// 서비스 데몬 검사주기 (1초)
        /// </summary>
        private static int loopTimerDuration { get; set; } = 1000 * 1;

        /// <summary>
        /// UUID 값
        /// </summary>
        private static Guid uuid = new Guid("00002415-0000-1000-8000-00805F9B34FB");

        /// <summary>
        /// 주소값
        /// </summary>
        //private static string Address { get; set; } = "00:CD:FE:6F:D5:86";
        private static string Address { get; set; } = "AC:5A:14:10:4F:63";


        /// <summary>
        /// 블루투스 
        /// </summary>
        private static IAsyncResult iasyncResult;

        /// <summary>
        /// 블루투스 주소
        /// </summary>
        private static BluetoothAddress bluetoothAddress;

        /// <summary>
        /// 블루투스 디바이스 정보
        /// </summary>
        private static BluetoothDeviceInfo bluetoothDeviceInfo;

        /// <summary>
        /// 락 카운트
        /// </summary>
        private static int lockCount;


        /// <summary>
        /// 모델 0: 안드로이드 1: 아이폰
        /// </summary>
        private static readonly int model = 0;


        /// <summary>
        /// 메인클래스
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                // 로그 세팅
                string modelType = (model == 0 ? "안드로이드" : "아이폰");
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                XmlConfigurator.Configure(new System.IO.FileInfo($@"{appPath}\log4net.xml"));
                logger = LogManager.GetLogger(typeof(Program));

                logger.Info($"로그기록 시작 {DateTime.Now.ToString()} =====");
                logger.Info($"로그모델 타입:{modelType}");
                logger.Info($"맥어드레스 입력");
                string addr = Console.ReadLine();

                Address = addr;

                Service();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }


        /// <summary>
        /// 블루투스 서비스를 테스트 한다
        /// </summary>
        private static void Service()
        {
            try
            {               
                // 타이머 실행
                loopTimer = new Timer(OnServiceTimer, null, loopTimerDuration, Timeout.Infinite);

                // 쓰레딩 유지
                Task maintain = new Task(delegate ()
                {
                    while (!shutDown)
                    {
                        Thread.Sleep(1000);
                    }
                });

                maintain.Start();
                maintain.Wait();

                // 타이머 중지
                loopTimer.Change(Timeout.Infinite, Timeout.Infinite);
                loopTimer.Dispose();
                loopTimer = null;

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                if (loopTimer != null)
                {
                    // 타이머 중지
                    loopTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    loopTimer.Dispose();
                    loopTimer = null;
                }
            }
        }



        /// <summary>
        /// 블루투스 서비스 검사 타이머
        /// </summary>
        /// <param name="state"></param>
        private static void OnServiceTimer(object state)
        {
            try
            {
                // 타이머 중지
                loopTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // 종료중이 아닐경우
                if (!shutDown)
                    BlueToothTest();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                // 종료중이 아닌경우
                if (!shutDown)
                    loopTimer.Change(loopTimerDuration, Timeout.Infinite);
            }
        }


        /// <summary>
        /// 블루투스 테스트를 진행한다
        /// </summary>
        private static void BlueToothTest()
        {
            try
            {                
                bluetoothAddress = BluetoothAddress.Parse(Address);

                // 블루투스 장치가 켜져있는지 확인한다
                while (true)
                {
                    // 장치가 켜져있지 않는경우
                    if (!BluetoothRadio.IsSupported)
                    {
                        Process.Start("bthprops.cpl");
                    }
                    // 장치가 켜져 있는 경우
                    else
                    {
                        if(bluetoothDeviceInfo == null)
                            bluetoothDeviceInfo = new BluetoothDeviceInfo(bluetoothAddress);

                        break;
                    }
                }


                // 블루투스 신호 설정
                iasyncResult = bluetoothDeviceInfo.BeginGetServiceRecords(uuid, ServiceAsyncCallBack, bluetoothDeviceInfo);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }



        /// <summary>
        /// 블루투스 신호 콜백 메서드
        /// </summary>
        /// <param name="ar"></param>
        private static void ServiceAsyncCallBack(IAsyncResult iasyncResult)
        {
            try
            {
                bluetoothDeviceInfo = iasyncResult.AsyncState as BluetoothDeviceInfo;

                if (!iasyncResult.IsCompleted)
                    logger.Info($"{DateTime.Now.ToString()} 통신실패");
                
                if (iasyncResult.IsCompleted)
                {
                    
                    // 안드로이드의 경우
                    if (model == 0)
                    {
                        try
                        {
                            ServiceRecord[] services = bluetoothDeviceInfo.EndGetServiceRecords(iasyncResult);
                            if(services.Count() > 0)
                            {
                                lockCount = 0;
                                logger.Info($"{DateTime.Now.ToString()} 통신성공");
                            }
                            else
                            {
                                logger.Info($"{DateTime.Now.ToString()} 통신실패, 카운트:{++lockCount}");

                                if(lockCount >= 3)
                                {
                                    logger.Info($"{DateTime.Now.ToString()} 라이브러리 오류, 카운트:{lockCount}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Info($"통신실패");
                            logger.Info(ex.Message);
                            throw;
                        }
                        
                    }
                    else
                    {
                        try
                        {
                            ServiceRecord[] serviceRecords = bluetoothDeviceInfo.EndGetServiceRecords(iasyncResult);
                            logger.Info($"IOS {DateTime.Now.ToString()} 통신성공");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex.Message);
                            logger.Info($"IOS {DateTime.Now.ToString()} 통신실패");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
