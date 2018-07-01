using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GreenLock
{
    public partial class FormScreenSaverCancel : Form
    {
        FormScreenSaver formScreenSaver;
        FormScreenSaver2 formScreenSaver2;

        public FormScreenSaverCancel()
        {
            InitializeComponent();
        }

        public FormScreenSaverCancel(FormScreenSaver formScreenSaver)
        {
            InitializeComponent();
            // 왜 있어야 하는지 모르겠음
            
            this.formScreenSaver = formScreenSaver;
            formScreenSaver.SetFormScreenSaverCancel(this); // 없어도 되나? 가보면 주석처리 돼있음

            this.TopMost = true;
            
        }

        
        // 새로 만든거
        // (FormScreenSaver2 에서 클릭 이벤트를 받았을 때 비밀번호 입력 창을 띄워주기 위한 생성자)
        public FormScreenSaverCancel(FormScreenSaver2 formScreenSaver2)
        {
            InitializeComponent();

            this.formScreenSaver2 = formScreenSaver2;
            //formScreenSaver2.SetFormScreenSaverCancel(this);

            this.TopMost = true;
        }
        

        // 취소 버튼
        /*
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        */

        // 확인 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            ButtonEvent();
        }

        // 비번 박스
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                MainForm.log.write("Button Event");

                if (e.KeyCode == Keys.Enter)
                {
                    ButtonEvent();
                }
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.StackTrace);
            }
        }

        void ButtonEvent()
        {
            try
            {
                // 패스워드가 일치할경우
                if (textBox1.Text == MainForm.userPw)
                {
                    this.Close();
                    // 1. 스크린 세이버만 종료
                    Screen[] screen = Screen.AllScreens; // 시스템 내 모든 디스플레이 배열을 가져옴

                    if (screen.GetLength(0) != 2)
                    {
                        formScreenSaver.main.screenSaverAllStop();
                    }
                    else
                    { // 듀얼 모니터인 경우
                        formScreenSaver2.main.screenSaverAllStop();
                    }

                    formScreenSaver.main.screensaverPasswordflag = true;
                }
                else
                {
                    MessageBox.Show("비밀번호가 틀렸습니다. \n다시 입력해 주세요.");
                    Service.AlertSoundStart();
                }
            }
            catch (Exception ex)
            {
                MainForm.log.write(ex.StackTrace);
            }

        }
    }
}
