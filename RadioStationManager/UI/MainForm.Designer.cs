namespace RadioStationManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private RadioButton rbPlaylistMode;
        private RadioButton rbServerMode;
        private GroupBox gbMode;
        private Label lblFiles;
        private ListBox lstFiles;
        private Button btnSelectFiles;
        private Button btnSelectFolder;
        private Button btnClearList;
        private Label lblFileCount;
        private Label lblServerUrl;
        private TextBox txtServerUrl;
        private Label lblApiKey;
        private TextBox txtApiKey;
        private Label lblServerPath;
        private TextBox txtServerPath;
        private Label lblDownloadLink;
        private TextBox txtDownloadLink;
        private Label lblPlaylistPath;
        private TextBox txtPlaylistPath;
        private Button btnBrowsePlaylist;
        private Button btnProcess;
        private ProgressBar progressBar1;
        private Label lblStatus;
        private TextBox txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            rbPlaylistMode = new RadioButton();
            rbServerMode = new RadioButton();
            gbMode = new GroupBox();
            lblFiles = new Label();
            lstFiles = new ListBox();
            btnSelectFiles = new Button();
            btnSelectFolder = new Button();
            btnClearList = new Button();
            lblFileCount = new Label();
            lblServerUrl = new Label();
            txtServerUrl = new TextBox();
            lblApiKey = new Label();
            txtApiKey = new TextBox();
            lblServerPath = new Label();
            txtServerPath = new TextBox();
            lblDownloadLink = new Label();
            txtDownloadLink = new TextBox();
            lblPlaylistPath = new Label();
            txtPlaylistPath = new TextBox();
            btnBrowsePlaylist = new Button();
            btnProcess = new Button();
            progressBar1 = new ProgressBar();
            lblStatus = new Label();
            txtLog = new TextBox();
            rbEditMode = new RadioButton();
            gbMode.SuspendLayout();
            SuspendLayout();
            // 
            // rbPlaylistMode
            // 
            rbPlaylistMode.AutoSize = true;
            rbPlaylistMode.Checked = true;
            rbPlaylistMode.Location = new Point(6, 22);
            rbPlaylistMode.Name = "rbPlaylistMode";
            rbPlaylistMode.Size = new Size(124, 19);
            rbPlaylistMode.TabIndex = 0;
            rbPlaylistMode.TabStop = true;
            rbPlaylistMode.Text = "Режим плейлиста";
            rbPlaylistMode.UseVisualStyleBackColor = true;
            // 
            // rbServerMode
            // 
            rbServerMode.AutoSize = true;
            rbServerMode.Location = new Point(136, 22);
            rbServerMode.Name = "rbServerMode";
            rbServerMode.Size = new Size(110, 19);
            rbServerMode.TabIndex = 1;
            rbServerMode.Text = "Режим сервера";
            rbServerMode.UseVisualStyleBackColor = true;
            rbServerMode.CheckedChanged += rbMode_CheckedChanged;
            // 
            // gbMode
            // 
            gbMode.Controls.Add(rbEditMode);
            gbMode.Controls.Add(rbServerMode);
            gbMode.Controls.Add(rbPlaylistMode);
            gbMode.Location = new Point(20, 20);
            gbMode.Name = "gbMode";
            gbMode.Size = new Size(630, 51);
            gbMode.TabIndex = 2;
            gbMode.TabStop = false;
            gbMode.Text = "Режим работы";
            // 
            // lblFiles
            // 
            lblFiles.AutoSize = true;
            lblFiles.Location = new Point(20, 100);
            lblFiles.Name = "lblFiles";
            lblFiles.Size = new Size(116, 15);
            lblFiles.TabIndex = 3;
            lblFiles.Text = "Выбранные файлы:";
            // 
            // lstFiles
            // 
            lstFiles.FormattingEnabled = true;
            lstFiles.Location = new Point(20, 120);
            lstFiles.Name = "lstFiles";
            lstFiles.Size = new Size(300, 184);
            lstFiles.TabIndex = 4;
            // 
            // btnSelectFiles
            // 
            btnSelectFiles.Location = new Point(20, 310);
            btnSelectFiles.Name = "btnSelectFiles";
            btnSelectFiles.Size = new Size(95, 30);
            btnSelectFiles.TabIndex = 5;
            btnSelectFiles.Text = "Файлы";
            btnSelectFiles.UseVisualStyleBackColor = true;
            btnSelectFiles.Click += btnSelectFiles_Click;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(125, 310);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(95, 30);
            btnSelectFolder.TabIndex = 6;
            btnSelectFolder.Text = "Папка";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // btnClearList
            // 
            btnClearList.Location = new Point(230, 310);
            btnClearList.Name = "btnClearList";
            btnClearList.Size = new Size(90, 30);
            btnClearList.TabIndex = 7;
            btnClearList.Text = "Очистить";
            btnClearList.UseVisualStyleBackColor = true;
            btnClearList.Click += btnClearList_Click;
            // 
            // lblFileCount
            // 
            lblFileCount.AutoSize = true;
            lblFileCount.Location = new Point(20, 350);
            lblFileCount.Name = "lblFileCount";
            lblFileCount.Size = new Size(61, 15);
            lblFileCount.TabIndex = 8;
            lblFileCount.Text = "Файлов: 0";
            // 
            // lblServerUrl
            // 
            lblServerUrl.AutoSize = true;
            lblServerUrl.Location = new Point(350, 100);
            lblServerUrl.Name = "lblServerUrl";
            lblServerUrl.Size = new Size(78, 15);
            lblServerUrl.TabIndex = 9;
            lblServerUrl.Text = "URL сервера:";
            // 
            // txtServerUrl
            // 
            txtServerUrl.Location = new Point(350, 120);
            txtServerUrl.Name = "txtServerUrl";
            txtServerUrl.Size = new Size(300, 23);
            txtServerUrl.TabIndex = 10;
            // 
            // lblApiKey
            // 
            lblApiKey.AutoSize = true;
            lblApiKey.Location = new Point(350, 150);
            lblApiKey.Name = "lblApiKey";
            lblApiKey.Size = new Size(61, 15);
            lblApiKey.TabIndex = 11;
            lblApiKey.Text = "API ключ:";
            // 
            // txtApiKey
            // 
            txtApiKey.Location = new Point(350, 170);
            txtApiKey.Name = "txtApiKey";
            txtApiKey.Size = new Size(300, 23);
            txtApiKey.TabIndex = 12;
            txtApiKey.UseSystemPasswordChar = true;
            // 
            // lblServerPath
            // 
            lblServerPath.AutoSize = true;
            lblServerPath.Location = new Point(350, 200);
            lblServerPath.Name = "lblServerPath";
            lblServerPath.Size = new Size(99, 15);
            lblServerPath.TabIndex = 13;
            lblServerPath.Text = "Путь на сервере:";
            // 
            // txtServerPath
            // 
            txtServerPath.Location = new Point(350, 220);
            txtServerPath.Name = "txtServerPath";
            txtServerPath.Size = new Size(300, 23);
            txtServerPath.TabIndex = 14;
            // 
            // lblDownloadLink
            // 
            lblDownloadLink.AutoSize = true;
            lblDownloadLink.Location = new Point(350, 250);
            lblDownloadLink.Name = "lblDownloadLink";
            lblDownloadLink.Size = new Size(124, 15);
            lblDownloadLink.TabIndex = 15;
            lblDownloadLink.Text = "Ссылка для загрузки:";
            // 
            // txtDownloadLink
            // 
            txtDownloadLink.Location = new Point(350, 270);
            txtDownloadLink.Name = "txtDownloadLink";
            txtDownloadLink.Size = new Size(300, 23);
            txtDownloadLink.TabIndex = 16;
            // 
            // lblPlaylistPath
            // 
            lblPlaylistPath.AutoSize = true;
            lblPlaylistPath.Location = new Point(350, 300);
            lblPlaylistPath.Name = "lblPlaylistPath";
            lblPlaylistPath.Size = new Size(100, 15);
            lblPlaylistPath.TabIndex = 17;
            lblPlaylistPath.Text = "Файл плейлиста:";
            // 
            // txtPlaylistPath
            // 
            txtPlaylistPath.Location = new Point(350, 320);
            txtPlaylistPath.Name = "txtPlaylistPath";
            txtPlaylistPath.Size = new Size(250, 23);
            txtPlaylistPath.TabIndex = 18;
            // 
            // btnBrowsePlaylist
            // 
            btnBrowsePlaylist.Location = new Point(610, 320);
            btnBrowsePlaylist.Name = "btnBrowsePlaylist";
            btnBrowsePlaylist.Size = new Size(40, 23);
            btnBrowsePlaylist.TabIndex = 19;
            btnBrowsePlaylist.Text = "...";
            btnBrowsePlaylist.UseVisualStyleBackColor = true;
            btnBrowsePlaylist.Click += btnBrowsePlaylist_Click;
            // 
            // btnProcess
            // 
            btnProcess.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnProcess.Location = new Point(350, 360);
            btnProcess.Name = "btnProcess";
            btnProcess.Size = new Size(300, 40);
            btnProcess.TabIndex = 20;
            btnProcess.Text = "ОБРАБОТАТЬ";
            btnProcess.UseVisualStyleBackColor = true;
            btnProcess.Click += btnProcess_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(20, 410);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(630, 20);
            progressBar1.TabIndex = 21;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 440);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(45, 15);
            lblStatus.TabIndex = 22;
            lblStatus.Text = "Готово";
            // 
            // txtLog
            // 
            txtLog.Location = new Point(20, 470);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(630, 150);
            txtLog.TabIndex = 23;
            // 
            // rbEditMode
            // 
            rbEditMode.AutoSize = true;
            rbEditMode.Location = new Point(252, 22);
            rbEditMode.Name = "rbEditMode";
            rbEditMode.Size = new Size(155, 19);
            rbEditMode.TabIndex = 2;
            rbEditMode.TabStop = true;
            rbEditMode.Text = "Режим редактирования";
            rbEditMode.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            ClientSize = new Size(684, 641);
            Controls.Add(txtLog);
            Controls.Add(lblStatus);
            Controls.Add(progressBar1);
            Controls.Add(btnProcess);
            Controls.Add(btnBrowsePlaylist);
            Controls.Add(txtPlaylistPath);
            Controls.Add(lblPlaylistPath);
            Controls.Add(txtDownloadLink);
            Controls.Add(lblDownloadLink);
            Controls.Add(txtServerPath);
            Controls.Add(lblServerPath);
            Controls.Add(txtApiKey);
            Controls.Add(lblApiKey);
            Controls.Add(txtServerUrl);
            Controls.Add(lblServerUrl);
            Controls.Add(lblFileCount);
            Controls.Add(btnClearList);
            Controls.Add(btnSelectFolder);
            Controls.Add(btnSelectFiles);
            Controls.Add(lstFiles);
            Controls.Add(lblFiles);
            Controls.Add(gbMode);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Radio Station Manager";
            gbMode.ResumeLayout(false);
            gbMode.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private RadioButton rbEditMode;
    }
}