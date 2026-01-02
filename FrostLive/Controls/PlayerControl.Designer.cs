using System.Drawing;
using System.Windows.Forms;

namespace FrostLive.Controls
{
    partial class PlayerControl
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this._playIconLabel = new System.Windows.Forms.Label();
            this._songTitleLabel = new System.Windows.Forms.Label();
            this._statusPanel = new System.Windows.Forms.Panel();
            this._statusLabel = new System.Windows.Forms.Label();
            this._playPauseButton = new System.Windows.Forms.Button();
            this._currentTimeLabel = new System.Windows.Forms.Label();
            this._peakMeter = new FrostLive.Controls.PeakMeterControl();
            this._volumeControl = new FrostLive.Controls.VolumeControl();
            this._statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _playIconLabel
            // 
            this._playIconLabel.BackColor = System.Drawing.Color.Transparent;
            this._playIconLabel.Font = new System.Drawing.Font("Segoe UI Emoji", 11F);
            this._playIconLabel.ForeColor = System.Drawing.Color.Lime;
            this._playIconLabel.Location = new System.Drawing.Point(16, 4);
            this._playIconLabel.Name = "_playIconLabel";
            this._playIconLabel.Size = new System.Drawing.Size(30, 20);
            this._playIconLabel.TabIndex = 0;
            this._playIconLabel.Text = "▶";
            this._playIconLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _songTitleLabel
            // 
            this._songTitleLabel.BackColor = System.Drawing.Color.Transparent;
            this._songTitleLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._songTitleLabel.Font = new System.Drawing.Font("Courier New", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._songTitleLabel.ForeColor = System.Drawing.Color.Lime;
            this._songTitleLabel.Location = new System.Drawing.Point(52, 4);
            this._songTitleLabel.Name = "_songTitleLabel";
            this._songTitleLabel.Size = new System.Drawing.Size(236, 23);
            this._songTitleLabel.TabIndex = 0;
            this._songTitleLabel.Text = "Current Track";
            this._songTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _statusPanel
            // 
            this._statusPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this._statusPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._statusPanel.Controls.Add(this._statusLabel);
            this._statusPanel.Location = new System.Drawing.Point(298, 3);
            this._statusPanel.Name = "_statusPanel";
            this._statusPanel.Size = new System.Drawing.Size(95, 27);
            this._statusPanel.TabIndex = 3;
            // 
            // _statusLabel
            // 
            this._statusLabel.BackColor = System.Drawing.Color.Transparent;
            this._statusLabel.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold);
            this._statusLabel.ForeColor = System.Drawing.Color.Orange;
            this._statusLabel.Location = new System.Drawing.Point(-5, 1);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(100, 23);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "Status";
            this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _playPauseButton
            // 
            this._playPauseButton.BackColor = System.Drawing.Color.Transparent;
            this._playPauseButton.FlatAppearance.BorderColor = System.Drawing.Color.Lime;
            this._playPauseButton.FlatAppearance.BorderSize = 2;
            this._playPauseButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(0)))));
            this._playPauseButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(0)))));
            this._playPauseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._playPauseButton.Font = new System.Drawing.Font("Courier New", 9F);
            this._playPauseButton.ForeColor = System.Drawing.Color.Lime;
            this._playPauseButton.Location = new System.Drawing.Point(20, 36);
            this._playPauseButton.Name = "_playPauseButton";
            this._playPauseButton.Size = new System.Drawing.Size(119, 38);
            this._playPauseButton.TabIndex = 4;
            this._playPauseButton.Text = "PLAY";
            this._playPauseButton.UseVisualStyleBackColor = false;
            // 
            // _currentTimeLabel
            // 
            this._currentTimeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._currentTimeLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._currentTimeLabel.Font = new System.Drawing.Font("Courier New", 11F, System.Drawing.FontStyle.Bold);
            this._currentTimeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._currentTimeLabel.Location = new System.Drawing.Point(304, 44);
            this._currentTimeLabel.Name = "_currentTimeLabel";
            this._currentTimeLabel.Size = new System.Drawing.Size(78, 23);
            this._currentTimeLabel.TabIndex = 5;
            this._currentTimeLabel.Text = "00:00";
            this._currentTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _peakMeter
            // 
            this._peakMeter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._peakMeter.BackColor = System.Drawing.Color.Transparent;
            this._peakMeter.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(5)))), ((int)(((byte)(8)))));
            this._peakMeter.BarCount = 15;
            this._peakMeter.BorderColor = System.Drawing.Color.Cyan;
            this._peakMeter.ChannelBackground = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._peakMeter.DividerColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._peakMeter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._peakMeter.Location = new System.Drawing.Point(20, 80);
            this._peakMeter.Name = "_peakMeter";
            this._peakMeter.NeonBlue = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._peakMeter.Size = new System.Drawing.Size(373, 40);
            this._peakMeter.TabIndex = 0;
            // 
            // _volumeControl
            // 
            this._volumeControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._volumeControl.BackColor = System.Drawing.Color.Transparent;
            this._volumeControl.Location = new System.Drawing.Point(149, 40);
            this._volumeControl.MinimumSize = new System.Drawing.Size(100, 30);
            this._volumeControl.Name = "_volumeControl";
            this._volumeControl.Size = new System.Drawing.Size(143, 30);
            this._volumeControl.TabIndex = 6;
            this._volumeControl.ThumbColor = System.Drawing.Color.Cyan;
            this._volumeControl.TrackBackground = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._volumeControl.TrackFillColor = System.Drawing.Color.Cyan;
            // 
            // PlayerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this._volumeControl);
            this.Controls.Add(this._songTitleLabel);
            this.Controls.Add(this._playIconLabel);
            this.Controls.Add(this._peakMeter);
            this.Controls.Add(this._statusPanel);
            this.Controls.Add(this._playPauseButton);
            this.Controls.Add(this._currentTimeLabel);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.MinimumSize = new System.Drawing.Size(400, 120);
            this.Name = "PlayerControl";
            this.Size = new System.Drawing.Size(400, 126);
            this._statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        // Подключаемые контролы
        private PeakMeterControl _peakMeter;

        // Основные контролы
        private Button _playPauseButton;
        private Label _songTitleLabel;
        private Panel _statusPanel;
        private Label _statusLabel;
        private Label _currentTimeLabel;
        private Label _playIconLabel;
        private VolumeControl _volumeControl;
    }
}