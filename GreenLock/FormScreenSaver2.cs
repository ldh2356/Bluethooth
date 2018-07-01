using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;//.Tasks;

namespace GreenLock
{
    public partial class FormScreenSaver2 : Form
    {
        //char[] keyBuffer;
        //int intLLKey;

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

        public FormScreenSaver2(MainForm main)
        {
            InitializeComponent();
            this.main = main;
            main.SetFormScreenSaver2(this);

            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

            StartPosition = FormStartPosition.Manual;
        }

        private void FormScreenSaver2_Load(object sender, EventArgs e)
        {
            // 폼 애니메이션(위에서 아래로)
            AnimateWindow(this.Handle, 500, AnimateWindowFlags.AW_VER_POSITIVE);
        }
        
        private void FormScreenSaver2_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 폼 애니메이션(아래서 위로)
            AnimateWindow(this.Handle, 500,
                AnimateWindowFlags.AW_VER_NEGATIVE | AnimateWindowFlags.AW_HIDE);
        }

        private void FormScreenSaver2_KeyDown(object sender, KeyEventArgs e)
        {

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
                MainForm.log.write("스크린세이버2 마우스 다운이벤트");
                MainForm.log.write("formScreenSaverCancel == null" + (formScreenSaverCancel == null));

                // 비밀번호 입력창을 오픈한다
                if(formScreenSaverCancel == null)
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
    }
}
