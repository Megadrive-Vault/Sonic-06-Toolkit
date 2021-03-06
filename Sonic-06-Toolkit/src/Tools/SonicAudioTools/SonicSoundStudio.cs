﻿using System;
using WMPLib;
using System.IO;
using System.Linq;
using NAudio.Wave;
using Toolkit.Text;
using System.Drawing;
using VGAudio.Formats;
using SonicAudioLib.IO;
using Toolkit.EnvironmentX;
using System.Windows.Forms;
using VGAudio.Containers.Adx;
using VGAudio.Containers.Wave;
using System.Collections.Generic;

// Sonic '06 Toolkit is licensed under the MIT License:
/*
 * MIT License

 * Copyright (c) 2020 Gabriel (HyperPolygon64)

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Toolkit.Tools
{
    public partial class SonicSoundStudio : Form
    {
        private Main mainForm = null;
        private string location = Paths.currentPath;
        private string nowPlaying = string.Empty;

        public SonicSoundStudio(Form callingForm) {
            mainForm = callingForm as Main;
            InitializeComponent();
            Console.WriteLine(location);
            tm_NoCheckOnClickTimer.Start();
        }

        private void SonicSoundStudio_Load(object sender, EventArgs e) {
            combo_Encoder.SelectedIndex = Properties.Settings.Default.sss_Encoder;
            check_PatchXMA.Checked = Properties.Settings.Default.sss_PatchXMA;
            axWMP_Player.settings.volume = tracker_Volume.Value = Properties.Settings.Default.sss_Volume;
        }

        private async void Btn_MediaControl_Click(object sender, EventArgs e) {
            if (btn_MediaControl.BackColor == Color.Tomato) { //Pause
                btn_MediaControl.Text = "►";
                btn_MediaControl.BackColor = Color.LightGreen;
                axWMP_Player.Ctlcontrols.pause();
            } else { //Play
                btn_MediaControl.Text = "❚❚";
                btn_MediaControl.BackColor = Color.Tomato;
                if (axWMP_Player.URL == string.Empty) {
                    string sound = clb_SNDs.SelectedItem.ToString();
                    string tempPath = Path.GetTempPath();
                    string tempFile = Path.GetRandomFileName();
                    if (Path.GetExtension(sound) == ".wav" || Path.GetExtension(sound) == ".mp3") {
                        if (File.Exists(Path.Combine(location, sound)) && Verification.VerifyMagicNumberCommon(Path.Combine(location, sound)))
                            axWMP_Player.URL = Path.Combine(location, sound);
                    } else if (Path.GetExtension(sound) == ".adx") {
                        if (File.Exists(Path.Combine(location, sound)) && Verification.VerifyMagicNumberExtended(Path.Combine(location, sound))) {
                            byte[] adxFile = File.ReadAllBytes(Path.Combine(location, sound));
                            AudioData audio = new AdxReader().Read(adxFile);
                            byte[] wavFile = new WaveWriter().GetFile(audio);
                            File.WriteAllBytes(Path.Combine(tempPath, $"{tempFile}.wav"), wavFile);
                            nowPlaying = axWMP_Player.URL = Path.Combine(tempPath, $"{tempFile}.wav");
                        }
                    } else if (Path.GetExtension(sound) == ".at3") {
                        if (File.Exists(Path.Combine(location, sound)) && Verification.VerifyMagicNumberCommon(Path.Combine(location, sound))) {
                            var process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                                $"-d \"{Path.Combine(location, sound)}\" \"{Path.Combine(tempPath, tempFile)}.wav\"",
                                                Application.StartupPath,
                                                100000);
                            if (process.ExitCode != 0)
                                MessageBox.Show(SystemMessages.ex_PreviewFailure, SystemMessages.tl_FatalError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            nowPlaying = axWMP_Player.URL = Path.Combine(tempPath, $"{tempFile}.wav");
                        }
                    } else if (Path.GetExtension(sound) == ".xma") {
                        try {
                            byte[] xmaBytes = File.ReadAllBytes(Path.Combine(location, sound)).ToArray();
                            string hexString = BitConverter.ToString(xmaBytes).Replace("-", "");
                            if (hexString.Contains(Properties.Resources.xma_Patch)) {
                                FileInfo fi = new FileInfo(Path.Combine(location, sound));
                                FileStream fs = fi.Open(FileMode.Open);
                                long bytesToDelete = 56;
                                fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                                fs.Close();
                            }
                        } catch { MessageBox.Show(SystemMessages.ex_PreviewFailure, SystemMessages.tl_FatalError, MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        if (File.Exists(Path.Combine(location, sound)) && Verification.VerifyMagicNumberCommon(Path.Combine(location, sound))) {
                            var process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.XMADecoder,
                                                $"\"{Path.Combine(location, sound)}\"",
                                                tempPath,
                                                100000);
                            if (process.ExitCode != 0)
                                MessageBox.Show(SystemMessages.ex_PreviewFailure, SystemMessages.tl_FatalError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            nowPlaying = axWMP_Player.URL = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(sound)}.wav");
                        }
                    }
                }
                else
                    axWMP_Player.Ctlcontrols.play();
            }
        }

        private void Combo_Encoder_SelectedIndexChanged(object sender, EventArgs e) {
            axWMP_Player.close();
            clb_SNDs.Items.Clear();
            btn_Process.Enabled = false;
            btn_MediaControl.Enabled = false;
            tracker_MediaBar.Enabled = false;
            tracker_Volume.Enabled = false;
            pnl_LoopOptions.Enabled = false;
            check_Loop.Enabled = false;

            if (combo_Encoder.SelectedIndex == 0) { //ADX
                pnl_Backdrop.BackgroundImage = Properties.Resources.adxBG;
                pic_Logo.BackgroundImage = Properties.Resources.adxLogo;
                Icon = Properties.Resources.adxIcon;
                btn_Process.BackColor = SystemColors.ControlLightLight;
                check_PatchXMA.Enabled = false;
                ListVerifiedSoundBytes("*.mp3");
                ListVerifiedSoundBytes("*.wav");
                ListVerifiedSoundBytes("*.csb");
            } else if (combo_Encoder.SelectedIndex == 1) { //AT3
                pnl_Backdrop.BackgroundImage = Properties.Resources.at3BG;
                pic_Logo.BackgroundImage = Properties.Resources.at3Logo;
                Icon = Properties.Resources.at3Icon;
                btn_Process.BackColor = Color.DeepSkyBlue;
                check_PatchXMA.Enabled = false;
                check_Loop.Enabled = true;
                if (check_Loop.Checked) pnl_LoopOptions.Enabled = true;
                ListVerifiedSoundBytes("*.mp3");
                ListVerifiedSoundBytes("*.wav");
            } else if (combo_Encoder.SelectedIndex == 2) { //CSB
                pnl_Backdrop.BackgroundImage = Properties.Resources.csbBG;
                pic_Logo.BackgroundImage = Properties.Resources.csbLogo;
                Icon = Properties.Resources.csbIcon;
                btn_Process.BackColor = Color.FromArgb(24, 127, 196);
                check_PatchXMA.Enabled = false;
                if (Directory.GetDirectories(location).Length > 0)
                    foreach (string CSB in Directory.GetDirectories(location))
                        if (Directory.Exists(Path.Combine(location, CSB)) && Verification.VerifyCriWareSoundBank(Path.Combine(location, CSB)))
                            clb_SNDs.Items.Add(Path.GetFileName(CSB));
            } else if (combo_Encoder.SelectedIndex == 3) { //WAV
                pnl_Backdrop.BackgroundImage = Properties.Resources.adxBG;
                pic_Logo.BackgroundImage = Properties.Resources.wavLogo;
                Icon = Properties.Resources.wavIcon;
                btn_Process.BackColor = SystemColors.ControlLightLight;
                check_PatchXMA.Enabled = false;
                ListVerifiedSoundBytes("*.adx");
                ListVerifiedSoundBytes("*.at3");
                ListVerifiedSoundBytes("*.csb");
                ListVerifiedSoundBytes("*.mp3");
                ListVerifiedSoundBytes("*.xma");
            } else if (combo_Encoder.SelectedIndex == 4) { //XMA
                pnl_Backdrop.BackgroundImage = Properties.Resources.xmaBG;
                pic_Logo.BackgroundImage = Properties.Resources.xmaLogo;
                Icon = Properties.Resources.xmaIcon;
                btn_Process.BackColor = Color.FromArgb(244, 121, 59);
                check_PatchXMA.Enabled = true;
                ListVerifiedSoundBytes("*.mp3");
                ListVerifiedSoundBytes("*.wav");
            }
            Properties.Settings.Default.sss_Encoder = combo_Encoder.SelectedIndex;
        }

        private void ListVerifiedSoundBytes(string filter) {
            if (Directory.GetFiles(location, filter).Length > 0)
                foreach (string SND in Directory.GetFiles(location, filter, SearchOption.TopDirectoryOnly))
                    if (File.Exists(SND) && Verification.VerifyMagicNumberCommon(SND))
                        clb_SNDs.Items.Add(Path.GetFileName(SND));
                    else if (File.Exists(SND) && Verification.VerifyMagicNumberExtended(SND))
                        clb_SNDs.Items.Add(Path.GetFileName(SND));
        }

        private async void Btn_Process_Click(object sender, EventArgs e) {
            try {
                List<object> filesToProcess = clb_SNDs.CheckedItems.OfType<object>().ToList();
                if (combo_Encoder.SelectedIndex == 0) { //ADX
                    foreach (string SND in filesToProcess)
                        if (File.Exists(Path.Combine(location, SND))) {
                            if (Path.GetExtension(SND).ToLower() == ".wav") {
                                try {
                                    mainForm.Status = StatusMessages.cmn_Converting(SND, "ADX", false);
                                    byte[] wavFile = File.ReadAllBytes(Path.Combine(location, SND));
                                    AudioData audio = new WaveReader().Read(wavFile);
                                    byte[] adxFile = new AdxWriter().GetFile(audio);
                                    File.WriteAllBytes(Path.Combine(location, $"{Path.GetFileNameWithoutExtension(SND)}.adx"), adxFile);
                                } catch { mainForm.Status = StatusMessages.cmn_ConvertFailed(SND, "ADX", false); }
                            } else if (Path.GetExtension(SND).ToLower() == ".csb") {
                                try {
                                    mainForm.Status = StatusMessages.cmn_Unpacking(SND, false);
                                    var extractor = new DataExtractor {
                                        MaxThreads = 1,
                                        BufferSize = 4096,
                                        EnableThreading = false
                                    };
                                    Directory.CreateDirectory(Path.Combine(location, Path.GetFileNameWithoutExtension(SND)));
                                    CSBTools.ExtractCSBNodes(extractor, Path.Combine(location, SND), Path.Combine(location, Path.GetFileNameWithoutExtension(SND)));
                                    extractor.Run();
                                    mainForm.Status = StatusMessages.cmn_Unpacked(SND, false);
                                } catch { mainForm.Status = StatusMessages.cmn_UnpackFailed(SND, false); }
                            } else if (Path.GetExtension(SND).ToLower() == ".mp3") {
                                try {
                                    mainForm.Status = StatusMessages.cmn_Converting(SND, "ADX", false);
                                    string wav = MP3.CreateTemporaryWAV(Path.Combine(location, SND));
                                    byte[] wavFile = File.ReadAllBytes(wav);
                                    AudioData audio = new WaveReader().Read(wavFile);
                                    byte[] adxFile = new AdxWriter().GetFile(audio);
                                    File.WriteAllBytes(Path.Combine(location, $"{Path.GetFileNameWithoutExtension(SND)}.adx"), adxFile);
                                    File.Delete(wav);
                                } catch { mainForm.Status = StatusMessages.cmn_ConvertFailed(SND, "ADX", false); }
                            }
                        }
                } else if (combo_Encoder.SelectedIndex == 1) { //AT3
                    foreach (string SND in filesToProcess)
                        if (File.Exists(Path.Combine(location, SND))) {
                            var process = new ProcessAsyncHelper.ProcessResult();
                            mainForm.Status = StatusMessages.cmn_Converting(SND, "AT3", false);
                            if (Path.GetExtension(SND).ToLower() == ".mp3") {
                                string wav = MP3.CreateTemporaryWAV(Path.Combine(location, SND));
                                if (check_Loop.Checked) {
                                    process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                                    $"-loop {nud_Start.Value} {nud_End.Value} -e \"{wav}\" \"{Path.Combine(location, Path.GetFileNameWithoutExtension(SND))}.at3\"",
                                                    location,
                                                    100000);
                                } else {
                                    process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                                    $"-e \"{wav}\" \"{Path.Combine(location, Path.GetFileNameWithoutExtension(SND))}.at3\"",
                                                    location,
                                                    100000);
                                }
                                if (process.ExitCode != 0)
                                    mainForm.Status = StatusMessages.cmn_ConvertFailed(SND, "AT3", false);
                                else File.Delete(wav);
                            } else {
                                if (check_Loop.Checked) {
                                    process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                                    $"-loop {nud_Start.Value} {nud_End.Value} -e \"{Path.Combine(location, SND)}\" \"{Path.Combine(location, Path.GetFileNameWithoutExtension(SND))}.at3\"",
                                                    location,
                                                    100000);
                                } else {
                                    process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                                    $"-e \"{Path.Combine(location, SND)}\" \"{Path.Combine(location, Path.GetFileNameWithoutExtension(SND))}.at3\"",
                                                    location,
                                                    100000);
                                }
                                if (process.ExitCode != 0)
                                    mainForm.Status = StatusMessages.cmn_ConvertFailed(SND, "AT3", false);
                            }
                        }
                } else if (combo_Encoder.SelectedIndex == 2) { //CSB
                    foreach (string CSB in filesToProcess)
                        if (Directory.Exists(Path.Combine(location, CSB))) {
                            foreach (string WAV in Directory.GetFiles(Path.Combine(location, CSB), "*.wav", SearchOption.AllDirectories))
                                try {
                                    mainForm.Status = StatusMessages.cmn_Converting(WAV, "ADX", true);
                                    byte[] wavFile = File.ReadAllBytes(WAV);
                                    AudioData audio = new WaveReader().Read(wavFile);
                                    byte[] adxFile = new AdxWriter().GetFile(audio);
                                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(WAV), $"{Path.GetFileNameWithoutExtension(WAV)}.adx"), adxFile);
                                } catch { mainForm.Status = StatusMessages.cmn_ConvertFailed(WAV, "ADX", true); }
                            try {
                                mainForm.Status = StatusMessages.cmn_Repacking(CSB, false);
                                CSBTools.WriteCSB(Path.Combine(location, CSB));
                                mainForm.Status = StatusMessages.cmn_Repacked(CSB, false);
                            } catch { mainForm.Status = StatusMessages.cmn_RepackFailed(CSB, false); }
                        }
                } else if (combo_Encoder.SelectedIndex == 3) { //WAV
                    foreach (string SND in filesToProcess)
                        DecodeAudioData(SND);
                } else if (combo_Encoder.SelectedIndex == 4) { //XMA
                    foreach (string SND in filesToProcess)
                        if (File.Exists(Path.Combine(location, SND))) {
                            string wav = string.Empty;
                            var process = new ProcessAsyncHelper.ProcessResult();
                            mainForm.Status = StatusMessages.cmn_Converting(SND, "XMA", false);
                            string xmaOutput = Path.Combine(location, $"{Path.GetFileNameWithoutExtension(SND)}.xma");
                            try { if (File.Exists(xmaOutput)) File.Delete(xmaOutput); } catch { }

                            if (Path.GetExtension(SND).ToLower() == ".mp3") {
                                wav = MP3.CreateTemporaryWAV(Path.Combine(location, SND));
                                process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.XMATool,
                                                $"\"{wav}\" /L",
                                                location,
                                                100000);
                            } else if (Path.GetExtension(SND).ToLower() == ".wav") {
                                process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.XMATool,
                                                $"\"{Path.Combine(location, SND)}\" /L",
                                                location,
                                                100000);
                            }

                            if (process.ExitCode != 0)
                                mainForm.Status = StatusMessages.cmn_ConvertFailed(SND, "XMA", false);
                            else {
                                try {
                                    if (wav != string.Empty) {
                                        File.Copy(wav, Path.Combine(location, $"{Path.GetFileNameWithoutExtension(SND)}.xma"), true);
                                        File.Delete(Path.Combine(Path.GetDirectoryName(wav), $"{Path.GetFileNameWithoutExtension(wav)}.xma"));
                                        File.Delete(wav);
                                        wav = string.Empty;
                                    }
                                } catch { }
                                try {
                                    if (check_PatchXMA.Checked) {
                                        byte[] xmaBytes = File.ReadAllBytes(xmaOutput).ToArray();
                                        string hexString = BitConverter.ToString(xmaBytes).Replace("-", "");
                                        if (!hexString.Contains(Properties.Resources.xma_Patch))
                                            ByteArray.ByteArrayToFile(xmaOutput, ByteArray.StringToByteArray(Properties.Resources.xma_Patch));
                                        else break;
                                    }
                                } catch { mainForm.Status = StatusMessages.xma_EncodeFooterError(SND, false); return; }
                            }
                        }
                }
            } catch (Exception ex) { MessageBox.Show($"{SystemMessages.ex_EncoderError}\n\n{ex}", SystemMessages.tl_FatalError, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void DecodeAudioData(string sound) {
            if (File.Exists(Path.Combine(location, sound))) {
                mainForm.Status = StatusMessages.cmn_Converting(sound, "WAV", false);
                if (Path.GetExtension(sound).ToLower() == ".adx") {
                    try {
                        byte[] adxFile = File.ReadAllBytes(Path.Combine(location, sound));
                        AudioData audio = new AdxReader().Read(adxFile);
                        byte[] wavFile = new WaveWriter().GetFile(audio);
                        File.WriteAllBytes(Path.Combine(location, $"{Path.GetFileNameWithoutExtension(sound)}.wav"), wavFile);
                    } catch { mainForm.Status = StatusMessages.cmn_ConvertFailed(sound, "WAV", false); }
                } else if (Path.GetExtension(sound).ToLower() == ".at3") {
                    var process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.AT3Tool,
                                        $"-d \"{Path.Combine(location, sound)}\" \"{Path.Combine(location, Path.GetFileNameWithoutExtension(sound))}.wav\"",
                                        Application.StartupPath,
                                        100000);
                    if (process.ExitCode != 0)
                        mainForm.Status = StatusMessages.cmn_ConvertFailed(sound, "WAV", false);
                } else if (Path.GetExtension(sound).ToLower() == ".csb") {
                    try {
                        mainForm.Status = StatusMessages.cmn_Unpacking(sound, false);
                        var extractor = new DataExtractor {
                            MaxThreads = 1,
                            BufferSize = 4096,
                            EnableThreading = false
                        };
                        mainForm.Status = StatusMessages.cmn_Unpacked(sound, false);
                        Directory.CreateDirectory(Path.Combine(location, Path.GetFileNameWithoutExtension(sound)));
                        CSBTools.ExtractCSBNodes(extractor, Path.Combine(location, sound), Path.Combine(location, Path.GetFileNameWithoutExtension(sound)));
                        extractor.Run();
                    } catch { mainForm.Status = StatusMessages.cmn_UnpackFailed(sound, false); }

                    if (combo_Encoder.SelectedIndex == 3)
                        if (Directory.Exists(Path.Combine(location, Path.GetFileNameWithoutExtension(sound))))
                            foreach (string ADX in Directory.GetFiles(Path.Combine(location, Path.GetFileNameWithoutExtension(sound)), "*.adx", SearchOption.AllDirectories)) {
                                try {
                                    mainForm.Status = StatusMessages.cmn_Converting(ADX, "WAV", true);
                                    byte[] adxFile = File.ReadAllBytes(ADX);
                                    AudioData audio = new AdxReader().Read(adxFile);
                                    byte[] wavFile = new WaveWriter().GetFile(audio);
                                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(ADX), $"{Path.GetFileNameWithoutExtension(ADX)}.wav"), wavFile);
                                    File.Delete(ADX);
                                } catch { mainForm.Status = StatusMessages.cmn_ConvertFailed(ADX, "WAV", true); }
                            }
                } else if (Path.GetExtension(sound).ToLower() == ".mp3") {
                    using (Mp3FileReader mp3 = new Mp3FileReader(Path.Combine(location, sound)))
                        using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
                            WaveFileWriter.CreateWaveFile(Path.Combine(location, $"{sound}.wav"), pcm);
                } else if (Path.GetExtension(sound).ToLower() == ".xma") {
                    try {
                        byte[] xmaBytes = File.ReadAllBytes(Path.Combine(location, sound)).ToArray();
                        string hexString = BitConverter.ToString(xmaBytes).Replace("-", "");
                        if (hexString.Contains(Properties.Resources.xma_Patch)) {
                            FileInfo fi = new FileInfo(Path.Combine(location, sound));
                            FileStream fs = fi.Open(FileMode.Open);
                            long bytesToDelete = 56;
                            fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                            fs.Close();
                        }
                    } catch { mainForm.Status = StatusMessages.xma_DecodeFooterError(sound, false); return; }
                    try {
                        if (File.Exists($"{Path.Combine(location, Path.GetFileNameWithoutExtension(sound))}.wav"))
                            File.Delete($"{Path.Combine(location, Path.GetFileNameWithoutExtension(sound))}.wav");
                    } catch { }
                    var process = await ProcessAsyncHelper.ExecuteShellCommand(Paths.XMADecoder,
                                        $"\"{Path.Combine(location, sound)}\"",
                                        location,
                                        100000);
                    if (process.ExitCode != 0)
                        mainForm.Status = StatusMessages.cmn_ConvertFailed(sound, "WAV", false);
                }
            }
        }

        private void Clb_SNDs_SelectedIndexChanged(object sender, EventArgs e) {
            axWMP_Player.close();
            tm_MediaPlayer.Stop();
            tracker_MediaBar.Value = 0;
            btn_MediaControl.Text = "►";
            btn_MediaControl.BackColor = Color.LightGreen;
            try { if (File.Exists(nowPlaying)) File.Delete(nowPlaying); } catch { }
            if (clb_SNDs.SelectedItems.Count > 0 && Path.GetExtension(clb_SNDs.SelectedItem.ToString()) != ".csb" && Path.HasExtension(clb_SNDs.SelectedItem.ToString())) {
                tracker_MediaBar.Enabled = true;
                btn_MediaControl.Enabled = true;
                tracker_Volume.Enabled = true;

                if (tracker_Volume.Value >= 75) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_high;
                else if (tracker_Volume.Value >= 50) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_medium;
                else if (tracker_Volume.Value >= 25) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_low;
                else if (tracker_Volume.Value >= 5) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_none;
                else if (tracker_Volume.Value == 0) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_mute;
            } else {
                tracker_MediaBar.Enabled = false;
                btn_MediaControl.Enabled = false;
                tracker_Volume.Enabled = false;
            }
        }

        private void Btn_SelectAll_Click(object sender, EventArgs e) {
            for (int i = 0; i < clb_SNDs.Items.Count; i++) clb_SNDs.SetItemChecked(i, true);
            btn_Process.Enabled = true;
        }

        private void Btn_DeselectAll_Click(object sender, EventArgs e) {
            for (int i = 0; i < clb_SNDs.Items.Count; i++) clb_SNDs.SetItemChecked(i, false);
            btn_Process.Enabled = false;
        }

        private void Check_PatchXMA_CheckedChanged(object sender, EventArgs e) {
            Properties.Settings.Default.sss_PatchXMA = check_PatchXMA.Checked;
        }

        private void SonicSoundStudio_FormClosing(object sender, FormClosingEventArgs e) {
            axWMP_Player.close();
            Properties.Settings.Default.Save();
            try { if (File.Exists(nowPlaying)) File.Delete(nowPlaying); } catch { }
        }

        private void Tm_NoCheckOnClickTimer_Tick(object sender, EventArgs e) {
            btn_Process.Enabled = clb_SNDs.CheckedItems.Count > 0;
        }

        private void AxWMP_Player_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e) {
            if (axWMP_Player.playState == WMPPlayState.wmppsPlaying) {
                tracker_MediaBar.Maximum = (int)axWMP_Player.Ctlcontrols.currentItem.duration;
                tm_MediaPlayer.Start();
            } else if (axWMP_Player.playState == WMPPlayState.wmppsPaused)
                tm_MediaPlayer.Stop();
            else if (axWMP_Player.playState == WMPPlayState.wmppsStopped) {
                tm_MediaPlayer.Stop();
                tracker_MediaBar.Value = 0;
                btn_MediaControl.Text = "►";
                btn_MediaControl.BackColor = Color.LightGreen;
            }
        }

        private void Tm_MediaPlayer_Tick(object sender, EventArgs e) {
            if (axWMP_Player.playState == WMPPlayState.wmppsPlaying)
                tracker_MediaBar.Value = (int)axWMP_Player.Ctlcontrols.currentPosition;
        }

        private void Tracker_MediaBar_Scroll(object sender, EventArgs e) {
            axWMP_Player.Ctlcontrols.currentPosition = tracker_MediaBar.Value;
            if (axWMP_Player.playState == WMPPlayState.wmppsPaused) {
                btn_MediaControl.Text = "❚❚";
                btn_MediaControl.BackColor = Color.Tomato;
                axWMP_Player.Ctlcontrols.play();
                tm_MediaPlayer.Start();
            }
        }

        private void Tracker_Volume_Scroll(object sender, EventArgs e) {
            if (tracker_Volume.Value >= 75) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_high;
            else if (tracker_Volume.Value >= 50) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_medium;
            else if (tracker_Volume.Value >= 25) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_low;
            else if (tracker_Volume.Value >= 5) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_none;
            else if (tracker_Volume.Value == 0) pic_Volume.BackgroundImage = Properties.Resources.audio_volume_mute;
            Properties.Settings.Default.sss_Volume = axWMP_Player.settings.volume = tracker_Volume.Value;
        }

        private void Check_Loop_CheckedChanged(object sender, EventArgs e) {
            if (check_Loop.Checked) pnl_LoopOptions.Enabled = true;
            else pnl_LoopOptions.Enabled = false;
        }
    }
}