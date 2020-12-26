using System;
using System.Windows;

namespace Downpatcher {

	public partial class AuthenticationDialog : Window {
		public AuthenticationDialog() {
			InitializeComponent();
		}

		private void btnDialogOk_Click(object sender, RoutedEventArgs e) {
			this.DialogResult = true;
		}

		private void Window_ContentRendered(object sender, EventArgs e) {
			tbAuthCode.SelectAll();
			tbAuthCode.Focus();
		}

		public string AuthCode {
			get { return tbAuthCode.Text; }
		}
	}
}