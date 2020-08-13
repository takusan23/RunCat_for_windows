// �f�G�Ȗ{�Ɨl�FCopyright 2020 Takuto Nakamura https://github.com/Kyome22/RunCat_for_windows
// �j�R���ovar�FCopyright 2020 takusan_23 
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCatForWindowsNicomobaVarDotNetCore.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// �j�R���o�������^�X�N�g���C�ɑ��点��R�[�h
/// .NET Core + WinForm
/// </summary>
namespace RunCatForWindowsNicomobaVarDotNetCore
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        /// <summary>
        /// CPU�g�p���Ƃ鉽��
        /// </summary>
        private PerformanceCounter cpuUsage;
        /// <summary>
        /// �^�X�N�o�[�ɕ\��������
        /// </summary>
        private NotifyIcon notifyIcon;
        /// <summary>
        /// ���\�����Ă�A�C�R���̈ʒu
        /// </summary>
        private int currentIconListPos = 0;
        /// <summary>
        /// �A�C�R���z��B����̓j�R���o�����i4���j
        /// </summary>
        private Icon[] icons;
        /// <summary>
        /// ������s���邽�߂�
        /// </summary>
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();

        public RunCatApplicationContext()
        {
            // CPU�g�p�����Ȃɂ�
            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // �ŏ��̖߂�l��j�����܂� #���ĉ�

            // �^�X�N�o�[�ɃA�C�R�����o���BWPF���Ƃ߂�ǂ��񂾂���
            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.nicomoba_chan_1,
                ContextMenuStrip = new ContextMenuStrip(),
                Text = "0.0%",
                Visible = true
            };
            // ����{�^���B�Ȃ�.NET Core�ɂ�����Ȃ񂩏������ς�����H
            var fruitToolStripMenuItem = new ToolStripMenuItem("���i�I���j", null, Exit, "Exit");
            notifyIcon.ContextMenuStrip.Items.Add(fruitToolStripMenuItem);

            // �A�C�R���z��p��
            SetIcons();
            // �A�C�R���؂�ւ��֐���o�^
            SetAnimation();
            // CPU�g�p��+�A�j���[�V�������x�ύX
            GetCPUUsageAndAnimationSpeedChange(null, EventArgs.Empty);
            // ����������I�ɌĂԂ悤�ɂ���
            StartObserveCPU();
            // ���݂̃A�C�R���z��̈ʒu�H
            currentIconListPos = 1;
        }

        /// <summary>
        /// �p���p������Ŏg���A�C�R����z��ɓ���ėp�ӂ���
        /// </summary>
        private void SetIcons()
        {
            var rm = Resources.ResourceManager;
            icons = new List<Icon>
            {
                (Icon)rm.GetObject("nicomoba_chan_1"),
                (Icon)rm.GetObject("nicomoba_chan_2"),
                (Icon)rm.GetObject("nicomoba_chan_3"),
                (Icon)rm.GetObject("nicomoba_chan_4")
            }
            .ToArray();
        }

        /// <summary>
        /// �I�����Ƀ^�C�}�[�~�߂�Ȃ�
        /// </summary>
        /// <param name="sender">�����</param>
        /// <param name="e">�킩���</param>
        private void Exit(object sender, EventArgs e)
        {
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// ChangeIcon�����I�ɌĂԂ悤�ɂ���
        /// </summary>
        private void SetAnimation()
        {
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(ChangeIcon);
        }

        /// <summary>
        /// ����������I�ɌĂ΂�A�摜��؂�ւ��Ă���B
        /// �ǂ����GetCPUUsageAndAnimationSpeedChange���X�V�p�x��ς��Ă�炵���H
        /// </summary>
        /// <param name="sender">�����</param>
        /// <param name="e">�킩���</param>
        private void ChangeIcon(object sender, EventArgs e)
        {
            notifyIcon.Icon = icons[currentIconListPos];
            currentIconListPos = (currentIconListPos + 1) % icons.Length;
        }

        /// <summary>
        /// GetCPUUsageAndAnimationSpeedChange�֐������I�ɌĂԂ悤�ɂ���
        /// </summary>
        private void StartObserveCPU()
        {
            cpuTimer.Interval = 3000;
            cpuTimer.Tick += new EventHandler(GetCPUUsageAndAnimationSpeedChange);
            cpuTimer.Start();
        }

        /// <summary>
        /// CPU�g�p�����Ƃ��ăA�j���[�V�����̑��x��ύX����
        /// </summary>
        /// <param name="sender">�����</param>
        /// <param name="e">�킩���</param>
        private void GetCPUUsageAndAnimationSpeedChange(object sender, EventArgs e)
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"{s:f1}%";
            // �p���p������֑̐ؑ��x�������ŕς��Ă�炵���H
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }
    }
}
