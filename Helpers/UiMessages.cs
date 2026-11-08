using System;
using System.Windows.Forms;

namespace EclatEtSoin.Helpers
{
    public static class UiMessages
    {
        public static void Info(string message)
        {
            MessageBox.Show(message, "Èclat & Soin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(string message)
        {
            MessageBox.Show(message, "Èclat & Soin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(string message, Exception ex = null)
        {
            var detail = ex == null ? message : message + Environment.NewLine + ex.Message;
            MessageBox.Show(detail, "Èclat & Soin", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
