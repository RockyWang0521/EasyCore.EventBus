namespace WinFormsApp
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btn_Local = new Button();
            btn_Distributed = new Button();
            btn_Web = new Button();
            btn_FailureCallback = new Button();
            SuspendLayout();
            // 
            // btn_Local
            // 
            btn_Local.Location = new Point(316, 88);
            btn_Local.Name = "btn_Local";
            btn_Local.Size = new Size(171, 74);
            btn_Local.TabIndex = 0;
            btn_Local.Text = "本地事件总线";
            btn_Local.UseVisualStyleBackColor = true;
            btn_Local.Click += btn_Local_Click;
            // 
            // btn_Distributed
            // 
            btn_Distributed.Location = new Point(316, 195);
            btn_Distributed.Name = "btn_Distributed";
            btn_Distributed.Size = new Size(171, 74);
            btn_Distributed.TabIndex = 1;
            btn_Distributed.Text = "winform分布式事件总线";
            btn_Distributed.UseVisualStyleBackColor = true;
            btn_Distributed.Click += btn_Distributed_Click;
            // 
            // btn_Web
            // 
            btn_Web.Location = new Point(316, 302);
            btn_Web.Name = "btn_Web";
            btn_Web.Size = new Size(171, 74);
            btn_Web.TabIndex = 2;
            btn_Web.Text = "web分布式事件总线";
            btn_Web.UseVisualStyleBackColor = true;
            btn_Web.Click += btn_Web_Click;
            // 
            // btn_FailureCallback
            // 
            btn_FailureCallback.Location = new Point(316, 409);
            btn_FailureCallback.Name = "btn_FailureCallback";
            btn_FailureCallback.Size = new Size(171, 74);
            btn_FailureCallback.TabIndex = 3;
            btn_FailureCallback.Text = "失败回调事件总线";
            btn_FailureCallback.UseVisualStyleBackColor = true;
            btn_FailureCallback.Click += btn_FailureCallback_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 549);
            Controls.Add(btn_FailureCallback);
            Controls.Add(btn_Web);
            Controls.Add(btn_Distributed);
            Controls.Add(btn_Local);
            Name = "Main";
            Text = "EventBus";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_Local;
        private Button btn_Distributed;
        private Button btn_Web;
        private Button btn_FailureCallback;
    }
}
