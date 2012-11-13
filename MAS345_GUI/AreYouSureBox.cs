using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MAS345_GUI
{
    public partial class AreYouSureBox : Form
    {
        public bool Answer;
        public AreYouSureBox()
        {
            InitializeComponent();
        }

        public static bool ShowDialog(string Question)
        {
            AreYouSureBox Instance = new AreYouSureBox();
            Instance.questionLabel.Text = Question;
            Instance.ShowDialog();
            return Instance.Answer;
        }

        private void yesButton_Click(object sender, EventArgs e)
        {
            Answer = true;
            this.Close();
        }

        private void noButton_Click(object sender, EventArgs e)
        {
            Answer = false;
            this.Close();
        }
    }
}
