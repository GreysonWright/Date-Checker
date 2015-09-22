using System.Windows;
using System;

namespace Date_Check_Tool
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    /// 

    public delegate void cancelWork(object sender, EventArgs e);

    public partial class ProgressDialog : Window
    {

        public event cancelWork didReceiveFatalError; 
        bool shouldClose = false; //Flag to determine when ProgressDialog can be closed

        // #--Window Lifetime--#
        public ProgressDialog()
        {

            InitializeComponent();
            
        }

        //Closes ProgressDialog called when process is completed
        public void closeDialog()
        {
            Dispatcher.Invoke((() => //Cross threaded operations need to be invoked
            {
                shouldClose = true; //Let us allow closing of progress
                Close(); //Closes Progress

            }));

        }

        private void ProgressDialog1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            e.Cancel = !shouldClose;

        }

        //#--Progress--#
        public void setProgress(int progress, string processTitle)
        {

            progressBar.Value = progress; //Sets the progressbar value
            processLabel.Text = processTitle; //Sets processlabel text

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            //cancelWork(this, new EventArgs());

        }

    }
}
