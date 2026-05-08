namespace Indexador_GCBA_
{
    partial class FormEducacionCAD
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormEducacionCAD));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtRutaLote = new System.Windows.Forms.TextBox();
            this.btnSeleccionarLote = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtNombreImagen = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtUsuario = new System.Windows.Forms.TextBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.lboxIndice = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnSepararIlegible = new System.Windows.Forms.Button();
            this.btnGrabar = new System.Windows.Forms.Button();
            this.btnSalir = new System.Windows.Forms.Button();
            this.txttipo = new System.Windows.Forms.TextBox();
            this.txtapellido = new System.Windows.Forms.TextBox();
            this.txtaño = new System.Windows.Forms.TextBox();
            this.txtacta = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtficha = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txttomo = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtseccion = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtRutaLote);
            this.groupBox2.Controls.Add(this.btnSeleccionarLote);
            this.groupBox2.Location = new System.Drawing.Point(10, 16);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(184, 150);
            this.groupBox2.TabIndex = 40;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Selección de Lote a Indexar:";
            // 
            // txtRutaLote
            // 
            this.txtRutaLote.Location = new System.Drawing.Point(7, 62);
            this.txtRutaLote.Multiline = true;
            this.txtRutaLote.Name = "txtRutaLote";
            this.txtRutaLote.ReadOnly = true;
            this.txtRutaLote.Size = new System.Drawing.Size(171, 82);
            this.txtRutaLote.TabIndex = 1;
            this.txtRutaLote.TabStop = false;
            // 
            // btnSeleccionarLote
            // 
            this.btnSeleccionarLote.Location = new System.Drawing.Point(7, 20);
            this.btnSeleccionarLote.Name = "btnSeleccionarLote";
            this.btnSeleccionarLote.Size = new System.Drawing.Size(171, 36);
            this.btnSeleccionarLote.TabIndex = 45;
            this.btnSeleccionarLote.Text = "Asignar Lote";
            this.btnSeleccionarLote.UseVisualStyleBackColor = true;
            this.btnSeleccionarLote.Click += new System.EventHandler(this.btnSeleccionarLote_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox1);
            this.groupBox3.Controls.Add(this.txtNombreImagen);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Location = new System.Drawing.Point(204, 17);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(290, 76);
            this.groupBox3.TabIndex = 60;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Datos Fijos del Indice:";
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(40, 99);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 100);
            this.groupBox1.TabIndex = 258;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // txtNombreImagen
            // 
            this.txtNombreImagen.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNombreImagen.Location = new System.Drawing.Point(119, 25);
            this.txtNombreImagen.Name = "txtNombreImagen";
            this.txtNombreImagen.ReadOnly = true;
            this.txtNombreImagen.Size = new System.Drawing.Size(155, 26);
            this.txtNombreImagen.TabIndex = 9;
            this.txtNombreImagen.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(7, 31);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(107, 16);
            this.label5.TabIndex = 2;
            this.label5.Text = "Nombre Imagen:";
            // 
            // txtUsuario
            // 
            this.txtUsuario.Location = new System.Drawing.Point(132, 86);
            this.txtUsuario.Name = "txtUsuario";
            this.txtUsuario.Size = new System.Drawing.Size(10, 20);
            this.txtUsuario.TabIndex = 11;
            this.txtUsuario.Visible = false;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.lboxIndice);
            this.groupBox6.Controls.Add(this.txtUsuario);
            this.groupBox6.Location = new System.Drawing.Point(12, 172);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(184, 443);
            this.groupBox6.TabIndex = 5;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Indice:";
            // 
            // lboxIndice
            // 
            this.lboxIndice.FormattingEnabled = true;
            this.lboxIndice.HorizontalScrollbar = true;
            this.lboxIndice.Location = new System.Drawing.Point(10, 16);
            this.lboxIndice.Name = "lboxIndice";
            this.lboxIndice.Size = new System.Drawing.Size(166, 420);
            this.lboxIndice.TabIndex = 0;
            this.lboxIndice.TabStop = false;
            this.lboxIndice.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lboxIndice_MouseClick);
            this.lboxIndice.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lboxIndice_KeyDown);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(17, 621);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(171, 36);
            this.button1.TabIndex = 46;
            this.button1.Text = "Forzar Cierre";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnSepararIlegible
            // 
            this.btnSepararIlegible.Location = new System.Drawing.Point(204, 559);
            this.btnSepararIlegible.Name = "btnSepararIlegible";
            this.btnSepararIlegible.Size = new System.Drawing.Size(130, 50);
            this.btnSepararIlegible.TabIndex = 10;
            this.btnSepararIlegible.Text = "&Separar Ilegible";
            this.btnSepararIlegible.UseVisualStyleBackColor = true;
            this.btnSepararIlegible.Click += new System.EventHandler(this.btnSepararIlegible_Click);
            // 
            // btnGrabar
            // 
            this.btnGrabar.Location = new System.Drawing.Point(202, 499);
            this.btnGrabar.Name = "btnGrabar";
            this.btnGrabar.Size = new System.Drawing.Size(321, 54);
            this.btnGrabar.TabIndex = 8;
            this.btnGrabar.Text = "&Grabar y Continuar";
            this.btnGrabar.UseVisualStyleBackColor = true;
            this.btnGrabar.Click += new System.EventHandler(this.btnGrabar_Click);
            this.btnGrabar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.btnGrabar_KeyDown);
            // 
            // btnSalir
            // 
            this.btnSalir.Location = new System.Drawing.Point(393, 559);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Size = new System.Drawing.Size(130, 50);
            this.btnSalir.TabIndex = 9;
            this.btnSalir.Text = "&Cerrar";
            this.btnSalir.UseVisualStyleBackColor = true;
            this.btnSalir.Click += new System.EventHandler(this.btnSalir_Click);
            // 
            // txttipo
            // 
            this.txttipo.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txttipo.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txttipo.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.txttipo.Location = new System.Drawing.Point(107, 74);
            this.txttipo.Name = "txttipo";
            this.txttipo.Size = new System.Drawing.Size(133, 24);
            this.txttipo.TabIndex = 2;
            this.txttipo.Enter += new System.EventHandler(this.txttipo_Enter);
            this.txttipo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txttipo_KeyDown);
            // 
            // txtapellido
            // 
            this.txtapellido.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtapellido.Location = new System.Drawing.Point(107, 14);
            this.txtapellido.Name = "txtapellido";
            this.txtapellido.Size = new System.Drawing.Size(208, 24);
            this.txtapellido.TabIndex = 1;
            this.txtapellido.TextChanged += new System.EventHandler(this.txtapellido_TextChanged);
            this.txtapellido.Enter += new System.EventHandler(this.txtapellido_Enter);
            this.txtapellido.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtapellido_KeyDown);
            this.txtapellido.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtapellido_KeyPress);
            // 
            // txtaño
            // 
            this.txtaño.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtaño.Location = new System.Drawing.Point(107, 134);
            this.txtaño.Name = "txtaño";
            this.txtaño.Size = new System.Drawing.Size(133, 24);
            this.txtaño.TabIndex = 3;
            this.txtaño.Enter += new System.EventHandler(this.txtaño_Enter);
            this.txtaño.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtaño_KeyDown);
            this.txtaño.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtaño_KeyPress);
            // 
            // txtacta
            // 
            this.txtacta.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtacta.Location = new System.Drawing.Point(107, 296);
            this.txtacta.Name = "txtacta";
            this.txtacta.Size = new System.Drawing.Size(133, 24);
            this.txtacta.TabIndex = 6;
            this.txtacta.Enter += new System.EventHandler(this.txtacta_Enter);
            this.txtacta.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtacta_KeyDown);
            this.txtacta.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtacta_KeyPress);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(10, 77);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 18);
            this.label6.TabIndex = 66;
            this.label6.Text = "Tipo:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(12, 16);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 18);
            this.label7.TabIndex = 35;
            this.label7.Text = "Ap y Nom";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(10, 137);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(34, 18);
            this.label8.TabIndex = 30;
            this.label8.Text = "Año";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(12, 296);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(37, 18);
            this.label9.TabIndex = 77;
            this.label9.Text = "Acta";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(12, 349);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(48, 18);
            this.label10.TabIndex = 255;
            this.label10.Text = "Ficha:";
            // 
            // txtficha
            // 
            this.txtficha.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtficha.Location = new System.Drawing.Point(107, 348);
            this.txtficha.Name = "txtficha";
            this.txtficha.Size = new System.Drawing.Size(133, 24);
            this.txtficha.TabIndex = 7;
            this.txtficha.Enter += new System.EventHandler(this.txtficha_Enter);
            this.txtficha.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtficha_KeyDown);
            this.txtficha.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtficha_KeyPress);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(12, 246);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(48, 18);
            this.label12.TabIndex = 26;
            this.label12.Text = "Tomo";
            // 
            // txttomo
            // 
            this.txttomo.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txttomo.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txttomo.Location = new System.Drawing.Point(107, 246);
            this.txttomo.Name = "txttomo";
            this.txttomo.Size = new System.Drawing.Size(133, 24);
            this.txttomo.TabIndex = 5;
            this.txttomo.Enter += new System.EventHandler(this.txttomo_Enter);
            this.txttomo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txttomo_KeyDown);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(12, 190);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(62, 18);
            this.label11.TabIndex = 257;
            this.label11.Text = "Seccion";
            // 
            // txtseccion
            // 
            this.txtseccion.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtseccion.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtseccion.Location = new System.Drawing.Point(107, 190);
            this.txtseccion.Name = "txtseccion";
            this.txtseccion.Size = new System.Drawing.Size(133, 24);
            this.txtseccion.TabIndex = 4;
            this.txtseccion.Enter += new System.EventHandler(this.txtseccion_Enter);
            this.txtseccion.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtseccion_KeyDown);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.txttipo);
            this.groupBox4.Controls.Add(this.txtseccion);
            this.groupBox4.Controls.Add(this.txtapellido);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Controls.Add(this.txtaño);
            this.groupBox4.Controls.Add(this.txtacta);
            this.groupBox4.Controls.Add(this.txttomo);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.txtficha);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Location = new System.Drawing.Point(204, 99);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(328, 394);
            this.groupBox4.TabIndex = 258;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Carga";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(458, 16);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(972, 614);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 13;
            this.pictureBox1.TabStop = false;
            // 
            // FormEducacionCAD
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1370, 749);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.btnSepararIlegible);
            this.Controls.Add(this.btnSalir);
            this.Controls.Add(this.btnGrabar);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.pictureBox1);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormEducacionCAD";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Educacion CAD";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TextBox txtRutaLote;
        private System.Windows.Forms.Button btnSeleccionarLote;
        private System.Windows.Forms.TextBox txtNombreImagen;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox lboxIndice;
        private System.Windows.Forms.Button btnGrabar;
        private System.Windows.Forms.Button btnSalir;
        private System.Windows.Forms.Button btnSepararIlegible;
        public System.Windows.Forms.TextBox txtUsuario;
        private System.Windows.Forms.TextBox txttipo;
        private System.Windows.Forms.TextBox txtapellido;
        private System.Windows.Forms.TextBox txtaño;
        private System.Windows.Forms.TextBox txtacta;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtficha;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txttomo;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtseccion;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
    }
}