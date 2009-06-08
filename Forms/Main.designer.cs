namespace HeavyDuck.Dnd.MacroMaker.Forms
{
    partial class Main
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
            this.label1 = new System.Windows.Forms.Label();
            this.character_box = new System.Windows.Forms.TextBox();
            this.browse_character_button = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.browse_macro_button = new System.Windows.Forms.Button();
            this.macro_box = new System.Windows.Forms.TextBox();
            this.go_button = new System.Windows.Forms.Button();
            this.compendium_check = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Character File";
            // 
            // character_box
            // 
            this.character_box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.character_box.Location = new System.Drawing.Point(12, 25);
            this.character_box.Name = "character_box";
            this.character_box.Size = new System.Drawing.Size(407, 20);
            this.character_box.TabIndex = 1;
            // 
            // browse_character_button
            // 
            this.browse_character_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browse_character_button.Location = new System.Drawing.Point(425, 23);
            this.browse_character_button.Name = "browse_character_button";
            this.browse_character_button.Size = new System.Drawing.Size(75, 23);
            this.browse_character_button.TabIndex = 2;
            this.browse_character_button.Text = "Browse...";
            this.browse_character_button.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Macro File (output)";
            // 
            // browse_macro_button
            // 
            this.browse_macro_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browse_macro_button.Location = new System.Drawing.Point(425, 62);
            this.browse_macro_button.Name = "browse_macro_button";
            this.browse_macro_button.Size = new System.Drawing.Size(75, 23);
            this.browse_macro_button.TabIndex = 5;
            this.browse_macro_button.Text = "Browse...";
            this.browse_macro_button.UseVisualStyleBackColor = true;
            // 
            // macro_box
            // 
            this.macro_box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.macro_box.Location = new System.Drawing.Point(12, 64);
            this.macro_box.Name = "macro_box";
            this.macro_box.Size = new System.Drawing.Size(407, 20);
            this.macro_box.TabIndex = 4;
            // 
            // go_button
            // 
            this.go_button.Location = new System.Drawing.Point(12, 90);
            this.go_button.Name = "go_button";
            this.go_button.Size = new System.Drawing.Size(75, 23);
            this.go_button.TabIndex = 6;
            this.go_button.Text = "Go";
            this.go_button.UseVisualStyleBackColor = true;
            // 
            // compendium_check
            // 
            this.compendium_check.AutoSize = true;
            this.compendium_check.Checked = true;
            this.compendium_check.CheckState = System.Windows.Forms.CheckState.Checked;
            this.compendium_check.Location = new System.Drawing.Point(93, 94);
            this.compendium_check.Name = "compendium_check";
            this.compendium_check.Size = new System.Drawing.Size(251, 17);
            this.compendium_check.TabIndex = 7;
            this.compendium_check.Text = "Download power descriptions from compendium";
            this.compendium_check.UseVisualStyleBackColor = true;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 125);
            this.Controls.Add(this.compendium_check);
            this.Controls.Add(this.go_button);
            this.Controls.Add(this.browse_macro_button);
            this.Controls.Add(this.macro_box);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.browse_character_button);
            this.Controls.Add(this.character_box);
            this.Controls.Add(this.label1);
            this.Name = "Main";
            this.Text = "Macro Maker";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox character_box;
        private System.Windows.Forms.Button browse_character_button;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button browse_macro_button;
        private System.Windows.Forms.TextBox macro_box;
        private System.Windows.Forms.Button go_button;
        private System.Windows.Forms.CheckBox compendium_check;
    }
}