﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;

namespace OSU_player
{

    public partial class Form1
    {
        public Form1()
        {
            InitializeComponent();
        }

        public Videofiles uni_Video = new Videofiles();
        public Audiofiles uni_Audio = new Audiofiles();
        public QQ uni_QQ = new QQ();
        // Thread DelayVideo = new Thread((delegate() { Thread.Sleep(10); }));
        int Nextmode = 3;
        ListView FullList;
        BeatmapSet CurrentSet;
        Beatmap CurrentBeatmap;
        BeatmapSet TmpSet;
        Beatmap TmpBeatmap;
        public void AskForExit(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            DialogResult close;
            close = MessageBox.Show("确认退出？", "提示", MessageBoxButtons.YesNo);
            if (close == DialogResult.Yes)
            {
                this.Dispose();
            }
            else
            {
                e.Cancel = true;
            }
            uni_QQ.Send2QQ(Core.uin, "");
        }
        public bool ThumbnailCallback()
        {
            return false;
        }
        private void printdetail()
        {
            ListDetail.Items.Clear();
            for (int i = (int)OSUfile.FileVersion; i < (int)OSUfile.OSUfilecount; i++)
            {
                ListViewItem tmpl = new ListViewItem(Enum.GetName(typeof(OSUfile), i).ToString());
                tmpl.SubItems.Add(TmpBeatmap.Rawdata[i]);
                ListDetail.Items.Add(tmpl);
            }
        }
        private void setbg()
        {
            printdetail();
            //System.Drawing.Image tmpimg = default(System.Drawing.Image);
            //  string bgpath = CurrentBeatmap.Background;
            // tmpimg = Image.FromFile(bgpath);
            // Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
            // Panel1.BackgroundImage = tmpimg.GetThumbnailImage(Panel1.Width, Panel1.Height, myCallback, IntPtr.Zero);
            // tmpimg.Dispose();
            uni_Video.initbg(CurrentBeatmap.Background);
            uni_Video.Play(this.panel2);
        }
        private void Stop()
        {
            AVsyncer.Enabled = false;
            uni_Audio.Stop();
            uni_Video.Stop();
            TrackBar1.Enabled = false;
            TrackBar1.Value = 0;
            StopButton.Enabled = false;
            //  DelayVideo.Abort();
        }
        private void Play()
        {
            uni_Audio.init(CurrentBeatmap.Audio);
            uni_Audio.Play();
            if (CurrentBeatmap.haveVideo)
            {
                uni_Video.init(Path.Combine(CurrentBeatmap.location, CurrentBeatmap.Video));
                if (CurrentBeatmap.VideoOffset > 0)
                {
                    Thread.Sleep(CurrentBeatmap.VideoOffset);
                    uni_Video.Play(this.panel2);

                }
                else { uni_Video.Play(this.panel2); }
            }
            TrackBar1.Enabled = true;
            AVsyncer.Enabled = true;
            uni_QQ.Send2QQ(Core.uin, CurrentBeatmap.name);
            PlayButton.Text = "暂停";
            StopButton.Enabled = true;
        }
        private void Pause()
        {
            uni_Video.Pause();
            uni_Audio.Pause();
            uni_QQ.Send2QQ(Core.uin, "");
            PlayButton.Text = "播放";

        }
        private void Resume()
        {
            uni_Video.Pause();
            uni_Audio.Pause();
            AVsyncer.Enabled = true;
            PlayButton.Text = "暂停";
        }
        private void Nextsong()
        {
            int next;
            int now;
            if (PlayList.SelectedItems.Count == 0) { now = 0; }
            else { now = PlayList.SelectedItems[0].Index; };
            switch (Nextmode)
            {
                case 1: next = (now + 1) % PlayList.Items.Count;
                    break;
                case 2: next = now;
                    break;
                case 3: next = new Random().Next() % PlayList.Items.Count;
                    break;
                default: next = 0;
                    break;
            }
            PlayList.Select();
            if (PlayList.SelectedItems.Count != 0)
            {
                PlayList.SelectedItems[0].Selected = false;
            }
            PlayList.Items[next].Selected = true;
            PlayList.EnsureVisible(next);
        }
        private void PlayList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PlayList.SelectedItems.Count == 0) { return; }
            DiffList.Items.Clear();
            TmpSet = Core.allsets[PlayList.SelectedItems[0].Index];
            if (!TmpSet.detailed)
            {
                TmpSet.GetDetail();
            }
            foreach (var s in TmpSet.diffstr)
            {
                DiffList.Items.Add(s);
            }
            DiffList.SelectedIndex = 0;
            //因为改变selectedindex会触发listbox的进程，所以以下省略了
        }
        public void DiffList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PlayList.SelectedIndices.Count == 0)
            {
                return;
            }
            TmpBeatmap = TmpSet.Diffs[DiffList.SelectedIndex];
            if (!StopButton.Enabled)
            {
                CurrentSet = TmpSet;
                CurrentBeatmap = TmpBeatmap;
                uni_Video.Stop();
                setbg();
            }
            else if (uni_Audio.isplaying)
            {
                printdetail();
            }
            else
            {
                Stop();
                setbg();
            }
        }
        private void PlayList_DoubleClick(object sender, EventArgs e)
        {
            TmpSet = Core.allsets[PlayList.SelectedItems[0].Index];
            TmpBeatmap = TmpSet.Diffs[0];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Stop();
            setbg();
            Play();
        }
        private void DiffList_DoubleClick(object sender, EventArgs e)
        {
            TmpSet = Core.allsets[PlayList.SelectedItems[0].Index];
            TmpBeatmap = TmpSet.Diffs[DiffList.SelectedIndex];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Stop();
            setbg();
            Play();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Un4seen.Bass.BassNet.Registration("sqh1994@163.com", "2X280331512622");
            new Thread(new ThreadStart(Selfupdate.check_update)).Start();
            Core.Getpath();
            MessageBox.Show("将开始初始化");
            initset();
        }
        private void initset()
        {
            try
            {
                if (Directory.Exists(Path.Combine(Core.osupath, "Songs")))
                {
                    this.backgroundWorker1.RunWorkerAsync(Path.Combine(Core.osupath, "Songs"));
                }
            }
            catch (SystemException ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw (new FormatException("Failed to read song path", ex));
            }
        }
        private void SearchButton_Click(object sender, EventArgs e)
        {
            if (TextBox1.Text == "")
            {
                foreach (BeatmapSet tmpbms in Core.allsets)
                {
                    ListViewItem tmpl = new ListViewItem(tmpbms.name);
                    PlayList.Items.Add(tmpl);
                }
            }
            else
            {
                PlayList.Clear();
                foreach (ListViewItem tmp in PlayList.Items)
                {
                    if (tmp.Text.ToLower().Contains(TextBox1.Text.ToLower()))
                    {
                        ListViewItem tmpl = new ListViewItem(tmp.Text);
                        PlayList.Items.Add(tmpl);
                    }
                }
            }
        }
        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (StopButton.Enabled == false)
            {
                Play();
            }
            else
            {
                if (PlayButton.Text == "播放")
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

        }
        private void TrackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            uni_Audio.seek(TrackBar1.Value * uni_Audio.durnation / TrackBar1.Maximum);
            uni_Video.seek(TrackBar1.Value * uni_Audio.durnation / TrackBar1.Maximum + CurrentBeatmap.VideoOffset);
            AVsyncer.Enabled = true;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            using (Form2 dialog = new Form2())
            {
                dialog.Show();
            }
            LabelQQ.Text = "当前同步QQ：" + Core.uin.ToString();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            initset();
        }
        private void AVsync(object sender, EventArgs e)
        {

            if (uni_Audio.durnation != 0)
            {
                TrackBar1.Value = (int)Math.Round((uni_Audio.position / uni_Audio.durnation) * TrackBar1.Maximum);
            }
            if (uni_Audio.durnation == uni_Audio.position)
            {
                Stop();
                Nextsong();
                TmpSet = Core.allsets[PlayList.SelectedItems[0].Index];
                TmpBeatmap = TmpSet.Diffs[0];
                CurrentSet = TmpSet;
                CurrentBeatmap = TmpBeatmap;
                Play();
            }
        }
        private void TrackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            AVsyncer.Enabled = false;
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            scanforset(e.Argument.ToString());
        }
        public void scanforset(string path)
        {
            string[] osufiles = Directory.GetFiles(path, "*.osu");
            if (osufiles.Length != 0)
            {
                BeatmapSet tmp = new BeatmapSet(path);
                //tmp.GetDetail();
                Core.allsets.Add(tmp);
                this.backgroundWorker1.ReportProgress(0, tmp.name);
            }
            else
            {
                string[] tmpfolder = Directory.GetDirectories(path);
                foreach (string subfolder in tmpfolder)
                {
                    scanforset(subfolder);
                }
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState.ToString().Length > 0)
            {
                ListViewItem tmpl = new ListViewItem(e.UserState.ToString());
                PlayList.Items.Add(tmpl);
            }
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (ListViewItem item in PlayList.Items)
            {
                item.SubItems.Add(item.Index.ToString());
            }
            MessageBox.Show(string.Format("初始化完毕，发现曲目{0}个", Core.allsets.Count));
            FullList = PlayList;
            button3.Enabled = false;
            PlayList.Items[0].Selected = true;
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void 随机播放ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Nextmode = 3;
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            Stop();
            Nextsong();
            TmpSet = Core.allsets[PlayList.SelectedItems[0].Index];
            TmpBeatmap = TmpSet.Diffs[0];
            CurrentSet = TmpSet;
            CurrentBeatmap = TmpBeatmap;
            Play();
        }

        private void 顺序播放ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Nextmode = 1;
        }

        private void 单曲循环ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Nextmode = 2;
        }



    }
}