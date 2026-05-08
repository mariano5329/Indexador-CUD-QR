namespace Indexador_GCBA_
{
    partial class FormUsuarios
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormUsuarios));
            this.txtArchivoUsuario = new System.Windows.Forms.TextBox();
            this.btnGrabarCerrar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtArchivoUsuario
            // 
            this.txtArchivoUsuario.Location = new System.Drawing.Point(12, 12);
            this.txtArchivoUsuario.Multiline = true;
            this.txtArchivoUsuario.Name = "txtArchivoUsuario";
            this.txtArchivoUsuario.Size = new System.Drawing.Size(387, 290);
            this.txtArchivoUsuario.TabIndex = 0;
            // 
            // btnGrabarCerrar
            // 
            this.btnGrabarCerrar.Location = new System.Drawing.Point(116, 308);
            this.btnGrabarCerrar.Name = "btnGrabarCerrar";
            this.btnGrabarCerrar.Size = new System.Drawing.Size(166, 23);
            this.btnGrabarCerrar.TabIndex = 1;
            this.btnGrabarCerrar.Text = "Guardar y Cerrar";
            this.btnGrabarCerrar.UseVisualStyleBackColor = true;
            this.btnGrabarCerrar.Click += new System.EventHandler(this.btnGrabarCerrar_Click);
            // 
            // FormUsuarios
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(411, 343);
            this.Controls.Add(this.btnGrabarCerrar);
            this.Controls.Add(this.txtArchivoUsuario);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormUsuarios";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Usuarios";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtArchivoUsuario;
        private System.Windows.Forms.Button btnGrabarCerrar;
    }
}