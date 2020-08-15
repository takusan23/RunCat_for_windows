﻿// 素敵な本家様：Copyright 2020 Takuto Nakamura https://github.com/Kyome22/RunCat_for_windows
// ニコモバvar：Copyright 2020 takusan_23 
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

using RunCatNicomobaChanVarDotNetCore.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
/// ニコモバちゃんをタスクトレイに走らせるコード
/// .NET Core + WinForm
/// </summary>
namespace RunCatNicomobaChanVarDotNetCore
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
        /// CPU使用率とる何か
        /// </summary>
        private PerformanceCounter cpuUsage;
        /// <summary>
        /// タスクバーに表示するやつ
        /// </summary>
        private NotifyIcon notifyIcon;
        /// <summary>
        /// 今表示してるアイコンの位置
        /// </summary>
        private int currentIconListPos = 0;
        /// <summary>
        /// アイコン配列。今回はニコモバちゃん（4枚）
        /// </summary>
        private Icon[] icons;
        /// <summary>
        /// 定期実行するための
        /// </summary>
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();

        public RunCatApplicationContext()
        {
            // CPU使用率取るなにか
            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // 最初の戻り値を破棄します #って何

            // タスクバーにアイコンを出す。WPFだとめんどいんだっけ
            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.nicomoba_chan_1,
                ContextMenuStrip = new ContextMenuStrip(),
                Text = "0.0%",
                Visible = true
            };
            notifyIcon.MouseUp += TaskTrayIconClick;
            // 閉じるボタン。なんか.NET Coreにしたらなんか書き方変わった？
            var exitMenuItem = new ToolStripMenuItem("おつ（終了）", null, Exit, "Exit");
            var sourceCodeMenuItem = new ToolStripMenuItem("GitHubを開く", null, OpenGitHub, "Open GitHub");
            var resitryStartup = new ToolStripMenuItem("スタートアップ登録/登録解除", null, RegistarStartUp, "Registar Startup");
            notifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip.Items.Add(sourceCodeMenuItem);
            notifyIcon.ContextMenuStrip.Items.Add(resitryStartup);
            // アイコン配列用意
            SetIcons();
            // アイコン切り替え関数を登録
            SetAnimation();
            // CPU使用率+アニメーション速度変更
            GetCPUUsageAndAnimationSpeedChange(null, EventArgs.Empty);
            // ↑これを定期的に呼ぶようにする
            StartObserveCPU();
            // 現在のアイコン配列の位置？
            currentIconListPos = 1;
        }

        /// <summary>
        /// ニコモバちゃんを押した時。今回は右クリックと同じメニューを出す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskTrayIconClick(object sender, MouseEventArgs e)
        {
            // なんか消せなくなるので：https://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
            if (e.Button == MouseButtons.Left)
            {
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            }
        }

        /// <summary>
        /// スタートアップにショートカットを作成する。
        /// なんか面倒くさい。
        /// プロジェクト右クリック > 追加 > COM参照 へ進み、 Windows Script Host Object Model にチャックを入れる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegistarStartUp(object sender, EventArgs e)
        {
            // パス。現在実行中のファイルのパス
            var appPath = Process.GetCurrentProcess().MainModule.FileName;
            // このアプリ名。拡張子は抜いてある
            var appName = Path.GetFileNameWithoutExtension(appPath);
            // ショートカット先。スタートアップ
            var shortcutAddress = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            // 追加か削除か。trueなら追加済み
            var isRegistered = false;
            var shortcutFiles = Directory.GetFiles(shortcutAddress);
            foreach (string fileName in shortcutFiles)
            {
                if (!isRegistered)
                {
                    // 同じ名前ならtrue
                    isRegistered = Path.GetFileNameWithoutExtension(fileName) == appName;
                }
            }
            if (isRegistered)
            {
                // 追加済みなので解除
                File.Delete(@$"{shortcutAddress}\{appName}.lnk");
                // 結果をダイアログ
                MessageBox.Show("スタートアップを解除しました", appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 追加する
                var shell = new IWshRuntimeLibrary.WshShell();
                // ショートカット作成
                var objShortcut = (IWshRuntimeLibrary.WshShortcut)shell.CreateShortcut(@$"{shortcutAddress}\{appName}.lnk");
                // ショートカット元。本家。
                objShortcut.TargetPath = appPath;
                // ショートカットを保存
                objShortcut.Save();
                // 結果をダイアログ
                MessageBox.Show("スタートアップに登録しました", appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// パラパラ漫画で使うアイコンを配列に入れて用意する
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
        /// 終了時にタイマー止めるなど
        /// </summary>
        /// <param name="sender">しらん</param>
        /// <param name="e">わからん</param>
        private void Exit(object sender, EventArgs e)
        {
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// GitHubを開く。.NET Coreから UseShellExecute=true しないとエラー出るようになった？
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGitHub(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "https://github.com/takusan23/RunCat_for_windows_nicomoba_chan_ver",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        /// <summary>
        /// ChangeIconを定期的に呼ぶようにする
        /// </summary>
        private void SetAnimation()
        {
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(ChangeIcon);
        }

        /// <summary>
        /// ここが定期的に呼ばれ、画像を切り替えている。
        /// どうやらGetCPUUsageAndAnimationSpeedChangeが更新頻度を変えてるらしい？
        /// </summary>
        /// <param name="sender">しらん</param>
        /// <param name="e">わからん</param>
        private void ChangeIcon(object sender, EventArgs e)
        {
            notifyIcon.Icon = icons[currentIconListPos];
            currentIconListPos = (currentIconListPos + 1) % icons.Length;
        }

        /// <summary>
        /// GetCPUUsageAndAnimationSpeedChange関数を定期的に呼ぶようにする
        /// </summary>
        private void StartObserveCPU()
        {
            cpuTimer.Interval = 3000;
            cpuTimer.Tick += new EventHandler(GetCPUUsageAndAnimationSpeedChange);
            cpuTimer.Start();
        }

        /// <summary>
        /// CPU使用率をとってアニメーションの速度を変更する
        /// </summary>
        /// <param name="sender">しらん</param>
        /// <param name="e">わからん</param>
        private void GetCPUUsageAndAnimationSpeedChange(object sender, EventArgs e)
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"{s:f1}%";
            // パラパラ漫画の切替速度をここで変えてるらしい？
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }
    }
}