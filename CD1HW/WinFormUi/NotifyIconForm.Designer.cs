namespace CD1HW.WinFormUi
{
    partial class NotifyIconForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotifyIconForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sel_cam_0 = new System.Windows.Forms.ToolStripMenuItem();
            this.sel_cam_1 = new System.Windows.Forms.ToolStripMenuItem();
            this.sel_cam_2 = new System.Windows.Forms.ToolStripMenuItem();
            this.dSSHOWToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mSMFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "CD1HW";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectCameraToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(148, 48);
            // 
            // selectCameraToolStripMenuItem
            // 
            this.selectCameraToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sel_cam_0,
            this.sel_cam_1,
            this.sel_cam_2,
            this.dSSHOWToolStripMenuItem,
            this.mSMFToolStripMenuItem});
            this.selectCameraToolStripMenuItem.Name = "selectCameraToolStripMenuItem";
            this.selectCameraToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.selectCameraToolStripMenuItem.Text = "select camera";
            // 
            // sel_cam_0
            // 
            this.sel_cam_0.Name = "sel_cam_0";
            this.sel_cam_0.Size = new System.Drawing.Size(180, 22);
            this.sel_cam_0.Text = "0";
            this.sel_cam_0.Click += new System.EventHandler(this.sel_cam_0_ToolStripMenuItem_Click);
            // 
            // sel_cam_1
            // 
            this.sel_cam_1.Name = "sel_cam_1";
            this.sel_cam_1.Size = new System.Drawing.Size(180, 22);
            this.sel_cam_1.Text = "1";
            this.sel_cam_1.Click += new System.EventHandler(this.sel_cam_1_ToolStripMenuItem_Click);
            // 
            // sel_cam_2
            // 
            this.sel_cam_2.Name = "sel_cam_2";
            this.sel_cam_2.Size = new System.Drawing.Size(180, 22);
            this.sel_cam_2.Text = "2";
            this.sel_cam_2.Click += new System.EventHandler(this.sel_cam_2_ToolStripMenuItem_Click);
            // 
            // dSSHOWToolStripMenuItem
            // 
            this.dSSHOWToolStripMenuItem.Name = "dSSHOWToolStripMenuItem";
            this.dSSHOWToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dSSHOWToolStripMenuItem.Text = "DSHOW";
            this.dSSHOWToolStripMenuItem.Click += new System.EventHandler(this.dSSHOWToolStripMenuItem_Click);
            // 
            // mSMFToolStripMenuItem
            // 
            this.mSMFToolStripMenuItem.Name = "mSMFToolStripMenuItem";
            this.mSMFToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.mSMFToolStripMenuItem.Text = "MSMF";
            this.mSMFToolStripMenuItem.Click += new System.EventHandler(this.mSMFToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // NotifyIconForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(120, 23);
            this.ControlBox = false;
            this.Name = "NotifyIconForm";
            this.Opacity = 0D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "CD1HW";
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem selectCameraToolStripMenuItem;
        private ToolStripMenuItem sel_cam_0;
        private ToolStripMenuItem sel_cam_1;
        private ToolStripMenuItem sel_cam_2;
        private ToolStripMenuItem dSSHOWToolStripMenuItem;
        private ToolStripMenuItem mSMFToolStripMenuItem;
    }
}