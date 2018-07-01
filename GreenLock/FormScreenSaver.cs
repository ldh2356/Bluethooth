using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;//.Tasks;
using System.Runtime.InteropServices;
using System.Media;
using System.Diagnostics;

namespace GreenLock
{
    public partial class FormScreenSaver : Form
    {
        int intLLKey;
        public MainForm main;
        public FormScreenSaverCancel formScreenSaverCancel;

        [DllImport("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hWnd, int time, AnimateWindowFlags flags);

        // 플래그 값
        public enum AnimateWindowFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }

        public FormScreenSaver(MainForm main)
        {
            try
            {
                InitializeComponent();
                this.main = main;
                main.SetFormScreenSaver(this);

                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                ShowInTaskbar = false;

                pb_screenSaver.BringToFront();

                // 키보드 후킹
                intLLKey = KeyboardHooking.SetHook(KeyboardHooking.hookProc);
                KeyboardHooking.BlockCtrlAltDel();
            }
            catch(Exception ex)
            {
                Console.WriteLine("ScreenSaverForm_init");
            }
        }

        // 폼 로드
        private void FormScreenSaver_Load(object sender, EventArgs e)
        {
            try
            {
                // 폼 애니메이션(위에서 아래로)
                AnimateWindow(this.Handle, 500, AnimateWindowFlags.AW_VER_POSITIVE);
            }
            catch(Exception ex)
            {
                Console.WriteLine("ScreenSaverForm");
            }
        }

        // 폼 액티베이티드
        private void FormScreenSaver_Activated(object sender, EventArgs e)
        {
            KeyboardHooking.TaskBarHide(); // 작업표시줄 숨김
        }

        // 폼 클로즈
        private void FormScreenSaver_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 폼 애니메이션(아래서 위로)
                AnimateWindow(this.Handle, 500,
                    AnimateWindowFlags.AW_VER_NEGATIVE | AnimateWindowFlags.AW_HIDE);

                if (KeyboardHooking.WINDOWSTATUS == KeyboardHooking.SWP_HIDEWINDOW)
                {
                    KeyboardHooking.TaskBarShow(); // 작업표시줄 드러냄
                }

                // 키보드 후킹 해제
                KeyboardHooking.UnHookWindowsEx(intLLKey);
                KeyboardHooking.UnBlockCtrlAltDel();

                main.rcvRssi = default(int);

                this.Dispose();
            }
            catch(Exception ee)
            {
                Console.WriteLine("Saver Closing");
            }
        }

        private void FormScreenSaver_KeyDown(object sender, KeyEventArgs e)
        {
        }

        public void SetFormScreenSaverCancel(FormScreenSaverCancel formScreenSaverCancel)
        {
            //this.formScreenSaverCancel = formScreenSaverCancel;
        }



        /// <summary>
        /// 스크린세이버를 동작시킨다
        /// </summary>
        private void ActiveScreenSavor()
        {
            try
            {
                // 비밀번호 입력창을 오픈한다
                if (formScreenSaverCancel == null)
                {
                    formScreenSaverCancel = new FormScreenSaverCancel(this);
                    formScreenSaverCancel.TopMost = true;
                    formScreenSaverCancel.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.Message);
            }
        }



        /// <summary>
        /// 폼의 마우스 다운 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pb_screenSaver_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                ActiveScreenSavor();
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.StackTrace);
            }
        }

        /// <summary>
        /// 마우스 클릭 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pb_screenSaver_Click(object sender, EventArgs e)
        {
            try
            {
                ActiveScreenSavor();
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.StackTrace);
            }
        }


        /// <summary>
        /// 두가지 폼중 하나에 진입했을때 폼을 엑티브 시킨다
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pb_screenSaver_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.Activate();
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.StackTrace);                
            }            
        }
    }
}
